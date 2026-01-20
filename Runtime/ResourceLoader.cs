using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEssentials
{
    public static class ResourceLoader
    {
        private static readonly Dictionary<string, Object> s_resourceCache = new();

        // Keep handles so we can release Addressables assets on cache clear.
        private static readonly Dictionary<string, AsyncOperationHandle> s_addressablesHandles = new();

        /// <summary>
        /// Loads an asset via Addressables (if available) and optionally falls back to Resources.
        /// 
        /// Resolution order:
        /// 1) Cache (if previously cached)
        /// 2) Addressables (when UNITY_ADDRESSABLES is defined)
        /// 3) Resources (when <paramref name="tryResourcesFallback"/> is true)
        /// 
        /// Note: Addressables loading is synchronous via WaitForCompletion. If you need async, we can add it.
        /// </summary>
        public static T TryGet<T>(
            string keyOrPath,
            bool cacheResource = false,
            bool tryResourcesFallback = true) where T : Object
        {
            if (string.IsNullOrWhiteSpace(keyOrPath))
            {
                Debug.LogWarning("ResourceLoader: keyOrPath is null or empty.");
                return null;
            }

            if (s_resourceCache.TryGetValue(keyOrPath, out var cachedObject))
            {
                if (cachedObject is T typedObject)
                    return typedObject;

                Debug.LogWarning($"ResourceLoader: Cached resource at '{keyOrPath}' is not of type {typeof(T).Name}.");
            }

            var addressable = TryLoadAddressables<T>(keyOrPath, cacheResource);
            if (addressable != null)
                return addressable;
            
            if (!tryResourcesFallback)
                return null;

            var resource = Resources.Load<T>(keyOrPath);
            if (resource == null)
            {
                Debug.LogWarning($"ResourceLoader: Could not find resource '{keyOrPath}' via Addressables or Resources.");
                return null;
            }

            if (cacheResource)
                s_resourceCache[keyOrPath] = resource;

            return resource;
        }

        /// <summary>
        /// Instantiates a prefab via Addressables (if available) and optionally falls back to Resources.
        /// </summary>
        public static GameObject InstantiatePrefab(
            string keyOrPath,
            string instantiatedName = null,
            Transform parent = null,
            bool tryResourcesFallback = true)
        {
            if (string.IsNullOrWhiteSpace(keyOrPath))
            {
                Debug.LogWarning("ResourceLoader: keyOrPath is null or empty.");
                return null;
            }

            var instance = TryInstantiateAddressables(keyOrPath, instantiatedName, parent);
            if (instance != null)
                return instance;

            if (!tryResourcesFallback)
                return null;

            var prefab = Resources.Load<GameObject>(keyOrPath);
            if (prefab == null)
            {
                Debug.LogWarning($"ResourceLoader: Could not find prefab '{keyOrPath}' via Addressables or Resources.");
                return null;
            }

            var resourcesInstance = Object.Instantiate(prefab, parent);
            if (!string.IsNullOrEmpty(instantiatedName))
                resourcesInstance.name = instantiatedName;

            return resourcesInstance;
        }

        private static T TryLoadAddressables<T>(string keyOrAddress, bool cacheResource) where T : Object
        {
            AsyncOperationHandle<T> handle;
            try { handle = Addressables.LoadAssetAsync<T>(keyOrAddress); }
            catch { return null; }

            // Can throw if Addressables isn't initialized properly.
            try { handle.WaitForCompletion(); }
            catch { }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Addressables.Release(handle);
                return null;
            }

            if (cacheResource)
            {
                s_resourceCache[keyOrAddress] = handle.Result;
                // Keep handle so the asset can be released later.
                s_addressablesHandles[keyOrAddress] = handle;
            }
            else
            {
                // If we're not caching, release immediately.
                Addressables.Release(handle);
            }

            return handle.Result;
        }

        private static GameObject TryInstantiateAddressables(string keyOrAddress, string instantiatedName, Transform parent)
        {
            AsyncOperationHandle<GameObject> handle;
            try { handle = Addressables.LoadAssetAsync<GameObject>(keyOrAddress); }
            catch { return null; }

            try { handle.WaitForCompletion(); }
            catch { }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Addressables.Release(handle);
                return null;
            }

            var instance = Object.Instantiate(handle.Result, parent);
            if (!string.IsNullOrEmpty(instantiatedName))
                instance.name = instantiatedName;

            // We only loaded the prefab asset to instantiate it. Release the loaded prefab reference now.
            Addressables.Release(handle);

            return instance;
        }

        // Intentionally not public: user requested only two public methods.
        internal static void ClearCache()
        {
            foreach (var kvp in s_addressablesHandles)
                // Ignore release errors (e.g., already released).
                try { Addressables.Release(kvp.Value); } catch { }

            s_addressablesHandles.Clear();

            s_resourceCache.Clear();
        }
    }
}