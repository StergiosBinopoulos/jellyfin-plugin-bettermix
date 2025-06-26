using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;

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
    private const int m_delayMilliseconds = 5000;
    private CancellationTokenSource m_cts = new();
    private readonly ConcurrentQueue<BetterMixScanBatch> m_batches = [];
    private int m_isRunning = 0;

    private readonly HashSet<string> m_dirpathsToScan = [];

    public BetterMixBackendBase()
    {
        foreach (var child in BetterMixPlugin.Instance.LibraryManager.RootFolder.Children.OfType<Folder>())
        {
            AddDirectoryToScan(child.Path);
        }
    }

    public abstract List<BaseItem>? GetPlaylistFromSong(string inputSongPath, int nsongs);

    public void AddDirectoryToScan(string dirpaths)
    {
        if (!string.IsNullOrEmpty(dirpaths))
        {
            m_dirpathsToScan.Add(dirpaths);
        }
        m_cts.Cancel();
        m_cts = new CancellationTokenSource();
        var token = m_cts.Token;
        // schedule a scan after m_delayMilliseconds ms.
        Task.Delay(m_delayMilliseconds, token).ContinueWith(CreateBatch, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    protected abstract void ScanTask(BetterMixScanBatch batch);

    private void CreateBatch(Task t)
    {
        m_batches.Enqueue(new BetterMixScanBatch(m_dirpathsToScan));
        m_dirpathsToScan.Clear();
        if (!IsTaskRunning())
        {
            Task.Run(RunQueuedBatch);
        }
    }

    private bool IsTaskRunning()
    {
        return Interlocked.CompareExchange(ref m_isRunning, 1, 1) == 1;
    }

    private void SetTaskRunningStatus(bool running)
    {
        if (running)
        {
            Interlocked.Exchange(ref m_isRunning, 1);
        }
        else
        {
            Interlocked.Exchange(ref m_isRunning, 0);
        }
    }

    private void RunQueuedBatch()
    {
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Running a batch scan.");
        SetTaskRunningStatus(true);
        if (m_batches.TryDequeue(out BetterMixScanBatch? batch))
        {
            ScanTask(batch);
        }
        
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Scan completed");

        if (!m_batches.IsEmpty)
        {
            RunQueuedBatch();
        }

        SetTaskRunningStatus(false);
    }
}
