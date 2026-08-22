namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class PathUtility
    {
        public static string RegularPath(string path)
        {
            return path.Replace('\\', '/').Replace("\\", "/");
        }

        public static string RemoveExtension(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            int num = str.LastIndexOf(".");
            if (num == -1)
            {
                return str;
            }
            return str.Remove(num);
        }

        public static string Combine(string path1, string path2)
        {
            return StringUtility.Format("{0}/{1}", path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            return StringUtility.Format("{0}/{1}/{2}", path1, path2, path3);
        }

        public static string Combine(string path1, string path2, string path3, string path4)
        {
            return StringUtility.Format("{0}/{1}/{2}/{3}", new object[]
            {
                path1,
                path2,
                path3,
                path4
            });
        }
    }
}
