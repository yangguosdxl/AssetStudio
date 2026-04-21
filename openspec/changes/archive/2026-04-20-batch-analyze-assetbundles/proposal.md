## Why

当前 AssetStudio.CLI.Analyzer 支持批量分析目录中的多个 AssetBundle，并为每个文件生成独立的报告。但缺乏汇总功能，无法快速定位问题 AssetBundle（如误差率异常、压缩效率低下等）。需要新增批量汇总报告功能，自动分析所有 AssetBundle 的误差率并识别异常原因。

## What Changes

- 新增批量分析模式：分析完成后自动生成汇总报告（Excel 格式）
- 新增汇总报告功能：按误差率降序排列所有 AssetBundle，对误差超过 10% 的进行分析
- 新增误差原因自动分析：识别导致高误差率的常见原因（如外部资源缺失、压缩效率低、元数据膨胀等）
- **BREAKING**: 批量分析时输出路径行为变更，现在需要明确的汇总报告输出路径参数

## Capabilities

### New Capabilities
- `batch-summary-report`: 批量分析汇总报告生成功能，包括误差率排序、异常检测和原因分析

### Modified Capabilities
- `analysis-report-export`: 扩展现有的报告导出能力，新增汇总报告 Sheet 结构和误差分析逻辑

## Impact

- **新增文件**:
  - `BatchSummaryExporter.cs` - 汇总报告导出器
  - `VarianceAnalyzer.cs` - 误差原因分析器
- **修改文件**:
  - `Program.cs` - 添加批量汇总模式和命令行参数
  - `BundleAnalyzer.cs` - 添加汇总数据收集接口
  - `ExcelReportExporter.cs` - 支持汇总报告 Sheet
- **依赖**:
  - 现有 EPPlus 库（Excel 导出）
  - 现有分析数据模型
