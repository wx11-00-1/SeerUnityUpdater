using System.Text;

namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class FileUtility
    {
        public static string ReadAllText(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        public static byte[] ReadAllBytes(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            return File.ReadAllBytes(filePath);
        }

        public static void WriteAllText(string filePath, string content)
        {
            FileUtility.CreateFileDirectory(filePath);
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            File.WriteAllBytes(filePath, bytes);
        }

        public static void WriteAllBytes(string filePath, byte[] data)
        {
            FileUtility.CreateFileDirectory(filePath);
            File.WriteAllBytes(filePath, data);
        }

        public static void CreateFileDirectory(string filePath)
        {
            FileUtility.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        public static void CreateDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static long GetFileSize(string filePath)
        {
            return new FileInfo(filePath).Length;
        }
    }
}
