Knowit.EPiModules.ImageScaling
==============================

Nuget package
------------------------------
The nuget package for this project is pending review at [EPiServer's nuget feed](https://nuget.episerver.com/en/Feed/). When accepted you can add the feed to Visual Studio and write Install-Package Knowit.EPiModules.ImageScaling in the package manager console.


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

Create a class that inherits BaseImageHandler. Implement the abstract function GetImageActions(HttpRequest request).

```csharp
    public class ImageHandler : BaseImageHandler<ImageMedia>
    {
        protected override List<ImageOperation> GetImageActions(HttpRequest request)
        {
            var preset = request.Params["preset"];
            
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
    }
```

To enable caching you must override the virtual function GetCacheKey(HttpRequest request). 
Typically the key will be the same as your preset, but if you also wish to change ImageQuality 
and/or ZoomFactor this should also be incorporated in the cachekey. 
By default the cached files will be stored within a subdirectory of the AppData folder 
(as specified in EPiServer.Framework config).


```csharp
    public class ImageHandler : BaseImageHandler<ImageMedia>
    {
        //... GetImageActions
    
        protected override string GetCacheKey(HttpRequest request)
        {
            //caching will be disabled if preset returns null
            return request.Params["preset"];
        }
    }
```

For further customization there are several methods and properties that you can override. 
Have a look at the source code, play around and have some fun with it to get an idea of what the possibilities are.


Notes:
------------------------------
Currently the solution only works with the default FileBlob storage that comes with EPiServer 7.5+.
We will look into adding support for Azure and other providers in the future.
