using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// Block 对资源的压缩贡献
    /// </summary>
    public class BlockContribution
    {
        /// <summary>
        /// Block 序号
        /// </summary>
        [JsonProperty("block_index")]
        public int BlockIndex { get; set; }

        /// <summary>
        /// 交集起始位置
        /// </summary>
        [JsonProperty("intersect_start")]
        public long IntersectStart { get; set; }

        /// <summary>
        /// 交集结束位置
        /// </summary>
        [JsonProperty("intersect_end")]
        public long IntersectEnd { get; set; }

        /// <summary>
        /// 交集大小
        /// </summary>
        [JsonProperty("intersect_size")]
        public long IntersectSize { get; set; }

        /// <summary>
        /// 占 Block 的比例
        /// </summary>
        [JsonProperty("ratio")]
        public double Ratio { get; set; }

        /// <summary>
        /// 压缩贡献大小
        /// </summary>
        [JsonProperty("compressed_contribution")]
        public long CompressedContribution { get; set; }
    }
}