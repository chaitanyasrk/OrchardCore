using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.Environment.Cache;
using StackExchange.Profiling;

namespace OrchardCore.Contents.Controllers
{
    public class ItemController : Controller, IUpdateModel
    {
        private readonly IContentManager _contentManager;
        private readonly IContentItemDisplayManager _contentItemDisplayManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMemoryCache _memoryCache;
        private readonly ISignal _signal;

        public ItemController(
            IContentManager contentManager,
            IContentItemDisplayManager contentItemDisplayManager,
            IAuthorizationService authorizationService,
            IMemoryCache memoryCache,
            ISignal signal)
        {
            _contentManager = contentManager;
            _contentItemDisplayManager = contentItemDisplayManager;
            _authorizationService = authorizationService;
            _memoryCache = memoryCache;
            _signal = signal;
        }

        public async Task<IActionResult> Display(string contentItemId, string jsonPath)
        {
            using (MiniProfiler.Current.Step("Time take for ItemController --> Display: "))
            {
                var cacheKey = GetCacheKey(contentItemId);

                if(!_memoryCache.TryGetValue(cacheKey, out ContentItem contentItem))
                {
                    contentItem = await _contentManager.GetAsync(contentItemId, jsonPath);
                }

                if (contentItem == null)
                {
                    return NotFound();
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .AddExpirationToken(_signal.GetToken(GetSignalName(contentItemId)))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _memoryCache.Set(cacheKey, contentItem, cacheEntryOptions);

                using (MiniProfiler.Current.Step("AuthorizeAsync"))
                {
                    if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.ViewContent, contentItem))
                    {
                        return this.ChallengeOrForbid();
                    }
                }

                using (MiniProfiler.Current.Step("BuildDisplayAsync"))
                {
                    var model = await _contentItemDisplayManager.BuildDisplayAsync(contentItem, this);

                    return View(model);
                }
            }
        }

        public async Task<IActionResult> Preview(string contentItemId)
        {
            if (contentItemId == null)
            {
                return NotFound();
            }

            var versionOptions = VersionOptions.Latest;

            var contentItem = await _contentManager.GetAsync(contentItemId, versionOptions);

            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, CommonPermissions.PreviewContent, contentItem))
            {
                return this.ChallengeOrForbid();
            }

            var model = await _contentItemDisplayManager.BuildDisplayAsync(contentItem, this);

            return View(model);
        }

        private string GetCacheKey(string contentItemId)
        {
            return $"ContentItemDisplay_{contentItemId}";
        }

        private string GetSignalName(string contentItemId)
        {
            return $"ContentItemDisplaySignal_{contentItemId}";
        }
    }
}
