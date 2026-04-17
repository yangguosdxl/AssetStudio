## ADDED Requirements

### Requirement: TypeTree nodeSize 修正

MetadataPositionTracker 的 `TrackTypeTreeBlob()` 方法 SHALL 使用正确的 nodeSize 计算：
- 基础版本（< TypeTreeNodeWithTypeFlags）：nodeSize = 24 bytes
- 新版本（>= TypeTreeNodeWithTypeFlags）：nodeSize = 32 bytes

#### Scenario: 基础版本 nodeSize 计算

- **WHEN** SerializedFile 版本 < TypeTreeNodeWithTypeFlags
- **THEN** TrackTypeTreeBlob SHALL 跳过 numberOfNodes × 24 bytes 的 node 数据

#### Scenario: 新版本 nodeSize 计算

- **WHEN** SerializedFile 版本 >= TypeTreeNodeWithTypeFlags
- **THEN** TrackTypeTreeBlob SHALL 跳过 numberOfNodes × 32 bytes 的 node 数据

### Requirement: TypeTree 精确大小追踪

分析报告 SHALL 使用 MetadataPositionTracker 精确追踪 TypeTree 大小，替代估算方案。

TypeTree 大小 SHALL 等于 MetadataPositionTracker.Parts 中 "Types" 条目的 Size 属性。

#### Scenario: 精确追踪替代估算

- **WHEN** BundleAnalyzer 计算 SerializedFile metadata 各部分大小
- **THEN** TypeTree 的 size_uncompressed SHALL 来自 MetadataPositionTracker 追踪结果，而非 EstimateTypesSize() 估算

#### Scenario: 反射失败降级处理

- **WHEN** 通过反射获取 SerializedFile.reader stream 失败
- **THEN** 系统 SHALL 降级到估算方案并输出警告日志

### Requirement: TypeTree blob 结构精确计算

TypeTree blob 大小 SHALL 按以下公式计算：

```
size = 4 (numberOfNodes) + 4 (stringBufferSize) + numberOfNodes × nodeSize + stringBufferSize
```

其中 nodeSize 按版本确定（24 或 32 bytes）。

#### Scenario: 多 TypeTree 累加

- **WHEN** SerializedFile 包含多个 SerializedType 且每个都有 TypeTree
- **THEN** Types 部分大小 SHALL 等于所有 SerializedType 基础字段 + 所有 TypeTree blob 的累加

### Requirement: 废弃估算函数

`EstimateTypesSize()` 函数 SHALL 标记为 `[Obsolete("Use MetadataPositionTracker instead")]`。

#### Scenario: 估算函数保留作为降级

- **WHEN** 精确追踪失败
- **THEN** 系统 SHALL 调用 EstimateTypesSize() 作为降级方案