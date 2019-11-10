namespace RobinBird.FirebaseTools.Storage.Addressables
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Firebase.Extensions;
    using Firebase.Storage;
    using UnityEngine;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    /// <summary>
    /// Loads bundles from the Firebase Storage CDN
    /// </summary>
    [DisplayName("Firebase AssetBundle Provider")]
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
                var bundleLoc = new ResourceLocationBase(url, url, typeof(AssetBundleProvider).FullName,
                    typeof(IResourceLocator), dependencies)
                {
                    Data = provideHandle.Location.Data,
                    PrimaryKey = provideHandle.Location.PrimaryKey
                };
                
                provideHandle.ResourceManager.ProvideResource<IAssetBundleResource>(bundleLoc).Completed += handle =>
                {
                    provideHandle.Complete(handle.Result, true, null);
                };
            });
        }
    }
}