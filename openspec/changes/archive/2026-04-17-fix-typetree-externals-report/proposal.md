## Why

当前 AssetStudio.CLI.Analyzer 报告中 TypeTree 与 Externals 的数据存在严重错误：
1. TypeTree 大小估算偏低（假设每个 type 100 bytes，实际 TypeTree blob 包含大量 nodes，每个 node 24-32 bytes）
2. Externals 大小通过减法计算，因为 TypeTree 估算偏低，导致大量 TypeTree 数据被错误归类到 Externals
3. Externals 定义模糊，实际包含 ScriptTypes + FileIdentifier + RefTypes + UserInformation 四个独立部分
4. MetadataPositionTracker.cs 中 nodeSize 计算有 bug（使用 20/28，实际应为 24/32）

修正后用户将获得准确的 metadata 各部分大小报告，便于分析 AssetBundle 文件结构。

## What Changes

- **修复** `MetadataPositionTracker.cs` 中 TypeTree nodeSize 计算错误（20→24, 28→32）
- **修改** `BundleAnalyzer.cs` 使用精确追踪替代估算方案
- **拆分** Externals 为四个细分部分：ScriptTypes、FileIdentifier、RefTypes、UserInformation
- **新增** NonResourceType 枚举值：ScriptTypes、RefTypes、UserInformation
- **删除** 或标记为废弃的估算函数：`EstimateTypesSize()`
- **追加** 文档：`docs/AssetBundle文件布局.md` 中 Externals 结构详解章节

## Capabilities

### New Capabilities

- `externals-breakdown`: Externals 细分报告功能，将原来的 Externals 拆分为 ScriptTypes、FileIdentifier、RefTypes、UserInformation 四个独立部分
- `typetree-precise-sizing`: TypeTree 精确大小计算功能，使用 MetadataPositionTracker 曽代估算

### Modified Capabilities

- 无（这是修复报告数据错误，不改变 API 或用户需求）

## Impact

### 代码影响

| 文件 | 修改类型 |
|------|----------|
| `AssetStudio.CLI.Analyzer/MetadataPositionTracker.cs` | 修复 bug，拆分 Externals 追踪 |
| `AssetStudio.CLI.Analyzer/BundleAnalyzer.cs` | 使用精确追踪替代估算 |
| `AssetStudio.Analyzer.Contracts/NonResourceType.cs` | 新增枚举值 |
| `docs/AssetBundle文件布局.md` | 追加章节 |

### 数据影响

- 报告中 TypeTree 大小将显著增加（从估算值变为真实值）
- 报告中 Externals 大小将显著减少（移除错误归类的 TypeTree 数据）
- 新增细分数据源：SerializedFileScriptTypes、SerializedFileFileIdentifier、SerializedFileRefTypes、SerializedFileUserInformation

### 兼容性

- 报告 JSON 格式向后兼容（新增字段，不删除现有字段）
- 现有消费报告的工具需要适配新的细分数据源字段