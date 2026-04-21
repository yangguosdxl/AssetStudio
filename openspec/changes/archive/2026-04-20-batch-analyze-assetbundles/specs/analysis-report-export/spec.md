## MODIFIED Requirements

### Requirement: 导出分析报告为 Excel

系统 SHALL 将分析结果导出为多 Sheet 的 xlsx 格式。

#### Scenario: 生成 Excel 报告文件
- **WHEN** 分析完成
- **THEN** 系统使用 EPPlus 库创建 xlsx 文件
- **AND** xlsx 文件包含 7 个 Sheet：Bundle概览、内部文件、资源明细、类型汇总、数据块分布、TypeTree结构、验证汇总
- **AND** 验证汇总 Sheet 包含数据类别汇总和文件验证信息

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
- **AND** 列包含：资源名称、数据类别、类型名称、类型ID、PathID、数据来源、大小（解压后）、大小（压缩后）、数据偏移、外部文件路径、Container路径、所属内部文件

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

#### Scenario: 验证汇总 Sheet
- **WHEN** Excel 报告生成
- **THEN** 验证汇总 Sheet 包含数据类别汇总表
- **AND** 验证汇总 Sheet 包含文件验证表（实际大小、计算总计、绝对误差、相对误差）

### Requirement: 导出汇总报告为 Excel

系统 SHALL 将批量分析结果导出为汇总 xlsx 格式。

#### Scenario: 生成汇总 Excel 报告文件
- **WHEN** 批量分析完成且启用汇总模式
- **THEN** 系统创建名为 summary_report.xlsx 的文件
- **AND** 文件包含两个 Sheet：误差率汇总、高误差分析

#### Scenario: 误差率汇总 Sheet
- **WHEN** 汇总报告生成
- **THEN** 误差率汇总 Sheet 每个 AssetBundle 一行
- **AND** 列包含：文件名、文件路径、总大小、压缩大小、解压大小、压缩率、误差率、状态
- **AND** 数据按误差率降序排列
- **AND** 误差率超过 10% 的行以红色背景高亮

#### Scenario: 高误差分析 Sheet
- **WHEN** 汇总报告生成且存在高误差 Bundle
- **THEN** 高误差分析 Sheet 每个高误差 Bundle 一行或多行
- **AND** 列包含：文件名、误差率、分析原因、影响比例、置信度、详细信息
