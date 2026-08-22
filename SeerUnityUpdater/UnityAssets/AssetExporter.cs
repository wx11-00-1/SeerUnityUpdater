using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using System.Text.RegularExpressions;

namespace SeerUnityUpdater.UnityAssets;

internal sealed record ExportResult(int Exported, int Failed, int Skipped);

/// <summary>导出前对目标目录采取的动作。</summary>
internal enum ExportAction
{
    /// <summary>不清空；目标目录中已存在同名文件时跳过该资源（增量/断点续导）。</summary>
    SkipExisting,
    /// <summary>导出前清空目标目录内所有文件与子目录，再导出全部匹配资源。</summary>
    Clear,
}

/// <summary>
/// 一个批量导出任务：用正则匹配资源路径，导出到指定目录，并可指定目录动作。
/// </summary>
internal sealed record ExportJob(Regex Pattern, string OutputDir, ExportAction Action = ExportAction.SkipExisting);

/// <summary>
/// 按正则匹配资源路径并导出：
/// - Texture2D / Sprite → PNG（图片只导出 PNG，不导出 .bin）
/// - TextAsset → .bytes（原始文本/字节数据）
/// - 其他类型 → .bin（原始序列化字节）
/// 所有文件扁平导出到目标目录，不按资源路径创建子文件夹。
/// </summary>
internal sealed class AssetExporter
{
    private const int Texture2DTypeId = 28;
    private const int SpriteTypeId = 213;
    private const int TextAssetTypeId = 49;

    private readonly bool _exportPng;
    private string? _loadedVersion;

    // 贴图解码缓存：同一文件内多个 Sprite 引用同一张 Texture2D 时避免重复解码。
    private AssetsFileInstance? _cacheFileInst;
    private readonly Dictionary<long, (byte[] bgra, int w, int h)> _textureCache = new();

    public AssetExporter(bool exportPng = true) => _exportPng = exportPng;

    /// <summary>
    /// 批量导出：对每个任务用其正则匹配所有资源并导出到对应目录。
    /// 同一资源若被多个任务匹配，会被导出多次到不同目录。
    /// </summary>
    public (ExportResult Total, IReadOnlyList<ExportResult> PerJob) ExportBatch(
        IEnumerable<AssetEntry> entries, IReadOnlyList<ExportJob> jobs)
    {
        if (jobs.Count == 0)
            return (new ExportResult(0, 0, 0), Array.Empty<ExportResult>());

        foreach (var job in jobs)
            Directory.CreateDirectory(job.OutputDir);

        var entryList = entries as IReadOnlyList<AssetEntry> ?? entries.ToList();
        var perJob = new ExportResult[jobs.Count];
        int totalExported = 0, totalFailed = 0, totalSkipped = 0;

        for (int i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            var actionName = job.Action == ExportAction.Clear ? "清空后导出" : "跳过同名";
            Console.Error.WriteLine($"[任务 {i + 1}/{jobs.Count}] 正则=\"{job.Pattern}\" 动作={actionName} -> {job.OutputDir}");

            // 准备目录状态：Clear 清空目录且快照为空；SkipExisting 快照已有文件名用于跳过判断。
            var preExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (job.Action == ExportAction.Clear)
            {
                int deleted = ClearDirectory(job.OutputDir);
                if (deleted > 0)
                    Console.Error.WriteLine($"  已清空 {deleted} 个文件/子目录");
            }
            else
            {
                foreach (var f in Directory.EnumerateFiles(job.OutputDir))
                    preExisting.Add(Path.GetFileName(f));
            }

            int exported = 0, failed = 0, skipped = 0;

            foreach (var e in entryList)
            {
                if (!job.Pattern.IsMatch(e.ContainerPath ?? string.Empty))
                {
                    skipped++;
                    continue;
                }

                EnsureClassDb(e);

                try
                {
                    string? outPath = ExportOne(e, job.OutputDir, job.Action, preExisting);
                    if (outPath == null)
                    {
                        skipped++;
                        //Console.WriteLine($"[SKIP] ({e.ClassName}): 目标目录已存在同名文件");
                        continue;
                    }
                    exported++;
                    //Console.WriteLine($"[OK] ({e.ClassName}) -> {Path.GetRelativePath(job.OutputDir, outPath)}");
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.Error.WriteLine($"[FAIL] ({e.ClassName}): {ex.Message}");
                }
            }

            perJob[i] = new ExportResult(exported, failed, skipped);
            totalExported += exported;
            totalFailed += failed;
            totalSkipped += skipped;
            Console.Error.WriteLine($"[任务 {i + 1} 完成] 匹配 {exported + failed}（成功 {exported}，失败 {failed}），跳过/未匹配 {skipped}");
        }

        return (new ExportResult(totalExported, totalFailed, totalSkipped), perJob);
    }

    private void EnsureClassDb(AssetEntry e)
    {
        if (e.Manager.ClassDatabase == null) return;
        try
        {
            var ver = e.FileInst.file.Metadata.UnityVersion;
            if (ver != _loadedVersion)
            {
                e.Manager.LoadClassDatabaseFromPackage(ver);
                _loadedVersion = ver;
            }
        }
        catch { /* 回退到 TypeTree */ }
    }

