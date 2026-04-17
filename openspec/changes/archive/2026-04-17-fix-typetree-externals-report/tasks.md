## 1. 修复 MetadataPositionTracker nodeSize bug

- [x] 1.1 修改 `TrackTypeTreeBlob()` 方法中的 nodeSize 计算：基础版本从 20 改为 24 bytes
- [x] 1.2 修改 `TrackTypeTreeBlob()` 方法中的 nodeSize 计算：新版本（>= TypeTreeNodeWithTypeFlags）从 28 改为 32 bytes
- [ ] 1.3 添加单元测试验证 nodeSize 计算正确性

## 2. 扩展 NonResourceType 枚举

- [x] 2.1 在 `AssetStudio.Analyzer.Contracts/NonResourceType.cs` 中新增 `ScriptTypes = 5`
- [x] 2.2 在 `AssetStudio.Analyzer.Contracts/NonResourceType.cs` 中新增 `RefTypes = 6`
- [x] 2.3 在 `AssetStudio.Analyzer.Contracts/NonResourceType.cs` 中新增 `UserInformation = 7`

## 3. 拆分 Externals 追踪为四个独立部分

- [x] 3.1 修改 `MetadataPositionTracker.TrackExternals()` 方法，拆分为四个独立的 `_parts.Add()` 调用
- [x] 3.2 创建 `TrackScriptTypes()` 私有方法，单独追踪 ScriptTypes 部分
- [x] 3.3 创建 `TrackFileIdentifier()` 私有方法，单独追踪 FileIdentifier 部分
- [x] 3.4 创建 `TrackRefTypes()` 私有方法，单独追踪 RefTypes 部分
- [x] 3.5 创建 `TrackUserInformation()` 私有方法，单独追踪 UserInformation 部分
- [x] 3.6 添加汇总的 "Externals" 条目（四个部分累加）保持向后兼容

## 4. 修改 BundleAnalyzer 使用精确追踪

- [x] 4.1 在 `CalculateSerializedFileMetadataParts()` 中使用 `MetadataPositionTracker.GetSerializedFileStream()` 获取 stream
- [x] 4.2 创建 `MetadataPositionTracker` 实例并调用 `Track()` 获取精确位置
- [x] 4.3 根据追踪结果 `tracker.Parts` 生成各部分的 `ResourceInfo`
- [x] 4.4 添加降级逻辑：反射失败时输出警告并使用估算方案
- [x] 4.5 标记 `EstimateTypesSize()` 为 `[Obsolete("Use MetadataPositionTracker instead")]`

## 5. 更新文档

- [x] 5.1 在 `docs/AssetBundle文件布局.md` 中追加 "SerializedFile Externals 结构详解" 章节
- [x] 5.2 编写 Externals 四个组成部分的结构图（ASCII 图）
- [x] 5.3 编写各部分大小计算公式表格
- [x] 5.4 编写常见大小分布表格及注意事项

## 6. 测试验证

- [x] 6.1 使用测试文件 `test/uf6c4d88c.dat` 重新生成分析报告
- [x] 6.2 对比新旧报告数据：验证 TypeTree 大小显著增加、Externals 大小显著减少
- [x] 6.3 验证四个细分条目正确生成：ScriptTypes、FileIdentifier、RefTypes、UserInformation
- [x] 6.4 验证原有 "Externals" 汇总条目大小正确（四个部分累加）

**验证结果**：
- CAB-c568c0a80ba27163251a9d1339205632:
  - TypeTree: 旧 2,214 bytes → 新 160,718 bytes ✓
  - Externals: 旧 158,508 bytes → 新 4 bytes ✓
- CAB-c568c0a80ba27163251a9d1339205632.sharedAssets:
  - TypeTree: 3,534 bytes ✓
  - Externals: 4 bytes ✓