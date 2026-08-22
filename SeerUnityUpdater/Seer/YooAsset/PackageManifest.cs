using System.Diagnostics;

namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class PackageManifest
    {
        public string TryMappingToAssetPath(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return string.Empty;
            }
            if (this.LocationToLower)
            {
                location = location.ToLower();
            }
            string result;
            if (this.AssetPathMapping1.TryGetValue(location, out result))
            {
                return result;
            }
            return string.Empty;
        }

        public PackageBundle GetMainPackageBundle(string assetPath)
        {
            PackageAsset packageAsset;
            if (!this.AssetDic.TryGetValue(assetPath, out packageAsset))
            {
                throw new Exception("Should never get here !");
            }
            int bundleID = packageAsset.BundleID;
            if (bundleID >= 0 && bundleID < this.BundleList.Count)
            {
                return this.BundleList[bundleID];
            }
            throw new Exception(string.Format("Invalid bundle id : {0} Asset path : {1}", bundleID, assetPath));
        }

        public PackageBundle[] GetAllDependencies(string assetPath)
        {
            PackageAsset packageAsset;
            if (this.AssetDic.TryGetValue(assetPath, out packageAsset))
            {
                List<PackageBundle> list = new List<PackageBundle>(packageAsset.DependIDs.Length);
                foreach (int num in packageAsset.DependIDs)
                {
                    if (num < 0 || num >= this.BundleList.Count)
                    {
                        throw new Exception(string.Format("Invalid bundle id : {0} Asset path : {1}", num, assetPath));
                    }
                    PackageBundle item = this.BundleList[num];
                    list.Add(item);
                }
                return list.ToArray();
            }
            throw new Exception("Should never get here !");
        }

        public string GetBundleName(int bundleID)
        {
            if (bundleID >= 0 && bundleID < this.BundleList.Count)
            {
                return this.BundleList[bundleID].BundleName;
            }
            throw new Exception(string.Format("Invalid bundle id : {0}", bundleID));
        }

        public bool TryGetPackageAsset(string assetPath, out PackageAsset result)
        {
            return this.AssetDic.TryGetValue(assetPath, out result);
        }

        public bool TryGetPackageBundle(string bundleName, out PackageBundle result)
        {
            return this.BundleDic.TryGetValue(bundleName, out result);
        }

        public bool IsIncludeBundleFile(string cacheGUID)
        {
            return this.CacheGUIDs.Contains(cacheGUID);
        }

        public AssetInfo[] GetAssetsInfoByTags(string[] tags)
        {
            List<AssetInfo> list = new List<AssetInfo>(100);
            foreach (PackageAsset packageAsset in this.AssetList)
            {
                if (packageAsset.HasTag(tags))
                {
                    AssetInfo item = new AssetInfo(packageAsset);
                    list.Add(item);
                }
            }
            return list.ToArray();
        }

        public AssetInfo ConvertLocationToAssetInfo(string location, Type assetType)
        {
            string assetPath = this.ConvertLocationToAssetInfoMapping(location);
            PackageAsset packageAsset;
            if (this.TryGetPackageAsset(assetPath, out packageAsset))
            {
                return new AssetInfo(packageAsset, assetType);
            }
            string error;
            if (string.IsNullOrEmpty(location))
            {
                error = "The location is null or empty !";
            }
            else
            {
                error = "The location is invalid : " + location;
            }
            return new AssetInfo(error);
        }

        private string ConvertLocationToAssetInfoMapping(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                YooLogger.Error("Failed to mapping location to asset path, The location is null or empty.");
                return string.Empty;
            }
            if (this.LocationToLower)
            {
                location = location.ToLower();
            }
            string result;
            if (this.AssetPathMapping1.TryGetValue(location, out result))
            {
                return result;
            }
            YooLogger.Warning("Failed to mapping location to asset path : " + location);
            return string.Empty;
        }

        public AssetInfo ConvertAssetGUIDToAssetInfo(string assetGUID, Type assetType)
        {
            if (!this.IncludeAssetGUID)
            {
                YooLogger.Warning("Package manifest not include asset guid ! Please check asset bundle collector settings.");
                return new AssetInfo("AssetGUID data is empty !");
            }
            string assetPath = this.ConvertAssetGUIDToAssetInfoMapping(assetGUID);
            PackageAsset packageAsset;
            if (this.TryGetPackageAsset(assetPath, out packageAsset))
            {
                return new AssetInfo(packageAsset, assetType);
            }
            string error;
            if (string.IsNullOrEmpty(assetGUID))
            {
                error = "The assetGUID is null or empty !";
            }
            else
            {
                error = "The assetGUID is invalid : " + assetGUID;
            }
            return new AssetInfo(error);
        }

        private string ConvertAssetGUIDToAssetInfoMapping(string assetGUID)
        {
            if (string.IsNullOrEmpty(assetGUID))
            {
                YooLogger.Error("Failed to mapping assetGUID to asset path, The assetGUID is null or empty.");
                return string.Empty;
            }
            string result;
            if (this.AssetPathMapping2.TryGetValue(assetGUID, out result))
            {
                return result;
            }
            YooLogger.Warning("Failed to mapping assetGUID to asset path : " + assetGUID);
            return string.Empty;
        }

        public string[] GetBundleIncludeAssets(string assetPath)
        {
            List<string> list = new List<string>();
            PackageAsset packageAsset;
            List<string> list2;
            if (this.TryGetPackageAsset(assetPath, out packageAsset) && this.AssetBundleIdDic.TryGetValue(packageAsset.BundleID, out list2))
            {
                list = list2;
            }
            return list.ToArray();
        }

        [Conditional("DEBUG")]
        private void DebugCheckLocation(string location)
        {
            if (!string.IsNullOrEmpty(location))
            {
                int num = location.LastIndexOf(" ");
                if (num != -1 && location.Length == num + 1)
                {
                    YooLogger.Warning("Found blank character in location : \"" + location + "\"");
                }
                if (location.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    YooLogger.Warning("Found illegal character in location : \"" + location + "\"");
                }
            }
        }

        public string FileVersion;

        public bool EnableAddressable;

        public bool LocationToLower;

        public bool IncludeAssetGUID;

        public int OutputNameStyle;

        public string PackageName;

        public string PackageVersion;

        public List<PackageAsset> AssetList = new List<PackageAsset>();

        public Dictionary<int, List<string>> AssetBundleIdDic;

        public List<PackageBundle> BundleList = new List<PackageBundle>();

        [NonSerialized]
        public Dictionary<string, PackageBundle> BundleDic;

        [NonSerialized]
        public Dictionary<string, PackageAsset> AssetDic;

        [NonSerialized]
        public Dictionary<string, string> AssetPathMapping1;

        [NonSerialized]
        public Dictionary<string, string> AssetPathMapping2;

        [NonSerialized]
        public HashSet<string> CacheGUIDs = new HashSet<string>();
    }
}
