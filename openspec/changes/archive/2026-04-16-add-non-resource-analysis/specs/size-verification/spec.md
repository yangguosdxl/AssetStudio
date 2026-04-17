# size-verification Specification

## Purpose

对比计算总和与文件实际大小，验证 AssetBundle 数据完整性，显示误差信息帮助开发者理解数据分布。

## Requirements

### Requirement: 按数据类别汇总大小

系统 SHALL 按数据类别汇总压缩前后大小。

#### Scenario: 汇总 Bundle 级非资源数据
- **WHEN** 验证汇总生成
- **THEN** 系统汇总 Bundle Header 和 BlocksInfo 的压缩前后大小
- **AND** Bundle 级数据标记为 BundleMeta 类别

#### Scenario: 汇总 SerializedFile 级非资源数据
- **WHEN** 验证汇总生成
- **THEN** 系统按类别汇总所有 SerializedFile 的 Metadata 数据
- **AND** 类别包括：FileHeader、TypeTree、ObjectDir、Externals
- **AND** 每个类别显示所有 SerializedFile 的总和

#### Scenario: 汇总资源数据
- **WHEN** 验证汇总生成
- **THEN** 系统汇总所有资源对象的压缩前后大小
- **AND** 资源数据标记为"资源数据"类别

### Requirement: 计算总计与误差

系统 SHALL 计算所有数据的总和并与文件实际大小对比。

#### Scenario: 计算压缩后总计
- **WHEN** 所有数据类别已汇总
- **THEN** 系统计算压缩后总计 = Σ(所有类别压缩后大小)
- **AND** 压缩后总计应接近文件实际大小

#### Scenario: 计算压缩前总计
- **WHEN** 所有数据类别已汇总
- **THEN** 系统计算压缩前总计 = Σ(所有类别压缩前大小)
- **AND** 压缩前总计应接近 blocksStream 总大小

#### Scenario: 计算绝对误差
- **WHEN** 压缩后总计已计算
- **THEN** 系统获取文件实际大小（文件系统）
- **AND** 系统计算绝对误差 = |压缩后总计 - 文件实际大小|

#### Scenario: 计算相对误差
- **WHEN** 绝对误差已计算
- **THEN** 系统计算相对误差 = 绝对误差 / 文件实际大小 × 100%
- **AND** 相对误差以百分比显示

### Requirement: 显示验证汇总

系统 SHALL 在报告中显示验证汇总信息。

#### Scenario: Excel 验证汇总 Sheet
- **WHEN** Excel 报告生成
- **THEN** 系统创建"验证汇总" Sheet
- **AND** Sheet 包含各数据类别的压缩前后大小和占比
- **AND** Sheet 包含计算总计、文件实际大小、误差信息

#### Scenario: JSON 验证汇总节点
- **WHEN** JSON 报告生成
- **THEN** 系统添加 verification_summary 节点
- **AND** 节点包含 category_totals 数组和 error_info 对象

### Requirement: 按偏移混合排序数据

系统 SHALL 将非资源数据与资源数据按偏移混合排序。

#### Scenario: 混合排序显示
- **WHEN** 生成资源明细列表
- **THEN** 系统将非资源数据和资源数据合并
- **AND** 系统按 DataOffset（偏移）升序排序
- **AND** Bundle 级数据（偏移最小）在最前，资源数据在后

#### Scenario: 数据类别列
- **WHEN** 显示数据行
- **THEN** 系统添加"数据类别"列
- **AND** 非资源数据显示"非资源"
- **AND** 资源数据显示"资源"