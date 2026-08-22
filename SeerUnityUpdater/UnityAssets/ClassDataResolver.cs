using System.IO.Compression;

namespace SeerUnityUpdater.UnityAssets;

/// <summary>
/// 定位或自动下载 AssetsTools.NET 所需的 classdata.tpk 类数据库。
/// 解析顺序：--tpk 显式路径 → exe 同目录 → 当前目录 → %APPDATA%/SeerUnityUpdater 缓存 → 自动下载。
/// </summary>
internal static class ClassDataResolver
{
    private const string UabeaWindowsZipUrl = "https://github.com/nesrak1/UABEA/releases/download/v8/uabea-windows.zip";

    private static string CacheDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SeerUnityUpdater");

    private static string CachePath => Path.Combine(CacheDir, "classdata.tpk");

    public static string? Resolve(string? explicitPath, TextWriter log)
    {
        if (!string.IsNullOrEmpty(explicitPath))
        {
            if (File.Exists(explicitPath)) return explicitPath;
            log.WriteLine($"警告: 指定的 --tpk 路径不存在: {explicitPath}");
        }

        var besideExe = Path.Combine(AppContext.BaseDirectory, "classdata.tpk");
        if (File.Exists(besideExe)) return besideExe;

        var cwdPath = Path.Combine(Directory.GetCurrentDirectory(), "classdata.tpk");
        if (File.Exists(cwdPath)) return cwdPath;

        if (File.Exists(CachePath)) return CachePath;

        try
        {
            log.WriteLine("未找到 classdata.tpk，正在从 UABEA 发布包下载...");
            return DownloadToCache(log);
        }
        catch (Exception ex)
        {
            log.WriteLine($"警告: 自动下载 classdata.tpk 失败: {ex.Message}");
            log.WriteLine("将仅依赖内嵌 TypeTree 解析（缺少 TypeTree 的资源将无法读取字段）。可通过 --tpk 显式指定。");
            return null;
        }
    }

    private static string DownloadToCache(TextWriter log)
    {
        Directory.CreateDirectory(CacheDir);
        var zipPath = Path.Combine(CacheDir, "uabea-windows.zip");

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        var bytes = client.GetByteArrayAsync(UabeaWindowsZipUrl).GetAwaiter().GetResult();
        File.WriteAllBytes(zipPath, bytes);

        log.WriteLine($"已下载 UABEA 压缩包 ({bytes.Length / 1024 / 1024} MB)，正在提取 classdata.tpk...");

        using var zip = ZipFile.OpenRead(zipPath);
        var entry = zip.GetEntry("classdata.tpk")
                    ?? throw new InvalidOperationException("压缩包中未找到 classdata.tpk");
        using var es = entry.Open();
        using var fs = File.Create(CachePath);
        es.CopyTo(fs);

        try { File.Delete(zipPath); } catch { /* 忽略清理失败 */ }

        log.WriteLine($"classdata.tpk 已缓存到: {CachePath}");
        return CachePath;
    }
}
