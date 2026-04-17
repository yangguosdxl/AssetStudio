# externals-breakdown Specification

## Purpose
TBD - created by archiving change fix-typetree-externals-report. Update Purpose after archive.
## Requirements
### Requirement: Externals 拆分报告功能

分析报告 SHALL 将 SerializedFile 的 Externals 部分拆分为四个独立的报告条目：ScriptTypes、FileIdentifier、RefTypes、UserInformation。

每个条目 SHALL 包含以下字段：
- `name`: 条目名称（如 "CAB-xxx ScriptTypes"）
- `data_source`: 数据来源标识（SerializedFileScriptTypes、SerializedFileFileIdentifier、SerializedFileRefTypes、SerializedFileUserInformation）
- `size_uncompressed`: 解压后大小（精确值）
- `size_compressed`: 压缩后大小（比例法估算）
- `data_offset`: 数据起始偏移

#### Scenario: ScriptTypes 报告条目生成

- **WHEN** SerializedFile 版本 >= HasScriptTypeIndex 且包含 ScriptTypes 数据
- **THEN** 报告 SHALL 包含一个 ScriptTypes 条目，其 size_uncompressed 等于 ScriptTypes 部分的精确大小

#### Scenario: FileIdentifier 报告条目生成

- **WHEN** SerializedFile 包含 Externals (FileIdentifier[]) 数据
- **THEN** 报告 SHALL 包含一个 FileIdentifier 条目，其 size_uncompressed 等于所有 FileIdentifier 结构的总大小

#### Scenario: RefTypes 报告条目生成

- **WHEN** SerializedFile 版本 >= SupportsRefObject 且包含 RefTypes 数据
- **THEN** 报告 SHALL 包含一个 RefTypes 条目，其 size_uncompressed 等于 RefTypes 部分的精确大小（含 TypeTree）

#### Scenario: UserInformation 报告条目生成

- **WHEN** SerializedFile 版本 >= Unknown_5 且包含 UserInformation 字符串
- **THEN** 报告 SHALL 包含一个 UserInformation 条目，其 size_uncompressed 等于字符串长度加 null terminator

#### Scenario: 空部分处理

- **WHEN** 某个部分数据为空（如 RefTypes count = 0）
- **THEN** 报告 SHALL 生成大小为 4 bytes（仅 count 字段）的条目

### Requirement: 保持原有 Externals 条目兼容性

报告 SHALL 继续包含原有的 "Externals" 汇总条目，其大小等于四个细分部分的累加，以保持向后兼容。

#### Scenario: Externals 汇总条目

- **WHEN** 报告生成
- **THEN** 报告 SHALL 包含一个 "Externals" 条目，其 size_uncompressed = ScriptTypes + FileIdentifier + RefTypes + UserInformation 的总和

### Requirement: NonResourceType 枚举扩展

NonResourceType 枚举 SHALL 新增以下值：
- `ScriptTypes = 5`
- `RefTypes = 6`
- `UserInformation = 7`

#### Scenario: 枚举值映射

- **WHEN** 报告生成 ScriptTypes 条目
- **THEN** 条目的 `non_resource_type` 字段 SHALL 为 5

