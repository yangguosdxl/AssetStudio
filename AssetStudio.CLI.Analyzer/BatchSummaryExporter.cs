using AssetStudio.Analyzer.Contracts;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// 批量汇总报告导出器
    /// 生成包含所有 AssetBundle 误差率汇总和高误差分析的 Excel 报告
    /// </summary>
    public class BatchSummaryExporter
    {
        private readonly VarianceAnalyzer _varianceAnalyzer = new VarianceAnalyzer();
        private const double HighVarianceThreshold = 10.0;  // 高误差阈值

        /// <summary>
        /// 导出批量汇总报告
        /// </summary>
        /// <param name="reports">所有 Bundle 的分析报告</param>
        /// <param name="outputPath">输出文件路径</param>
        public void Export(Dictionary<string, AnalysisReport> reports, string outputPath)
        {
            // 设置 EPPlus 许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // 生成汇总数据
                var summaryItems = BuildSummaryItems(reports);

                // 写入误差率汇总 Sheet
                WriteSummarySheet(package, summaryItems);

                // 写入高误差分析 Sheet
                WriteAnalysisSheet(package, reports, summaryItems);

                // 保存文件
                package.SaveAs(new FileInfo(outputPath));
            }
        }

        /// <summary>
        /// 构建汇总项列表
        /// </summary>
        private List<BundleSummaryItem> BuildSummaryItems(Dictionary<string, AnalysisReport> reports)
        {
            var items = new List<BundleSummaryItem>();

            foreach (var kvp in reports)
            {
                var bundlePath = kvp.Key;
                var report = kvp.Value;

                var compressedSize = report.Bundle.TotalSizeCompressed;
                var uncompressedSize = report.Bundle.TotalSizeUncompressed;
                var compressionRatio = uncompressedSize > 0 ? (double)compressedSize / uncompressedSize * 100 : 0;
                var variancePercent = report.VerificationSummary.ErrorInfo.RelativeErrorPercent;

                items.Add(new BundleSummaryItem
                {
                    FileName = report.Bundle.FileName,
                    FilePath = bundlePath,
                    TotalSize = compressedSize,
                    CompressedSize = compressedSize,
                    UncompressedSize = uncompressedSize,
                    CompressionRatioPercent = compressionRatio,
                    VariancePercent = variancePercent,
                    Status = variancePercent > HighVarianceThreshold ? "高误差" : "正常",
                    UnityVersion = report.Bundle.UnityVersion,
                    ResourceCount = report.Bundle.ResourceCount
                });
            }

            // 按误差率降序排列
            return items.OrderByDescending(i => i.VariancePercent).ToList();
        }

        /// <summary>
        /// 写入误差率汇总 Sheet
        /// </summary>
        private void WriteSummarySheet(ExcelPackage package, List<BundleSummaryItem> items)
        {
            var sheet = package.Workbook.Worksheets.Add("误差率汇总");

            // 设置列标题
            var headers = new[] {
                "文件名", "文件路径", "总大小", "压缩大小", "解压大小",
                "压缩率", "误差率", "状态", "Unity版本", "资源数量"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < items.Count; row++)
            {
                var item = items[row];
                int rowNum = row + 2;

                sheet.Cells[rowNum, 1].Value = item.FileName;
                sheet.Cells[rowNum, 2].Value = item.FilePath;
                sheet.Cells[rowNum, 3].Value = FormatSize(item.TotalSize);
                sheet.Cells[rowNum, 4].Value = FormatSize(item.CompressedSize);
                sheet.Cells[rowNum, 5].Value = FormatSize(item.UncompressedSize);
                sheet.Cells[rowNum, 6].Value = FormatPercentage(item.CompressionRatioPercent);
                sheet.Cells[rowNum, 7].Value = FormatPercentage(item.VariancePercent);
                sheet.Cells[rowNum, 8].Value = item.Status;
                sheet.Cells[rowNum, 9].Value = item.UnityVersion;
                sheet.Cells[rowNum, 10].Value = item.ResourceCount;

                // 高误差行红色高亮
                if (item.VariancePercent > HighVarianceThreshold)
                {
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        sheet.Cells[rowNum, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        sheet.Cells[rowNum, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 200, 200));
                    }
                }
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入高误差分析 Sheet
        /// </summary>
        private void WriteAnalysisSheet(ExcelPackage package, Dictionary<string, AnalysisReport> reports, List<BundleSummaryItem> summaryItems)
        {
            var sheet = package.Workbook.Worksheets.Add("高误差分析");

            // 筛选高误差 Bundle
            var highVarianceItems = summaryItems.Where(i => i.VariancePercent > HighVarianceThreshold).ToList();

            if (highVarianceItems.Count == 0)
            {
                // 无高误差 Bundle
                sheet.Cells[1, 1].Value = "无高误差 AssetBundle（所有 Bundle 误差率均 ≤ 10%）";
                sheet.Cells[1, 1].Style.Font.Bold = true;
                return;
            }

            // 设置列标题
            var headers = new[] {
                "文件名", "误差率", "分析原因", "影响比例", "置信度", "详细信息", "是否首要原因"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 分析每个高误差 Bundle
            int rowNum = 2;
            foreach (var item in highVarianceItems)
            {
                var report = reports[item.FilePath];
                var analysisResults = _varianceAnalyzer.Analyze(report);

                foreach (var result in analysisResults)
                {
                    sheet.Cells[rowNum, 1].Value = result.FileName;
                    sheet.Cells[rowNum, 2].Value = FormatPercentage(result.VariancePercent);
                    sheet.Cells[rowNum, 3].Value = result.CauseDescription;
                    sheet.Cells[rowNum, 4].Value = FormatPercentage(result.ImpactPercent);
                    sheet.Cells[rowNum, 5].Value = GetConfidenceText(result.Confidence);
                    sheet.Cells[rowNum, 6].Value = result.Details;
                    sheet.Cells[rowNum, 7].Value = result.IsPrimary ? "是" : "否";

                    // 首要原因行加粗
                    if (result.IsPrimary)
                    {
                        for (int col = 1; col <= headers.Length; col++)
                        {
                            sheet.Cells[rowNum, col].Style.Font.Bold = true;
                        }
                    }

                    rowNum++;
                }
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 获取置信度文本
        /// </summary>
        private string GetConfidenceText(ConfidenceLevel confidence)
        {
            return confidence switch
            {
                ConfidenceLevel.High => "高",
                ConfidenceLevel.Medium => "中",
                ConfidenceLevel.Low => "低",
                _ => "未知"
            };
        }

        /// <summary>
        /// 自适应列宽
        /// </summary>
        private void AutoFitColumns(ExcelWorksheet sheet)
        {
            if (sheet.Dimension == null) return;

            for (int col = sheet.Dimension.Start.Column; col <= sheet.Dimension.End.Column; col++)
            {
                sheet.Column(col).AutoFit();
            }
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

        /// <summary>
        /// 格式化百分比
        /// </summary>
        private string FormatPercentage(double value)
        {
            return $"{value:F2}%";
        }
    }
}
