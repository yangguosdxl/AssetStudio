using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// Bundle 汇总项 - 用于批量汇总报告
    /// </summary>
    public class BundleSummaryItem
    {
        /// <summary>
        /// 文件名（不含路径）
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; } = "";

        /// <summary>
        /// 完整文件路径
        /// </summary>
        [JsonProperty("file_path")]
        public string FilePath { get; set; } = "";

        /// <summary>
        /// 文件总大小（字节）
        /// </summary>
        [JsonProperty("total_size")]
        public long TotalSize { get; set; }

        /// <summary>
        /// 压缩后大小（字节）
        /// </summary>
        [JsonProperty("compressed_size")]
        public long CompressedSize { get; set; }

        /// <summary>
        /// 解压后大小（字节）
        /// </summary>
        [JsonProperty("uncompressed_size")]
        public long UncompressedSize { get; set; }

        /// <summary>
        /// 压缩率（百分比）
        /// </summary>
        [JsonProperty("compression_ratio_percent")]
        public double CompressionRatioPercent { get; set; }

        /// <summary>
        /// 相对误差率（百分比）
        /// </summary>
        [JsonProperty("variance_percent")]
        public double VariancePercent { get; set; }

        /// <summary>
        /// 状态：正常/高误差
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// Unity 版本
        /// </summary>
        [JsonProperty("unity_version")]
        public string UnityVersion { get; set; } = "";

        /// <summary>
        /// 资源数量
        /// </summary>
        [JsonProperty("resource_count")]
        public int ResourceCount { get; set; }
    }
}
