namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class ManifestTools
    {
        public static string GetRemoteBundleFileExtension(string bundleName)
        {
            return Path.GetExtension(bundleName);
        }

        public static string GetRemoteBundleFileName(int nameStyle, string bundleName, string fileExtension, string fileHash)
        {
            if (nameStyle == 1)
            {
                return fileHash;
            }
            if (nameStyle == 4)
            {
                string arg = bundleName.Remove(bundleName.LastIndexOf('.'));
                return StringUtility.Format("{0}_{1}", arg, fileHash);
            }
            throw new NotImplementedException(string.Format("Invalid name style : {0}", nameStyle));
        }

        public static BundleInfo ConvertToUnpackInfo(PackageBundle packageBundle)
        {
            string text = PersistentTools.ConvertToWWWPath(packageBundle.StreamingFilePath);
            return new BundleInfo(packageBundle, BundleInfo.ELoadMode.LoadFromStreaming, text, text);
        }

        public static List<BundleInfo> ConvertToUnpackInfos(List<PackageBundle> unpackList)
        {
            List<BundleInfo> list = new List<BundleInfo>(unpackList.Count);
            foreach (PackageBundle packageBundle in unpackList)
            {
                BundleInfo item = ManifestTools.ConvertToUnpackInfo(packageBundle);
                list.Add(item);
            }
            return list;
        }
    }
}
