## Why

AssetBundle 文件大小验证需要完整的数据统计。当前分析报告仅统计资源数据（Mesh、Texture2D 等），缺少非资源数据（Bundle Header、BlocksInfo、SerializedFile Metadata 等）的大小信息，导致无法验证所有数据加起来是否与文件实际大小相等。开发者无法精确追踪 AssetBundle 尺寸变化的根本原因。

## What Changes

- 在资源明细中添加非资源数据行，与资源数据按偏移混合排序显示
- 非资源数据按类型拆分为多行：
  - Bundle Header（Bundle 级，不压缩）
  - BlocksInfo Metadata（Bundle 级，压缩存储）
  - File Header（每个 SerializedFile 的头部信息）
  - Types + TypeTree（每个 SerializedFile 的类型定义，汇总为一行）
  - ObjectDir（每个 SerializedFile 的 Objects 数组目录）
  - Externals（ScriptTypes + Externals + RefTypes + UserInfo）
- 新增"数据类别"列区分"资源"/"非资源"
- 新增"验证汇总" Sheet，显示：
  - 各数据类别的压缩前后大小汇总
  - 计算总计与文件实际大小的对比
  - 绝对误差和相对误差百分比
- JSON 报告同步添加非资源数据和验证汇总节点

## Capabilities

### New Capabilities

- `non-resource-analysis`: 非资源数据分析能力，追踪 AssetBundle 中除资源对象外的所有元数据大小，包括 Bundle Header、BlocksInfo、SerializedFile Metadata 各部分的位置和大小
- `size-verification`: 文件大小验证能力，对比计算总和与文件实际大小，显示误差信息

### Modified Capabilities

- `analysis-report-export`: 扩展报告导出格式，新增非资源数据行和验证汇总 Sheet

## Impact

### 代码变更

| 文件 | 变更类型 | 说明 |
|------|---------|------|
| `AssetStudio.Analyzer.Contracts/ResourceInfo.cs` | 修改 | 添加 DataCategory 字段 |
| `AssetStudio.Analyzer.Contracts/NonResourceDataCategory.cs` | 新增 | 非资源数据类型枚举 |
| `AssetStudio.Analyzer.Contracts/VerificationSummary.cs` | 新增 | 验证汇总数据模型 |
| `AssetStudio.Analyzer.Contracts/AnalysisReport.cs` | 修改 | 添加 VerificationSummary 字段 |
| `AssetStudio.CLI.Analyzer/MetadataPositionTracker.cs` | 新增 | Metadata 位置追踪器 |
| `AssetStudio.CLI.Analyzer/BundleAnalyzer.cs` | 修改 | 生成非资源数据行 |
| `AssetStudio.CLI.Analyzer/ExcelReportExporter.cs` | 修改 | 添加"数据类别"列和"验证汇总" Sheet |
| `AssetStudio.CLI.Analyzer/JsonReportExporter.cs` | 修改 | 添加 non_resource_data 和 verification_summary 节点 |

### 输出格式变更

**Excel 报告**：
- 资源明细 Sheet 新增"数据类别"列
- 新增"验证汇总" Sheet

**JSON 报告**：
- 新增 `non_resource_data` 数组节点
- 新增 `verification_summary` 节点