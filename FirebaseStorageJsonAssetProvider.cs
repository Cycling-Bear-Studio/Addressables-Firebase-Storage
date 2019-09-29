namespace RobinBird.FirebaseTools
{
    using Firebase.Extensions;
    using Firebase.Storage;
    using UnityEngine;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    /// <summary>
    /// Loads JSON data from FirebaseStorage. Needed mainly for ContentCatalog downloads
    /// </summary>
    public class FirebaseStorageJsonAssetProvider : JsonAssetProvider
    {
        private ProvideHandle provideHandle;

        /// <summary>
        /// Unfortunately we have to override this because the method CanProvide is only called once and when the InternalId
        /// changes this Provider is still selected for non-Firebase Json data. We just call into base when that happens
        /// </summary>
        public override string ProviderId => typeof(JsonAssetProvider).FullName;

        public override void Provide(ProvideHandle provideHandle)
        {
            if (provideHandle.Location.InternalId.StartsWith("gs") == false)
            {
                base.Provide(provideHandle);
                return;
            }
            
            this.provideHandle = provideHandle;
            if (FirebaseAddressablesManager.IsFirebaseSetupFinished)
            {
                LoadManifest();
            }
            else
            {
                FirebaseAddressablesManager.FirebaseSetupFinished += LoadManifest;
            }
        }

        private void LoadManifest()
        {
            FirebaseAddressablesManager.FirebaseSetupFinished -= LoadManifest;
            Debug.Log("Loading Json at: " + provideHandle.Location.InternalId);
            
            var reference = FirebaseStorage.DefaultInstance.GetReferenceFromUrl(provideHandle.Location.InternalId.ToLowerInvariant());

            reference.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Could not load manifest: " + task.Exception);
                    provideHandle.Complete<object>(null, false, task.Exception);
                }
                else
                {
                    string url = task.Result.ToString();
                    Debug.Log("Got URL: " + url);
                    
                    var catalogLoc = new ResourceLocationBase(url, url, typeof(JsonAssetProvider).FullName, typeof(IResourceLocator));

                    
                    if (provideHandle.Location.ResourceType == typeof(ContentCatalogData))
                    {
                        provideHandle.ResourceManager.ProvideResource<ContentCatalogData>(catalogLoc).Completed += handle =>
                        {
                            provideHandle.Complete(handle.Result, true, null);
                        };
                    }
                    else
                    {
                        Debug.LogError("Could not convert type because function is private. Please add own type here");
                        // provideHandle.ResourceManager.ProvideResource(catalogLoc, provideHandle.Location.ResourceType).Completed += handle =>
                        // {
                        //     provideHandle.Complete(handle.Result, true, null);
                        // };
                        provideHandle.Complete<object>(null, false, null);
                    }
                }
            });
        }
    }
}