namespace RobinBird.FirebaseTools.Storage.Addressables
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Firebase.Extensions;
    using Firebase.Storage;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    /// <summary>
    /// Loads bundles from the Firebase Storage CDN
    /// </summary>
    [DisplayName("Firebase AssetBundle Provider")]
    public class FirebaseStorageAssetBundleProvider : AssetBundleProvider
    {
        private static bool hasPrintedProtocolWarning;
        
        public readonly Dictionary<string, AsyncOperationHandle<IAssetBundleResource>> bundleOperationHandles = new Dictionary<string, AsyncOperationHandle<IAssetBundleResource>>();
        
        public override void Release(IResourceLocation location, object asset)
        {
            base.Release(location, asset);

            // We have to make sure that the actual Bundle Load operation for this asset also gets released together with the Firebase Resource
            if (bundleOperationHandles.TryGetValue(location.InternalId, out AsyncOperationHandle<IAssetBundleResource> operation))
            {
                if (operation.IsValid())
                {
                    Addressables.ResourceManager.Release(operation);
                }
                bundleOperationHandles.Remove(location.InternalId);
            }
        }
        
        

        public override void Provide(ProvideHandle provideHandle)
        {
            string path = provideHandle.ResourceManager.TransformInternalId(provideHandle.Location);
            if (FirebaseAddressablesManager.IsFirebaseStorageLocation(path) == false)
            {
                base.Provide(provideHandle);
                return;
            }

            if (FirebaseAddressablesManager.IsFirebaseSetupFinished)
            {
                LoadResource(provideHandle);
            }
            else
            {
                FirebaseAddressablesManager.FirebaseSetupFinished += () => { LoadResource(provideHandle); };
            }
        }

        private void LoadResource(ProvideHandle provideHandle)
        {
            string firebaseUrl = provideHandle.ResourceManager.TransformInternalId(provideHandle.Location);
            
            var reference = FirebaseStorage.DefaultInstance.GetReferenceFromUrl(firebaseUrl);

            reference.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Could not get url for: {firebaseUrl}, {task.Exception}");
                    provideHandle.Complete(this, false, task.Exception);
                    return;
                }

                string url = task.Result.ToString();
                FirebaseAddressablesCache.SetInternalIdToStorageUrlMapping(firebaseUrl, url);
                IResourceLocation[] dependencies;
                IList<IResourceLocation> originalDependencies = provideHandle.Location.Dependencies;
                if (originalDependencies != null)
                {
                    dependencies = new IResourceLocation[originalDependencies.Count];
                    for (int i = 0; i < originalDependencies.Count; i++)
                    {
                        var dependency = originalDependencies[i];

                        dependencies[i] = dependency;
                    }
                }
                else
                {
                    dependencies = new IResourceLocation[0];
                }
                var bundleLoc = new ResourceLocationBase(url, url, GetType().FullName,
                    typeof(IResourceLocator), dependencies)
                {
                    Data = provideHandle.Location.Data,
                    PrimaryKey = provideHandle.Location.PrimaryKey
                };

                AsyncOperationHandle<IAssetBundleResource> asyncOperationHandle;
                if (bundleOperationHandles.TryGetValue(firebaseUrl, out asyncOperationHandle))
                {
                    // Release already running handler
                    if (asyncOperationHandle.IsValid())
                    {
                        provideHandle.ResourceManager.Release(asyncOperationHandle);
                    }
                }
                asyncOperationHandle = provideHandle.ResourceManager.ProvideResource<IAssetBundleResource>(bundleLoc);
                bundleOperationHandles.Add(firebaseUrl, asyncOperationHandle);
                asyncOperationHandle.Completed += handle =>
                {
                    provideHandle.Complete(handle.Result, true, null);
                };
            });
        }
    }
}