using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Jellyfin.Plugin.BetterMix.Services;

namespace Jellyfin.Plugin.BetterMix.Filters;

public class ItemInstantMixFilter(BetterMixService service)
        : IAsyncActionFilter
{
    private readonly BetterMixService m_instantMixService = service;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult &&
            objectResult.Value is QueryResult<BaseItemDto> originalResult)
        {
            context.ActionArguments.TryGetValue("itemId", out var itemObj);
            var itemId = itemObj as Guid?;
            if (itemId == null)
            {
                return;
            }

            context.ActionArguments.TryGetValue("userId", out var userIdObj);
            var userId = userIdObj as Guid?;

            context.ActionArguments.TryGetValue("limit", out var limitObj);
            var limit = limitObj as int?;

            context.ActionArguments.TryGetValue("fields", out var fieldsObj);
            var fields = fieldsObj as ItemFields[] ?? [];

            context.ActionArguments.TryGetValue("enableImages", out var enableImagesObj);
            var enableImages = enableImagesObj as bool?;

            context.ActionArguments.TryGetValue("enableUserData", out var enableUserDataObj);
            var enableUserData = enableUserDataObj as bool?;

            context.ActionArguments.TryGetValue("imageTypeLimit", out var imageTypeLimitObj);
            var imageTypeLimit = imageTypeLimitObj as int?;

            context.ActionArguments.TryGetValue("enableImageTypes", out var enableImageTypesObj);
            var enableImageTypes = enableImageTypesObj as ImageType[] ?? [];

            var currentUser = context.HttpContext.User;

            var newResult = m_instantMixService.GenerateMix(
                itemId.Value, userId, limit, fields, enableImages,
                enableUserData, imageTypeLimit, enableImageTypes, currentUser
            );

            executedContext.Result = new ObjectResult(newResult)
            {
                StatusCode = objectResult.StatusCode
            };
        }
    }
}