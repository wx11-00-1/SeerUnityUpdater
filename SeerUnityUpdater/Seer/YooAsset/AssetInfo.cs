namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class AssetInfo
    {
        public Type AssetType { get; private set; }

        public string Error { get; private set; }

        internal string GUID
        {
            get
            {
                if (!string.IsNullOrEmpty(this._providerGUID))
                {
                    return this._providerGUID;
                }
                if (this.AssetType == null)
                {
                    this._providerGUID = this.AssetPath + "[null]";
                }
                else
                {
                    this._providerGUID = this.AssetPath + "[" + this.AssetType.Name + "]";
                }
                return this._providerGUID;
            }
        }

        internal bool IsInvalid
        {
            get
            {
                return this._packageAsset == null;
            }
        }

        public string Address
        {
            get
            {
                if (this._packageAsset == null)
                {
                    return string.Empty;
                }
                return "";
            }
        }

        public string AssetPath
        {
            get
            {
                if (this._packageAsset == null)
                {
                    return string.Empty;
                }
                return this._packageAsset.AssetPath;
            }
        }

        private AssetInfo()
        {
        }

        internal AssetInfo(PackageAsset packageAsset, Type assetType)
        {
            if (packageAsset == null)
            {
                throw new Exception("Should never get here !");
            }
            this._providerGUID = string.Empty;
            this._packageAsset = packageAsset;
            this.AssetType = assetType;
            this.Error = string.Empty;
        }

        internal AssetInfo(PackageAsset packageAsset)
        {
            if (packageAsset == null)
            {
                throw new Exception("Should never get here !");
            }
            this._providerGUID = string.Empty;
            this._packageAsset = packageAsset;
            this.AssetType = null;
            this.Error = string.Empty;
        }

        internal AssetInfo(string error)
        {
            this._providerGUID = string.Empty;
            this._packageAsset = null;
            this.AssetType = null;
            this.Error = error;
        }

        private readonly PackageAsset _packageAsset;

        // Token: 0x040001C5 RID: 453
        private string _providerGUID;
    }
}
