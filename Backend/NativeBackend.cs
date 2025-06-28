using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using System;
using System.Threading;

namespace Jellyfin.Plugin.BetterMix.Backend;

public class NativeBackend : BetterMixBackendBase
{
    public override List<BaseItem>? GetPlaylistFromSongs(List<string> inputSongPaths, int nsongs)
    {
        return null;
    }

    public override void ScanTaskFunction(IProgress<double> progress, CancellationToken cancellationToken)
    { 
        // dequeue batches
        GetCurrentBatch();
    }
}