namespace CLocalization
{
    /// <summary>
    /// 资源路径类型。决定运行时如何从路径加载资源。
    /// </summary>
    public enum AssetPathType
    {
        /// <summary>
        /// Resources 相对路径（无 "Resources/" 前缀、无扩展名），如 "CLocalization/Assets/zh-CN/logo"。
        /// 运行时用 Resources.Load 加载。资源必须在 Resources 目录下。
        /// </summary>
        Resources = 0,

        /// <summary>
        /// 完整工程路径（含 "Assets/" 前缀和扩展名），如 "Assets/Art/zh-CN/logo.png"。
        /// 编辑器用 AssetDatabase.LoadAssetAtPath 加载；运行时（打包后）需自定义 Loader（AssetBundle/Addressables）。
        /// 资源可在工程任意位置，方便打 AB 分包。
        /// </summary>
        FullPath = 1,
    }
}
