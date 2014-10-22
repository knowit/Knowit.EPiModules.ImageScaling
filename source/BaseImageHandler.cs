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
using EPiServer.Security;
using EPiServer.ServiceLocation; 
using EPiServer.Web;
using EPiServer.Web.Routing;
using log4net;


namespace Knowit.EPiModules.ImageScaling
{
    public abstract class BaseImageHandler<T> : IHttpHandler, IRenderTemplate<T> where T : ImageData
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

        public void ProcessRequest(HttpContext context)
        {
            var routedContent = GetRoutedContent();

            if (!HasAccess(routedContent))
            {
                NoAccess();
                return;
            }

            var imageFile = GetImageFile(routedContent);
            var filePath = GetFilePath(imageFile);
            var extension = Path.GetExtension(filePath);
            var contentType = GetMimeType(extension);

            var cacheKey = GetCacheKey(context.Request);
            var cachePath = GetCachedFilePath(imageFile.ContentLink, cacheKey);

            if (cachePath != null && File.Exists(cachePath) && File.GetLastWriteTimeUtc(cachePath) > imageFile.Changed.ToUniversalTime())
            {
                Log.Debug("Returning scaled image from cache: " + cachePath);
                ReturnResponse(context, contentType, () => context.Response.WriteFile(cachePath));
            }
            else
            {
                var imageActions = GetImageActions(context.Request);

                if (imageActions != null)
                {
                    var imageQuality = GetImageQuality(context.Request);
                    var imageZoomFactor = GetZoomFactor(context.Request);
                    var processedImage = ProcessImage(filePath, imageActions, contentType, imageZoomFactor, imageQuality);
                    if (processedImage != null && processedImage.Length > 0)
                    {
                        SaveToCache(cachePath, processedImage);
                        ReturnResponse(context, contentType, () => context.Response.BinaryWrite(processedImage));
                        return;
                    }
                }
                ReturnResponse(context, contentType, () => context.Response.WriteFile(filePath));
            }
        }

        #endregion

        #region Must be implemented
        protected abstract List<ImageOperation> GetImageActions(HttpRequest request);
        #endregion

        #region Overrideable

        /// <summary>
        /// Default is [AppDataPath]/ImageScalingCache/
        /// </summary>
        protected virtual string CacheFolder
        {
            get
            {
                return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    EPiServer.Framework.Configuration.EPiServerFrameworkSection.Instance.AppData.BasePath + "\\ImageScalingCache");
            }
        }

        /// <summary>
        /// Default is one day.
        /// </summary>
        protected virtual TimeSpan ClientSideCacheMaxAge { get { return TimeSpan.FromDays(1); } }

        /// <summary>
        /// Default returns null. This effectivly disables caching.
        /// Should be relative to OperationPresets, as well as ImageQuality and Zoomfactor if modified from defaults.
        /// </summary>
        protected virtual string GetCacheKey(HttpRequest request)
        {
            return null;
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

        private void SaveToCache(string cachePath, byte[] processedImage)
        {
            if (cachePath == null)
            {
                Log.Debug("SaveToCache: Caching disabled");
                return;
            }

            Log.Info("SaveToCache: Saving scaled image to cache: " + cachePath);
            var directory = Path.GetDirectoryName(cachePath);
            if (directory != null)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Log.Info("SaveToCache: Created cache directory for: " + cachePath);
                }
                File.WriteAllBytes(cachePath, processedImage);
                Log.Info("SaveToCache: Cache successfully saved: " + cachePath);
            }
            else
            {
                Log.Error("SaveToCache: Could not save to cache: " + cachePath);
            }


        }

        private string GetCachedFilePath(ContentReference contentLink, string cacheKey)
        {
            if (cacheKey == null)
            {
                Log.Debug("GetCachedFilePath: Caching disabled");
                return null;
            }
            return CacheFolder + "\\" + contentLink.ID + "\\" + cacheKey + ".cache";

        }

        private string GetMimeType(string extension)
        {
            // Only the following mimetypes are supported by EPiServer's ImageService: 
            // - image/png - image/x-icon - image/bmp - image/gif - image/tiff - image/jpg - image/jpe - image/jpeg - image/pjpeg
            // Extending beyond this will cause exceptions further down the line.

            var extensionLowered = extension.Replace(".", string.Empty).ToLower();
            switch (extensionLowered)
            {
                case "ico":
                    return "image/x-icon";
                case "bmp":
                case "gif":
                case "tiff":
                case "jpg":
                case "jpe":
                case "jpeg":
                case "pjpeg":
                case "png":
                    return "image/" + extensionLowered;
                default:
                    var message = "Could not find supporting Mimetype to extenstion: " + extension;
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

        private string GetFilePath(T imageFile)
        {
            var blob = imageFile.BinaryData as FileBlob;
            if (blob != null && File.Exists(blob.FilePath)) return blob.FilePath;

            NotFound();
            return null;
        }

        private void ReturnResponse(HttpContext context, string contentType, Action writeImage)
        {
            context.Response.Clear();
            context.Response.ContentType = contentType;
            context.Response.Cache.SetCacheability(HttpCacheability.Private);
            context.Response.Cache.SetMaxAge(ClientSideCacheMaxAge);
            writeImage();
            context.Response.End();
        }

        private byte[] ProcessImage(string filePath, IEnumerable<ImageOperation> imageActions, string contentType, float zoomFactor, int imageQuality)
        {
            var imageBytes = File.ReadAllBytes(filePath);
            var processedImage = _imageService.RenderImage(imageBytes, imageActions, contentType, zoomFactor, imageQuality);
            return processedImage;
        }

        #endregion

    }
}
