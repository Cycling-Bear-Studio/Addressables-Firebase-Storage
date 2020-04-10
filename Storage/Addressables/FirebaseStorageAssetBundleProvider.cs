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
        public readonly Dictionary<string, AsyncOperationHandle<IAssetBundleResource>> bundleOperationHandles = new Dictionary<string, AsyncOperationHandle<IAssetBundleResource>>();
        
        public override void Release(IResourceLocation location, object asset)
        {
            base.Release(location, asset);

            // We have to make sure that the actual Bundle Load operation for this asset also gets released together with the Firebase Resource
            if (bundleOperationHandles.TryGetValue(location.InternalId, out AsyncOperationHandle<IAssetBundleResource> opertion))
            {
                if (opertion.IsValid())
                {
                    Addressables.ResourceManager.Release(opertion);
                }
                bundleOperationHandles.Remove(location.InternalId);
            }
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            if (provideHandle.Location.InternalId.StartsWith("gs") == false
                && provideHandle.Location.InternalId.StartsWith("httpgs") == false)
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
            string firebaseUrl = provideHandle.Location.InternalId;
            if (firebaseUrl.StartsWith("httpgs://"))
            {
                // Workaround for GetDownloadSizeAsync method because only ids with http at the beginning are considered
                // in the calculation
                firebaseUrl = firebaseUrl.Replace("httpgs://", "gs://");
            }
            var reference =
                FirebaseStorage.DefaultInstance.GetReferenceFromUrl(firebaseUrl);

            reference.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"Could not get url for: {firebaseUrl}, {task.Exception}");
                    provideHandle.Complete(this, false, task.Exception);
                    return;
                }

                string url = task.Result.ToString();
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
                if (bundleOperationHandles.TryGetValue(provideHandle.Location.InternalId, out asyncOperationHandle))
                {
                    // Release already running handler
                    if (asyncOperationHandle.IsValid())
                    {
                        provideHandle.ResourceManager.Release(asyncOperationHandle);
                    }
                }
                asyncOperationHandle = provideHandle.ResourceManager.ProvideResource<IAssetBundleResource>(bundleLoc);
                bundleOperationHandles.Add(provideHandle.Location.InternalId, asyncOperationHandle);
                asyncOperationHandle.Completed += handle =>
                {
                    provideHandle.Complete(handle.Result, true, null);
                };
            });
        }
    }
}