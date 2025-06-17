using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BetterMix.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string SelectedBackend { get; set; } = "deejai";

    public double DeejaiNoise { get; set; } = 0.2;

    public int DeejaiLookback { get; set; } = 3;

    public double DeejaiEpsilon { get; set; } = 0.001;
}

