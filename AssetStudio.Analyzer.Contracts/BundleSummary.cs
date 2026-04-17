using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// Bundle 文件概览信息
    /// </summary>
    public class BundleSummary
    {
        /// <summary>
        /// Bundle 文件名
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Bundle 文件总大小（压缩后）
        /// </summary>
        [JsonProperty("total_size_compressed")]
        public long TotalSizeCompressed { get; set; }

        /// <summary>
        /// Bundle 文件总大小（解压后）
        /// </summary>
        [JsonProperty("total_size_uncompressed")]
        public long TotalSizeUncompressed { get; set; }

        /// <summary>
        /// Bundle Header 大小
        /// </summary>
        [JsonProperty("header_size")]
        public long HeaderSize { get; set; }

        /// <summary>
        /// BlocksInfo 区域大小（压缩后）
        /// </summary>
        [JsonProperty("blocks_info_size_compressed")]
        public long BlocksInfoSizeCompressed { get; set; }

        /// <summary>
        /// BlocksInfo 区域大小（解压后）
        /// </summary>
        [JsonProperty("blocks_info_size_uncompressed")]
        public long BlocksInfoSizeUncompressed { get; set; }

        /// <summary>
        /// DataBlocks 总大小（压缩后）
        /// </summary>
        [JsonProperty("data_blocks_size_compressed")]
        public long DataBlocksSizeCompressed { get; set; }

        /// <summary>
        /// DataBlocks 总大小（解压后）
        /// </summary>
        [JsonProperty("data_blocks_size_uncompressed")]
        public long DataBlocksSizeUncompressed { get; set; }

        /// <summary>
        /// 压缩类型（LZ4, LZMA, None等）
        /// </summary>
        [JsonProperty("compression_type")]
        public string CompressionType { get; set; } = string.Empty;

        /// <summary>
        /// Unity 版本
        /// </summary>
        [JsonProperty("unity_version")]
        public string UnityVersion { get; set; } = string.Empty;

        /// <summary>
        /// 内部文件数量
        /// </summary>
        [JsonProperty("internal_file_count")]
        public int InternalFileCount { get; set; }

        /// <summary>
        /// 资源对象数量
        /// </summary>
        [JsonProperty("resource_count")]
        public int ResourceCount { get; set; }
    }
}