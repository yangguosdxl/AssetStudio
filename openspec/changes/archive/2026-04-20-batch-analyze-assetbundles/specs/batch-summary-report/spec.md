## ADDED Requirements

### Requirement: 生成批量汇总报告

系统 SHALL 在批量分析完成后生成汇总报告，包含所有 AssetBundle 的关键指标。

#### Scenario: 生成误差率汇总 Sheet
- **WHEN** 批量分析目录完成
- **THEN** 系统创建名为"误差率汇总"的 Sheet
- **AND** Sheet 包含所有 AssetBundle 的信息：文件名、总大小、压缩大小、解压大小、误差率
- **AND** 数据按误差率降序排列
- **AND** 误差率超过 10% 的行标红显示

#### Scenario: 生成高误差分析 Sheet
- **WHEN** 存在误差率超过 10% 的 AssetBundle
- **THEN** 系统创建名为"高误差分析"的 Sheet
- **AND** Sheet 列出每个高误差 Bundle 的分析结果
- **AND** 每行包含：文件名、误差率、分析原因、置信度、详细信息

### Requirement: 分析误差原因

系统 SHALL 自动分析导致高误差率的常见原因。

#### Scenario: 检测外部资源缺失
- **WHEN** Bundle 包含 DataSource 为 "ExternalMissing" 的资源
- **THEN** 系统识别原因为"外部资源缺失"
- **AND** 系统计算缺失资源占 Bundle 的比例
- **AND** 置信度为"高"

#### Scenario: 检测压缩效率低
- **WHEN** Bundle 整体压缩率超过 90%
- **THEN** 系统识别原因为"压缩效率低"
- **AND** 系统列出各 Block 的压缩率
- **AND** 置信度为"中"

#### Scenario: 检测元数据膨胀
- **WHEN** 元数据区域（Header + BlocksInfo）占 Bundle 比例超过 30%
- **THEN** 系统识别原因为"元数据膨胀"
- **AND** 系统列出元数据各部分的大小
- **AND** 置信度为"高"

#### Scenario: 检测未识别数据
- **WHEN** 非资源数据区域占比超过 20%
- **THEN** 系统识别原因为"未识别数据"
- **AND** 系统列出未识别数据的偏移范围
- **AND** 置信度为"中"

#### Scenario: 多原因组合分析
- **WHEN** Bundle 存在多个高误差原因
- **THEN** 系统按影响程度排序列出所有原因
- **AND** 系统标注主要原因为"首要原因"

### Requirement: CLI 汇总模式支持

系统 SHALL 通过命令行参数启用汇总报告模式。

#### Scenario: 启用汇总报告
- **WHEN** 用户使用 `-s` 或 `--summary` 参数
- **THEN** 系统在批量分析完成后生成汇总报告
- **AND** 汇总报告文件名为 `summary_report.xlsx`
- **AND** 汇总报告保存在输出目录中

#### Scenario: 指定汇总报告输出路径
- **WHEN** 用户使用 `-s` 参数并指定输出目录 `-o <path>`
- **THEN** 系统将汇总报告和各 Bundle 报告都保存到指定目录
