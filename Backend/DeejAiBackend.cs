using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;

namespace Jellyfin.Plugin.BetterMix.Backend;

public class DeejAiBackend : BetterMixBackendBase
{
    private const string m_deejAiDir = "Deej-AI";
    static private readonly string m_pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
        throw new InvalidOperationException("Plugin directory not found.");
    private readonly string m_binaryPath = Path.Combine(m_pluginDirectory, m_deejAiDir, "deej-ai.cpp/bin/deej-ai");
    private readonly string m_modelPath = Path.Combine(m_pluginDirectory, m_deejAiDir, "deej-ai.onnx");
    private const string m_vecsDirName = "audio_vecs";
    private readonly string m_vecsDirPath = Path.Combine(m_pluginDirectory, m_deejAiDir, m_vecsDirName);

    private static string Enquote(string str)
    {
        return $"\"{str}\"";
    }

    private void Shuffle<T>(List<T> list)
    {
        Random rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]); // tuple swap
        }
    }

    public string VectorsPath()
    {
        var config = BetterMixPlugin.Instance.Configuration;
        if (config.CustomPathEnabled)
        {
            return Path.Combine(config.CustomPath, m_vecsDirName);
        }

        return m_vecsDirPath;
    }

    public override List<BaseItem>? GetPlaylist(List<string> inputSongPaths, int nsongs, PlaylistType type)
    {
        var config = BetterMixPlugin.Instance.Configuration;
        double noise = config.DeejaiNoise;
        int lookback = config.DeejaiLookback;
        string method = config.DeejaiMethod;

        if (type == PlaylistType.FromAlbum)
        {
            Shuffle(inputSongPaths);
            inputSongPaths = inputSongPaths.Take(30).ToList();
            method = "cluster --reorder-output ";
            nsongs = inputSongPaths.Count * 2;
        }
        return GetPlaylist(inputSongPaths, nsongs, noise, lookback, method);
    }

    public List<BaseItem>? GetPlaylist(
        List<string> inputSongPaths,
        int nsongs,
        double noise,
        int lookback,
        string method
        )
    {
        string inputArgs = " -i " + string.Join(" -i ", inputSongPaths.Select(Enquote));
        string arguments = " --generate " + method + " --vec-dir " + Enquote(VectorsPath()) + inputArgs + " --noise " + noise.ToString() + " --lookback " + lookback.ToString() + " --nsongs " + nsongs.ToString();
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Executing Deej-AI GetPlaylist with arguments: {args}", arguments);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string argsFile = Path.Combine(location, "args_generate.txt");
            File.WriteAllText(argsFile, arguments);
            arguments = " @" + argsFile;
        }

        using Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = m_binaryPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Deej-AI GetPlaylist: {error}", error);
        }

        List<BaseItem> items = [];
        foreach (string path in output.Split("\n", StringSplitOptions.RemoveEmptyEntries))
        {
            BaseItem? item = BetterMixPlugin.Instance.GetItemFromPath(path.Trim());
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

    public override void ScanTaskFunction(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var batch = GetCurrentBatch();
        if (batch == null)
        {
            return;
        }
        string ffmpeg = "";
        string? ffmpegPath = BetterMixPlugin.Instance.FFmpegPath();
        var config = BetterMixPlugin.Instance.Configuration;
        int jobs = config.DeejaiMaxJobs;

        if (ffmpegPath != null)
        {
            ffmpeg = " --ffmpeg " + Enquote(ffmpegPath);
        }
        string arguments = " --vec-dir " + Enquote(VectorsPath()) + " --model " + Enquote(m_modelPath) + " --scan " + batch.GetFilepathsString(" --scan ") + ffmpeg + " -j " + jobs.ToString() ;
        BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Executing Deej-AI Scan with arguments: {args}", arguments);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string argsFile = Path.Combine(location, "args_scan.txt");
            File.WriteAllText(argsFile, arguments);
            arguments = " @" + argsFile;
        }

        using Process process = new()
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

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Deej-AI Scan Output: {line}", e.Data);
                var match = System.Text.RegularExpressions.Regex.Match(e.Data, @"Scan progress: (\d+)\s*/\s*(\d+)");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out int current) &&
                        int.TryParse(match.Groups[2].Value, out int total) &&
                        total > 0)
                    {
                        double percent = (double)current / total * 100.0;
                        progress.Report(percent);
                    }
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                BetterMixPlugin.Instance.Logger.LogError("BetterMix: Deej-AI Scan Error: {line}", e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Monitor cancellation and wait for exit with short timeouts to check cancellation frequently
        while (!process.WaitForExit(500))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (Exception ex)
                {
                    BetterMixPlugin.Instance.Logger.LogError("BetterMix: Failed to kill process on cancellation: {error}", ex.Message);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        progress.Report(100);
    }
}
