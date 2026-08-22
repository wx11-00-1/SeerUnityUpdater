namespace SeerUnityUpdater.UnityAssets;

/// <summary>
/// 常见 Unity 类 ID 到名称的映射，用于无类数据库时也能显示可读类型名。
/// </summary>
internal static class UnityClassNames
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [1] = "GameObject",
        [4] = "Transform",
        [12] = "Animation",
        [21] = "Material",
        [23] = "MeshRenderer",
        [25] = "Renderer",
        [26] = "ParticleRenderer",
        [27] = "SkinnedMeshRenderer",
        [28] = "Texture2D",
        [33] = "MeshFilter",
        [41] = "OcclusionPortal",
        [43] = "Mesh",
        [48] = "Shader",
        [49] = "TextAsset",
        [50] = "Rigidbody2D",
        [56] = "Collider2D",
        [60] = "PolygonCollider2D",
        [65] = "BoxCollider",
        [68] = "MeshCollider",
        [74] = "AnimationClip",
        [75] = "AnimatorController",
        [83] = "AudioClip",
        [89] = "CubeMap",
        [108] = "Light",
        [109] = "Behaviour",
        [114] = "MonoBehaviour",
        [115] = "MonoScript",
        [128] = "Font",
        [129] = "PlayerSettings",
        [142] = "AssetBundle",
        [150] = "PreloadData",
        [184] = "SpriteMask",
        [194] = "SpriteRenderer",
        [212] = "RayTracingShader",
        [213] = "Sprite",
        [222] = "CanvasRenderer",
        [224] = "RectTransform",
        [225] = "Canvas",
        [329] = "SpriteAtlas",
        [1024] = "AnimatorControllerLayer",
        [1025] = "AnimatorControllerParameter",
    };

    public static string Get(int typeId) => Names.TryGetValue(typeId, out var n) ? n : ("Class_" + typeId);
}
