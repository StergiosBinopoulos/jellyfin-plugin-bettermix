using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.BetterMix.Tasks;

public class ScanTask : IScheduledTask
{
    public string Name => "BetterMix Scan Task";

    public string Description => "Scan the music library to enable playlist generation.";

    public string Category => "Library";

    public string Key => "BetterMixScanTask";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            BetterMixPlugin.Instance.ActiveBackend.ScanTaskFunction(progress, cancellationToken);
        });
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
            {
            new TaskTriggerInfo
            {
                Type = "StartupTrigger"
            },
            new TaskTriggerInfo
            {
                Type = "DailyTrigger",
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        };
    }
}
