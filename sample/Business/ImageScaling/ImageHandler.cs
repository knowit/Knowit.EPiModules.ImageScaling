
using System;
using System.Collections.Generic;
using System.Web;
using EPiServer.ImageLibrary;
using Knowit.EpiModules.ImageScaling;
using Knowit.EpiModules.ImageScaling.Caching;
using Knowit.EPiModules.ImageScaling.Caching.FileSystem;
using Knowit.EPiModules.ImageScaling.Sample.Models.Media;

namespace Knowit.EPiModules.ImageScaling.Sample.Business.ImageScaling
{
    public class ImageHandler : BaseImageHandler<ImageMedia>
    {
        private ImagePreset GetPresetFromRequest(HttpRequest request)
        {
            ImagePreset preset;
            if (!Enum.TryParse(request.Params["preset"], out preset))
            {
                preset = ImagePreset.NoTransform;
            }
            return preset;
        }


        protected override List<ImageOperation> GetImageActions(HttpContext context)
        {
            return ImageOperationPresets.Get(GetPresetFromRequest(context.Request));
        }

        protected override ICacheHandler<ImageMedia> CacheHandler
        {
            get { return new FileCacheHandler<ImageMedia>(); }
        }

        protected override string GetCacheKey(HttpContext context)
        {
            return GetPresetFromRequest(context.Request).ToString();
        }
    }
}