namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class PackageBundle
    {
        public string PackageName { get; private set; }

        public string CacheGUID
        {
            get
            {
                return this.FileHash;
            }
        }

        public string CachedDataFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(this._cachedDataFilePath))
                {
                    return this._cachedDataFilePath;
                }
                string path = this.FileHash.Substring(0, 2);
                if (this.IsRawFile)
                {
                    string sandboxCacheRawFilesRoot = PersistentTools.GetPersistent(this.PackageName).SandboxCacheRawFilesRoot;
                    this._cachedDataFilePath = PathUtility.Combine(sandboxCacheRawFilesRoot, path, this.CacheGUID, "__data");
                    this._cachedDataFilePath += this._fileExtension;
                }
                else
                {
                    string sandboxCacheBundleFilesRoot = PersistentTools.GetPersistent(this.PackageName).SandboxCacheBundleFilesRoot;
                    this._cachedDataFilePath = PathUtility.Combine(sandboxCacheBundleFilesRoot, path, this.CacheGUID, "__data");
                }
                return this._cachedDataFilePath;
            }
        }

        public string CachedInfoFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(this._cachedInfoFilePath))
                {
                    return this._cachedInfoFilePath;
                }
                string path = this.FileHash.Substring(0, 2);
                if (this.IsRawFile)
                {
                    string sandboxCacheRawFilesRoot = PersistentTools.GetPersistent(this.PackageName).SandboxCacheRawFilesRoot;
                    this._cachedInfoFilePath = PathUtility.Combine(sandboxCacheRawFilesRoot, path, this.CacheGUID, "__info");
                }
                else
                {
                    string sandboxCacheBundleFilesRoot = PersistentTools.GetPersistent(this.PackageName).SandboxCacheBundleFilesRoot;
                    this._cachedInfoFilePath = PathUtility.Combine(sandboxCacheBundleFilesRoot, path, this.CacheGUID, "__info");
                }
                return this._cachedInfoFilePath;
            }
        }

        public string TempDataFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(this._tempDataFilePath))
                {
                    return this._tempDataFilePath;
                }
                this._tempDataFilePath = this.CachedDataFilePath + ".temp";
                return this._tempDataFilePath;
            }
        }

        public string StreamingFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(this._streamingFilePath))
                {
                    return this._streamingFilePath;
                }
                string buildinPackageRoot = PersistentTools.GetPersistent(this.PackageName).BuildinPackageRoot;
                this._streamingFilePath = PathUtility.Combine(buildinPackageRoot, this.FileName);
                return this._streamingFilePath;
            }
        }

        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(this._fileName))
                {
                    throw new Exception("Should never get here !");
                }
                return this._fileName;
            }
        }

        public string FileExtension
        {
            get
            {
                //if (string.IsNullOrEmpty(this._fileExtension))
                //{
                //    throw new Exception("Should never get here !");
                //}
                return this._fileExtension;
            }
        }

        public void ParseBundle(string packageName, int nameStype)
        {
            this.PackageName = packageName;
            this._fileName = ManifestTools.GetRemoteBundleFileName(nameStype, this.BundleName, string.Empty, this.FileHash);
        }

        public bool HasTag(string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }
            if (this.Tags == null || this.Tags.Length == 0)
            {
                return false;
            }
            foreach (string value in tags)
            {
                if (this.Tags.Contains(value))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasAnyTags()
        {
            return this.Tags != null && this.Tags.Length != 0;
        }

        public bool Equals(PackageBundle otherBundle)
        {
            return this.FileHash == otherBundle.FileHash;
        }

        public string BundleName;

        public uint UnityCRC;

        public string FileHash;

        public string FileCRC;

        public long FileSize;

        public bool IsRawFile;

        public byte LoadMethod;

        public string[] Tags;

        public int[] ReferenceIDs;

        private string _cachedDataFilePath;

        private string _cachedInfoFilePath;

        private string _tempDataFilePath;

        private string _streamingFilePath;

        private string _fileName;

        private string _fileExtension;
    }
}
