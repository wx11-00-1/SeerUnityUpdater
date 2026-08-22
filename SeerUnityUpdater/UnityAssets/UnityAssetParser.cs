using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace SeerUnityUpdater.UnityAssets;

/// <summary>
/// 扫描文件夹并解析其中所有 Unity 资源（.assets / .bundle / .unity3d 等以及无扩展名的序列化文件）。
/// </summary>
internal sealed class UnityAssetParser : IDisposable
{
    private const int AssetBundleClassId = 142;

    public AssetsManager Manager { get; } = new();

    private readonly bool _hasClassDb;
    private string? _loadedVersion;

    public UnityAssetParser(string? tpkPath)
    {
        if (!string.IsNullOrEmpty(tpkPath) && File.Exists(tpkPath))
        {
            Manager.LoadClassPackage(tpkPath);
            _hasClassDb = true;
        }
    }

    /// <summary>解析文件夹内所有资源，返回资源条目列表。</summary>
    public IReadOnlyList<AssetEntry> ParseFolder(string folderPath, Action<string, int>? progress = null)
    {
        var entries = new List<AssetEntry>();
        foreach (var file in SafeEnumerateFiles(folderPath))
        {
            try
            {
                int before = entries.Count;
                ParseFile(file, folderPath, entries);
                progress?.Invoke(file, entries.Count - before);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[跳过] {Path.GetRelativePath(folderPath, file)}: {ex.Message}");
            }
        }
        return entries;
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root)
    {
        // 已知的非资源数据文件扩展名，跳过以避免无谓探测。
        var skipExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".ress", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".gif",
            ".wav", ".mp3", ".ogg", ".aac", ".flac",
            ".ttf", ".otf", ".fon",
            ".xml", ".json", ".txt", ".csv", ".log", ".cs", ".meta",
            ".exe", ".dll", ".so", ".dylib", ".pdb", ".cache",
            ".zip", ".gz", ".7z", ".rar",
            ".mp4", ".avi", ".mov", ".webm",
        };

        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[]? subDirs = null;
            string[]? files = null;
            try { subDirs = Directory.GetDirectories(dir); } catch { }
            try { files = Directory.GetFiles(dir); } catch { }
            if (subDirs != null)
                foreach (var sd in subDirs) stack.Push(sd);
            if (files == null) continue;

