using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Controller.Playlists;
using System;
using System.Collections.Generic;
using Jellyfin.Plugin.BetterMix.Configuration;
using Jellyfin.Plugin.BetterMix.Backend;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using System.Linq;

namespace Jellyfin.Plugin.BetterMix.Services;

public class DailyMixService(IPlaylistManager playlistManager, ILibraryManager libraryManager, IUserManager userManager)
{
    private readonly IPlaylistManager m_playlistManager = playlistManager;
    private readonly ILibraryManager m_libraryManager = libraryManager;
    private readonly IUserManager m_userManager = userManager;

    public void CreateDailyMixes()
    {
        foreach (var user in m_userManager.Users)
        {
            CreateDailyMixesForUser(user);
        }
    }
    
    public void CreateDailyMixesForUser(User user)
    {
        DeejAiBackend deejai = new();
        var config = BetterMixPlugin.Instance.Configuration;
        var playlists = m_libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Playlist]
        });


        foreach (var mix in config.DailyMixes)
        {
            for (int i = playlists.Count - 1; i >= 0; i--)
            {
                var playlist = playlists[i];
                if (config.DailyMixes.Any(mix => mix.Name == playlist.Name))
                {
                    var options = new DeleteOptions { DeleteFileLocation = true };
                    m_libraryManager.DeleteItem(playlist, options);
                    playlists.RemoveAt(i);
                }
            }

            IReadOnlyList<BaseItem> inputSongs = CreateInputSample(mix.SampleMethod, mix.InputSize, user);
            List<string> inputSongPaths = inputSongs.Select(item => item.Path).ToList();
            List<BaseItem>? items = deejai.GetPlaylist(inputSongPaths, mix.OutputSize, 0.01, 3, "cluster --reorder-output ");
            if (items == null)
            {
                continue;
            }

            var request = new PlaylistCreationRequest
            {
                Name = mix.Name,
                ItemIdList = items.Select(item => item.Id).ToArray(),
                MediaType = MediaType.Audio,
                UserId = user.Id,
            };

            m_playlistManager.CreatePlaylist(request);
        }
    }

    public IReadOnlyList<BaseItem> CreateInputSample(SampleMethod method, int size, User user)
    {
        IReadOnlyList<(ItemSortBy OrderBy, SortOrder SortOrder)> order = [(ItemSortBy.Random, SortOrder.Ascending)];
        List<Guid> artists = new();
        int limit = size;
        switch (method)
        {
            case SampleMethod.Top50:
                order = [(ItemSortBy.PlayCount, SortOrder.Descending)];
                limit = 50;
                break;
            case SampleMethod.Top100:
                order = [(ItemSortBy.PlayCount, SortOrder.Descending)];
                limit = 100;
                break;
            case SampleMethod.Top200:
                order = [(ItemSortBy.PlayCount, SortOrder.Descending)];
                limit = 200;
                break;
            case SampleMethod.RandomArtist:
                var randomArtistResult = m_libraryManager.GetArtists(new InternalItemsQuery
                {
                    OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)],
                    Limit = 1,
                    User = user
                });
                var (Item, ItemCounts) = randomArtistResult.Items.FirstOrDefault();
                artists.Add(Item.Id);
                break;
            case SampleMethod.RandomSongs:
            default:
                break;
        }


        var result = m_libraryManager.GetItemsResult(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            ArtistIds = artists.ToArray(),
            AlbumArtistIds = artists.ToArray(),
            OrderBy = order,
            Limit = limit,
            User = user
        });

        var itemList = result.Items;
        var rng = new Random();
        var shuffled = result.Items.OrderBy(_ => rng.Next()).ToList();
        return shuffled.Take(size).ToList();
    }
}
