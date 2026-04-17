namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 数据类别枚举
    /// 用于区分资源数据和非资源数据
    /// </summary>
    public enum DataCategory
    {
        /// <summary>
        /// 资源数据
        /// Mesh, Texture2D, AnimationClip 等实际资源对象
        /// </summary>
        Resource,

        /// <summary>
        /// 非资源数据
        /// Bundle Header, BlocksInfo, SerializedFile Metadata 等元数据
        /// </summary>
        NonResource
    }
}