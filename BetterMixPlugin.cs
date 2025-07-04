using System;
using System.Collections.Generic;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Jellyfin.Plugin.BetterMix.Configuration;
using Jellyfin.Plugin.BetterMix.Backend;
using Jellyfin.Plugin.BetterMix.Filters;
using MediaBrowser.Model.Tasks;
using Jellyfin.Plugin.BetterMix.Tasks;

namespace Jellyfin.Plugin.BetterMix;

public class BetterMixPlugin : BasePlugin<PluginConfiguration>, IHasPluginConfiguration, IHasWebPages
{
    public override Guid Id => Guid.Parse("573ed94b-6a8e-4f7e-977a-c0aef8d0bbff");
    public override string Name => "BetterMix";
    public override string Description => "BetterMix, a Jellyfin plugin for better Instant Mix.";
    public static BetterMixPlugin Instance { get; private set; } = null!;
    public readonly ILogger<BetterMixPlugin> Logger;
    public readonly ILibraryManager LibraryManager;
    public readonly ITaskManager TaskManager;

    public BetterMixBackendBase ActiveBackend;
    public BetterMixPlugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILibraryManager libraryManager,
        IServiceProvider serviceProvider,
        IActionDescriptorCollectionProvider provider,
        IHostApplicationLifetime hostApplicationLifetime,
        ITaskManager taskManager,
        ILogger<BetterMixPlugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        LibraryManager = libraryManager;
        TaskManager = taskManager;
        Logger = logger;

        if (Configuration.SelectedBackend == "deejai")
        {
            ActiveBackend = new DeejAiBackend();
        }
        else
        {
            ActiveBackend = new NativeBackend();
        }


        LibraryManager.ItemAdded += OnItemChanged;
        LibraryManager.ItemRemoved += OnItemChanged;
        LibraryManager.ItemUpdated += OnItemChanged;
        TaskManager.TaskCompleted += OnTaskCompleted;
        ConfigurationChanged += OnConfigurationChanged;


        hostApplicationLifetime.ApplicationStarted.Register(() => { TryAddFilter(provider, serviceProvider); });
    }

    private void OnTaskCompleted(object? sender, TaskCompletionEventArgs e)
    {
        if (e.Task.Name == "Scan Media Library")
        {
            ActiveBackend.GeneralScan();
        }
    }

    public BaseItem? GetItemFromPath(string path)
    {
        BaseItem? item = LibraryManager.FindByPath(path, false);
        if (item == null)
        {
            return null;
        }
        return item;
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "BetterMixConfig",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html",
        };
    }

    internal void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
    {
        if (e is PluginConfiguration)
        {
            Logger.LogInformation("BetterMix: Configuration changed.");
            if (Configuration.SelectedBackend == "deejai")
            {
                if (ActiveBackend is not DeejAiBackend)
                {
                    ActiveBackend = new DeejAiBackend();
                }
            }
            else if (ActiveBackend is not NativeBackend)
            {
                ActiveBackend = new NativeBackend();
            }
            ActiveBackend.GeneralScan();
        }
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        ActiveBackend.AddDirectoryToScan(e.Item.ContainingFolderPath);
    }

    private void TryAddFilter(IActionDescriptorCollectionProvider provider, IServiceProvider serviceProvider)
    {
        var count = provider.AddDynamicFilter<ItemInstantMixFilter>(serviceProvider, t =>
        {
            return t.MethodInfo.Name == "GetInstantMixFromItem"
                && t.ControllerTypeInfo.FullName == "Jellyfin.Api.Controllers.InstantMixController";
        });

        count += provider.AddDynamicFilter<ItemInstantMixFilter>(serviceProvider, t =>
        {
            return t.MethodInfo.Name == "GetInstantMixFromAlbum"
                && t.ControllerTypeInfo.FullName == "Jellyfin.Api.Controllers.InstantMixController";
        });
        Logger.LogInformation("BetterMix: {Count} action filter(s) added.", count);
    }
}

