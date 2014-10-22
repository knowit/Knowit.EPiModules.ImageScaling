using System;
using System.Collections.Generic;
using System.Web;
using EPiServer.ImageLibrary;
using EPiServer.Web;
using Knowit.EPiModules.ImageScaling.Sample.Models.Media;

namespace Knowit.EPiModules.ImageScaling.Sample.Business.ImageScaling
{
    public class ImageHandler : BaseImageHandler<ImageMedia>
    {
        protected override List<ImageOperation> GetImageActions(HttpRequest request)
        {
            return ImageOperationPresets.Get(GetPresetFromRequest(request));
        }

        private ImagePreset GetPresetFromRequest(HttpRequest request)
        {
            ImagePreset preset;
            if (!Enum.TryParse(request.Params["preset"], out preset))
            {
                preset = ImagePreset.NoTransform;
            }
            return preset;
        }

        /// <summary>
        /// Required for caching to be enabled. Default path is [appDataPath]/ImageScalingCache
        /// </summary>
        protected override string GetCacheKey(HttpRequest request)
        {
            return GetPresetFromRequest(request).ToString();
        }
    }
}