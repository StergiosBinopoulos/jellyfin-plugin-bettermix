using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Jellyfin.Plugin.BetterMix.Filters;

public static class InjectActionFilter
{
    public static int AddDynamicFilter<T>(
        this IActionDescriptorCollectionProvider provider,
        IServiceProvider serviceProvider,
        Func<ControllerActionDescriptor, bool> matcher)
        where T : IFilterMetadata
    {
        var actionDescriptors = provider.ActionDescriptors.Items;

        var targetActions = actionDescriptors.Where(ad =>
        {
            return ad is ControllerActionDescriptor cad && matcher(cad);
        }).ToArray();

        foreach (var action in targetActions)
        {
            var filter = ActivatorUtilities.CreateInstance<T>(serviceProvider);

            var filterMetadata = action.FilterDescriptors;
            filterMetadata.Add(new FilterDescriptor(filter, FilterScope.Global));
        }

        return targetActions.Length;
    }
}