            foreach (var f in files)
            {
                var ext = Path.GetExtension(f);
                if (skipExts.Contains(ext)) continue;
                if (f.EndsWith(".resS", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    if (new FileInfo(f).Length == 0) continue;
                }
                catch { continue; }
                yield return f;
            }
        }
    }

    private void ParseFile(string file, string rootFolder, List<AssetEntry> entries)
    {
        var relPath = Path.GetRelativePath(rootFolder, file).Replace('\\', '/');

        if (IsBundleFile(file))
        {
            var bunInst = Manager.LoadBundleFile(file, unpackIfPacked: true);
            var fileNames = bunInst.file.GetAllFileNames();
            for (int i = 0; i < fileNames.Count; i++)
            {
                if (!bunInst.file.IsAssetsFile(i)) continue; // 跳过 .resS 等非序列化条目
                AssetsFileInstance afInst;
                try { afInst = Manager.LoadAssetsFileFromBundle(bunInst, i, loadDeps: false); }
                catch { continue; }
                LoadClassDbFor(afInst);
                var containerMap = BuildContainerMap(afInst);
                AddEntries(relPath, afInst, containerMap, entries);
            }
        }
        else
        {
            bool isAssets;
            try { isAssets = AssetsFile.IsAssetsFile(file); }
            catch { isAssets = false; }
            if (!isAssets) return;

            var afInst = Manager.LoadAssetsFile(file, loadDeps: false);
            LoadClassDbFor(afInst);
            var containerMap = BuildContainerMap(afInst);
            AddEntries(relPath, afInst, containerMap, entries);
        }
    }

    private void LoadClassDbFor(AssetsFileInstance afInst)
    {
        if (!_hasClassDb) return;
        var ver = afInst.file.Metadata.UnityVersion;
        if (ver == _loadedVersion) return;
        try
        {
            Manager.LoadClassDatabaseFromPackage(ver);
            _loadedVersion = ver;
        }
        catch { /* 该版本不在 tpk 中，回退到内嵌 TypeTree */ }
    }

    /// <summary>读取 AssetBundle 对象的 m_Container，构建 PathID → 资源路径 的映射。</summary>
    private Dictionary<long, string> BuildContainerMap(AssetsFileInstance afInst)
    {
        var map = new Dictionary<long, string>();
        if (!_hasClassDb) return map;

        IList<AssetFileInfo> abInfos;
        try { abInfos = afInst.file.GetAssetsOfType(AssetBundleClassId); }
        catch { return map; }

        foreach (var info in abInfos)
        {
            AssetTypeValueField? baseField;
            try { baseField = Manager.GetBaseField(afInst, info, AssetReadFlags.None); }
            catch { continue; }
            if (baseField == null) continue;

            var container = baseField.Get("m_Container");
            if (container == null || container.IsDummy) continue;
            var array = container.Get("Array");
            if (array?.Children == null) continue;

            foreach (var elem in array.Children)
            {
                var first = elem.Get("first");
                var second = elem.Get("second");
                if (first == null || second == null) continue;

                string? pathStr = first.IsDummy ? null : first.AsString;
                if (string.IsNullOrEmpty(pathStr)) continue;

                // 结构: second(AssetInfo) → asset(PPtr<Object>) → m_PathID
                var assetField = second.Get("asset");
                var pathIdField = assetField?.Get("m_PathID");
                if (pathIdField == null || pathIdField.IsDummy) continue;

                long pathId = pathIdField.AsLong;
                if (!map.ContainsKey(pathId))
                    map[pathId] = pathStr!;
            }
        }
        return map;
    }

    private void AddEntries(string relPath, AssetsFileInstance afInst,
        Dictionary<long, string> containerMap, List<AssetEntry> entries)
    {
        foreach (var info in afInst.file.AssetInfos)
        {
            int typeId;
            try { typeId = info.GetTypeId(afInst.file); }
            catch { typeId = info.TypeId; }

            string? name = null;
            if (_hasClassDb)
            {
                try { name = AssetHelper.GetAssetNameFast(afInst.file, Manager.ClassDatabase, info); }
                catch { /* 回退到 null */ }
                if (string.IsNullOrEmpty(name)) name = null;
            }

            string? containerPath = containerMap.TryGetValue(info.PathId, out var cp) ? cp : null;
            var className = ResolveClassName(typeId);

            entries.Add(new AssetEntry(
                relPath, className, typeId, info.PathId, info.ByteSize,
                name, containerPath, Manager, afInst, info));
        }
    }

    /// <summary>优先从类数据库解析类型名，缺失时回退到内置常见类型名表。</summary>
    private string ResolveClassName(int typeId)
    {
        if (_hasClassDb && Manager.ClassDatabase != null)
        {
            try
            {
                var ct = Manager.ClassDatabase.FindAssetClassByID(typeId);
                if (ct != null)
                {
                    var nm = Manager.ClassDatabase.GetString(ct.Name);
                    if (!string.IsNullOrEmpty(nm)) return nm;
                }
            }
            catch { /* 回退到内置表 */ }
        }
        return UnityClassNames.Get(typeId);
    }

    private static bool IsBundleFile(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var buf = new byte[8];
            int read = fs.Read(buf, 0, 8);
            if (read < 7) return false;
            var head = Encoding.ASCII.GetString(buf, 0, read);
            return head.StartsWith("UnityFS", StringComparison.Ordinal)
                || head.StartsWith("UnityRaw", StringComparison.Ordinal)
                || head.StartsWith("UnityWeb", StringComparison.Ordinal);
        }
        catch { return false; }
    }

    public void Dispose()
    {
        Manager.UnloadAll(unloadClassData: true);
    }
}
