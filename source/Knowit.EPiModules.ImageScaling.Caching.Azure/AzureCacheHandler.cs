using System;
using System.IO;
using EPiServer.Core;
using EPiServer.Logging.Compatibility;
using Knowit.EpiModules.ImageScaling.Caching;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Knowit.EPiModules.ImageScaling.Caching.Azure
{
    public class AzureCacheHandler<T> : ICacheHandler<T> where T : ImageData
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AzureCacheHandler<T>));
        private readonly CloudBlobContainer _blobContainer;

        public AzureCacheHandler(CloudBlobContainer blobContainer)
        {
            _blobContainer = blobContainer;
        }

        protected virtual string GetBlobCacheName(ContentReference contentLink, string cacheKey, string cacheName, string name)
        {
            return string.Format("{0}/{1}-{2}-{3}", cacheName, contentLink.ID, cacheKey, name);
        }

        public Stream GetImageCacheStream(T originalImage, string cacheKey, string cacheName)
        {
            var blobCacheName = GetBlobCacheName(originalImage.ContentLink, cacheKey, cacheName, originalImage.Name);
            var blob = _blobContainer.GetBlockBlobReference(blobCacheName);
            if (blob == null) return null;

            Stream stream;
            try
            {
                stream = blob.OpenRead();
            }
            catch // probably doesn't exist
            {
                return null;
            }

            try
            {
                if (blob.Properties.LastModified < originalImage.Changed.ToUniversalTime()) // invalidate cache, it is too old
                {
                    blob.DeleteIfExists();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("GetImageCacheStream: Unable to invalidate cache: {0}", blobCacheName), ex);
                return null;
            }

            return stream;
        }

        public Stream SaveImageCache(byte[] imageToCache, T originalImage, string cacheKey, string cacheName)
        {
            var blobCacheName = GetBlobCacheName(originalImage.ContentLink, cacheKey, cacheName, originalImage.Name);
            var blob = _blobContainer.GetBlockBlobReference(blobCacheName);
            if (blob == null) return null;

            try
            {
                using (var stream = blob.OpenWrite())
                using (var ms = new MemoryStream(imageToCache))
                {
                    ms.CopyTo(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("SaveImageCache: Unable to save cache: {0}", blobCacheName), ex);
                return new MemoryStream(imageToCache);
            }

            return new MemoryStream(imageToCache);
        }
    }
}
