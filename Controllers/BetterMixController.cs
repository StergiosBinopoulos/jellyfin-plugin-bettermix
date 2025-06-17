using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.BetterMix.ModelBinders;
using Jellyfin.Extensions;
using Jellyfin.Plugin.BetterMix.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.BetterMix.Controllers;

[ApiController]
[Route("BetterMix")]
public class BetterMixController : ControllerBase
{
    private readonly IUserManager m_userManager;
    private readonly IDtoService m_dtoService;
    private readonly ILibraryManager m_libraryManager;
    private readonly IMusicManager m_musicManager;

    public BetterMixController(
        IUserManager userManager,
        IDtoService dtoService,
        IMusicManager musicManager,
        ILibraryManager libraryManager)
    {
        m_userManager = userManager;
        m_dtoService = dtoService;
        m_musicManager = musicManager;
        m_libraryManager = libraryManager;
    }

    [HttpGet("Items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<BaseItemDto>> GetInstantMixFromItem(
        [FromRoute, Required] Guid itemId,
        [FromQuery] Guid? userId,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableImages,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes)
    {
        var user = userId.IsNullOrEmpty()
            ? null
            : m_userManager.GetUserById(userId.Value);
        var item = m_libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        int numsongs = limit ?? 200;

        List<BaseItem>? items = null;
        if (item is Audio)
        {
            items = BetterMixPlugin.Instance.ActiveBackend.GetPlaylistFromSong(item.Path, numsongs);
        }

        // fallback to Native Instant Mix
        if (items is null)
        {
            BetterMixPlugin.Instance.Logger.LogInformation("BetterMix: Falling back to Native Instant Mix.");
            items = m_musicManager.GetInstantMixFromItem(item, user, dtoOptions);
        }

        return GetResult(items, user, numsongs, dtoOptions);
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