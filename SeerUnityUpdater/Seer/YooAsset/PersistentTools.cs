namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class PersistentTools
    {
        public static Persistent GetPersistent(string packageName)
        {
            if (!PersistentTools._persitentDic.ContainsKey(packageName))
            {
                throw new Exception("Should never get here !");
            }
            return PersistentTools._persitentDic[packageName];
        }

        public static Persistent GetOrCreatePersistent(string packageName)
        {
            if (!PersistentTools._persitentDic.ContainsKey(packageName))
            {
                Persistent value = new Persistent(packageName);
                PersistentTools._persitentDic.Add(packageName, value);
            }
            return PersistentTools._persitentDic[packageName];
        }

        public static string ConvertToWWWPath(string path)
        {
            return StringUtility.Format("file:///{0}", path);
        }

        private static readonly Dictionary<string, Persistent> _persitentDic = new Dictionary<string, Persistent>(100);
    }
}
