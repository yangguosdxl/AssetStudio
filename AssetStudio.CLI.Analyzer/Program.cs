using System;
using System.Collections.Generic;
using System.IO;

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
            if (string.IsNullOrEmpty(options.InputPath))
            {
                Console.WriteLine("错误: 必须指定输入文件或目录路径");
                ShowHelp();
                return 1;
            }

            // 执行分析
            try
            {
                var analyzer = new BundleAnalyzer(options.Verbose, options.ShowExternals);
                var exporter = new ExcelReportExporter();
                var jsonExporter = new JsonReportExporter();

                if (Directory.Exists(options.InputPath))
                {
                    // 批量目录分析
                    var reports = analyzer.AnalyzeDirectory(options.InputPath);
                    foreach (var kvp in reports)
                    {
                        var bundlePath = kvp.Key;
                        var report = kvp.Value;

                        var outputDir = options.OutputPath ?? Path.GetDirectoryName(bundlePath);
                        Directory.CreateDirectory(outputDir);

                        var baseName = Path.GetFileNameWithoutExtension(bundlePath);
                        var xlsxPath = Path.Combine(outputDir, $"{baseName}_analysis.xlsx");
                        var jsonPath = Path.Combine(outputDir, $"{baseName}_analysis.json");

                        exporter.Export(report, xlsxPath);
                        jsonExporter.Export(report, jsonPath);

                        Console.WriteLine($"[完成] {bundlePath}");
                        if (options.Verbose)
                        {
                            Console.WriteLine($"  → {xlsxPath}");
                            Console.WriteLine($"  → {jsonPath}");
                        }
                    }
                    Console.WriteLine($"批量分析完成: {reports.Count} 个文件");
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
        /// 命令行选项
        /// </summary>
        class CliOptions
        {
            public string InputPath { get; set; } = "";
            public string? OutputPath { get; set; }
            public bool Verbose { get; set; }
            public bool ShowExternals { get; set; }
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
        /// 显示帮助信息
        /// </summary>
        static void ShowHelp()
        {
            Console.WriteLine("AssetStudio.CLI.Analyzer - AssetBundle 分析工具");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  AssetStudio.CLI.Analyzer <input> [-o <output>] [-v]");
            Console.WriteLine();
            Console.WriteLine("参数:");
            Console.WriteLine("  <input>       输入文件或目录路径");
            Console.WriteLine("  -o, --output  输出目录（默认：输入文件所在目录）");
            Console.WriteLine("  -v, --verbose 详细输出模式");
            Console.WriteLine("  -e, --externals 输出 Externals 详情 Sheet（ScriptTypes、Externals、RefTypes、UserInformation）");
            Console.WriteLine("  -h, --help    显示帮助信息");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  AssetStudio.CLI.Analyzer game.bundle");
            Console.WriteLine("  AssetStudio.CLI.Analyzer ./bundles/ -o ./reports/");
            Console.WriteLine("  AssetStudio.CLI.Analyzer game.bundle -v");
            Console.WriteLine();
            Console.WriteLine("输出:");
            Console.WriteLine("  {bundle名}_analysis.xlsx  - Excel 报告（含中文列标题）");
            Console.WriteLine("  {bundle名}_analysis.json  - JSON 报告（英文字段名）");
        }
    }
}