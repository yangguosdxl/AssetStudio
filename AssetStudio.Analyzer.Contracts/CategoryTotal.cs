using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 数据类别汇总信息
    /// 用于验证汇总中按类别统计大小
    /// </summary>
    public class CategoryTotal
    {
        /// <summary>
        /// 类别名称
        /// BundleMeta/FileHeader/TypeTree/ObjectDir/Externals/资源数据
        /// </summary>
        [JsonProperty("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 非资源数据类型（仅非资源数据类别有值）
        /// </summary>
        [JsonProperty("non_resource_type")]
        public NonResourceType? NonResourceType { get; set; }

        /// <summary>
        /// 该类别压缩前总计大小
        /// </summary>
        [JsonProperty("size_uncompressed")]
        public long SizeUncompressed { get; set; }

        /// <summary>
        /// 该类别压缩后总计大小
        /// </summary>
        [JsonProperty("size_compressed")]
        public long SizeCompressed { get; set; }

        /// <summary>
        /// 占文件总大小的比例（百分比）
        /// </summary>
        [JsonProperty("percentage")]
        public double Percentage { get; set; }

        /// <summary>
        /// 该类别包含的数据行数量
        /// </summary>
        [JsonProperty("item_count")]
        public int ItemCount { get; set; }
    }
}