using SeerUnityUpdater.Seer.YooAsset;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using SeerUnityUpdater.UnityAssets;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

#region 导出Unity资源
/// <summary>解析导出动作字符串：clear=清空后导出，skip-existing=跳过同名（默认）。</summary>
static ExportAction ParseExportAction(string? value, out int errorCode)
{
    errorCode = 0;
    var v = (value ?? "").Trim().ToLowerInvariant();
    return v switch
    {
        "" or "skip-existing" or "skip" => ExportAction.SkipExisting,
        "clear" => ExportAction.Clear,
        _ => ErrorAction(out errorCode),
    };

    static ExportAction ErrorAction(out int code)
    {
        code = 1;
        Console.Error.WriteLine("--action 无效：可选值为 clear（清空后导出）或 skip-existing（跳过同名，默认）");
        return ExportAction.SkipExisting;
    }
}

static int RunExportBatch(string folder, string[][] exportList)
{
    var jobs = new List<ExportJob>();
    try
    {
        int idx = 0;
        foreach (var elem in exportList)
        {
            idx++;
            if (elem.Length < 2)
            {
                Console.Error.WriteLine($"任务 #{idx} 格式错误：应为 [\"正则\",\"导出目录\",\"动作?\"]");
                return 1;
            }
            string pattern = elem[0] ?? "";
            string outDir = elem[1] ?? "";
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(outDir))
            {
                Console.Error.WriteLine($"任务 #{idx} 正则或导出目录为空");
                return 1;
            }
            ExportAction action = ExportAction.SkipExisting;
            if (elem.Length >= 3)
            {
                action = ParseExportAction(elem[2], out int actErr);
                if (actErr != 0)
                {
                    Console.Error.WriteLine($"任务 #{idx} 动作无效");
                    return 1;
                }
            }
            Regex regex;
            try { regex = new Regex(pattern, RegexOptions.Compiled); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"任务 #{idx} 正则无效 \"{pattern}\": {ex.Message}");
                return 1;
            }
            jobs.Add(new ExportJob(regex, outDir, action));
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"解析任务文件失败: {ex.Message}");
        return 1;
    }

    if (jobs.Count == 0)
    {
        Console.Error.WriteLine("任务文件为空");
        return 1;
    }

    string? tpkPath = ClassDataResolver.Resolve("classdata.tpk", Console.Error);

    using var parser = new UnityAssetParser(tpkPath);
    var entries = parser.ParseFolder(folder);
    Console.Error.WriteLine($"解析到 {entries.Count} 个资源，开始执行 {jobs.Count} 个批量导出任务 ...");

    var exporter = new AssetExporter();
    var (total, perJob) = exporter.ExportBatch(entries, jobs);

    Console.WriteLine($"全部完成: 共导出 {total.Exported} 个（失败 {total.Failed}），未匹配 {total.Skipped} 个");
    return total.Failed > 0 ? 2 : 0;
}
#endregion

#region Main
// 1. 下载最新版本资源
const string SEER_DOWNLOAD_FOLDER = "seer_download", SEER_GAME_LOGIC_DLL_FILE_NAME = "game_dll_gamelogic_dll_bytes";

await updateGame(SEER_DOWNLOAD_FOLDER, [
        new(){
            name = "ConfigPackage",
            list = [
                ".*"
                ]
        },
        new(){
            name = "DefaultPackage",
            list = [
                "defaultpackage_assets_art_ui_assets_pet_head_\\d+\\.bundle",
                "defaultpackage_assets_art_autocard_texture_cards_\\d+\\.bundle",
                SEER_GAME_LOGIC_DLL_FILE_NAME
                ]
        },
        new(){
            name = "FollowPackage",
            list = []
        },
        new(){
            name = "PetAnimPackage",
            list = []
        },
        new(){
            name = "StartupPackage",
            list = []
        },
        ]);

// 2. 导出
const string EXPORT_CLEAR = "clear", EXPORT_SKIP_EXISTING = "skip-existing";

