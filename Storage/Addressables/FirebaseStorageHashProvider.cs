namespace RobinBird.FirebaseTools.Storage.Addressables
{
    using System.ComponentModel;
    using Firebase.Extensions;
    using Firebase.Storage;
    using UnityEngine;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    /// <summary>
    /// Downloads Hash of the ContentCatalog from FirebaseStorage
    /// </summary>
    [DisplayName("Firebase Hash Provider")]
    public class FirebaseStorageHashProvider : ResourceProviderBase
    {
        private ProvideHandle provideHandle;

        public override void Provide(ProvideHandle provideHandle)
        {
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
            Debug.Log("Loading manifest: " + provideHandle.Location.InternalId);

            var reference =
                FirebaseStorage.DefaultInstance.GetReferenceFromUrl(provideHandle.Location.InternalId);

            reference.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Could not load hash: " + task.Exception);
                    provideHandle.Complete((string) null, false, task.Exception);
                }
                else
                {
                    string url = task.Result.ToString();
                    Debug.Log("Loading via URL: " + url);

                    
                    var catalogLoc =
                        new ResourceLocationBase(url, url, typeof(TextDataProvider).FullName, typeof(string));

                    provideHandle.ResourceManager.ProvideResource<string>(catalogLoc).Completed += handle =>
                    {
                        Debug.Log("Got hash for catalog: " + handle.Result);
                        provideHandle.Complete(handle.Result, true, null);
                    };
                }
            });
        }
    }
}