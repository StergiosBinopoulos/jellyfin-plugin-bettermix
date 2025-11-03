using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Dto;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Plugin.BetterMix.Extensions;
using Jellyfin.Plugin.BetterMix.Backend;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;

namespace Jellyfin.Plugin.BetterMix.Services;

public class BetterMixService(
    IUserManager userManager,
    ILibraryManager libraryManager,
    IMusicManager musicManager,
    IDtoService dtoService
    )
{
    private readonly IUserManager m_userManager = userManager;
    private readonly ILibraryManager m_libraryManager = libraryManager;
    private readonly IMusicManager m_musicManager = musicManager;
    private readonly IDtoService m_dtoService = dtoService;

    public QueryResult<BaseItemDto> GenerateMix(
        Guid itemId,
        Guid? userId,
        int? limit,
        ItemFields[] fields,
        bool? enableImages,
        bool? enableUserData,
        int? imageTypeLimit,
        ImageType[] enableImageTypes,
        ClaimsPrincipal currentUser)
    {
        var user = userId.IsNullOrEmpty()
            ? null
            : m_userManager.GetUserById(userId.Value);

        // var item = m_libraryManager.GetItemById<BaseItem>(itemId, user);
        var item = m_libraryManager.GetItemById<BaseItem>(itemId);
        if (item is null)
        {
            return new QueryResult<BaseItemDto> { Items = [], TotalRecordCount = 0 };
        }

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(currentUser)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        int numsongs = limit ?? 200;

        List<BaseItem>? items = null;
        if (item is Audio)
        {
            items = BetterMixPlugin.Instance.ActiveBackend.GetPlaylist([item.Path], numsongs, BetterMixBackendBase.PlaylistType.FromAudio);
        }
        else if (item is MusicAlbum album)
        {    
            List<string> inputItems = album.Children?
                .Where(child => child is Audio)
                .Select(child => child.Path)
                .ToList() ?? new List<string>();

            items = BetterMixPlugin.Instance.ActiveBackend.GetPlaylist(inputItems, numsongs, BetterMixBackendBase.PlaylistType.FromAlbum);
        }

        if (items is null)
            {
                BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Falling back to Native Instant Mix.");
                items = m_musicManager.GetInstantMixFromItem(item, user, dtoOptions).ToList();
            }

        var result = GetResult(items, user, numsongs, dtoOptions);
        return result;
    }

    private QueryResult<BaseItemDto> GetResult(IReadOnlyList<BaseItem> items, User? user, int? limit, DtoOptions dtoOptions)
    {
        var totalCount = items.Count;

        if (limit.HasValue && limit < items.Count)
        {
            items = items.Take(limit.Value).ToArray();
        }

        var result = new QueryResult<BaseItemDto>(
            0,
            totalCount,
            m_dtoService.GetBaseItemDtos(items, dtoOptions, user));

        return result;
    }
}
