using AssetStudio.Analyzer.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// 误差原因分析器
    /// 分析导致高误差率的常见原因
    /// </summary>
    public class VarianceAnalyzer
    {
        // 阈值常量
        private const double LowCompressionThreshold = 90.0;  // 压缩率超过90%视为效率低
        private const double MetadataBloatThreshold = 30.0;    // 元数据占比超过30%视为膨胀
        private const double UnidentifiedDataThreshold = 20.0; // 未识别数据占比超过20%
        private const double ExternalMissingThreshold = 5.0;   // 外部资源缺失占比超过5%

        /// <summary>
        /// 分析单个 Bundle 的误差原因
        /// </summary>
        /// <param name="report">分析报告</param>
        /// <returns>误差原因列表，按影响程度排序</returns>
        public List<VarianceAnalysisResult> Analyze(AnalysisReport report)
        {
            var results = new List<VarianceAnalysisResult>();

            // 检测各种原因
            results.AddRange(AnalyzeExternalMissing(report));
            results.AddRange(AnalyzeLowCompression(report));
            results.AddRange(AnalyzeMetadataBloat(report));
            results.AddRange(AnalyzeUnidentifiedData(report));
            results.AddRange(AnalyzeBlockFragmentation(report));

            // 按影响比例降序排序
            results = results.OrderByDescending(r => r.ImpactPercent).ToList();

            // 标记首要原因
            if (results.Count > 0)
            {
                results[0].IsPrimary = true;
            }

            return results;
        }

        /// <summary>
        /// 检测外部资源缺失
        /// </summary>
        private List<VarianceAnalysisResult> AnalyzeExternalMissing(AnalysisReport report)
        {
            var results = new List<VarianceAnalysisResult>();

            // 查找外部资源缺失的资源
            var externalMissing = report.Resources
                .Where(r => r.DataSource == "ExternalMissing")
                .ToList();

            if (externalMissing.Count == 0)
            {
                return results;
            }

            // 计算缺失资源占比
            var missingSize = externalMissing.Sum(r => r.SizeUncompressed);
            var totalSize = report.Bundle.TotalSizeUncompressed;
            var impactPercent = totalSize > 0 ? (double)missingSize / totalSize * 100 : 0;

            // 只有占比超过阈值才报告
            if (impactPercent >= ExternalMissingThreshold)
            {
                results.Add(new VarianceAnalysisResult
                {
                    FileName = report.Bundle.FileName,
                    VariancePercent = report.VerificationSummary.ErrorInfo.RelativeErrorPercent,
                    Cause = VarianceCause.ExternalMissing,
                    CauseDescription = "外部资源缺失",
                    ImpactPercent = impactPercent,
                    Confidence = ConfidenceLevel.High,
                    Details = $"缺失 {externalMissing.Count} 个外部资源，共 {FormatSize(missingSize)}，" +
                              $"包括: {string.Join(", ", externalMissing.Take(3).Select(r => r.Name))}" +
                              (externalMissing.Count > 3 ? $" 等 {externalMissing.Count} 个" : "")
                });
            }

            return results;
        }

        /// <summary>
        /// 检测压缩效率低
        /// </summary>
        private List<VarianceAnalysisResult> AnalyzeLowCompression(AnalysisReport report)
        {
            var results = new List<VarianceAnalysisResult>();

            // 计算整体压缩率
            var compressedSize = report.Bundle.TotalSizeCompressed;
            var uncompressedSize = report.Bundle.TotalSizeUncompressed;
            var compressionRatio = uncompressedSize > 0 ? (double)compressedSize / uncompressedSize * 100 : 0;

            // 压缩率超过阈值视为效率低
            if (compressionRatio >= LowCompressionThreshold)
            {
                // 查找压缩率最低的 Block
                var lowCompressionBlocks = report.Blocks
                    .Where(b => b.CompressionRatio >= 0.9)
                    .ToList();

                results.Add(new VarianceAnalysisResult
                {
                    FileName = report.Bundle.FileName,
                    VariancePercent = report.VerificationSummary.ErrorInfo.RelativeErrorPercent,
                    Cause = VarianceCause.LowCompression,
                    CauseDescription = "压缩效率低",
                    ImpactPercent = 100 - compressionRatio,  // 影响程度 = 未压缩比例
                    Confidence = ConfidenceLevel.Medium,
                    Details = $"整体压缩率 {compressionRatio:F1}%，" +
                              $"有 {lowCompressionBlocks.Count}/{report.Blocks.Count} 个 Block 压缩率超过90%"
                });
            }

            return results;
        }

        /// <summary>
        /// 检测元数据膨胀
        /// </summary>
        private List<VarianceAnalysisResult> AnalyzeMetadataBloat(AnalysisReport report)
        {
            var results = new List<VarianceAnalysisResult>();

            // 计算元数据区域大小
            var headerSize = report.Bundle.HeaderSize;
            var blocksInfoSize = report.Bundle.BlocksInfoSizeUncompressed;
            var metadataSize = headerSize + blocksInfoSize;
            var totalSize = report.Bundle.TotalSizeUncompressed;

            var metadataRatio = totalSize > 0 ? (double)metadataSize / totalSize * 100 : 0;

            // 元数据占比超过阈值
            if (metadataRatio >= MetadataBloatThreshold)
            {
                results.Add(new VarianceAnalysisResult
                {
                    FileName = report.Bundle.FileName,
                    VariancePercent = report.VerificationSummary.ErrorInfo.RelativeErrorPercent,
                    Cause = VarianceCause.MetadataBloat,
                    CauseDescription = "元数据膨胀",
                    ImpactPercent = metadataRatio,
                    Confidence = ConfidenceLevel.High,
                    Details = $"元数据区域共 {FormatSize(metadataSize)}，占 Bundle {metadataRatio:F1}% " +
                              $"(Header: {FormatSize(headerSize)}, BlocksInfo: {FormatSize(blocksInfoSize)})"
                });
            }

            return results;
        }

        /// <summary>
        /// 检测未识别数据
        /// </summary>
        private List<VarianceAnalysisResult> AnalyzeUnidentifiedData(AnalysisReport report)
        {
            var results = new List<VarianceAnalysisResult>();

            // 从验证汇总中获取非资源数据占比
            var nonResourceCategory = report.VerificationSummary.CategoryTotals
                .FirstOrDefault(c => c.CategoryName == "非资源数据");

            if (nonResourceCategory == null)
            {
                return results;
            }

            var nonResourcePercent = nonResourceCategory.Percentage;

            // 未识别数据占比超过阈值
            if (nonResourcePercent >= UnidentifiedDataThreshold)
            {
                results.Add(new VarianceAnalysisResult
                {
                    FileName = report.Bundle.FileName,
                    VariancePercent = report.VerificationSummary.ErrorInfo.RelativeErrorPercent,
                    Cause = VarianceCause.UnidentifiedData,
                    CauseDescription = "未识别数据",
                    ImpactPercent = nonResourcePercent,
                    Confidence = ConfidenceLevel.Medium,
                    Details = $"非资源数据区域共 {FormatSize(nonResourceCategory.SizeUncompressed)}，" +
                              $"占 Bundle {nonResourcePercent:F1}%，" +
                              $"包含 {nonResourceCategory.ItemCount} 个数据块"
                });
            }

            return results;
        }

        /// <summary>
        /// 检测数据块碎片化
        /// </summary>
        private List<VarianceAnalysisResult> AnalyzeBlockFragmentation(AnalysisReport report)
        {
            var results = new List<VarianceAnalysisResult>();

            // 检查小块数量
            var avgBlockSize = report.Blocks.Count > 0
                ? report.Bundle.TotalSizeUncompressed / report.Blocks.Count
                : 0;

            var smallBlocks = report.Blocks
                .Where(b => b.SizeUncompressed < avgBlockSize * 0.1)  // 小于平均10%的块
                .Count();

            // 如果超过一半的块都是小块，视为碎片化
            if (report.Blocks.Count > 0 && smallBlocks > report.Blocks.Count / 2)
            {
                results.Add(new VarianceAnalysisResult
                {
                    FileName = report.Bundle.FileName,
                    VariancePercent = report.VerificationSummary.ErrorInfo.RelativeErrorPercent,
                    Cause = VarianceCause.BlockFragmentation,
                    CauseDescription = "数据块碎片化",
                    ImpactPercent = (double)smallBlocks / report.Blocks.Count * 100,
                    Confidence = ConfidenceLevel.Low,
                    Details = $"共 {report.Blocks.Count} 个 Block，其中 {smallBlocks} 个为小块（<平均大小的10%）"
                });
            }

            return results;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatSize(long size)
        {
            if (size >= 1024 * 1024 * 1024)
            {
                return $"{size / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
            else if (size >= 1024 * 1024)
            {
                return $"{size / (1024.0 * 1024.0):F2} MB";
            }
            else if (size >= 1024)
            {
                return $"{size / 1024.0:F2} KB";
            }
            else
            {
                return $"{size} B";
            }
        }
    }
}
