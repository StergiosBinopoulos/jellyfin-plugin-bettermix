using System;
using System.ComponentModel.DataAnnotations;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Jellyfin.Plugin.BetterMix.ModelBinders;
using Jellyfin.Plugin.BetterMix.Services;

namespace Jellyfin.Plugin.BetterMix.Controllers;

[ApiController]
[Route("BetterMix")]
public class BetterMixController(
    BetterMixService betterMixService) : ControllerBase
{
    private readonly BetterMixService m_betterMixService = betterMixService;

    [HttpGet("Items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<BaseItemDto>> GetBetterMixFromItem(
        [FromRoute, Required] Guid itemId,
        [FromQuery] Guid? userId,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableImages,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes)
    {
        return m_betterMixService.GenerateMix(itemId, userId, limit, fields, enableImages, enableUserData, imageTypeLimit, enableImageTypes, User);
    }
}