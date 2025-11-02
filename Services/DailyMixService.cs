using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Controller.Playlists;
using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Jellyfin.Plugin.BetterMix.Configuration;
using Jellyfin.Plugin.BetterMix.Backend;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Entities;


namespace Jellyfin.Plugin.BetterMix.Services;

public class DailyMixData
{
    public List<string> Ids { get; set; } = [];
}

public class DailyMixService(IPlaylistManager playlistManager, ILibraryManager libraryManager, IUserManager userManager)
{
    private readonly IPlaylistManager m_playlistManager = playlistManager;
    private readonly ILibraryManager m_libraryManager = libraryManager;
    private readonly IUserManager m_userManager = userManager;
    
    static private readonly string m_pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
        throw new InvalidOperationException("Plugin directory not found.");
    private readonly string m_dataFilePath = Path.Combine(m_pluginDirectory, "dailymix.txt");

    public async Task CreateDailyMixes()
    {
        // remove old mixes
        DailyMixData data = new();
        if (File.Exists(m_dataFilePath))
        {
            string dataString = File.ReadAllText(m_dataFilePath);
            if (!string.IsNullOrWhiteSpace(dataString))
            {
                data.Ids = [.. dataString.Split('\n')];
            }
        }
        
        foreach (var id in data.Ids)
        {
            var options = new DeleteOptions { DeleteFileLocation = true };
            var playlist = m_libraryManager.GetItemById(id);
            if (playlist != null)
            {
                m_libraryManager.DeleteItem(playlist, options);
            }
        }   

        DeejAiBackend deejai = new();
        var config = BetterMixPlugin.Instance.Configuration;
        List<string> newGuids = [];
        foreach (var user in m_userManager.Users)
        {
            foreach (var mix in config.DailyMixes)
            {
                IReadOnlyList<BaseItem> inputSongs = CreateInputSample(mix.SampleMethod, mix.InputSize, user);
                if (inputSongs.Count < 1)
                {
                    continue;
                }
                List<string> inputSongPaths = inputSongs.Select(item => item.Path).ToList();
                List<BaseItem>? items = deejai.GetPlaylist(inputSongPaths, mix.OutputSize, 0.01, 3, "cluster --reorder-output ");
                if (items == null)
                {
                    continue;
                }

                var request = new PlaylistCreationRequest
                {
                    Name = string.Format("{0} ({1})", mix.Name, user.Username),
                    ItemIdList = items.Select(item => item.Id).ToArray(),
                    MediaType = MediaType.Audio,
                    UserId = user.Id,
                    Public = false
                };

                var guid = await m_playlistManager.CreatePlaylist(request);
                newGuids.Add(guid.Id);
            }
        }

        data.Ids = newGuids;
        string line = string.Join("\n", data.Ids);
        File.WriteAllText(m_dataFilePath, line);
    }

    public IReadOnlyList<BaseItem> CreateInputSample(SampleMethod method, int size, User user)
    {
        IReadOnlyList<(ItemSortBy OrderBy, SortOrder SortOrder)> order = [(ItemSortBy.Random, SortOrder.Ascending)];
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
                var rd = new Random();
                var artists = m_libraryManager.GetArtists(new InternalItemsQuery
                {
                    OrderBy = order,
                    Limit = 100,
                    User = user
                }).Items.OrderBy(_ => rd.Next()).ToList();
                foreach (var artist in artists)
                {
                    var artistSongResult = m_libraryManager.GetItemsResult(new InternalItemsQuery
                    {
                        IncludeItemTypes = [BaseItemKind.Audio],
                        ArtistIds = [artist.Item.Id],
                        OrderBy = order,
                        Limit = size,
                        User = user
                    });

                    // find an artist with at least 5 songs
                    if (artistSongResult.Items.Count > 4 || artistSongResult.Items.Count > size)
                    {
                        var artistShuffled = artistSongResult.Items.OrderBy(_ => rd.Next()).ToList();
                        return artistShuffled.Take(size).ToList();
                    } 
                }
                return [];
            case SampleMethod.RandomSongs:
            default:
                break;
        }

        var result = m_libraryManager.GetItemsResult(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
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
