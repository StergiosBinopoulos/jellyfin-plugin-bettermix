using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BetterMix.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string SelectedBackend { get; set; } = "deejai";

    public string DeejaiMethod { get; set; } = "append";

    public double DeejaiNoise { get; set; } = 0.2;

    public int DeejaiLookback { get; set; } = 3;

}

