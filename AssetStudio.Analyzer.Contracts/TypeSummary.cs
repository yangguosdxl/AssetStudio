using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 类型汇总统计
    /// </summary>
    public class TypeSummary
    {
        /// <summary>
        /// 资源类型名称
        /// </summary>
        [JsonProperty("type_name")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 资源类型 ID
        /// </summary>
        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        /// <summary>
        /// 该类型的资源数量
        /// </summary>
        [JsonProperty("resource_count")]
        public int ResourceCount { get; set; }

        /// <summary>
        /// 总大小（解压后）
        /// </summary>
        [JsonProperty("total_size_uncompressed")]
        public long TotalSizeUncompressed { get; set; }

        /// <summary>
        /// 总大小（压缩后）
        /// </summary>
        [JsonProperty("total_size_compressed")]
        public long TotalSizeCompressed { get; set; }

        /// <summary>
        /// 平均大小
        /// </summary>
        [JsonProperty("average_size")]
        public long AverageSize { get; set; }

        /// <summary>
        /// 最大资源名称
        /// </summary>
        [JsonProperty("largest_resource_name")]
        public string LargestResourceName { get; set; } = string.Empty;

        /// <summary>
        /// 最大资源大小
        /// </summary>
        [JsonProperty("largest_size")]
        public long LargestSize { get; set; }

        /// <summary>
        /// 占 Bundle 的比例
        /// </summary>
        [JsonProperty("bundle_ratio")]
        public double BundleRatio { get; set; }
    }
}