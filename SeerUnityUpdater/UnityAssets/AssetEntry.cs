using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace SeerUnityUpdater.UnityAssets;

/// <summary>
/// 描述一个已解析的 Unity 资源对象。
/// </summary>
internal sealed class AssetEntry
{
    public string SourceFile { get; }
    public string ClassName { get; }
    public int TypeId { get; }
    public long PathId { get; }
    public uint ByteSize { get; }
    public string? Name { get; }
    /// <summary>来自 AssetBundle m_Container 的资源路径（如 "assets/res/hero.prefab"）。</summary>
    public string? ContainerPath { get; }

    // 以下为导出时回访对象所需的内部句柄。
    internal AssetsManager Manager { get; }
    internal AssetsFileInstance FileInst { get; }
    internal AssetFileInfo Info { get; }

    public AssetEntry(
        string sourceFile, string className, int typeId, long pathId, uint byteSize,
        string? name, string? containerPath,
        AssetsManager manager, AssetsFileInstance fileInst, AssetFileInfo info)
    {
        SourceFile = sourceFile;
        ClassName = className;
        TypeId = typeId;
        PathId = pathId;
        ByteSize = byteSize;
        Name = name;
        ContainerPath = containerPath;
        Manager = manager;
        FileInst = fileInst;
        Info = info;
    }
}
