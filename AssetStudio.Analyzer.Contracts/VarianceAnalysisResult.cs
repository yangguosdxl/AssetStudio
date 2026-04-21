using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 置信度级别
    /// </summary>
    public enum ConfidenceLevel
    {
        /// <summary>
        /// 高置信度 - 原因明确，数据支持充分
        /// </summary>
        High,

        /// <summary>
        /// 中置信度 - 原因可能，需要进一步确认
        /// </summary>
        Medium,

        /// <summary>
        /// 低置信度 - 原因不确定，需人工确认
        /// </summary>
        Low
    }

    /// <summary>
    /// 误差分析结果
    /// </summary>
    public class VarianceAnalysisResult
    {
        /// <summary>
        /// Bundle 文件名
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; } = "";

        /// <summary>
        /// 误差率（百分比）
        /// </summary>
        [JsonProperty("variance_percent")]
        public double VariancePercent { get; set; }

        /// <summary>
        /// 误差原因类型
        /// </summary>
        [JsonProperty("cause")]
        public VarianceCause Cause { get; set; }

        /// <summary>
        /// 原因描述（中文）
        /// </summary>
        [JsonProperty("cause_description")]
        public string CauseDescription { get; set; } = "";

        /// <summary>
        /// 影响比例（百分比）- 该原因导致的误差占比
        /// </summary>
        [JsonProperty("impact_percent")]
        public double ImpactPercent { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        [JsonProperty("confidence")]
        public ConfidenceLevel Confidence { get; set; }

        /// <summary>
        /// 详细信息
        /// </summary>
        [JsonProperty("details")]
        public string Details { get; set; } = "";

        /// <summary>
        /// 是否为首要原因
        /// </summary>
        [JsonProperty("is_primary")]
        public bool IsPrimary { get; set; }
    }
}
