using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 误差原因类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VarianceCause
    {
        /// <summary>
        /// 外部资源缺失 - .resource 文件未找到
        /// </summary>
        ExternalMissing,

        /// <summary>
        /// 压缩效率低 - 数据几乎未被压缩
        /// </summary>
        LowCompression,

        /// <summary>
        /// 元数据膨胀 - TypeTree 或类型信息过大
        /// </summary>
        MetadataBloat,

        /// <summary>
        /// 未识别数据 - 存在未解析的数据区域
        /// </summary>
        UnidentifiedData,

        /// <summary>
        /// 数据块碎片化 - 存在大量小块
        /// </summary>
        BlockFragmentation
    }
}
