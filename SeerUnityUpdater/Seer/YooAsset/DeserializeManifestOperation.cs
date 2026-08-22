using System;
using System.Collections.Generic;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class DeserializeManifestOperation
    {
        private readonly BufferReader _buffer;

        private int _packageAssetCount;

        private int _packageBundleCount;

        private int _progressTotalValue;

        public PackageManifest Manifest { get; private set; }


        public DeserializeManifestOperation(byte[] binaryData)
        {
            this._buffer = new BufferReader(binaryData);
        }

        public void Update(Action<string> log)
        {
            // 1. DeserializeFileHeader
            if (!_buffer.IsValid || _buffer.ReadUInt32() != 5853007U)
            {
                Console.WriteLine("读取清单文件头出错");
                return;
            }
            string text = _buffer.ReadUTF8();
            if (text != "1.5.2")
            {
                Console.WriteLine("清单文件版本错误");
                return;
            }
            this.Manifest = new PackageManifest();
            this.Manifest.FileVersion = text;
            this.Manifest.EnableAddressable = this._buffer.ReadBool();
            this.Manifest.LocationToLower = this._buffer.ReadBool();
            this.Manifest.IncludeAssetGUID = this._buffer.ReadBool();
            this.Manifest.OutputNameStyle = this._buffer.ReadInt32();
            this.Manifest.PackageName = this._buffer.ReadUTF8();
            this.Manifest.PackageVersion = this._buffer.ReadUTF8();
            if (this.Manifest.EnableAddressable && this.Manifest.LocationToLower)
            {
                throw new Exception("Addressable not support location to lower !");
            }

            // 2. PrepareAssetList
            this._packageAssetCount = this._buffer.ReadInt32();
            this.Manifest.AssetList = new List<PackageAsset>(this._packageAssetCount);
            this.Manifest.AssetDic = new Dictionary<string, PackageAsset>(this._packageAssetCount);
            this.Manifest.AssetBundleIdDic = new Dictionary<int, List<string>>(this.Manifest.AssetList.Count);
            if (this.Manifest.EnableAddressable)
            {
                this.Manifest.AssetPathMapping1 = new Dictionary<string, string>(this._packageAssetCount * 3);
            }
            else
            {
                this.Manifest.AssetPathMapping1 = new Dictionary<string, string>(this._packageAssetCount * 2);
            }
            if (this.Manifest.IncludeAssetGUID)
            {
                this.Manifest.AssetPathMapping2 = new Dictionary<string, string>(this._packageAssetCount);
            }
            else
            {
                this.Manifest.AssetPathMapping2 = new Dictionary<string, string>();
            }
            this._progressTotalValue = this._packageAssetCount;

            // 3. DeserializeAssetList
            while (this._packageAssetCount > 0)
            {
                PackageAsset packageAsset = new PackageAsset();
                packageAsset.AssetPath = this._buffer.ReadUTF8();
                packageAsset.BundleID = this._buffer.ReadInt32();
                packageAsset.DependIDs = this._buffer.ReadInt32Array();
                this.Manifest.AssetList.Add(packageAsset);
                string assetPath = packageAsset.AssetPath;
                if (this.Manifest.AssetDic.ContainsKey(assetPath))
                {
                    throw new Exception("AssetPath have existed : " + assetPath);
                }
                this.Manifest.AssetDic.Add(assetPath, packageAsset);
                List<string> list;
                if (this.Manifest.AssetBundleIdDic.TryGetValue(packageAsset.BundleID, out list))
                {
                    list.Add(packageAsset.AssetPath);
                }
                else
                {
                    list = new List<string>
                            {
                                packageAsset.AssetPath
                            };
                    this.Manifest.AssetBundleIdDic.Add(packageAsset.BundleID, list);
                }
                string text2 = packageAsset.AssetPath;
                if (this.Manifest.LocationToLower)
                {
                    text2 = text2.ToLower();
                }
                if (this.Manifest.AssetPathMapping1.ContainsKey(text2))
                {
                    throw new Exception("Location have existed : " + text2);
                }
                this.Manifest.AssetPathMapping1.Add(text2, packageAsset.AssetPath);
                if (Path.HasExtension(text2))
                {
                    string key = PathUtility.RemoveExtension(text2);
                    if (!this.Manifest.AssetPathMapping1.ContainsKey(key))
                    {
                        this.Manifest.AssetPathMapping1.Add(key, packageAsset.AssetPath);
                    }
                }
                this._packageAssetCount--;
                float Progress = 1f - (float)(this._packageAssetCount / this._progressTotalValue);
                log(Progress.ToString());
                //if (OperationSystem.IsBusy)
                //{
                    //break;
                //}
            }
            if (this._packageAssetCount > 0)
            {
                Console.WriteLine("PackageAsset 解析不完整");
                return;
            }

            // 4. PrepareBundleList
            this._packageBundleCount = this._buffer.ReadInt32();
            this.Manifest.BundleList = new List<PackageBundle>(this._packageBundleCount);
            this.Manifest.BundleDic = new Dictionary<string, PackageBundle>(this._packageBundleCount);
            this._progressTotalValue = this._packageBundleCount;

            // 5. DeserializeBundleList
            while (this._packageBundleCount > 0)
            {
                PackageBundle packageBundle = new PackageBundle();
                packageBundle.BundleName = this._buffer.ReadUTF8();
                packageBundle.UnityCRC = this._buffer.ReadUInt32();
                packageBundle.FileHash = this._buffer.ReadUTF8();
                packageBundle.FileCRC = this._buffer.ReadUTF8();
                packageBundle.FileSize = this._buffer.ReadInt64();
                packageBundle.IsRawFile = this._buffer.ReadBool();
                packageBundle.LoadMethod = this._buffer.ReadByte();
                packageBundle.ReferenceIDs = this._buffer.ReadInt32Array();
                this.Manifest.BundleList.Add(packageBundle);
                packageBundle.ParseBundle(this.Manifest.PackageName, this.Manifest.OutputNameStyle);
                this.Manifest.BundleDic.Add(packageBundle.BundleName, packageBundle);
                if (!this.Manifest.CacheGUIDs.Contains(packageBundle.CacheGUID))
                {
                    this.Manifest.CacheGUIDs.Add(packageBundle.CacheGUID);
                }
                this._packageBundleCount--;
                float Progress = 1f - (float)(this._packageAssetCount / this._progressTotalValue);
                log(Progress.ToString());
                //if (OperationSystem.IsBusy)
                //{
                //break;
                //}
            }
            if (this._packageBundleCount > 0)
            {
                Console.WriteLine("PackageBundle 解析不完整");
                return;
            }
        }
    }
}
