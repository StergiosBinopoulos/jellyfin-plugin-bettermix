using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Jellyfin.Plugin.BetterMix.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BetterMix.Registration;

public class BetterMixRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<BetterMixService>();
    }
}
