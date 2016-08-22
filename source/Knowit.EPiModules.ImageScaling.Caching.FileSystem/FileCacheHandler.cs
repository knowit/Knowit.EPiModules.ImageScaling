using System;
using System.IO;
using EPiServer.Core;
using EPiServer.Logging.Compatibility;
using Knowit.EpiModules.ImageScaling.Caching;

namespace Knowit.EPiModules.ImageScaling.Caching.FileSystem
{
    public class FileCacheHandler<T> : ICacheHandler<T> where T : ImageData
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileCacheHandler<T>));

        protected virtual string GetFileCachePath(string cacheName, ContentReference contentLink, string cacheKey)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                EPiServer.Framework.Configuration.EPiServerFrameworkSection.Instance.AppData.BasePath + string.Format("\\{0}\\{1}\\{2}.cache", cacheName, contentLink.ID, cacheKey));
        }

        public Stream GetImageCacheStream(T originalImage, string cacheKey, string cacheName)
        {
            var cacheFile = GetFileCachePath(cacheName, originalImage.ContentLink, cacheKey);

            try
            {
                if (File.Exists(cacheFile))
                {
                    if (File.GetLastWriteTimeUtc(cacheFile) < originalImage.Changed.ToUniversalTime()) // invalidate cache, it is too old
                    {
                        File.Delete(cacheFile);
                        return null;
                    }

                    return new FileStream(cacheFile, FileMode.Open, FileAccess.Read);
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("GetImageCacheStream: Could not get from cache: {0}", cacheFile), ex);
                return null;
            }
        }

        public Stream SaveImageCache(byte[] imageToCache, T originalImage, string cacheKey, string cacheName)
        {
            var cacheFile = GetFileCachePath(cacheName, originalImage.ContentLink, cacheKey);

            Log.InfoFormat("SaveImageCache: Saving scaled image to cache: {0}", cacheFile);
            var directory = Path.GetDirectoryName(cacheFile);
            if (directory != null)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Log.InfoFormat("SaveImageCache: Created cache directory for: {0}", cacheFile);
                }
                File.WriteAllBytes(cacheFile, imageToCache);
                Log.InfoFormat("SaveImageCache: Cache successfully saved: {0}", cacheFile);
            }
            else
            {
                Log.ErrorFormat("SaveImageCache: Could not save to cache: {0}", cacheFile);
            }

            return new MemoryStream(imageToCache);
        }
    }
}
