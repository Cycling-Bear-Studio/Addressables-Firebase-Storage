namespace RobinBird.FirebaseTools.Editor.Storage.Addressables
{
    using System.IO;
    using FirebaseTools.Storage.Addressables;
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Build.DataBuilders;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.AddressableAssets.Initialization;
    using UnityEngine.AddressableAssets.ResourceLocators;

    /// <summary>
    /// Modifies the settings so we use the Firebase Storage Proxy Loaders to get the Remote Data files
    /// </summary>
    [CreateAssetMenu(fileName = "FirebaseBuildScript.asset", menuName = "Addressable Assets/Data Builders/Firebase Build")]
    public class FireStorageBuildScript : BuildScriptPackedMode
    {
        public override string Name => "FirebaseStorage Build";

        protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            var result = base.DoBuild<TResult>(builderInput, aaContext);

            var settingsPath = Addressables.BuildPath + "/" + builderInput.RuntimeSettingsFilename;

            var data = JsonUtility.FromJson<ResourceManagerRuntimeData>(File.ReadAllText(settingsPath));

            var remoteHash = data.CatalogLocations.Find(locationData =>
                locationData.Keys[0] == "AddressablesMainContentCatalogRemoteHash");

            if (remoteHash != null)
            {
                var newRemoteHash = new ResourceLocationData(remoteHash.Keys, remoteHash.InternalId,
                    typeof(FirebaseStorageHashProvider), remoteHash.ResourceType, remoteHash.Dependencies);

                data.CatalogLocations.Remove(remoteHash);
                data.CatalogLocations.Add(newRemoteHash);

                File.WriteAllText(settingsPath, JsonUtility.ToJson(data));
            }
            
            return result;
        }
    }
}