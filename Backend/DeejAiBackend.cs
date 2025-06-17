using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.BetterMix.Backend;

public class DeejAiBackend : BetterMixBackendBase
{
    private readonly string m_scanBinaryPath = Path.Combine(BetterMixPlugin.Instance.DataFolderPath, "Deej-AI/Deej-AI-scanner");
    private readonly string m_modelPath = Path.Combine(BetterMixPlugin.Instance.DataFolderPath, "Deej-AI/Deej-AI-model");
    private readonly string m_generateBinaryPath = Path.Combine(BetterMixPlugin.Instance.DataFolderPath, "Deej-AI/Deej-AI-generator");
    private readonly string m_picklesDir = Path.Combine(BetterMixPlugin.Instance.DataFolderPath, "Deej-AI/Pickles");

    private const string combinedPicklesDir = "mp3tovecs";


    public override List<BaseItem>? GetPlaylistFromSong(string inputSongPath, int nsongs)
    {
        var config = BetterMixPlugin.Instance.Configuration;
        double noise = config.DeejaiNoise;
        double lookback = config.DeejaiLookback;
        double epsilon = config.DeejaiEpsilon;

        string arguments = m_picklesDir + " " + combinedPicklesDir + " --inputsong " + $"\"{inputSongPath}\"" + " --noise " + noise.ToString() + " --epsilon " + epsilon.ToString() + " --lookback " + lookback.ToString() + " --nsongs " + nsongs.ToString();
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: GetPlaylist: {args}", arguments);

        using Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = m_generateBinaryPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        List<BaseItem> items = [];
        foreach (string path in output.Split("\n", StringSplitOptions.RemoveEmptyEntries))
        {
            BaseItem? item = BetterMixPlugin.Instance.GetItemFromPath(path);
            if (item != null)
            {
                items.Add(item);
            }
        }
        if (items.Count == 0)
        {
            return null;
        }
        return items;
    }

    protected override void ScanTask(BetterMixScanBatch batch)
    {
        string arguments = m_picklesDir + " " + combinedPicklesDir + " --model " + m_modelPath + " --scan " + batch.GetFilepathsString();
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Scan Task: {args}", arguments);

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = m_scanBinaryPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
}
