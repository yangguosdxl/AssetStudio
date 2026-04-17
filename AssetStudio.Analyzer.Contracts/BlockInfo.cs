using Newtonsoft.Json;
using System.Collections.Generic;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 数据块信息
    /// </summary>
    public class BlockInfo
    {
        /// <summary>
        /// Block 序号
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// 压缩类型
        /// </summary>
        [JsonProperty("compression_type")]
        public string CompressionType { get; set; } = string.Empty;

        /// <summary>
        /// 压缩大小
        /// </summary>
        [JsonProperty("size_compressed")]
        public long SizeCompressed { get; set; }

        /// <summary>
        /// 解压大小
        /// </summary>
        [JsonProperty("size_uncompressed")]
        public long SizeUncompressed { get; set; }

        /// <summary>
        /// 压缩率
        /// </summary>
        [JsonProperty("compression_ratio")]
        public double CompressionRatio { get; set; }

        /// <summary>
        /// 包含的文件列表
        /// </summary>
        [JsonProperty("contained_files")]
        public List<string> ContainedFiles { get; set; } = new List<string>();
    }
}