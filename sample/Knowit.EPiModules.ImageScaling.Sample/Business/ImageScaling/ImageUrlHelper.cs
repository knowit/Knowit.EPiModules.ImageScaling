using System.Web.Mvc;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace Knowit.EPiModules.ImageScaling.Sample.Business.ImageScaling
{
    public static class ImageUrlHelper
    {
        public static string ScaledImage(this UrlHelper urlHelper, ContentReference contentReference, ImagePreset preset)
        {
            var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
            var url = urlResolver.GetUrl(
                contentReference,
                ContentLanguage.PreferredCulture.Name,
                new VirtualPathArguments { ContextMode = ContextMode.Default });
            url += url.Contains("?") ? "&" : "?";
            url += "preset=" + preset;
            return url;
        }
    }
}