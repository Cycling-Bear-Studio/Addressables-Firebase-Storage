namespace RobinBird.FirebaseTools.Storage.Addressables
{
    using Firebase.Extensions;
    using Firebase.Storage;
    using UnityEngine;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    /// <summary>
    /// Loads bundles from the Firebase Storage CDN
    /// </summary>
    public class FirebaseStorageAssetBundleProvider : AssetBundleProvider
    {
        public override void Provide(ProvideHandle provideHandle)
        {
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
            var reference =
                FirebaseStorage.DefaultInstance.GetReferenceFromUrl(provideHandle.Location.InternalId.ToLowerInvariant());

            reference.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Could not get url for: " + provideHandle.Location.InternalId + ", " + task.Exception);
                    provideHandle.Complete(this, false, task.Exception);
                    return;
                }

                var url = task.Result.ToString();
                var bundleLoc = new ResourceLocationBase(url, url, typeof(AssetBundleProvider).FullName,
                    typeof(IResourceLocator));

                provideHandle.ResourceManager.ProvideResource<IAssetBundleResource>(bundleLoc).Completed += handle =>
                {
                    provideHandle.Complete(handle.Result, true, null);
                };
            });
        }
    }
}