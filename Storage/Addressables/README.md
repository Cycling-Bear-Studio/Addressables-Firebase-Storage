# Firebase Storage with Addressables

Works with any Addressable Version but not with all features
Using `Addressables.GetDownloadSizeAsync` requires Addressables >=1.75.

## Motivation

Firebase offers a global CDN infrastructure to distribute content to authenticated clients. If should be easy to use this infrastructure in combination with Unity Addressables to provide downloadable AssetBundles to user clients.

## Examples

Examples can be found [HERE](https://gitlab.com/robinbird-studios/hives/firebase-tools-hive)

## Setup

### Configure Remote Path

![RemotePathConfig](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/RemovePathConfig.png?inline=false)

Configure the `RemoteBuildPath` and most importantly the `RemoteLoadPath` to point to your Firebase Project Storage. You can find the FirebaseStorage link by going to the Firebase Console and clicking on `Storage`. After doing the setup you should see your FirebaseStorage link fairly visible at the top. It will end in something like `appspot.com`. Replace the `YOUR_PROJECT` with this link.
Replace the `PATH_TO_ASSETS` with the path within Firebase Storage where the assets are uploaded. The `[BuildTarget]` at the end is optional but if you want to provide to multiple platforms it is recommended.

### Configure Remote Group

![GroupConfig](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/GroupConfiguration.png?inline=false)

Make sure that all groups configured in the Addressables window that you want to download from Firebase use the `FirebaseStorageAssetBundleProvider` as `Asset Bundle Provider Type`. This tells Addressables that it should load this bundle from Firebase Storage. If you have other bundles which should stay local with the game files don't change it.

### Configure Scripts

Before you load any Addressable Asset you have to add the FirebaseStorage Providers and hooks to the Addressables API:

``` csharp
Addressables.ResourceManager.ResourceProviders.Add(new FirebaseStorageAssetBundleProvider());
Addressables.ResourceManager.ResourceProviders.Add(new FirebaseStorageJsonAssetProvider());
Addressables.ResourceManager.ResourceProviders.Add(new FirebaseStorageHashProvider());

// This requires Addressables >=1.75 and can be commented out for lower versions
Addressables.InternalIdTransformFunc += FirebaseAddressablesCache.IdTransformFunc;
```

Once the Firebase Intitializion has finished you should set 
``` csharp
FirebaseAddressablesManager.IsFirebaseSetupFinished = true;
```
to tell the Addressables system that the remote assets can now be loaded. If you don't set it the Addressables system won't load anything.


If your FirebaseStorage authentication rules only allow signed in users to download assets (which I recommend) you have to wait until your user is signed-in. Best practice is to give on additional frame between the "Sign-in User Event" from Firebase and starting to use the APIs because the Firebase Systems use the same event to initialize and thus could spawn problems when you already start download assets during init of FirebaseStorage.


### Configure Build Script (Optional)

1. Add the `FirebaseStorageBuild` Scriptable Object to your Assets directory

![BuildScriptSO](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/CreateBuildScriptAsset.png)

2. Reference the DataBuilder on the main `AddressableAssetSettings`

![ReferenceDataBuilder](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/ReferenceBuildScriptDataBuilder.png)

3. Select the build script in the Addressable window and rebuild your bundles


![BuildScript](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/BuildScriptSelection.png?inline=false)

If you want that the `catalog.json` is also loaded from Firebase Storage you have to select a custom build script which will make sure that the catalog location is properly set.
You also have to set the property `Build Remote Catalog` to true in the main `AddressableAssetSettings.asset` settings. If you want to keep your `catalog.json` in the build you should not configure this build script and just normally build with the default `Packed Mode`.

If everyting is setup and you build your bundles with the `FirebaseBase Storage Build` then you can check the `settings.json` which was created in the `LocalBuildPath` ( configured in `AddressableAssetSettings`). The settings should contain this entry:

``` json
{
    "m_Keys": [
        "AddressablesMainContentCatalogRemoteHash"
    ],
    "m_InternalId": "gs://robinbird-firebasetools.appspot.com/assets/Android/catalog_1.hash",
    "m_Provider": "RobinBird.FirebaseTools.Storage.Addressables.FirebaseStorageHashProvider",
    "m_Dependencies": [],
    "m_ResourceType": {
        "m_AssemblyName": "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "m_ClassName": "System.String"
    }
}
```

Important to note here is that the `m_Provider` is set to the `FirebaseStorageHashProvider`.

#### Understanding this decision:

The `catalog.json` is the data structure which Addressables uses to find your assets. The catalog specifies that asset `coin.png` is in bundle `local.bundle` and is found in the local build .apk files. Also that `chair.png` is in bundle `remote.bundle` and is found on the remote server.
If you want to change assets on the remote server and expect users to get this updated assets you need to have your `catalog.json` along with the `catalog.hash` on your server and also retrieve it from the server.
If you serve assets that never change or only change when you also update your app/game to the customer than you can also use a local catalog and skip this step.

### GetDownloadSizeAsync

ATTENTION: This requires Addressables Version >1.75 because we need the `Addressables.InternalIdTransformFunc` callback.

You can retrieve the download size by using the `Addressables.GetDownloadSizeAsync()` method but there is a special condition when you want to get the download size of Addressables that reside on Firebase Storage.
First you have to call `FirebaseAddressablesCache.PreWarmDependencies()` to instruct Firebase Storage to retrieve the information of the CDN which then is used by Unity's method to calculate the download size.

Code from the example project:
``` csharp
FirebaseAddressablesCache.PreWarmDependencies(downloadAssetKey, () =>
{
    var handler = Addressables.GetDownloadSizeAsync(downloadAssetKey);
    
    handler.Completed += handle =>
    {
        if (handle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError($"Get Download size failed because of error: {handle.OperationException}");
        }
        else
        {
            Debug.Log($"Got download size of: {handle.Result}");
        }
    }
});
```


### Usage

Use the Addressables window and select `Build->Build Player Content`. Now you should have the bundles in the specified `RemoteBuildPath`. Upload these bundles to Firebase Storage matching the path you set at `RemoteLoadPath`. Hit play and after a small delay to download the bundles you should see your remote Firebase Storage bundles popping up. :)


### FAQ

#### If your bundles don't load and you get the error
```
Addressables - initialization failed.
UnityEngine.AddressableAssets.Initialization.<>c__DisplayClass14_0:<LoadContentCatalogInternal>b__0(AsyncOperationHandle`1)
```

You probably have not built your bundles with the `Firebase Storage Build`. Check `Configure Build Script (Optional)`

#### Addressables.GetDownloadSizeAsync() returns always 0

Check if you cleared your cache with [Caching.ClearCache](https://docs.unity3d.com/ScriptReference/Caching.ClearCache.html). Addressables which are in the cache will not count towards the download size.
Check if you have registered the `Addressables.InternalIdTransformFunc` as described above
Check if you call and wait for the callback of `FirebaseAddressablesCache.PreWarmDependencies()`