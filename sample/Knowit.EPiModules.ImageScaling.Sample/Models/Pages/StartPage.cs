using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Web;

namespace Knowit.EPiModules.ImageScaling.Sample.Models.Pages
{
    [ContentType(GUID = "285f1d97-d07f-4851-8507-766eb0b47f2f")]
    public class StartPage : PageData
    {
        [UIHint(UIHint.Image)]
        public virtual ContentReference MainImage { get; set; }
    }
}