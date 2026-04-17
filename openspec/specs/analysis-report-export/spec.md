# analysis-report-export Specification

## Purpose
TBD - created by archiving change ab-analyzer-cli. Update Purpose after archive.
## Requirements
### Requirement: 导出分析报告为 JSON

系统 SHALL 将分析结果导出为结构化 JSON 格式。

#### Scenario: 生成 JSON 报告文件
- **WHEN** 分析完成
- **THEN** 系统创建包含 AnalysisReport 结构的 JSON 文件
- **AND** JSON 文件包含 BundleSummary、InternalFiles、Resources、TypeSummaries、Blocks、TypeTrees 数组
- **AND** JSON 输出文件使用 snake_case 属性名

#### Scenario: JSON 类型库独立
- **WHEN** 其他工具需要反序列化分析报告
- **THEN** 可引用 AssetStudio.Analyzer.Contracts 类库
- **AND** Contracts 类库包含所有数据模型类
- **AND** Contracts 类库目标为 netstandard2.0 以保证兼容性

### Requirement: 导出分析报告为 Excel

系统 SHALL 将分析结果导出为多 Sheet 的 xlsx 格式。

#### Scenario: 生成 Excel 报告文件
- **WHEN** 分析完成
- **THEN** 系统使用 EPPlus 库创建 xlsx 文件
- **AND** xlsx 文件包含 6 个 Sheet：Bundle概览、内部文件、资源明细、类型汇总、数据块分布、TypeTree结构

#### Scenario: Bundle 概览 Sheet
- **WHEN** Excel 报告生成
- **THEN** Bundle概览 Sheet 包含一行数据：FileName、TotalSizeCompressed、TotalSizeUncompressed、HeaderSize、BlocksInfoSizeCompressed、BlocksInfoSizeUncompressed、DataBlocksSizeCompressed、DataBlocksSizeUncompressed、CompressionType、UnityVersion、InternalFileCount、ResourceCount

#### Scenario: 内部文件 Sheet
- **WHEN** Excel 报告生成
- **THEN** 内部文件 Sheet 每个内部文件一行
- **AND** 列包含：文件名、大小（解压后）、文件类型、偏移、元数据大小、数据区大小、资源数量、类型数量

#### Scenario: 资源明细 Sheet
- **WHEN** Excel 报告生成
- **THEN** 资源明细 Sheet 每个资源一行
- **AND** 列包含：资源名称、类型名称、类型ID、PathID、数据来源、大小（解压后）、大小（压缩后）、数据偏移、外部文件路径、Container路径、所属内部文件

#### Scenario: 类型汇总 Sheet
- **WHEN** Excel 报告生成
- **THEN** 类型汇总 Sheet 每个 ClassIDType 一行
- **AND** 列包含：类型名称、类型ID、资源数量、总大小（解压后）、总大小（压缩后）、平均大小、最大资源名称、最大大小、占Bundle比例

#### Scenario: 数据块分布 Sheet
- **WHEN** Excel 报告生成
- **THEN** 数据块分布 Sheet 每个 StorageBlock 一行
- **AND** 列包含：Block序号、压缩类型、压缩大小、解压大小、压缩率、包含文件列表

#### Scenario: TypeTree结构 Sheet
- **WHEN** Excel 报告生成
- **THEN** TypeTree结构 Sheet 每个 TypeTree 类型一行
- **AND** 列包含：类型ID、类型名称、是否剥离、脚本类型索引、所属内部文件、节点数量

### Requirement: 使用中文列标题

系统 SHALL 在 Excel 输出中使用中文列标题以提高可读性。

#### Scenario: Excel 列标题为中文
- **WHEN** Excel 报告生成
- **THEN** 所有列标题使用中文文本
- **AND** JSON 属性名保持英文以便程序访问

### Requirement: 同时输出两种格式

系统 SHALL 为每次分析同时输出 JSON 和 xlsx 文件。

#### Scenario: 双格式输出
- **WHEN** 分析完成
- **THEN** 系统写入 {bundle名}_analysis.json
- **AND** 系统写入 {bundle名}_analysis.xlsx
- **AND** 两个文件包含等效数据

### Requirement: CLI 输出路径指定

系统 SHALL 允许用户通过命令行指定输出目录。

#### Scenario: 指定输出目录
- **WHEN** 用户提供 -o 参数和目录路径
- **THEN** 系统将报告文件写入指定目录
- **AND** 系统在目录不存在时创建目录

#### Scenario: 默认输出目录
- **WHEN** 用户未提供 -o 参数
- **THEN** 系统将报告文件写入输入文件所在目录

