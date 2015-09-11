using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;

namespace Knowit.EPiModules.ImageScaling.Sample.Models.Media
{
    [ContentType(GUID = "DF31A9CB-119D-46BF-84FC-CA0E6C445EC4")]
    [MediaDescriptor(ExtensionString = "jpg,jpeg,jpe,pjpeg,ico,gif,bmp,png,tiff")]
    public class ImageMedia : ImageData
    {
         
    }
}