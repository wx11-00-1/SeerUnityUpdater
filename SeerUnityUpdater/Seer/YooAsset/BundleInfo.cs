namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class BundleInfo
    {
        public string RemoteMainURL { get; private set; }

        public string RemoteFallbackURL { get; private set; }

        public string DeliveryFilePath { get; private set; }

        public ulong DeliveryFileOffset { get; private set; }

        private BundleInfo()
        {
        }

        public BundleInfo(PackageBundle bundle, BundleInfo.ELoadMode loadMode, string mainURL, string fallbackURL)
        {
            this.Bundle = bundle;
            this.LoadMode = loadMode;
            this.RemoteMainURL = mainURL;
            this.RemoteFallbackURL = fallbackURL;
            this.DeliveryFilePath = string.Empty;
            this.DeliveryFileOffset = 0UL;
        }

        public BundleInfo(PackageBundle bundle, BundleInfo.ELoadMode loadMode, string deliveryFilePath, ulong deliveryFileOffset)
        {
            this.Bundle = bundle;
            this.LoadMode = loadMode;
            this.RemoteMainURL = string.Empty;
            this.RemoteFallbackURL = string.Empty;
            this.DeliveryFilePath = deliveryFilePath;
            this.DeliveryFileOffset = deliveryFileOffset;
        }

        public BundleInfo(PackageBundle bundle, BundleInfo.ELoadMode loadMode)
        {
            this.Bundle = bundle;
            this.LoadMode = loadMode;
            this.RemoteMainURL = string.Empty;
            this.RemoteFallbackURL = string.Empty;
            this.DeliveryFilePath = string.Empty;
            this.DeliveryFileOffset = 0UL;
        }

        public static bool IsBuildinJarFile(string streamingPath)
        {
            return streamingPath.StartsWith("jar:");
        }

        public readonly PackageBundle Bundle;

        public readonly BundleInfo.ELoadMode LoadMode;

        public string[] IncludeAssets;

        public enum ELoadMode
        {
            None,
            LoadFromDelivery,
            LoadFromStreaming,
            LoadFromCache,
            LoadFromRemote,
            LoadFromEditor
        }
    }
}
