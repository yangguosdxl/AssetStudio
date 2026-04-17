using Newtonsoft.Json;
using System.Collections.Generic;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 误差信息
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>
        /// 文件实际大小（字节）
        /// </summary>
        [JsonProperty("actual_file_size")]
        public long ActualFileSize { get; set; }

        /// <summary>
        /// 计算总计（字节）
        /// </summary>
        [JsonProperty("calculated_total")]
        public long CalculatedTotal { get; set; }

        /// <summary>
        /// 绝对误差（字节）
        /// </summary>
        [JsonProperty("absolute_error")]
        public long AbsoluteError { get; set; }

        /// <summary>
        /// 相对误差（百分比）
        /// </summary>
        [JsonProperty("relative_error_percent")]
        public double RelativeErrorPercent { get; set; }
    }

    /// <summary>
    /// 验证汇总信息
    /// 对比计算总和与文件实际大小，显示误差信息
    /// </summary>
    public class VerificationSummary
    {
        /// <summary>
        /// 各数据类别的大小汇总
        /// </summary>
        [JsonProperty("category_totals")]
        public List<CategoryTotal> CategoryTotals { get; set; } = new List<CategoryTotal>();

        /// <summary>
        /// 压缩前总计大小
        /// </summary>
        [JsonProperty("total_uncompressed")]
        public long TotalUncompressed { get; set; }

        /// <summary>
        /// 压缩后总计大小（计算值）
        /// </summary>
        [JsonProperty("total_compressed")]
        public long TotalCompressed { get; set; }

        /// <summary>
        /// 误差信息
        /// </summary>
        [JsonProperty("error_info")]
        public ErrorInfo ErrorInfo { get; set; } = new ErrorInfo();
    }
}