RunExportBatch(SEER_DOWNLOAD_FOLDER, [
    ["assets/art/ui/assets/pet/head/\\d+\\.png", "C:\\Users\\Administrator\\Downloads\\game\\SeerUnity\\forDll\\pet-head", EXPORT_SKIP_EXISTING], // 精灵头像
    ["assets/art/autocard/texture/cards/card_\\d+\\.png", "C:\\Users\\Administrator\\Downloads\\game\\SeerUnity\\forDll\\cards", EXPORT_SKIP_EXISTING], // 群星牌卡面
    ["assets/game/configs/bytes/.*", "C:\\Users\\Administrator\\Downloads\\game\\SeerUnity\\forDll\\config-data", EXPORT_CLEAR], // 文本信息
    ]);

// 3. 反编译
const string GAME_LOGIC_OP_PATH = "GameLogic";
Decompile(Path.Combine(SEER_DOWNLOAD_FOLDER, "DefaultPackage", SEER_GAME_LOGIC_DLL_FILE_NAME), GAME_LOGIC_OP_PATH);

// 4. 用反编译出来的新版代码替换文本信息解析项目的旧代码
{
    const string PATH_CONFIG_OLD_VER = "C:\\Users\\Administrator\\Downloads\\game\\SeerUnityTextParse\\SeerUnityTextParse\\core\\config";
    AssetExporter.ClearDirectory(PATH_CONFIG_OLD_VER);
    Directory.CreateDirectory(PATH_CONFIG_OLD_VER);
    string srcDir = Path.Combine(GAME_LOGIC_OP_PATH, "core", "config");
    foreach (var file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(srcDir, file);
        var destFile = Path.Combine(PATH_CONFIG_OLD_VER, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
        File.Copy(file, destFile, true);
    }
    // 修复部分反编译异常的文件
    srcDir = "C:\\Users\\Administrator\\Downloads\\game\\SeerUnityTextParse\\SeerUnityTextParse\\core\\config-fixed";
    foreach (var file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(srcDir, file);
        var destFile = Path.Combine(PATH_CONFIG_OLD_VER, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
        File.Copy(file, destFile, true);
    }
}

#endregion

#region 反编译
string SanitizeFileName(string name)
{
    var invalid = Path.GetInvalidFileNameChars();
    var sb = new StringBuilder(name.Length);
    foreach (char c in name)
    {
        sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
    }
    return sb.ToString();
}

string BuildTypeFilePath(string outputDir, string ns, string name)
{
    string safeName = SanitizeFileName(name) + ".cs";

    if (string.IsNullOrEmpty(ns))
        return Path.Combine(outputDir, safeName);

    string[] nsParts = ns.Split('.');
    for (int i = 0; i < nsParts.Length; i++)
        nsParts[i] = SanitizeFileName(nsParts[i]);

    string nsPath = Path.Combine(nsParts);
    return Path.Combine(outputDir, nsPath, safeName);
}

void Decompile(string inputFile, string outputDir)
{
    AssetExporter.ClearDirectory(outputDir);

    var settings = new DecompilerSettings(LanguageVersion.CSharp4)
    {
        ThrowOnAssemblyResolveErrors = false,
        RemoveDeadCode = true,
        RemoveDeadStores = true,
        UseDebugSymbols = true,
        FileScopedNamespaces = false,
    };

    using var peFile = new PEFile(
            inputFile,
            PEStreamOptions.PrefetchEntireImage,
            MetadataReaderOptions.Default
        );

    string targetFramework = peFile.DetectTargetFrameworkId();
    var resolver = new UniversalAssemblyResolver(
        inputFile,
        false,
        targetFramework
    );
    var typeSystem = new DecompilerTypeSystem(peFile, resolver, settings);
    var decompiler = new CSharpDecompiler(typeSystem, settings);

    {
        Directory.CreateDirectory(outputDir);

        var metadata = peFile.Metadata;

        // Collect top-level (non-nested) type handles
        var typeHandles = new List<TypeDefinitionHandle>();
        foreach (var handle in metadata.TypeDefinitions)
        {
            var td = metadata.GetTypeDefinition(handle);
            if (td.IsNested)
                continue;
            typeHandles.Add(handle);
        }

        Console.WriteLine($"[INFO] Found {typeHandles.Count} top-level types");
        Console.WriteLine($"[INFO] Output directory: {outputDir}");
        Console.WriteLine();

        int success = 0;
        int failed = 0;

        for (int i = 0; i < typeHandles.Count; i++)
        {
            var handle = typeHandles[i];
            var td = metadata.GetTypeDefinition(handle);
            string ns = metadata.GetString(td.Namespace);
            string name = metadata.GetString(td.Name);

            string fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
            //Console.Write($"[{i + 1}/{typeHandles.Count}] {fullName} ... ");

            try
            {
                string code = decompiler.DecompileTypesAsString([handle]);
                string filePath = BuildTypeFilePath(outputDir, ns, name);
                string? dir = Path.GetDirectoryName(filePath);
                if (dir is not null)
                    Directory.CreateDirectory(dir);
                File.WriteAllText(filePath, code, new UTF8Encoding(false));
                //Console.WriteLine("OK");
                success++;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"FAILED ({ex.Message})");
                failed++;
            }
        }

        // Decompile assembly-level attributes to a separate file
        try
        {
            string attrs = decompiler.DecompileModuleAndAssemblyAttributesToString();
            if (!string.IsNullOrWhiteSpace(attrs))
            {
                string attrsPath = Path.Combine(outputDir, "AssemblyAttributes.cs");
                File.WriteAllText(attrsPath, attrs, new UTF8Encoding(false));
                //Console.WriteLine("[INFO] Generated AssemblyAttributes.cs");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Failed to decompile assembly attributes: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine($"[INFO] Decompilation complete: {success} succeeded, {failed} failed");
    }
}

#endregion

#region 下载资源
async Task SeerUpdatePackage(string packageName, List<string> strList)
{
    Console.WriteLine($"{packageName} 开始更新");
    string packageDownloadPath = Path.Combine(SEER_DOWNLOAD_FOLDER, packageName);
    if (!Directory.Exists(packageDownloadPath))
    {
        Directory.CreateDirectory(packageDownloadPath);
    }
    AssetExporter.ClearDirectory(packageDownloadPath);
    PersistentTools.GetOrCreatePersistent(packageName);
    const string BASE_URL = "https://newseer.61.com/Assets/StandaloneWindows64";
    string packageVersion;
    // 1. 获取版本号
    {
        using var httpClient = new HttpClient();
        // 抄 SeerApi 项目的请求头（用 Fiddler 也可以抓到）
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("referer", "https://newseer.61.com");
        packageVersion = await httpClient.GetStringAsync($"{BASE_URL}/{packageName}/{YooAssetSettingsData.GetPackageVersionFileName(packageName)}?{DateTime.UtcNow.Ticks}");

        // 2. 获取原始清单文件
        var manifestBytes = await httpClient.GetByteArrayAsync($"{BASE_URL}/{packageName}/{YooAssetSettingsData.GetManifestBinaryFileName(packageName, packageVersion)}");

        // 3. 解析清单文件
        var dmOpeation = new DeserializeManifestOperation(manifestBytes);
        dmOpeation.Update(s => {});
        File.WriteAllText(Path.Combine(SEER_DOWNLOAD_FOLDER, $"{packageName}Manifest.json"), JsonSerializer.Serialize(dmOpeation));

        // 4.1 下载 Assets
        var regList = strList.Select(str => new Regex(str));
        foreach (var bundle in dmOpeation.Manifest.BundleList)
        {
            foreach (var reg in regList)
            {
                if (reg.IsMatch(bundle.BundleName))
                {
                    FileUtility.WriteAllBytes(
                        Path.Combine(packageDownloadPath, bundle.BundleName),
                        await httpClient.GetByteArrayAsync($"{BASE_URL}/{packageName}/{bundle.FileName}")
                        );
                    break;
                }
            }
        }
    }
    Console.WriteLine($"{packageName} 更新完成");
}

async Task updateGame(string path, List<UpdateCustomListNode> configObj)
{
    foreach (var obj in configObj)
    {
        await SeerUpdatePackage(obj.name, obj.list);
    }
}

class UpdateCustomListNode
{
    public string name { get; set; }
    public List<string> list { get; set; }
}
#endregion
