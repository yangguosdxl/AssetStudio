## Context

当前 AssetStudio.CLI.Analyzer 报告系统存在以下问题：

### 问题根源

1. **TypeTree 大小估算偏低**
   - `EstimateTypesSize()` 函数假设每个 type 平均 100 bytes
   - 实际 TypeTree blob 结构：`numberOfNodes(4) + stringBufferSize(4) + nodes × nodeSize + stringBuffer`
   - 每个 node 大小：24 bytes（基础）或 32 bytes（含 RefTypeHash）
   - 测试文件示例：4734 nodes × 32 = 151,488 bytes，估算仅 2,214 bytes

2. **Externals 大小计算错误**
   - 当前通过减法：`externalsSize = m_DataOffset - headerSize - typesSize - objectDirSize`
   - 因为 TypeTree 估算偏低，剩余部分包含了大量被错误归类的 TypeTree 数据
   - 测试文件示例：报告显示 Externals 158,508 bytes，实际 Externals 仅约 1KB

3. **MetadataPositionTracker.cs nodeSize bug**
   - `TrackTypeTreeBlob()` 使用 nodeSize = 20/28
   - 实际应为 24/32（相差 4 bytes）

### 现有解决方案

`MetadataPositionTracker.cs` 已存在，提供了精确追踪 metadata 各部分位置的功能，但有 bug 且未被使用。

### 约束

- SerializedFile 版本差异（3.4 - 2022.3）
- TypeTree 有两种格式：Blob（新版本）和 Legacy（旧版本）
- 反射获取 SerializedFile.reader stream

## Goals / Non-Goals

**Goals:**

- 修复 TypeTree nodeSize 计算错误
- 使用精确追踪替代估算
- 拆分 Externals 为四个细分部分
- 提供准确的 metadata 各部分大小报告

**Non-Goals:**

- 不修改 SerializedFile 解析逻辑
- 不改变 AssetBundle 文件读取流程
- 不优化性能（保持现有效率）

## Decisions

### Decision 1: 使用 MetadataPositionTracker 替代估算

**方案对比:**

| 方案 | 优点 | 缺点 |
|------|------|------|
| A: 修复估算函数 | 简单快速 | 无法处理动态内容（字符串长度） |
| B: 使用 MetadataPositionTracker | 精确、完整 | 需要修复 bug、增加反射依赖 |
| C: 重写追踪逻辑 | 完全控制 | 重复代码、维护成本高 |

**选择**: 方案 B - 使用 MetadataPositionTracker

**理由**:
- MetadataPositionTracker 已存在，只需修复 bug
- 精确追踪能处理所有动态内容
- 反射依赖已在现有代码中使用

### Decision 2: 拆分 Externals 的粒度

**方案对比:**

| 方案 | 描述 | 报告字段数 |
|------|------|------------|
| A: 保持单一 Externals | 不拆分 | 1 |
| B: 拆分为两部分 | ScriptTypes + Others | 2 |
| C: 拆分为四部分 | ScriptTypes、FileIdentifier、RefTypes、UserInfo | 4 |

**选择**: 方案 C - 拆分为四部分

**理由**:
- 四个部分各有独立的结构和大小特征
- 用户可以精确了解各部分占用
- 符合 SerializedFile 原始结构定义

### Decision 3: NonResourceType 枚举扩展

**选择**: 新增 ScriptTypes、RefTypes、UserInformation

**理由**:
- 与拆分方案匹配
- 保持语义一致性
- 兼容现有报告格式

## Risks / Trade-offs

### Risk 1: 反射获取 stream 失败

**影响**: 无法使用精确追踪，报告数据不准确

**缓解措施**:
- 保留估算代码作为降级方案
- 添加 fallback 日志提示用户

### Risk 2: 旧版本 SerializedFile 格式差异

**影响**: 追踪结果不准确（版本 < 12 使用 Legacy TypeTree）

**缓解措施**:
- MetadataPositionTracker 已处理版本差异
- 测试覆盖多个版本

### Risk 3: 报告消费者兼容性

**影响**: 消费报告的工具需适配新字段

**缓解措施**:
- 保持现有 Externals 字段（不删除）
- 新增细分字段作为补充
- 文档说明字段变化

## Migration Plan

### 部署步骤

1. 修复 MetadataPositionTracker.cs nodeSize bug
2. 新增 NonResourceType 枚举值
3. 修改 TrackExternals() 拆分为四部分
4. 修改 BundleAnalyzer.cs 使用 MetadataPositionTracker
5. 更新文档

### 回滚策略

- 保留估算函数代码（标记 `[Obsolete]`）
- 如果反射失败，自动降级到估算方案