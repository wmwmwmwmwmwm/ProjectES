using UnityEngine;
using UnityEngine.Serialization;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ResourceProviderConfiguration : Configuration
    {
        /// <summary>
        /// Assembly-qualified type name of the built-in project resource provider.
        /// </summary>
        public const string ProjectTypeName = "Naninovel.ProjectResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assembly-qualified type name of the built-in local resource provider.
        /// </summary>
        public const string LocalTypeName = "Naninovel.LocalResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assembly-qualified type name of the built-in Google Drive resource provider.
        /// </summary>
        public const string GoogleDriveTypeName = "Naninovel.GoogleDriveResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assembly-qualified type name of the built-in virtual resource provider.
        /// </summary>
        public const string VirtualTypeName = "Naninovel.VirtualResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Assembly-qualified type name of the built-in addressable resource provider.
        /// </summary>
        public const string AddressableTypeName = "Naninovel.AddressableResourceProvider, Elringus.Naninovel.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        /// <summary>
        /// Unique identifier (group name, address prefix, label) used with assets managed by the Naninovel resource provider.
        /// </summary>
        public const string AddressableId = "Naninovel";
        /// <summary>
        /// Assigned from the editor assembly when the application is running under Unity editor.
        /// </summary>
        public static IResourceProvider EditorProvider = default;

        /// <summary>
        /// Used by the <see cref="IResourceProviderManager"/> before all the other providers.
        /// </summary>
        public virtual IResourceProvider MasterProvider => EditorProvider;

        [Header("Resources Management")]
        [Tooltip("Dictates when the resources are loaded and unloaded during script execution:" +
                 "\n • Conservative — The default mode with balanced memory and CPU utilization. All the resources required for script execution are preloaded when starting the playback and unloaded when the script has finished playing. Scripts referenced in [@gosub] commands are preloaded as well. Additional scripts can be preloaded by using `hold` parameter of [@goto] command." +
                 "\n • Optimistic — All the resources required by the played script, as well all resources of all the scripts specified in [@goto] and [@gosub] commands are preloaded and not unloaded unless `release` parameter is specified in [@goto] command. This minimizes loading screens and allows smooth rollback, but requires manually specifying when the resources have to be unloaded, increasing risk of out of memory exceptions." +
                 "\n • Lazy — Minimal memory usage at the expense of high CPU utilization during playback. Only the resources required for the next `Lazy Policy Steps` commands are preloaded and kept in memory, while other resources are unloaded immediately. This mode is not recommended, unless targeting platforms with strict memory limitations and it's impossible to properly organize naninovel scripts. Expect unstable FPS and hiccups during playback caused by resources being un-/loaded in the background.")]
        public ResourcePolicy ResourcePolicy = ResourcePolicy.Conservative;
        [FormerlySerializedAs("DynamicPolicySteps"), Tooltip("When lazy resource policy is selected, defines the number of script commands to pre-load.")]
        public int LazyPolicySteps = 25;
        [Tooltip("When lazy resource policy is enabled, this will set Unity's background loading thread priority to low to minimize hiccups when loading resources during script playback.")]
        public bool OptimizeLoadingPriority = true;
        [Tooltip("Whether to log resource un-/loading operations.")]
        public bool LogResourceLoading;

        [Header("Build Processing")]
        [Tooltip("Whether to register a custom build player handle to process the assets assigned as Naninovel resources.\n\nWarning: In order for this setting to take effect, it's required to restart the Unity editor.")]
        public bool EnableBuildProcessing = true;
        [Tooltip("When the Addressable Asset System is installed, enabling this property will optimize asset processing step improving the build time.")]
        public bool UseAddressables = true;
        [Tooltip("Whether to automatically build the addressable asset bundles when building the player. Has no effect when `Use Addressables` is disabled.")]
        public bool AutoBuildBundles = true;

        [Header("Addressable Provider")]
        [Tooltip("Whether to use addressable provider in editor. Enable if you're manually exposing resources via addressable address instead of assigning them with Naninovel's resource managers. Be aware, that enabling this could cause issues when resources are assigned both in resources manager and registered with an addressable address and then renamed or duplicated.")]
        public bool AllowAddressableInEditor;
        [Tooltip("Whether to create an addressable group per Naninovel resource category: scripts, characters, audio, etc. When disabled, will use a single `Naninovel` group for all the resources.")]
        public bool GroupByCategory;
        [Tooltip("Addressable provider will only work with assets, that have the assigned labels in addition to `Naninovel` label. Can be used to filter assets used by the engine based on custom criteria (eg, HD vs SD textures).")]
        public string[] ExtraLabels;

        [Header("Local Provider")]
        [Tooltip("Path root to use for the local resource provider. Can be an absolute path to the folder where the resources are located, or a relative path with one of the available origins:" +
                 "\n • %DATA% — Game data folder on the target device (UnityEngine.Application.dataPath)." +
                 "\n • %PDATA% — Persistent data directory on the target device (UnityEngine.Application.persistentDataPath)." +
                 "\n • %STREAM% — `StreamingAssets` folder (UnityEngine.Application.streamingAssetsPath)." +
                 "\n • %SPECIAL{F}% — An OS special folder (where F is value from System.Environment.SpecialFolder).")]
        public string LocalRootPath = "%DATA%/Resources";
        [Tooltip("When streaming videos under WebGL (movies, video backgrounds), specify the extension of the video files.")]
        public string VideoStreamExtension = ".mp4";

        [Header("Project Provider")]
        [Tooltip("Path relative to `Resources` folders, under which the naninovel-specific assets are located.")]
        public string ProjectRootPath = "Naninovel";

        #if UNITY_GOOGLE_DRIVE_AVAILABLE
        [Header("Google Drive Provider")]
        [Tooltip("Path root to use for the Google Drive resource provider.")]
        public string GoogleDriveRootPath = "Resources";
        [Tooltip("Maximum allowed concurrent requests when contacting Google Drive API.")]
        public int GoogleDriveRequestLimit = 2;
        [Tooltip("Cache policy to use when downloading resources. `Smart` will attempt to use Changes API to check for the modifications on the drive. `PurgeAllOnInit` will to re-download all the resources when the provider is initialized.")]
        public GoogleDriveResourceProvider.CachingPolicyType GoogleDriveCachingPolicy = GoogleDriveResourceProvider.CachingPolicyType.Smart;
        #endif
    }
}
