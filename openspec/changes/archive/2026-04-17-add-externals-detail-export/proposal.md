## Why

当前 Externals 相关数据（ScriptTypes、Externals、RefTypes、UserInformation）只在 Excel 报告的"验证汇总"中显示总大小统计，用户无法查看具体内容详情。这些数据对于理解 AssetBundle 间的依赖关系和跨 Bundle 引用至关重要。

## What Changes

- 新增命令行开关 `-e/--externals`，控制是否输出 Externals 详情
- 新增四个数据模型类：`ScriptTypeInfo`、`ExternalDetailInfo`、`RefTypeInfo`、`UserInformationInfo`
- 修改 `BundleAnalyzer` 添加数据收集逻辑
- 修改 `ExcelReportExporter` 新增四个 Sheet 写入方法
- 修改 `JsonReportExporter` 同步输出新字段
- 更新 `AnalysisReport` 数据结构

新增的四个 Excel Sheet：
- **ScriptTypes**：脚本类型引用列表
- **Externals**：外部文件依赖列表
- **RefTypes**：外部 Assembly 类型引用
- **UserInformation**：用户自定义信息

## Capabilities

### New Capabilities

- `externals-detail-export`: 导出 SerializedFile 的 Externals 四部分详情数据到 Excel/JSON 报告

### Modified Capabilities

无（这是新增功能，不修改现有 spec 的需求）

## Impact

**代码影响**：
- `AssetStudio.Analyzer.Contracts/` - 新增数据模型
- `AssetStudio.CLI.Analyzer/Program.cs` - 命令行参数处理
- `AssetStudio.CLI.Analyzer/BundleAnalyzer.cs` - 数据收集逻辑
- `AssetStudio.CLI.Analyzer/ExcelReportExporter.cs` - Sheet 输出
- `AssetStudio.CLI.Analyzer/JsonReportExporter.cs` - JSON 输出

**依赖**：
- 使用已有的 `SerializedFile.m_ScriptTypes`、`m_Externals`、`m_RefTypes`、`userInformation` 字段
- 无新增外部依赖

**向后兼容**：
- 默认不输出（开关关闭），不影响现有行为
- 新增字段在 JSON 输出中为可选（空数组时存在但为空）