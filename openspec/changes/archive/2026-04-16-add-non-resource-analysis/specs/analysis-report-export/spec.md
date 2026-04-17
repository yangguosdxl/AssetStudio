## ADDED Requirements

### Requirement: 资源明细包含非资源数据

系统 SHALL 在资源明细中包含非资源数据行。

#### Scenario: 非资源数据行
- **WHEN** Excel 资源明细 Sheet 生成
- **THEN** Sheet 包含非资源数据行
- **AND** 非资源数据行包含：Bundle Header、BlocksInfo、各 SerializedFile 的 Metadata 部分
- **AND** 非资源数据行标记数据类别为"非资源"

#### Scenario: 数据类别列
- **WHEN** 资源明细 Sheet 生成
- **THEN** 新增"数据类别"列
- **AND** 列值为"资源"或"非资源"
- **AND** 列位于"资源名称"之后

#### Scenario: 按偏移排序
- **WHEN** 所有数据行生成完成
- **THEN** 系统按 DataOffset 升序排序所有行
- **AND** Bundle 级非资源数据（偏移最小）在最前
- **AND** SerializedFile 级非资源数据在对应的资源数据之前

### Requirement: 新增验证汇总 Sheet

系统 SHALL 在 Excel 报告中新增验证汇总 Sheet。

#### Scenario: 验证汇总 Sheet 结构
- **WHEN** Excel 报告生成
- **THEN** 系统创建"验证汇总" Sheet
- **AND** Sheet 位于所有现有 Sheet 之后
- **AND** Sheet 包含各数据类别的大小汇总

#### Scenario: 数据类别汇总行
- **WHEN** 验证汇总 Sheet 生成
- **THEN** Sheet 包含以下类别汇总行：
  - BundleMeta（Bundle Header + BlocksInfo）
  - FileHeader（所有 SerializedFile Header）
  - TypeTree（所有 Types + TypeTree）
  - ObjectDir（所有 Objects 目录）
  - Externals（所有 Externals 部分）
  - 资源数据（所有资源对象）
- **AND** 每行显示：压缩前总计、压缩后总计、占文件比例

#### Scenario: 总计与误差行
- **WHEN** 验证汇总 Sheet 生成
- **THEN** Sheet 包含计算总计行
- **AND** Sheet 包含文件实际大小行
- **AND** Sheet 包含误差行（绝对误差和相对误差）

### Requirement: JSON 报告包含非资源数据和验证汇总

系统 SHALL 在 JSON 报告中添加非资源数据和验证汇总节点。

#### Scenario: non_resource_data 节点
- **WHEN** JSON 报告生成
- **THEN** 添加 non_resource_data 数组节点
- **AND** 数组包含所有非资源数据行
- **AND** 每行包含：名称、类型、压缩前后大小、偏移、所属文件

#### Scenario: verification_summary 节点
- **WHEN** JSON 报告生成
- **THEN** 添加 verification_summary 对象节点
- **AND** 对象包含 category_totals 数组和 error_info 对象

## MODIFIED Requirements

### Requirement: 导出分析报告为 Excel

系统 SHALL 将分析结果导出为多 Sheet 的 xlsx 格式。

#### Scenario: 生成 Excel 报告文件
- **WHEN** 分析完成
- **THEN** 系统使用 EPPlus 库创建 xlsx 文件
- **AND** xlsx 文件包含 7 个 Sheet：Bundle概览、内部文件、资源明细、类型汇总、数据块分布、TypeTree结构、验证汇总

#### Scenario: 资源明细 Sheet
- **WHEN** Excel 报告生成
- **THEN** 资源明细 Sheet 每个数据项一行（含非资源数据）
- **AND** 列包含：资源名称、数据类别、类型名称、类型ID、PathID、数据来源、大小（解压后）、大小（压缩后）、数据偏移、外部文件路径、Container路径、所属内部文件
- **AND** 所有行按数据偏移升序排序

### Requirement: 导出分析报告为 JSON

系统 SHALL 将分析结果导出为结构化 JSON 格式。

#### Scenario: 生成 JSON 报告文件
- **WHEN** 分析完成
- **THEN** 系统创建包含 AnalysisReport 结构的 JSON 文件
- **AND** JSON 文件包含 BundleSummary、InternalFiles、Resources、NonResourceData、TypeSummaries、Blocks、TypeTrees、VerificationSummary 节点
- **AND** JSON 输出文件使用 snake_case 属性名