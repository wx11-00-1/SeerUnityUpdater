using System.Text;

namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class StringUtility
    {
        public static string Format(string format, object arg0)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException();
            }
            StringUtility._cacheBuilder.Length = 0;
            StringUtility._cacheBuilder.AppendFormat(format, arg0);
            return StringUtility._cacheBuilder.ToString();
        }

        public static string Format(string format, object arg0, object arg1)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException();
            }
            StringUtility._cacheBuilder.Length = 0;
            StringUtility._cacheBuilder.AppendFormat(format, arg0, arg1);
            return StringUtility._cacheBuilder.ToString();
        }

        public static string Format(string format, object arg0, object arg1, object arg2)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException();
            }
            StringUtility._cacheBuilder.Length = 0;
            StringUtility._cacheBuilder.AppendFormat(format, arg0, arg1, arg2);
            return StringUtility._cacheBuilder.ToString();
        }

        public static string Format(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException();
            }
            if (args == null)
            {
                throw new ArgumentNullException();
            }
            StringUtility._cacheBuilder.Length = 0;
            StringUtility._cacheBuilder.AppendFormat(format, args);
            return StringUtility._cacheBuilder.ToString();
        }

        [ThreadStatic]
        private static StringBuilder _cacheBuilder = new StringBuilder(2048);
    }
}
