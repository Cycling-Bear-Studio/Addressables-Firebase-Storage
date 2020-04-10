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
            bool isUsingNativeUrl = provideHandle.Location.InternalId.StartsWith(FirebaseAddressablesConstants.NATIVE_GS_URL_START);
            if (isUsingNativeUrl == false
                && provideHandle.Location.InternalId.StartsWith(FirebaseAddressablesConstants.PATCHED_GS_URL_START) == false)
            {
                base.Provide(provideHandle);
                return;
            }
            
            if (isUsingNativeUrl && hasPrintedProtocolWarning == false)
            {
                string patchedUrl = provideHandle.Location.InternalId.Replace(
                    FirebaseAddressablesConstants.NATIVE_GS_URL_START,
                    FirebaseAddressablesConstants.PATCHED_GS_URL_START);
                
                Debug.LogWarning($"You are currently using an url with the" +
                                 $" '{FirebaseAddressablesConstants.NATIVE_GS_URL_START}' protocol. This will work but" +
                                 $" it is recommended to use the '{FirebaseAddressablesConstants.PATCHED_GS_URL_START}'" +
                                 $" format to get full features of Addressables. Currently Addressables checks for 'http'" +
                                 $" in the InternalId to decide if the asset is remote. Things like GetDownloadSizeAsync()" +
                                 $" will not work. Please change your url to '{patchedUrl}'");
                
                hasPrintedProtocolWarning = true;
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
            if (firebaseUrl.StartsWith(FirebaseAddressablesConstants.PATCHED_GS_URL_START))
            {
                firebaseUrl = firebaseUrl.Replace(
                    FirebaseAddressablesConstants.PATCHED_GS_URL_START,
                    FirebaseAddressablesConstants.NATIVE_GS_URL_START);
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