    /// <summary>
    /// 导出单个资源。返回写入路径；若因 SkipExisting 动作跳过则返回 null。
    /// </summary>
    private string? ExportOne(AssetEntry e, string outputDir, ExportAction action, HashSet<string> preExisting)
    {
        string ext = DecideExt(e);
        string fileName = SanitizeFileName(BaseName(e)) + ext;

        // SkipExisting：若目标目录（导出前的快照）已存在同名文件，跳过该资源。
        if (action == ExportAction.SkipExisting && preExisting.Contains(fileName))
            return null;

        string fullPath = Path.Combine(outputDir, fileName);
        // 本轮已导出过同名文件，跳过
        if (File.Exists(fullPath))
            return null;

        switch (e.TypeId)
        {
            case Texture2DTypeId when _exportPng:
                if (!TryExportTexturePng(e, fullPath))
                    throw new InvalidOperationException("Texture2D 解码为 PNG 失败");
                return fullPath;
            case SpriteTypeId when _exportPng:
                if (!TryExportSpritePng(e, fullPath))
                    throw new InvalidOperationException("Sprite 导出为 PNG 失败（可能无法解析引用的纹理）");
                return fullPath;
            case TextAssetTypeId:
                ExportTextAssetBytes(e, fullPath);
                return fullPath;
            default:
                ExportRawBytes(e, fullPath);
                return fullPath;
        }
    }

