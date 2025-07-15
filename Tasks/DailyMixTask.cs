using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Jellyfin.Plugin.BetterMix.Services;

namespace Jellyfin.Plugin.BetterMix.Tasks;

public class DailyMixTask(DailyMixService DailyMixService) : IScheduledTask
{
    private readonly DailyMixService m_DailyMixService = DailyMixService;

    public string Name => "BetterMix Daily Mix Task";

    public string Description => "Generate daily playlists for each user.";

    public string Category => "Library";

    public string Key => "BetterMixDailyMixTask";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        return m_DailyMixService.CreateDailyMixes();
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            new TaskTriggerInfo
            {
                Type = "DailyTrigger",
                TimeOfDayTicks = TimeSpan.FromHours(0).Ticks
            }
        ];
    }
}
