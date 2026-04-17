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
    /// Excel 报告导出器
    /// 输出包含中文列标题的 Excel 报告
    /// </summary>
    public class ExcelReportExporter
    {
        /// <summary>
        /// 导出分析报告到 Excel 文件
        /// </summary>
        /// <param name="report">分析报告</param>
        /// <param name="outputPath">输出文件路径</param>
        public void Export(AnalysisReport report, string outputPath)
        {
            // 设置 EPPlus 许可证上下文（非商业用途）
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // 写入各 Sheet
                WriteBundleOverviewSheet(package, report.Bundle);
                WriteInternalFilesSheet(package, report.InternalFiles);
                WriteResourcesSheet(package, report.Resources, report.NonResourceData);
                WriteTypeSummarySheet(package, report.TypeSummaries, report.Bundle.TotalSizeUncompressed);
                WriteBlocksSheet(package, report.Blocks);
                WriteTypeTreesSheet(package, report.TypeTrees);
                WriteVerificationSummarySheet(package, report.VerificationSummary);

                // Externals 详情 Sheet（可选）
                if (report.ScriptTypes.Any())
                {
                    WriteScriptTypesSheet(package, report.ScriptTypes);
                }
                if (report.ExternalDetails.Any())
                {
                    WriteExternalsSheet(package, report.ExternalDetails);
                }
                if (report.RefTypes.Any())
                {
                    WriteRefTypesSheet(package, report.RefTypes);
                }
                if (report.UserInformations.Any())
                {
                    WriteUserInformationSheet(package, report.UserInformations);
                }

                // 保存文件
                package.SaveAs(new FileInfo(outputPath));
            }
        }

        /// <summary>
        /// 写入 Bundle 概览 Sheet
        /// </summary>
        private void WriteBundleOverviewSheet(ExcelPackage package, BundleSummary bundle)
        {
            var sheet = package.Workbook.Worksheets.Add("Bundle概览");

            // 设置列标题
            var headers = new[] { "属性", "值" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            var rows = new List<(string Property, object Value)>
            {
                ("文件名", bundle.FileName),
                ("总大小（压缩后）", FormatSize(bundle.TotalSizeCompressed)),
                ("总大小（解压后）", FormatSize(bundle.TotalSizeUncompressed)),
                ("Header 大小", FormatSize(bundle.HeaderSize)),
                ("BlocksInfo 大小（压缩后）", FormatSize(bundle.BlocksInfoSizeCompressed)),
                ("BlocksInfo 大小（解压后）", FormatSize(bundle.BlocksInfoSizeUncompressed)),
                ("DataBlocks 大小（压缩后）", FormatSize(bundle.DataBlocksSizeCompressed)),
                ("DataBlocks 大小（解压后）", FormatSize(bundle.DataBlocksSizeUncompressed)),
                ("压缩类型", bundle.CompressionType),
                ("Unity 版本", bundle.UnityVersion),
                ("内部文件数量", bundle.InternalFileCount),
                ("资源对象数量", bundle.ResourceCount),
                ("压缩率", FormatPercentage(bundle.TotalSizeCompressed, bundle.TotalSizeUncompressed))
            };

            for (int row = 0; row < rows.Count; row++)
            {
                sheet.Cells[row + 2, 1].Value = rows[row].Property;
                sheet.Cells[row + 2, 2].Value = rows[row].Value;
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入内部文件 Sheet
        /// </summary>
        private void WriteInternalFilesSheet(ExcelPackage package, List<InternalFileInfo> internalFiles)
        {
            var sheet = package.Workbook.Worksheets.Add("内部文件");

            // 设置列标题
            var headers = new[] { "文件名", "大小（解压后）", "文件类型", "偏移", "元数据大小", "数据区大小", "资源数量", "类型数量" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < internalFiles.Count; row++)
            {
                var file = internalFiles[row];
                sheet.Cells[row + 2, 1].Value = file.FileName;
                sheet.Cells[row + 2, 2].Value = FormatSize(file.SizeUncompressed);
                sheet.Cells[row + 2, 3].Value = file.FileType;
                sheet.Cells[row + 2, 4].Value = file.Offset;
                sheet.Cells[row + 2, 5].Value = file.MetadataSize.HasValue ? FormatSize(file.MetadataSize.Value) : "-";
                sheet.Cells[row + 2, 6].Value = file.DataSize.HasValue ? FormatSize(file.DataSize.Value) : "-";
                sheet.Cells[row + 2, 7].Value = file.ResourceCount.HasValue ? file.ResourceCount.Value.ToString() : "-";
                sheet.Cells[row + 2, 8].Value = file.TypeCount.HasValue ? file.TypeCount.Value.ToString() : "-";
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入资源明细 Sheet（含非资源数据）
        /// </summary>
        private void WriteResourcesSheet(ExcelPackage package, List<ResourceInfo> resources, List<ResourceInfo> nonResourceData)
        {
            var sheet = package.Workbook.Worksheets.Add("资源明细");

            // 合并资源和非资源数据，按偏移排序
            var allData = new List<ResourceInfo>();
            allData.AddRange(nonResourceData);
            allData.AddRange(resources);
            allData = allData.OrderBy(r => r.DataOffset).ToList();

            // 设置列标题（新增"数据类别"列）
            var headers = new[] { "资源名称", "数据类别", "类型名称", "类型ID", "PathID", "数据来源", "大小（解压后）", "大小（压缩后）", "数据偏移", "外部文件路径", "Container路径", "所属内部文件" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < allData.Count; row++)
            {
                var item = allData[row];
                string dataCategoryStr = item.DataCategory == DataCategory.Resource ? "资源" : "非资源";

                sheet.Cells[row + 2, 1].Value = item.Name;
                sheet.Cells[row + 2, 2].Value = dataCategoryStr;
                sheet.Cells[row + 2, 3].Value = item.TypeName;
                sheet.Cells[row + 2, 4].Value = item.TypeId;
                sheet.Cells[row + 2, 5].Value = item.PathId;
                sheet.Cells[row + 2, 6].Value = item.DataSource;
                sheet.Cells[row + 2, 7].Value = FormatSize(item.SizeUncompressed);
                sheet.Cells[row + 2, 8].Value = FormatSize(item.SizeCompressed);
                sheet.Cells[row + 2, 9].Value = item.DataOffset;
                sheet.Cells[row + 2, 10].Value = item.ExternalFilePath ?? "-";
                sheet.Cells[row + 2, 11].Value = item.ContainerPath ?? "-";
                sheet.Cells[row + 2, 12].Value = item.InternalFileName;
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入验证汇总 Sheet
        /// </summary>
        private void WriteVerificationSummarySheet(ExcelPackage package, VerificationSummary verification)
        {
            var sheet = package.Workbook.Worksheets.Add("验证汇总");

            // 第一部分：数据类别汇总
            sheet.Cells[1, 1].Value = "数据类别汇总";
            sheet.Cells[1, 1].Style.Font.Bold = true;
            sheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            var headers = new[] { "数据类别", "压缩前总计", "压缩后总计", "占文件比例", "行数" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[2, col + 1].Value = headers[col];
                sheet.Cells[2, col + 1].Style.Font.Bold = true;
            }

            for (int row = 0; row < verification.CategoryTotals.Count; row++)
            {
                var cat = verification.CategoryTotals[row];
                sheet.Cells[row + 3, 1].Value = cat.CategoryName;
                sheet.Cells[row + 3, 2].Value = FormatSize(cat.SizeUncompressed);
                sheet.Cells[row + 3, 3].Value = FormatSize(cat.SizeCompressed);
                sheet.Cells[row + 3, 4].Value = FormatPercentage(cat.Percentage);
                sheet.Cells[row + 3, 5].Value = cat.ItemCount;
            }

            // 添加总计行
            int totalRow = verification.CategoryTotals.Count + 3;
            sheet.Cells[totalRow, 1].Value = "计算总计";
            sheet.Cells[totalRow, 1].Style.Font.Bold = true;
            sheet.Cells[totalRow, 2].Value = FormatSize(verification.TotalUncompressed);
            sheet.Cells[totalRow, 3].Value = FormatSize(verification.TotalCompressed);
            sheet.Cells[totalRow, 4].Value = FormatPercentage(100);

            // 第二部分：文件验证（空一行）
            int verifyStartRow = totalRow + 2;
            sheet.Cells[verifyStartRow, 1].Value = "文件验证";
            sheet.Cells[verifyStartRow, 1].Style.Font.Bold = true;
            sheet.Cells[verifyStartRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            sheet.Cells[verifyStartRow, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            sheet.Cells[verifyStartRow + 1, 1].Value = "文件实际大小";
            sheet.Cells[verifyStartRow + 1, 2].Value = FormatSize(verification.ErrorInfo.ActualFileSize);

            sheet.Cells[verifyStartRow + 2, 1].Value = "计算总计";
            sheet.Cells[verifyStartRow + 2, 2].Value = FormatSize(verification.ErrorInfo.CalculatedTotal);

            sheet.Cells[verifyStartRow + 3, 1].Value = "绝对误差";
            sheet.Cells[verifyStartRow + 3, 2].Value = FormatSize(verification.ErrorInfo.AbsoluteError);

            sheet.Cells[verifyStartRow + 4, 1].Value = "相对误差";
            sheet.Cells[verifyStartRow + 4, 2].Value = FormatPercentage(verification.ErrorInfo.RelativeErrorPercent);

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入类型汇总 Sheet
        /// </summary>
        private void WriteTypeSummarySheet(ExcelPackage package, List<TypeSummary> typeSummaries, long totalUncompressedSize)
        {
            var sheet = package.Workbook.Worksheets.Add("类型汇总");

            // 设置列标题
            var headers = new[] { "类型名称", "类型ID", "资源数量", "总大小（解压后）", "总大小（压缩后）", "平均大小", "最大资源名称", "最大资源大小", "占Bundle比例" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < typeSummaries.Count; row++)
            {
                var summary = typeSummaries[row];
                sheet.Cells[row + 2, 1].Value = summary.TypeName;
                sheet.Cells[row + 2, 2].Value = summary.TypeId;
                sheet.Cells[row + 2, 3].Value = summary.ResourceCount;
                sheet.Cells[row + 2, 4].Value = FormatSize(summary.TotalSizeUncompressed);
                sheet.Cells[row + 2, 5].Value = FormatSize(summary.TotalSizeCompressed);
                sheet.Cells[row + 2, 6].Value = FormatSize(summary.AverageSize);
                sheet.Cells[row + 2, 7].Value = summary.LargestResourceName;
                sheet.Cells[row + 2, 8].Value = FormatSize(summary.LargestSize);
                sheet.Cells[row + 2, 9].Value = FormatPercentage(summary.BundleRatio * 100);
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入数据块分布 Sheet
        /// </summary>
        private void WriteBlocksSheet(ExcelPackage package, List<BlockInfo> blocks)
        {
            var sheet = package.Workbook.Worksheets.Add("数据块分布");

            // 设置列标题
            var headers = new[] { "Block序号", "压缩类型", "压缩大小", "解压大小", "压缩率", "包含文件数", "包含的文件列表" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < blocks.Count; row++)
            {
                var block = blocks[row];
                sheet.Cells[row + 2, 1].Value = block.Index;
                sheet.Cells[row + 2, 2].Value = block.CompressionType;
                sheet.Cells[row + 2, 3].Value = FormatSize(block.SizeCompressed);
                sheet.Cells[row + 2, 4].Value = FormatSize(block.SizeUncompressed);
                sheet.Cells[row + 2, 5].Value = FormatPercentage(block.CompressionRatio * 100);
                sheet.Cells[row + 2, 6].Value = block.ContainedFiles.Count;
                sheet.Cells[row + 2, 7].Value = string.Join(", ", block.ContainedFiles);
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入 TypeTree 类型结构 Sheet
        /// </summary>
        private void WriteTypeTreesSheet(ExcelPackage package, List<TypeTreeInfo> typeTrees)
        {
            var sheet = package.Workbook.Worksheets.Add("TypeTree结构");

            // 设置列标题
            var headers = new[] { "类型ID", "类型名称", "是否剥离", "脚本类型索引", "所属内部文件", "节点数量" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < typeTrees.Count; row++)
            {
                var typeTree = typeTrees[row];
                sheet.Cells[row + 2, 1].Value = typeTree.TypeId;
                sheet.Cells[row + 2, 2].Value = typeTree.TypeName;
                sheet.Cells[row + 2, 3].Value = typeTree.IsStripped ? "是" : "否";
                sheet.Cells[row + 2, 4].Value = typeTree.ScriptTypeIndex.HasValue ? typeTree.ScriptTypeIndex.Value.ToString() : "-";
                sheet.Cells[row + 2, 5].Value = typeTree.InternalFileName;
                sheet.Cells[row + 2, 6].Value = typeTree.NodeCount;
            }

            AutoFitColumns(sheet);
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
            if (size >= 1024 * 1024 * 1024) // GB
            {
                return $"{size / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
            else if (size >= 1024 * 1024) // MB
            {
                return $"{size / (1024.0 * 1024.0):F2} MB";
            }
            else if (size >= 1024) // KB
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

        /// <summary>
        /// 格式化百分比（两个数值的比率）
        /// </summary>
        private string FormatPercentage(long compressed, long uncompressed)
        {
            if (uncompressed == 0) return "0%";
            double ratio = (double)compressed / uncompressed;
            return FormatPercentage(ratio * 100);
        }

        /// <summary>
        /// 写入 ScriptTypes 详情 Sheet
        /// </summary>
        private void WriteScriptTypesSheet(ExcelPackage package, List<ScriptTypeInfo> scriptTypes)
        {
            var sheet = package.Workbook.Worksheets.Add("ScriptTypes");

            // 设置列标题
            var headers = new[] { "所属文件", "本地文件索引", "本地标识符" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < scriptTypes.Count; row++)
            {
                var item = scriptTypes[row];
                sheet.Cells[row + 2, 1].Value = item.InternalFileName;
                sheet.Cells[row + 2, 2].Value = item.LocalSerializedFileIndex;
                sheet.Cells[row + 2, 3].Value = item.LocalIdentifierInFile;
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入 Externals 详情 Sheet
        /// </summary>
        private void WriteExternalsSheet(ExcelPackage package, List<ExternalDetailInfo> externals)
        {
            var sheet = package.Workbook.Worksheets.Add("Externals");

            // 设置列标题
            var headers = new[] { "所属文件", "GUID", "类型值", "类型名", "路径", "文件名" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < externals.Count; row++)
            {
                var item = externals[row];
                sheet.Cells[row + 2, 1].Value = item.InternalFileName;
                sheet.Cells[row + 2, 2].Value = item.Guid;
                sheet.Cells[row + 2, 3].Value = item.Type;
                sheet.Cells[row + 2, 4].Value = item.TypeName;
                sheet.Cells[row + 2, 5].Value = item.PathName;
                sheet.Cells[row + 2, 6].Value = item.FileName;
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入 RefTypes 详情 Sheet
        /// </summary>
        private void WriteRefTypesSheet(ExcelPackage package, List<RefTypeInfo> refTypes)
        {
            var sheet = package.Workbook.Worksheets.Add("RefTypes");

            // 设置列标题
            var headers = new[] { "所属文件", "类型ID", "是否剥离", "脚本索引", "类名", "命名空间", "程序集" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < refTypes.Count; row++)
            {
                var item = refTypes[row];
                sheet.Cells[row + 2, 1].Value = item.InternalFileName;
                sheet.Cells[row + 2, 2].Value = item.ClassID;
                sheet.Cells[row + 2, 3].Value = item.IsStrippedType ? "是" : "否";
                sheet.Cells[row + 2, 4].Value = item.ScriptTypeIndex;
                sheet.Cells[row + 2, 5].Value = item.KlassName;
                sheet.Cells[row + 2, 6].Value = item.NameSpace;
                sheet.Cells[row + 2, 7].Value = item.AsmName;
            }

            AutoFitColumns(sheet);
        }

        /// <summary>
        /// 写入 UserInformation 详情 Sheet
        /// </summary>
        private void WriteUserInformationSheet(ExcelPackage package, List<UserInformationInfo> userInformations)
        {
            var sheet = package.Workbook.Worksheets.Add("UserInformation");

            // 设置列标题
            var headers = new[] { "所属文件", "用户信息" };
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cells[1, col + 1].Value = headers[col];
                sheet.Cells[1, col + 1].Style.Font.Bold = true;
                sheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 写入数据行
            for (int row = 0; row < userInformations.Count; row++)
            {
                var item = userInformations[row];
                sheet.Cells[row + 2, 1].Value = item.InternalFileName;
                sheet.Cells[row + 2, 2].Value = item.UserInformation;
            }

            AutoFitColumns(sheet);
        }
    }
}