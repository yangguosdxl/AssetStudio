using AssetStudio;
using AssetStudio.Analyzer.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// AssetBundle 分析 CLI 入口
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            // 解析命令行参数
            var options = ParseArgs(args);

            // 显示帮助
            if (options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            // 验证输入参数
            bool hasInput = !string.IsNullOrEmpty(options.InputPath);
            bool hasFileList = !string.IsNullOrEmpty(options.FileListPath);
            if (!hasInput && !hasFileList)
            {
                Console.WriteLine("错误: 必须指定输入文件/目录路径或文件列表 (-l)");
                ShowHelp();
                return 1;
            }

            // 执行分析
            try
            {
                var analyzer = new BundleAnalyzer(options.Verbose, options.ShowExternals);
                var exporter = new ExcelReportExporter();
                var jsonExporter = new JsonReportExporter();

                if (hasFileList)
                {
                    // 文件列表批量分析模式
                    return RunFileListAnalysis(options, analyzer, exporter, jsonExporter);
                }
                else if (Directory.Exists(options.InputPath))
                {
                    // 批量目录分析 - 边分析边导出，避免崩溃丢失数据
                    var reports = new Dictionary<string, AnalysisReport>();

                    // 查找所有可能的 AssetBundle 文件
                    var files = Directory.GetFiles(options.InputPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsBundleFile(f, options.Verbose))
                        .ToList();

                    Console.WriteLine($"[批量分析] 找到 {files.Count} 个 AssetBundle 文件");
                    Console.WriteLine($"[开始分析] {DateTime.Now:HH:mm:ss}");

                    int successCount = 0;
                    int failCount = 0;
                    int current = 0;

                    foreach (var bundlePath in files)
                    {
                        current++;
                        try
                        {
                            // 显示进度
                            if (current % 100 == 0 || current == files.Count)
                            {
                                Console.WriteLine($"[进度] {current}/{files.Count} ({(double)current/files.Count*100:F1}%)");
                            }

                            var report = analyzer.AnalyzeBundleFile(bundlePath);
                            reports[bundlePath] = report;

                            var outputDir = options.OutputPath ?? Path.GetDirectoryName(bundlePath);
                            Directory.CreateDirectory(outputDir);

                            var baseName = Path.GetFileNameWithoutExtension(bundlePath);
                            var xlsxPath = Path.Combine(outputDir, $"{baseName}_analysis.xlsx");
                            var jsonPath = Path.Combine(outputDir, $"{baseName}_analysis.json");

                            exporter.Export(report, xlsxPath);
                            jsonExporter.Export(report, jsonPath);

                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            Console.WriteLine($"[错误] {bundlePath}: {ex.Message}");
                        }
                    }

                    Console.WriteLine($"批量分析完成: 成功 {successCount}, 失败 {failCount}");
                    Console.WriteLine($"[结束时间] {DateTime.Now:HH:mm:ss}");

                    // 生成汇总报告
                    if (options.Summary && reports.Count > 0)
                    {
                        var summaryOutputDir = options.OutputPath ?? options.InputPath;
                        Directory.CreateDirectory(summaryOutputDir);
                        var summaryPath = Path.Combine(summaryOutputDir, "summary_report.xlsx");

                        var summaryExporter = new BatchSummaryExporter();
                        summaryExporter.Export(reports, summaryPath);

                        Console.WriteLine($"[汇总报告] {summaryPath}");
                    }
                }
                else if (File.Exists(options.InputPath))
                {
                    // 单文件分析
                    var report = analyzer.AnalyzeBundleFile(options.InputPath);

                    var outputDir = options.OutputPath ?? Path.GetDirectoryName(options.InputPath);
                    Directory.CreateDirectory(outputDir);

                    var baseName = Path.GetFileNameWithoutExtension(options.InputPath);
                    var xlsxPath = Path.Combine(outputDir, $"{baseName}_analysis.xlsx");
                    var jsonPath = Path.Combine(outputDir, $"{baseName}_analysis.json");

                    exporter.Export(report, xlsxPath);
                    jsonExporter.Export(report, jsonPath);

                    Console.WriteLine($"[完成] 分析报告已生成:");
                    Console.WriteLine($"  → {xlsxPath}");
                    Console.WriteLine($"  → {jsonPath}");
                }
                else
                {
                    Console.WriteLine($"错误: 输入路径不存在: {options.InputPath}");
                    return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                if (options.Verbose)
                {
                    Console.WriteLine($"详情: {ex.StackTrace}");
                }
                return 1;
            }
        }

        /// <summary>
        /// 从文件列表批量分析
        /// </summary>
        private static int RunFileListAnalysis(
            CliOptions options,
            BundleAnalyzer analyzer,
            ExcelReportExporter exporter,
            JsonReportExporter jsonExporter)
        {
            // 读取文件列表
            if (!File.Exists(options.FileListPath))
            {
                Console.WriteLine($"错误: 文件列表不存在: {options.FileListPath}");
                return 1;
            }

            var lines = File.ReadAllLines(options.FileListPath)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                .Select(l => l.Trim())
                .ToList();

            Console.WriteLine($"[文件列表] 共 {lines.Count} 个文件路径");
            Console.WriteLine($"[开始分析] {DateTime.Now:HH:mm:ss}");

            var outputDir = options.OutputPath ?? ".";
            Directory.CreateDirectory(outputDir);

            var reports = new Dictionary<string, AnalysisReport>();
            int successCount = 0;
            int failCount = 0;
            int skipCount = 0;
            int current = 0;

            foreach (var bundlePath in lines)
            {
                current++;

                // 检查文件是否存在
                if (!File.Exists(bundlePath))
                {
                    skipCount++;
                    continue;
                }

                try
                {
                    // 显示进度
                    if (current % 100 == 0 || current == lines.Count)
                    {
                        Console.WriteLine($"[进度] {current}/{lines.Count} ({(double)current/lines.Count*100:F1}%) 成功={successCount} 失败={failCount} 跳过={skipCount}");
                    }

                    var report = analyzer.AnalyzeBundleFile(bundlePath);
                    reports[bundlePath] = report;

                    var baseName = Path.GetFileNameWithoutExtension(bundlePath);
                    var xlsxPath = Path.Combine(outputDir, $"{baseName}_analysis.xlsx");
                    var jsonPath = Path.Combine(outputDir, $"{baseName}_analysis.json");

                    exporter.Export(report, xlsxPath);
                    jsonExporter.Export(report, jsonPath);

                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    Console.WriteLine($"[错误] {bundlePath}: {ex.Message}");
                }
            }

            Console.WriteLine($"批量分析完成: 成功={successCount}, 失败={failCount}, 跳过={skipCount}");
            Console.WriteLine($"[结束时间] {DateTime.Now:HH:mm:ss}");

            // 生成汇总报告
            if (options.Summary && reports.Count > 0)
            {
                var summaryPath = Path.Combine(outputDir, "summary_report.xlsx");
                var summaryExporter = new BatchSummaryExporter();
                summaryExporter.Export(reports, summaryPath);
                Console.WriteLine($"[汇总报告] {summaryPath}");
            }

            return 0;
        }

        /// <summary>
        /// 命令行选项
        /// </summary>
        class CliOptions
        {
            public string InputPath { get; set; } = "";
            public string? OutputPath { get; set; }
            public string? FileListPath { get; set; }
            public bool Verbose { get; set; }
            public bool ShowExternals { get; set; }
            public bool Summary { get; set; }
            public bool ShowHelp { get; set; }
        }

        /// <summary>
        /// 解析命令行参数
        /// </summary>
        static CliOptions ParseArgs(string[] args)
        {
            var options = new CliOptions();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "-h" || arg == "--help")
                {
                    options.ShowHelp = true;
                }
                else if (arg == "-v" || arg == "--verbose")
                {
                    options.Verbose = true;
                }
                else if (arg == "-e" || arg == "--externals")
                {
                    options.ShowExternals = true;
                }
                else if (arg == "-s" || arg == "--summary")
                {
                    options.Summary = true;
                }
                else if (arg == "-l" || arg == "--list")
                {
                    if (i + 1 < args.Length)
                    {
                        options.FileListPath = args[i + 1];
                        i++;
                    }
                }
                else if (arg == "-o" || arg == "--output")
                {
                    if (i + 1 < args.Length)
                    {
                        options.OutputPath = args[i + 1];
                        i++;
                    }
                }
                else if (!arg.StartsWith("-"))
                {
                    options.InputPath = arg;
                }
            }

            return options;
        }

        /// <summary>
        /// 判断文件是否为 AssetBundle 文件
        /// </summary>
        static bool IsBundleFile(string filePath, bool verbose = false)
        {
            try
            {
                using var reader = new FileReader(filePath);
                return reader.FileType == FileType.BundleFile;
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    Console.WriteLine($"[跳过] {filePath}: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        static void ShowHelp()
        {
            Console.WriteLine("AssetStudio.CLI.Analyzer - AssetBundle 分析工具");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  AssetStudio.CLI.Analyzer <input> [-o <output>] [-v] [-s]");
            Console.WriteLine("  AssetStudio.CLI.Analyzer -l <filelist> [-o <output>] [-s]");
            Console.WriteLine();
            Console.WriteLine("参数:");
            Console.WriteLine("  <input>       输入文件或目录路径");
            Console.WriteLine("  -l, --list    文件列表路径（每行一个 AssetBundle 绝对路径）");
            Console.WriteLine("  -o, --output  输出目录（默认：输入文件所在目录）");
            Console.WriteLine("  -v, --verbose 详细输出模式");
            Console.WriteLine("  -e, --externals 输出 Externals 详情 Sheet");
            Console.WriteLine("  -s, --summary 批量分析时生成汇总报告（误差率排序+高误差分析）");
            Console.WriteLine("  -h, --help    显示帮助信息");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  AssetStudio.CLI.Analyzer game.bundle");
            Console.WriteLine("  AssetStudio.CLI.Analyzer ./bundles/ -o ./reports/ -s");
            Console.WriteLine("  AssetStudio.CLI.Analyzer -l filelist.txt -o ./report2/ -s");
            Console.WriteLine();
            Console.WriteLine("输出:");
            Console.WriteLine("  {bundle名}_analysis.xlsx  - Excel 报告（含中文列标题）");
            Console.WriteLine("  {bundle名}_analysis.json  - JSON 报告（英文字段名）");
            Console.WriteLine("  summary_report.xlsx       - 汇总报告（使用 -s 参数时生成）");
        }
    }
}
