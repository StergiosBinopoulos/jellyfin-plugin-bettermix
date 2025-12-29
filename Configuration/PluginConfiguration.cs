using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BetterMix.Configuration;

public enum SampleMethod
{
    Top50,
    Top100,
    Top200,
    RandomArtist,
    RandomSongs
    
}

public class DailyMixConfig
{
    public required string Name { get; set; }
    public required SampleMethod SampleMethod { get; set; }
    public required int InputSize { get; set; }
    public required int OutputSize { get; set; }
    public required double Noise { get; set; }
    public required int MinSongLength { get; set; }
}

public class PluginConfiguration : BasePluginConfiguration
{
    public bool CustomPathEnabled { get; set; } = false;
    
    public string CustomPath { get; set; } = "";

    public string SelectedBackend { get; set; } = "deejai";

    public string DeejaiMethod { get; set; } = "append";

    public double DeejaiNoise { get; set; } = 0.01;

    public int DeejaiLookback { get; set; } = 3;

    public required int DeejaiMaxJobs { get; set; } = -1;

    public DailyMixConfig[] DailyMixes { get; set; } = [
        new DailyMixConfig
        {
            Name = "DailyMix 1",
            SampleMethod = SampleMethod.Top100,
            InputSize = 25,
            OutputSize = 50,
            Noise = 0.01,
            MinSongLength = 90,
        },
        new DailyMixConfig
        {
            Name = "DailyMix 2",
            SampleMethod = SampleMethod.Top200,
            InputSize = 25,
            OutputSize = 50,
            Noise = 0.01,
            MinSongLength = 90,
        },
        new DailyMixConfig
        {
            Name = "DailyMix 3",
            SampleMethod = SampleMethod.Top50,
            InputSize = 5,
            OutputSize = 25,
            Noise = 0.01,
            MinSongLength = 90,
        },
        new DailyMixConfig
        {
            Name = "Discover",
            SampleMethod = SampleMethod.RandomSongs,
            InputSize = 2,
            OutputSize = 30,
            Noise = 0.01,
            MinSongLength = 90,
        },
        new DailyMixConfig
        {
            Name = "Artist Radio",
            SampleMethod = SampleMethod.RandomArtist,
            InputSize = 30,
            OutputSize = 40,
            Noise = 0.01,
            MinSongLength = 90,
        },
    ];
}

