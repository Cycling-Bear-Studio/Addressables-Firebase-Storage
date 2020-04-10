namespace RobinBird.FirebaseTools.Storage.Addressables
{
    public class FirebaseAddressablesConstants
    {
        public const string NATIVE_GS_URL_START = "gs://";
        
        /// <summary>
        /// Workaround for GetDownloadSizeAsync method because only ids with http at the beginning are considered
        /// in the calculation
        /// </summary>
        public const string PATCHED_GS_URL_START = "httpgs://";
    }
}