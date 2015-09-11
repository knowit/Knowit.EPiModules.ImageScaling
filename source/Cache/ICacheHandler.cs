using System.IO;
using EPiServer.Core;

namespace Knowit.EpiModules.ImageScaling.Cache
{
    public interface ICacheHandler<T> where T : ImageData
    {
        /// <summary>
        /// If the cached image exists, retrieves the image stream.
        /// </summary>
        /// <param name="originalImage">Original image, must only be used for reference and timestamp checking.</param>
        /// <param name="cacheKey">The cache key to retrive by.</param>
        /// <param name="cacheName">The name of the cache which the image is saved under.</param>
        /// <returns>Returns stream if the cache image exists. NULL if it doesn't.</returns>
        Stream GetImageCacheStream(T originalImage, string cacheKey, string cacheName);

        /// <summary>
        /// Save the image in the given cache.
        /// </summary>
        /// <param name="imageToCache">Bytes for the image to cache.</param>
        /// <param name="originalImage">Original image, must only be used for reference.</param>
        /// <param name="cacheKey">The cache key to save by.</param>
        /// <param name="cacheName">The name of the cache which the image should be saved under.</param>
        /// <returns>Returns a stream with the cached image.</returns>
        Stream SaveImageCache(byte[] imageToCache, T originalImage, string cacheKey, string cacheName);
    }
}