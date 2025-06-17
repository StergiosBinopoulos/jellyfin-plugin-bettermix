using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.BetterMix.Backend;

public class NativeBackend : BetterMixBackendBase
{
    public override List<BaseItem>? GetPlaylistFromSong(string inputSongPath, int nsongs)
    {
        return null;
    }

    protected override void ScanTask(BetterMixScanBatch batch) { }
}