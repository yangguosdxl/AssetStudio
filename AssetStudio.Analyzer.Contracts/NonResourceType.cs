namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 非资源数据类型枚举
    /// 用于标识 AssetBundle 中除资源对象外的各种元数据类型
    /// </summary>
    public enum NonResourceType
    {
        /// <summary>
        /// Bundle 级元数据（Bundle Header + BlocksInfo）
        /// 属于整个 Bundle 文件，不关联特定 SerializedFile
        /// </summary>
        BundleMeta,

        /// <summary>
        /// SerializedFile Header + Version/Platform 信息
        /// 每个 SerializedFile 文件的头部区域
        /// </summary>
        FileHeader,

        /// <summary>
        /// Types 数组 + TypeTree 数据
        /// 所有 SerializedType 及其 TypeTree 结构数据
        /// </summary>
        TypeTree,

        /// <summary>
        /// Objects 目录数组
        /// 所有 ObjectInfo 条目，用于定位资源数据
        /// </summary>
        ObjectDir,

        /// <summary>
        /// 外部引用区域
        /// ScriptTypes + Externals + RefTypes + UserInfo
        /// </summary>
        Externals,

        /// <summary>
        /// ScriptTypes 部分
        /// LocalSerializedObjectIdentifier[] 数组
        /// </summary>
        ScriptTypes,

        /// <summary>
        /// RefTypes 部分
        /// 引用类型 SerializedType[] 数组（含 TypeTree）
        /// </summary>
        RefTypes,

        /// <summary>
        /// UserInformation 字符串
        /// 用户自定义信息
        /// </summary>
        UserInformation
    }
}