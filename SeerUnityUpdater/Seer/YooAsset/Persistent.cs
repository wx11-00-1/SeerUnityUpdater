namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class Persistent
    {
        public string BuildinRoot { get; private set; }

        public string BuildinPackageRoot { get; private set; }

        public string SandboxRoot { get; private set; }

        public string SandboxPackageRoot { get; private set; }

        public string SandboxCacheBundleFilesRoot { get; private set; }

        public string SandboxCacheRawFilesRoot { get; private set; }

        public string SandboxManifestFilesRoot { get; private set; }

        public string SandboxAppFootPrintFilePath { get; private set; }

        public Persistent(string packageName)
        {
            this._packageName = packageName;
        }

        public void OverwriteRootDirectory(string buildinRoot, string sandboxRoot)
        {
            if (string.IsNullOrEmpty(buildinRoot))
            {
                this.BuildinRoot = Persistent.CreateDefaultBuildinRoot();
            }
            else
            {
                this.BuildinRoot = buildinRoot;
            }
            if (string.IsNullOrEmpty(sandboxRoot))
            {
                this.SandboxRoot = Persistent.CreateDefaultSandboxRoot();
            }
            else
            {
                this.SandboxRoot = sandboxRoot;
            }
            this.BuildinPackageRoot = PathUtility.Combine(this.BuildinRoot, this._packageName);
            this.SandboxPackageRoot = PathUtility.Combine(this.SandboxRoot, this._packageName);
            this.SandboxCacheBundleFilesRoot = PathUtility.Combine(this.SandboxPackageRoot, "CacheBundleFiles");
            this.SandboxCacheRawFilesRoot = PathUtility.Combine(this.SandboxPackageRoot, "CacheRawFiles");
            this.SandboxManifestFilesRoot = PathUtility.Combine(this.SandboxPackageRoot, "ManifestFiles");
            this.SandboxAppFootPrintFilePath = PathUtility.Combine(this.SandboxPackageRoot, "ApplicationFootPrint.bytes");
        }

        private static string CreateDefaultBuildinRoot()
        {
            //return PathUtility.Combine(Application.streamingAssetsPath, "yoo");
            return "yoo";
        }

        private static string CreateDefaultSandboxRoot()
        {
            //return PathUtility.Combine(Application.dataPath, "yoo");
            return "yoo";
        }

        public void DeleteSandboxPackageFolder()
        {
            if (Directory.Exists(this.SandboxPackageRoot))
            {
                Directory.Delete(this.SandboxPackageRoot, true);
            }
        }

        public void DeleteSandboxCacheFilesFolder()
        {
            if (Directory.Exists(this.SandboxCacheBundleFilesRoot))
            {
                Directory.Delete(this.SandboxCacheBundleFilesRoot, true);
            }
            if (Directory.Exists(this.SandboxCacheRawFilesRoot))
            {
                Directory.Delete(this.SandboxCacheRawFilesRoot, true);
            }
        }

        public void DeleteSandboxManifestFilesFolder()
        {
            if (Directory.Exists(this.SandboxManifestFilesRoot))
            {
                Directory.Delete(this.SandboxManifestFilesRoot, true);
            }
        }

        public string GetSandboxPackageManifestFilePath(string packageVersion)
        {
            string manifestBinaryFileName = YooAssetSettingsData.GetManifestBinaryFileName(this._packageName, packageVersion);
            return PathUtility.Combine(this.SandboxManifestFilesRoot, manifestBinaryFileName);
        }

        public string GetSandboxPackageHashFilePath(string packageVersion)
        {
            string packageHashFileName = YooAssetSettingsData.GetPackageHashFileName(this._packageName, packageVersion);
            return PathUtility.Combine(this.SandboxManifestFilesRoot, packageHashFileName);
        }

        public string GetSandboxPackageVersionFilePath()
        {
            string packageVersionFileName = YooAssetSettingsData.GetPackageVersionFileName(this._packageName);
            return PathUtility.Combine(this.SandboxManifestFilesRoot, packageVersionFileName);
        }

        public void SaveSandboxPackageVersionFile(string version)
        {
            FileUtility.WriteAllText(this.GetSandboxPackageVersionFilePath(), version);
        }

        public string GetBuildinPackageManifestFilePath(string packageVersion)
        {
            string manifestBinaryFileName = YooAssetSettingsData.GetManifestBinaryFileName(this._packageName, packageVersion);
            return PathUtility.Combine(this.BuildinPackageRoot, manifestBinaryFileName);
        }

        public string GetBuildinPackageHashFilePath(string packageVersion)
        {
            string packageHashFileName = YooAssetSettingsData.GetPackageHashFileName(this._packageName, packageVersion);
            return PathUtility.Combine(this.BuildinPackageRoot, packageHashFileName);
        }

        public string GetBuildinPackageVersionFilePath()
        {
            string packageVersionFileName = YooAssetSettingsData.GetPackageVersionFileName(this._packageName);
            return PathUtility.Combine(this.BuildinPackageRoot, packageVersionFileName);
        }

        private readonly string _packageName;
    }
}
