namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class PackageAsset
    {
        public bool HasTag(string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }
            if (this.AssetTags == null || this.AssetTags.Length == 0)
            {
                return false;
            }
            foreach (string value in tags)
            {
                if (this.AssetTags.Contains(value))
                {
                    return true;
                }
            }
            return false;
        }

        public string AssetPath;

        public string[] AssetTags;

        public int BundleID;

        public int[] DependIDs;
    }
}