    /// <summary>清空目录内所有文件与子目录（保留目录本身），返回被清除的条目数。</summary>
    public static int ClearDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return 0;
        int count = 0;
        foreach (var f in Directory.EnumerateFiles(dir))
        {
            File.Delete(f);
            count++;
        }
        foreach (var d in Directory.EnumerateDirectories(dir))
        {
            Directory.Delete(d, recursive: true);
            count++;
        }
        return count;
    }

    /// <summary>根据类型决定输出扩展名。</summary>
    private string DecideExt(AssetEntry e)
    {
        if (_exportPng && (e.TypeId == Texture2DTypeId || e.TypeId == SpriteTypeId))
            return ".png";
        if (e.TypeId == TextAssetTypeId)
            return ".bytes";
        return ".bin";
    }

    /// <summary>扁平输出时的文件名（不含扩展名）。</summary>
    private static string BaseName(AssetEntry e)
    {
        if (!string.IsNullOrEmpty(e.Name))
            return e.Name!;
        return "uname";
    }

    // ---------- Texture2D → PNG ----------

    private bool TryExportTexturePng(AssetEntry e, string outputPath)
    {
        var decoded = DecodeTextureEntry(e);
        if (decoded == null) return false;
        var (bgra, w, h) = decoded.Value;
        try
        {
            using var img = Image.LoadPixelData<Bgra32>(bgra, w, h);
            img.Mutate(c => c.Flip(FlipMode.Vertical));
            img.SaveAsPng(outputPath);
            return true;
        }
        catch { return false; }
    }

    /// <summary>解码一个 Texture2D 资源为 BGRA 原始像素（未翻转，Unity 底朝向）。</summary>
    private (byte[] bgra, int w, int h)? DecodeTextureEntry(AssetEntry e)
    {
        AssetTypeValueField? baseField;
        try { baseField = e.Manager.GetBaseField(e.FileInst, e.Info, AssetReadFlags.None); }
        catch { return null; }
        if (baseField == null) return null;
        return DecodeTextureField(baseField, e.FileInst);
    }

    private (byte[] bgra, int w, int h)? DecodeTextureField(AssetTypeValueField baseField, AssetsFileInstance afInst)
    {
        TextureFile tf;
        try { tf = TextureFile.ReadTextureFile(baseField); }
        catch { return null; }

        byte[]? raw;
        try { raw = tf.FillPictureData(afInst); }
        catch { return null; }
        if (raw == null || raw.Length == 0) return null;

        int w = tf.m_Width, h = tf.m_Height;
        if (w <= 0 || h <= 0) return null;

        byte[]? bgra = null;
        try { bgra = tf.DecodeTextureRaw(raw, useBgra: true); }
        catch { /* 尝试备用方案 */ }

        if (bgra != null && bgra.Length >= w * h * 4)
            return (bgra, w, h);

        // 备用：DecodeTextureImage 输出整图 PNG 字节；这里无法直接拿到 BGRA，故返回 null 让上层失败。
        return null;
    }

    // ---------- Sprite → PNG（从引用的 Texture2D 裁剪 m_Rect 区域） ----------

    private bool TryExportSpritePng(AssetEntry e, string outputPath)
    {
        AssetTypeValueField? baseField;
        try { baseField = e.Manager.GetBaseField(e.FileInst, e.Info, AssetReadFlags.None); }
        catch { return false; }
        if (baseField == null) return false;

        // m_Rect: x, y, width, height（float，Unity 左下角原点）
        var rect = baseField.Get("m_Rect");
        if (rect == null || rect.IsDummy) return false;
        float rx = AsFloat(rect.Get("x"));
        float ry = AsFloat(rect.Get("y"));
        float rw = AsFloat(rect.Get("width"));
        float rh = AsFloat(rect.Get("height"));

        // 纹理 PPtr 位于 m_RD.texture（SpriteRenderData），而非顶层的 m_Texture
        var rd = baseField.Get("m_RD");
        var texPtr = rd?.Get("texture");
        var pathIdField = texPtr?.Get("m_PathID");
        if (pathIdField == null || pathIdField.IsDummy) return false;
        long texPathId = pathIdField.AsLong;
        if (texPathId == 0) return false;

        var texInfo = FindInfoByPathId(e.FileInst, texPathId);
        if (texInfo == null) return false;

        var decoded = DecodeTextureCached(e.Manager, texInfo, e.FileInst);
        if (decoded == null) return false;
        var (bgra, tw, th) = decoded.Value;

        int ix = ClampInt(rx, 0, tw);
        int iy = ClampInt(ry, 0, th);
        int iw = ClampInt(rw, 0, tw - ix);
        int ih = ClampInt(rh, 0, th - iy);
        if (iw <= 0 || ih <= 0) return false;

        try
        {
            using var img = Image.LoadPixelData<Bgra32>(bgra, tw, th);
            img.Mutate(c => c.Flip(FlipMode.Vertical)); // 转为正常上朝向
            // m_Rect 以左下角为原点；翻转后顶部 Y = th - ry - rh
            int cropY = th - iy - ih;
            if (cropY < 0) cropY = 0;
            img.Mutate(c => c.Crop(new Rectangle(ix, cropY, iw, ih)));
            img.SaveAsPng(outputPath);
            return true;
        }
        catch { return false; }
    }

    private (byte[] bgra, int w, int h)? DecodeTextureCached(AssetsManager manager, AssetFileInfo texInfo, AssetsFileInstance afInst)
    {
        if (!ReferenceEquals(_cacheFileInst, afInst))
        {
            _textureCache.Clear();
            _cacheFileInst = afInst;
        }
        if (_textureCache.TryGetValue(texInfo.PathId, out var cached))
            return cached;

        AssetTypeValueField? baseField;
        try { baseField = manager.GetBaseField(afInst, texInfo, AssetReadFlags.None); }
        catch { return null; }
        if (baseField == null) return null;

        var decoded = DecodeTextureField(baseField, afInst);
        if (decoded != null)
            _textureCache[texInfo.PathId] = decoded.Value;
        return decoded;
    }

    private static AssetFileInfo? FindInfoByPathId(AssetsFileInstance afInst, long pathId)
    {
        foreach (var i in afInst.file.AssetInfos)
            if (i.PathId == pathId) return i;
        return null;
    }

    private static float AsFloat(AssetTypeValueField? f)
    {
        if (f == null || f.IsDummy) return 0f;
        try { return f.AsFloat; }
        catch { return 0f; }
    }

    private static int ClampInt(float v, int min, int max)
    {
        int i = (int)Math.Round(v);
        if (i < min) i = min;
        if (i > max) i = max;
        return i;
    }

    // ---------- TextAsset → .bytes ----------

    private static void ExportTextAssetBytes(AssetEntry e, string outputPath)
    {
        AssetTypeValueField? baseField;
        try { baseField = e.Manager.GetBaseField(e.FileInst, e.Info, AssetReadFlags.None); }
        catch { throw; }
        if (baseField == null) throw new InvalidOperationException("无法读取 TextAsset 字段");

        // TextAsset: m_Name (string) + m_Script (string 或 byte 数组)
        var script = baseField.Get("m_Script");
        if (script == null || script.IsDummy)
            throw new InvalidOperationException("TextAsset 缺少 m_Script 字段");

        byte[] data;
        try
        {
            // m_Script 在多数版本中是字符串类型，AsString 返回 UTF-8 文本；
            // AssetsTools.NET 对含二进制内容的 m_Script 也通过字节数组值暴露。
            if (script.Value != null)
                data = script.Value.AsByteArray ?? Encoding.UTF8.GetBytes(script.Value.AsString ?? "");
            else
                data = Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"读取 TextAsset 内容失败: {ex.Message}");
        }

        File.WriteAllBytes(outputPath, data);
    }

    // ---------- 其他类型 → 原始字节 ----------

    private static void ExportRawBytes(AssetEntry e, string outputPath)
    {
        var reader = e.FileInst.file.Reader;
        long pos = e.Info.GetAbsoluteByteOffset(e.FileInst.file);
        int size = (int)e.Info.ByteSize;
        reader.Position = pos;
        byte[] data = reader.ReadBytes(size);
        File.WriteAllBytes(outputPath, data);
    }

    // ---------- 路径工具 ----------

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        var result = sb.ToString().Trim();
        return string.IsNullOrEmpty(result) ? "unnamed" : result;
    }
}
