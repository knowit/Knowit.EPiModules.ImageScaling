Knowit.EPiModules.ImageScaling
==============================

Nuget package
------------------------------
The nuget package for this project used to be located at [EPiServer's nuget feed](https://nuget.episerver.com/en/Feed/), but its currently outdated for the new versions of episerver. Feel free to use the source code for now.


Usage
------------------------------
__An example project on the intended usage is available in the [sample](/sample) folder within the git repository. This also includes how you can incorporate it with your views, as well as structuring presets. The following example only shows the implementation of the handler itself.__

Create a media class that inherits EPiServer.Core.ImageData with a MediaDescriptor attribute. 
The following extensions can be used: jpg, jpeg, jpe, pjpeg, ico, gif, bmp, png, tiff

```csharp
    [ContentType(GUID = "DF31A9CB-119D-46BF-84FC-CA0E6C445EC4")]
    [MediaDescriptor(ExtensionString = "jpg,jpeg,jpe,pjpeg,ico,gif,bmp,png,tiff")]
    public class ImageMedia : ImageData
    {
         //...
    }
```

Create a class that inherits BaseImageHandler. Implement the abstract member GetImageActions. If you want to cache the processed images you can override CacheHandler and GetCacheKey. Implementations for ICacheHandler using the filesystem or azure storage can be found in the source.

```csharp
    public class ImageHandler : BaseImageHandler<ImageMedia>
    {
        protected override List<ImageOperation> GetImageActions(HttpContext context)
        {
            var preset = context.Request.Params["preset"];

            if (preset != null && preset == "500width")
            {
                // The image will be resized to 500px wide, with the same aspect ratio
                return new List<ImageOperation>
                {
                    new ImageOperation(ImageEditorCommand.Resize) {Width = 500}
                };
            }

            return null; //The image will not be processed
        }
        
        //Required for caching of processed images

        //protected override ICacheHandler<ImageMedia> CacheHandler
        //{
        //    get { return new FileCacheHandler<ImageMedia>(); }
        //}

        //protected override string GetCacheKey(HttpContext context)
        //{
        //    return GetPresetFromRequest(context.Request).ToString();
        //}
    }
```

For further customization there are several methods and properties that you can override. 
Have a look at the source code, play around and have some fun with it to get an idea of what the possibilities are.
