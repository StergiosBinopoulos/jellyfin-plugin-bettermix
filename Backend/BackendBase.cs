using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using Jellyfin.Plugin.BetterMix.Tasks;
using System;
using System.Numerics;

namespace Jellyfin.Plugin.BetterMix.Backend;

public class BetterMixScanBatch(HashSet<string> dirpaths)
{
    private readonly HashSet<string> m_filepathsToScan = [.. dirpaths];

    public string GetFilepathsString(string join = " ")
    {
        return string.Join(join, m_filepathsToScan.Select(s => $"\"{s}\""));
    }
}

public abstract class BetterMixBackendBase
{
    private static bool m_firstTimeConstructing = true;
    private const int m_delayMilliseconds = 2;
    private CancellationTokenSource m_cts = new();
    readonly ConcurrentQueue<BetterMixScanBatch> m_batches = [];
    private readonly HashSet<string> m_dirpathsToScan = [];

    public enum PlaylistType
    {
        FromAudio,
        FromAlbum,
    }

    public BetterMixBackendBase()
    {
        if (m_firstTimeConstructing)
        {
            m_batches.Enqueue(GeneralScanBatch());
            m_firstTimeConstructing = false;
        }
    }

    public abstract List<BaseItem>? GetPlaylist(List<string> inputSongPaths, int nsongs, PlaylistType type);

    public void GeneralScan()
    {
        m_batches.Enqueue(GeneralScanBatch());
        BetterMixPlugin.Instance.TaskManager.QueueScheduledTask<ScanTask>();
    }

    public void AddDirectoryToScan(string dirpaths)
    {
        BetterMixPlugin.Instance.Logger.LogInformation("adding directory");
        if (!string.IsNullOrEmpty(dirpaths))
        {
            m_dirpathsToScan.Add(dirpaths);
        }
        m_cts.Cancel();
        m_cts = new CancellationTokenSource();
        var token = m_cts.Token;
        // schedule a scan after m_delayMilliseconds ms.
        Task.Delay(m_delayMilliseconds, token).ContinueWith(QueueBatch, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    public abstract void ScanTaskFunction(IProgress<double> progress, CancellationToken cancellationToken);

    private static BetterMixScanBatch GeneralScanBatch()
    {
        HashSet<string> generelScanBatch = [];
        foreach (var child in BetterMixPlugin.Instance.LibraryManager.RootFolder.Children.OfType<Folder>())
        {
            generelScanBatch.Add(child.Path);
        }
        return new BetterMixScanBatch(generelScanBatch);
    }

    private void QueueBatch(Task t)
    {
        m_batches.Enqueue(new BetterMixScanBatch(m_dirpathsToScan));
        m_dirpathsToScan.Clear();
        BetterMixPlugin.Instance.TaskManager.QueueScheduledTask<ScanTask>();
    }

    protected BetterMixScanBatch? GetCurrentBatch()
    {
        m_batches.TryDequeue(out BetterMixScanBatch? batch);
        return batch;
    }
}
