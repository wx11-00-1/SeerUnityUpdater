namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class YooAssetSettingsData
    {
        public static YooAssetSettings Setting
        {
            get
            {
                if (YooAssetSettingsData._setting == null)
                {
                    YooAssetSettingsData.LoadSettingData();
                }
                return YooAssetSettingsData._setting;
            }
        }

        private static void LoadSettingData()
        {
            //YooAssetSettingsData._setting = Resources.Load<YooAssetSettings>("YooAssetSettings");
            //if (YooAssetSettingsData._setting == null)
            //{
            //    YooAssetSettingsData._setting = ScriptableObject.CreateInstance<YooAssetSettings>();
            //}
        }

        public static string GetReportFileName(string packageName, string packageVersion)
        {
            return string.Concat(new string[]
            {
                "BuildReport_",
                packageName,
                "_",
                packageVersion,
                ".json"
            });
        }

        public static string GetManifestBinaryFileName(string packageName, string packageVersion)
        {
            return string.Concat(new string[]
            {
                //YooAssetSettingsData.Setting.ManifestFileName,
                YooAssetSettings.ManifestFileName,
                "_",
                packageName,
                "_",
                packageVersion,
                ".bytes"
            });
        }

        public static string GetManifestJsonFileName(string packageName, string packageVersion)
        {
            return string.Concat(new string[]
            {
                //YooAssetSettingsData.Setting.ManifestFileName,
                YooAssetSettings.ManifestFileName,
                "_",
                packageName,
                "_",
                packageVersion,
                ".json"
            });
        }

        public static string GetPackageHashFileName(string packageName, string packageVersion)
        {
            return string.Concat(new string[]
            {
                //YooAssetSettingsData.Setting.ManifestFileName,
                YooAssetSettings.ManifestFileName,
                "_",
                packageName,
                "_",
                packageVersion,
                ".hash"
            });
        }

        public static string GetPackageVersionFileName(string packageName)
        {
            //return YooAssetSettingsData.Setting.ManifestFileName + "_" + packageName + ".version";
            return YooAssetSettings.ManifestFileName + "_" + packageName + ".version";
        }

        private static YooAssetSettings _setting;
    }
}
