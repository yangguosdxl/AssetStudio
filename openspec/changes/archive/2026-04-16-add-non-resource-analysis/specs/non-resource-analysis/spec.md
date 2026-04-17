# non-resource-analysis Specification

## Purpose

追踪 AssetBundle 中除资源对象外的所有元数据大小，包括 Bundle Header、BlocksInfo、SerializedFile Metadata 各部分的位置和大小，为文件大小验证提供完整数据。

## Requirements

### Requirement: 解析 Bundle Header 大小

系统 SHALL 计算 AssetBundle 文件 Header 的精确大小。

#### Scenario: 计算 UnityFS Header 大小
- **WHEN** AssetBundle 使用 UnityFS 格式
- **THEN** 系统计算 Header 大小包括：signature + null、version (4 bytes)、unityVersion + null、unityRevision + null、size (8 bytes)、compressedBlocksInfoSize (4 bytes)、uncompressedBlocksInfoSize (4 bytes)、flags (4 bytes)
- **AND** Header 大小不压缩，压缩前后大小相等

#### Scenario: 计算 UnityWeb Header 大小
- **WHEN** AssetBundle 使用 UnityWeb 格式
- **THEN** 系统根据版本解析旧版 Header 结构
- **AND** Header 大小不压缩

### Requirement: 计算 BlocksInfo Metadata 大小

系统 SHALL 计算 BlocksInfo Metadata 的压缩前后大小。

#### Scenario: BlocksInfo 大小已知
- **WHEN** Bundle Header 已解析
- **THEN** 系统从 header.compressedBlocksInfoSize 获取压缩后大小
- **AND** 系统从 header.uncompressedBlocksInfoSize 获取压缩前大小

### Requirement: 解析 SerializedFile Metadata 各部分大小

系统 SHALL 解析每个 SerializedFile 的 Metadata 区域各部分的位置和大小。

#### Scenario: 追踪 Header 部分
- **WHEN** SerializedFile 开始解析
- **THEN** 系统记录 Header + Version/Platform 部分的起始和结束位置
- **AND** 系统计算该部分大小 = 结束位置 - 赫起始位置

#### Scenario: 追踪 Types 部分
- **WHEN** Types 数组解析完成
- **THEN** 系统记录 Types 部分的起始和结束位置
- **AND** Types 部分包含所有 SerializedType 及其 TypeTree 数据
- **AND** 系统计算该部分大小 = 结束位置 - 起始位置

#### Scenario: 追踪 ObjectDir 部分
- **WHEN** Objects 数组解析完成
- **THEN** 系统记录 ObjectDir 部分的起始和结束位置
- **AND** ObjectDir 部分包含所有 ObjectInfo 条目
- **AND** 系统计算该部分大小 = 结束位置 - 起始位置

#### Scenario: 追踪 Externals 部分
- **WHEN** ScriptTypes、Externals、RefTypes、UserInfo 解析完成
- **THEN** 系统记录 Externals 部分的起始和结束位置
- **AND** Externals 部分包含 ScriptTypes + Externals + RefTypes + UserInfo
- **AND** 系统计算该部分大小 = 结束位置 - 起始位置

#### Scenario: 验证 Metadata 各部分总和
- **WHEN** 所有 Metadata 部分追踪完成
- **THEN** 系统验证 Header + Types + ObjectDir + Externals ≈ m_DataOffset
- **AND** 若误差超过阈值，系统记录警告

### Requirement: 计算非资源数据压缩大小

系统 SHALL 使用 Block 比例法计算非资源数据的压缩后大小。

#### Scenario: 使用 NodeBlockMapper 计算压缩大小
- **WHEN** 非资源数据部分的位置和大小已知
- **THEN** 系统计算在 blocksStream 中的绝对位置 = Node.offset + 部分起始位置
- **AND** 系统调用 NodeBlockMapper.CalculateCompressedSize() 计算压缩贡献
- **AND** 系统汇总所有 Block 贡献得到压缩后大小

### Requirement: 区分非资源数据类型

系统 SHALL 为每种非资源数据分配类型标识。

#### Scenario: BundleMeta 类型
- **WHEN** 数据属于 Bundle Header 或 BlocksInfo
- **THEN** 系统标记类型为 BundleMeta
- **AND** BundleMeta 数据属于 Bundle 级别，不关联特定 SerializedFile

#### Scenario: FileHeader 类型
- **WHEN** 数据属于 SerializedFile Header + Version/Platform
- **THEN** 系统标记类型为 FileHeader
- **AND** 关联所属 SerializedFile 文件名

#### Scenario: TypeTree 类型
- **WHEN** 数据属于 SerializedFile 的 Types[] + TypeTree
- **THEN** 系统标记类型为 TypeTree
- **AND** 关联所属 SerializedFile 文件名

#### Scenario: ObjectDir 类型
- **WHEN** 数据属于 SerializedFile 的 Objects[] 目录
- **THEN** 系统标记类型为 ObjectDir
- **AND** 关联所属 SerializedFile 文件名

#### Scenario: Externals 类型
- **WHEN** 数据属于 ScriptTypes + Externals + RefTypes + UserInfo
- **THEN** 系统标记类型为 Externals
- **AND** 关联所属 SerializedFile 文件名