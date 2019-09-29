# Firebase Storage with Addressables

## Motivation

Firebase offers a global CDN infrastructure to distribute content to authenticated clients. If should be easy to use this infrastructure in combination with Unity Addressables to provide downloadable AssetBundles to user clients.

## Setup

### Configure Remote Path

![RemotePathConfig](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/RemovePathConfig.png?inline=false)

Configure the `RemoteBuildPath` and most importantly the `RemoteLoadPath` to point to your Firebase Project Storage. You can find the FirebaseStorage link by going to the Firebase Console and clicking on `Storage`. After doing the setup you should see your FirebaseStorage link fairly visible at the top. It will end in something like `appspot.com`. Replace the `YOUR_PROJECT` with this link.
Replace the `PATH_TO_ASSETS` with the path within Firebase Storage where the assets are uploaded. The `[BuildTarget]` at the end is optional but if you want to provide to multiple platforms it is recommended.

### Configure Remote Group

![GroupConfig](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/GroupConfiguration.png?inline=false)

Make sure that all groups configured in the Addressables window that you want to download from Firebase use the `FirebaseStorageAssetBundleProvider` as `Asset Bundle Provider Type`. This tells Addressables that it should load this bundle from Firebase Storage. If you have other bundles which should stay local with the game files don't change it.

### Configure Scripts

Before you load any Addressable Asset you have to add the FirebaseStorage Provides to the Addressables API:

``` csharp
Addressables.ResourceManager.ResourceProviders.Add(new FirebaseStorageAssetBundleProvider());
Addressables.ResourceManager.ResourceProviders.Add(new FirebaseStorageJsonAssetProvider());
Addressables.ResourceManager.ResourceProviders.Add(new FirebaseStorageHashProvider());
```


### Configure Build Script (Optional)

![BuildScript](https://gitlab.com/robinbird-studios/misc/readme-assets/raw/master/firebase-tools/BuildScriptSelection.png?inline=false)

If you want that the `catalog.json` is also loaded from Firebase Storage you have to select a custom build script which will make sure that the catalog location is properly set.
You also have to set the property `Build Remote Catalog` to true in the main `AddressableAssetSettings.asset` settings.

#### Understanding this decision:

The `catalog.json` is the data structure which Addressables uses to find your assets. The catalog specifies that asset `coin.png` is in bundle `local.bundle` and is found in the local build .apk files. Also that `chair.png` is in bundle `remote.bundle` and is found on the remote server.
If you want to change assets on the remote server and expect users to get this updated assets you need to have your `catalog.json` along with the `catalog.hash` on your server and also retrieve it from the server.
If you serve assets that never change or only change when you also update your app/game to the customer than you can also use a local catalog and skip this step.
