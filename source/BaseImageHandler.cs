//Copyright 2014 Knowit Reaktor Oslo AS

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.


using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.ImageLibrary;
using EPiServer.Personalization;
using EPiServer.Security;
using EPiServer.ServiceLocation; 
using EPiServer.Web;
using EPiServer.Web.Routing;
using Knowit.EpiModules.ImageScaling.Cache;
using log4net;
using Task = System.Threading.Tasks.Task;


namespace Knowit.EpiModules.ImageScaling
{
    public abstract class BaseImageHandler<T> : IHttpAsyncHandler, IRenderTemplate<T> where T : ImageData
    {
        private readonly IImageService _imageService;
        private readonly ContentRouteHelper _routeHelper;
        private static readonly ILog Log = LogManager.GetLogger(typeof(BaseImageHandler<T>));

        protected BaseImageHandler()
        {
            _imageService = ServiceLocator.Current.GetInstance<ImageService>();
            _routeHelper = ServiceLocator.Current.GetInstance<ContentRouteHelper>();
        }

        #region Implementation of IHttpHandler

        public bool IsReusable
        {
            get { return false; }
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            return new AsyncResult(cb, extraData, AsyncImageOperations(context));
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            ((AsyncResult)result).Task.Wait(); // wait to let the task throw potential exceptions
        }

        async Task AsyncImageOperations(HttpContext context)
        {
            var routedContent = GetRoutedContent();

            if (!HasAccess(routedContent))
            {
                NoAccess();
                return;
            }

            var imageFile = GetImageFile(routedContent);
            var contentType = GetMimeType(imageFile);

            if (!ApplyImageOperations(context)) // return the original image
            {
                using (var stream = imageFile.BinaryData.OpenRead())
                {
                    await AsyncResponse(context, contentType, stream);
                    return;
                }
            }
            
            
            var cacheKey = GetCacheKey(context);
            if (!string.IsNullOrEmpty(cacheKey))
            {
                var cacheStream = CacheHandler.GetImageCacheStream(imageFile, cacheKey, CacheName);
                if (cacheStream != null)
                {
                    using (cacheStream)
                    {
                        await AsyncResponse(context, contentType, cacheStream);
                        return;
                    }
                }
            }
                
                
            var imageActions = GetImageActions(context);

            var originalStream = new MemoryStream();
            using (var stream = imageFile.BinaryData.OpenRead()) // get original image
            {
                await stream.CopyToAsync(originalStream);
            }

            using (originalStream)
            {
                if (imageActions != null)
                {
                    var imageQuality = GetImageQuality(context.Request);
                    var imageZoomFactor = GetZoomFactor(context.Request);

                    var processedImage = ProcessImage(originalStream.GetBuffer(), imageActions, contentType, imageZoomFactor, imageQuality);

                    if (processedImage != null && processedImage.Length > 0)
                    {
                        var stream = !string.IsNullOrEmpty(cacheKey) 
                            ? CacheHandler.SaveImageCache(processedImage, imageFile, cacheKey, CacheName) 
                            : new MemoryStream(processedImage);

                        await AsyncResponse(context, contentType, stream);
                        return;
                    }
                }

                originalStream.Seek(0, SeekOrigin.Begin); 
                await AsyncResponse(context, contentType, originalStream);
            }
                
            
        }

        async Task AsyncResponse(HttpContext context, string contentType, Stream stream)
        {
            context.Response.Clear();
            context.Response.ContentType = contentType;
            context.Response.Cache.SetCacheability(HttpCacheability.Private);
            context.Response.Cache.SetMaxAge(ClientSideCacheMaxAge);
            await stream.CopyToAsync(context.Response.OutputStream);
        }

        public async void ProcessRequest(HttpContext context)
        {
            await AsyncImageOperations(context);
        }

        #endregion

        #region Must be implemented
        protected abstract List<ImageOperation> GetImageActions(HttpContext context);

        /// <summary>
        /// The cache handler to use.
        /// </summary>
        protected abstract ICacheHandler<T> CacheHandler { get; }

        /// <summary>
        /// Should be relative to OperationPresets, as well as ImageQuality and Zoomfactor if modified from defaults.
        /// </summary>
        protected abstract string GetCacheKey(HttpContext context);

        #endregion

        #region Overrideable

        /// <summary>
        /// Used to check if the handler should apply image operations or not.
        /// Override and make it context-aware where necessary. True by default.
        /// </summary>
        protected virtual bool ApplyImageOperations(HttpContext context)
        {
            return true;
        }

        /// <summary>
        /// Default is one day.
        /// </summary>
        protected virtual TimeSpan ClientSideCacheMaxAge { get { return TimeSpan.FromDays(1); } }

        /// <summary>
        /// Default is ImageHandlerCache
        /// </summary>
        protected virtual string CacheName
        {
            get
            {
                return "ImageHandlerCache";
            }
        }

        /// <summary>
        /// Default is 100. Value returned should be between 1 and 100.
        /// </summary>
        protected virtual int GetImageQuality(HttpRequest request)
        {
            return 100;
        }

        /// <summary>
        /// Defailt is 1.0 This results in no zoom.
        /// </summary>
        protected virtual float GetZoomFactor(HttpRequest request)
        {
            return 1;
        }

        /// <summary>
        /// Default: Uses EPiServer ACL to check for read access
        /// </summary>
        protected virtual bool HasAccess(IContent content)
        {
            return content.QueryDistinctAccess(AccessLevel.Read);
        }

        /// <summary>
        /// Defaults to throwing a HTTPException with statuscode 404
        /// </summary>
        protected virtual void NotFound()
        {
            Log.Info("Requested image not found.");
            throw new HttpException(404, "Not Found.");
        }

        /// <summary>
        /// Defaults to EPiServer's Access Denied Handler
        /// </summary>
        protected virtual void NoAccess()
        {
            Log.Info("Access to requested image denied.");
            AccessDeniedDelegate accessDenied = DefaultAccessDeniedHandler.CreateAccessDeniedDelegate();
            accessDenied(this);
        }

        #endregion

        #region Private

        // Only the following mimetypes are supported by EPiServer's ImageService: 
        // - image/png - image/x-icon - image/bmp - image/gif - image/tiff - image/jpg - image/jpe - image/jpeg - image/pjpeg
        // Extending beyond this will cause exceptions further down the line.
        private string GetMimeType(T imageFile)
        {
            var mimetype = imageFile.MimeType;
            switch (mimetype)
            {

                case "image/x-icon":
                case "image/bmp":
                case "image/gif":
                case "image/tiff":
                case "image/jpg":
                case "image/jpe":
                case "image/jpeg":
                case "image/pjpeg":
                case "image/png":
                    return mimetype;
                default:
                    var message = "Unsupported mimetype: " + mimetype;
                    Log.Fatal("GetMimeType: " + message);
                    throw new NotSupportedException(message);
            }
        }

        private IContent GetRoutedContent()
        {

            var content = _routeHelper.Content;
            if (content == null)
            {
                NotFound();
            }
            return content;
        }

        private T GetImageFile(IContent routedContent)
        {
            var customFile = routedContent as T;
            if (customFile != null && customFile.BinaryData != null)
            {
                return customFile;
            }
            NotFound();
            return null;
        }

        private byte[] ProcessImage(byte[] imageBytes, IEnumerable<ImageOperation> imageActions, string contentType, float zoomFactor, int imageQuality)
        {
            var processedImage = _imageService.RenderImage(imageBytes, imageActions, contentType, zoomFactor, imageQuality);
            return processedImage;
        }

        #endregion
    }
}
