using System.Diagnostics;

namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class YooLogger
    {
        [Conditional("DEBUG")]
        public static void Log(string info)
        {
            //if (YooLogger.Logger != null)
            //{
            //    YooLogger.Logger.Log(info);
            //}
            Console.WriteLine(info);
        }

        public static void Warning(string info)
        {
            //if (YooLogger.Logger != null)
            //{
            //    YooLogger.Logger.Warning(info);
            //}
            Console.WriteLine(info);
        }

        public static void Error(string info)
        {
            //if (YooLogger.Logger != null)
            //{
            //    YooLogger.Logger.Error(info);
            //}
            Console.WriteLine(info);
        }

        public static void Exception(Exception exception)
        {
            //if (YooLogger.Logger != null)
            //{
            //    YooLogger.Logger.Exception(exception);
            //}
            Console.WriteLine(exception);
        }

        //public static ILogger Logger;
    }
}
