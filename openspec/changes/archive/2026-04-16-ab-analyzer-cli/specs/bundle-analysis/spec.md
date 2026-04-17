# Bundle 分析规格说明

## ADDED Requirements

### Requirement: 解析 AssetBundle 结构

系统 SHALL 解析 AssetBundle 文件结构，包括 Header、BlocksInfo 和 DataBlocks。

#### Scenario: 解析 UnityFS 格式 Bundle
- **WHEN** 用户提供 UnityFS 格式的 AssetBundle 文件
- **THEN** 系统提取 Header 字段（signature、version、unityVersion、size、flags）
- **AND** 系统提取 StorageBlock 数组，包含压缩前后大小
- **AND** 系统提取 Node 数组，包含 offset、size、path

#### Scenario: 解析 UnityWeb 格式 Bundle
- **WHEN** 用户提供 UnityWeb 格式的 AssetBundle 文件
- **THEN** 系统提取旧版格式 Header 和 blocks
- **AND** 系统解压 LZMA 压缩的数据块

### Requirement: 计算 Block 累积偏移

系统 SHALL 计算 StorageBlock 的累积偏移表，用于位置映射。

#### Scenario: 构建 Block 偏移表
- **WHEN** StorageBlock 数组已解析
- **THEN** 系统计算 BlockStart[i] = Σ(uncompressedSize[0..i-1])
- **AND** 系统计算 BlockEnd[i] = BlockStart[i] + uncompressedSize[i]
- **AND** 系统存储偏移表供后续映射使用

### Requirement: 映射 Node 到 StorageBlock

系统 SHALL 将每个内部文件（Node）映射到其所属的 StorageBlock，并计算压缩贡献。

#### Scenario: 计算 Node 压缩大小
- **WHEN** Node 在 blocksStream 中有 offset 和 size
- **THEN** 系统识别所有与 Node 范围相交的 Block
- **AND** 系统计算每个 Block 的 intersectSize
- **AND** 系统计算 ratio = intersectSize / Block.uncompressedSize
- **AND** 系统计算 contribution = Block.compressedSize × ratio
- **AND** 系统汇总所有贡献得到 Node 压缩大小

### Requirement: 解析 SerializedFile 结构

系统 SHALL 解析 SerializedFile (.assets) 结构，包括 header、types、objects 和 externals。

#### Scenario: 解析 SerializedFile header
- **WHEN** 内部文件被识别为 SerializedFile
- **THEN** 系统提取 m_MetadataSize、m_FileSize、m_Version、m_DataOffset
- **AND** 系统计算元数据区域大小和数据区域大小

#### Scenario: 解析 ObjectInfo 数组
- **WHEN** SerializedFile 元数据已解析
- **THEN** 系统提取所有 ObjectInfo 条目
- **AND** 每个条目包含 byteStart、byteSize、typeID、classID、m_PathID

### Requirement: 映射资源到 StorageBlock

系统 SHALL 使用 Object → Node → Block 映射链计算每个资源的压缩大小。

#### Scenario: 计算嵌入资源压缩大小
- **WHEN** 资源在 SerializedFile 中有 byteStart 和 byteSize
- **THEN** 系统计算 absoluteStart = Node.offset + byteStart
- **AND** 系统计算 absoluteEnd = absoluteStart + byteSize
- **AND** 系统使用 Block 映射计算压缩大小

#### Scenario: 计算外部资源压缩大小
- **WHEN** 资源是 Texture2D 且有 StreamingInfo
- **THEN** 系统查找 .resource 文件对应的 Node
- **AND** 系统计算 absoluteStart = Node.offset + StreamingInfo.offset
- **AND** 系统计算 absoluteSize = StreamingInfo.size
- **AND** 系统使用 Block 映射计算压缩大小

#### Scenario: 处理外部文件缺失
- **WHEN** 外部 .resource 文件未找到
- **THEN** 系统标记 DataSource 为 "ExternalMissing"
- **AND** 系统记录元数据中声明的大小
- **AND** 系统将压缩大小设为 0 并发出警告

### Requirement: 支持批量分析

系统 SHALL 支持分析目录中的多个 AssetBundle 文件。

#### Scenario: 分析目录中的 Bundle
- **WHEN** 用户提供目录路径
- **THEN** 系统查找目录中所有 AssetBundle 文件
- **AND** 系统依次分析每个文件
- **AND** 系统为每个文件生成独立报告

### Requirement: 解析 TypeTree 类型结构

系统 SHALL 解析 SerializedFile 中的 TypeTree 类型结构信息。

#### Scenario: 提取 TypeTree 节点
- **WHEN** SerializedType 包含 TypeTree 数据
- **THEN** 系统提取每个 TypeTreeNode 的类型、名称、层级、大小等信息
- **AND** 系统按类型组织 TypeTree 结构数据