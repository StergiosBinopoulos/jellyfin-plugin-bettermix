using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.BetterMix.Backend;

public class DeejAiBackend : BetterMixBackendBase
{
    private const string m_deejAiDir = "Deej-AI";
    static private readonly string m_pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
        throw new InvalidOperationException("Plugin directory not found.");
    private readonly string m_binaryPath = Path.Combine(m_pluginDirectory, m_deejAiDir, "deej-ai.cpp/bin/deej-ai");
    private readonly string m_modelPath = Path.Combine(m_pluginDirectory, m_deejAiDir, "deej-ai.onnx");
    private readonly string m_vecsDir = Path.Combine(m_pluginDirectory, m_deejAiDir, "audio_vecs");

    public override List<BaseItem>? GetPlaylistFromSong(string inputSongPath, int nsongs)
    {
        var config = BetterMixPlugin.Instance.Configuration;
        double noise = config.DeejaiNoise;
        double lookback = config.DeejaiLookback;
        double epsilon = config.DeejaiEpsilon;

        string arguments = " --generate append" + " --vec-dir " + m_vecsDir + " -i " + $"\"{inputSongPath}\"" + " --noise " + noise.ToString() + " --epsilon " + epsilon.ToString() + " --lookback " + lookback.ToString() + " --nsongs " + nsongs.ToString();
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: GetPlaylist: {args}", arguments);
    
        using Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = m_binaryPath,
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
        string arguments = " --vec-dir " + m_vecsDir + " --model " + m_modelPath +  " --scan " + batch.GetFilepathsString(" --scan ");
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Scan Task: {args}", arguments);

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = m_binaryPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
}
