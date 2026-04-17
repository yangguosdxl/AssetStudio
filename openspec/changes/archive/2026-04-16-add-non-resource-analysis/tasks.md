## 1. 数据模型扩展

- [x] 1.1 创建 `NonResourceType` 枚举（BundleMeta, FileHeader, TypeTree, ObjectDir, Externals）
- [x] 1.2 创建 `DataCategory` 枚举（Resource, NonResource）
- [x] 1.3 扩展 `ResourceInfo` 类，添加 DataCategory 和 NonResourceType 字段
- [x] 1.4 创建 `CategoryTotal` 类用于验证汇总
- [x] 1.5 创建 `VerificationSummary` 类，包含 category_totals 和 error_info
- [x] 1.6 扩展 `AnalysisReport` 类，添加 NonResourceData 和 VerificationSummary 字段

## 2. Metadata 位置追踪器

- [x] 2.1 创建 `MetadataPositionTracker` 类基础结构
- [x] 2.2 实现 Bundle Header 大小计算方法
- [x] 2.3 实现 SerializedFile Header + Version/Platform 位置追踪
- [x] 2.4 实现 Types + TypeTree 位置追踪（含版本判断逻辑）
- [x] 2.5 实现 ObjectDir 位置追踪
- [x] 2.6 实现 Externals 位置追踪（ScriptTypes + Externals + RefTypes + UserInfo）
- [x] 2.7 实现 Metadata 总和验证（检查是否等于 m_DataOffset）
- [x] 2.8 实现获取 SerializedFile stream 的反射逻辑（或重新打开文件）

## 3. 非资源数据压缩大小计算

- [x] 3.1 扩展 `NodeBlockMapper` 支持计算任意范围的压缩大小
- [x] 3.2 实现 Bundle Header 压缩大小计算（不压缩，相等）
- [x] 3.3 实现 BlocksInfo 压缩大小计算（从 header 直接获取）
- [x] 3.4 实现 SerializedFile Metadata 各部分压缩大小计算（比例法）

## 4. BundleAnalyzer 扩展

- [x] 4.1 添加 `ParseNonResourceData` 方法生成非资源数据行
- [x] 4.2 修改 `BuildBundleSummary` 添加非资源数据统计
- [x] 4.3 实现 `BuildVerificationSummary` 方法生成验证汇总
- [x] 4.4 修改 `AnalyzeBundleFile` 调用新方法并合并数据

## 5. Excel 报告导出修改

- [x] 5.1 修改 `WriteResourcesSheet` 添加"数据类别"列
- [x] 5.2 修改 `WriteResourcesSheet` 合并非资源数据并按偏移排序
- [x] 5.3 实现 `WriteVerificationSummarySheet` 方法
- [x] 5.4 修改 `Export` 方法调用验证汇总 Sheet 生成

## 6. JSON 报告导出修改

- [x] 6.1 修改 `JsonReportExporter` 添加 non_resource_data 节点
- [x] 6.2 修改 `JsonReportExporter` 添加 verification_summary 节点
- [x] 6.3 确保 ResourceInfo 和 NonResourceData 的 JSON 序列化正确

## 7. 测试与验证

- [x] 7.1 编写单元测试验证 MetadataPositionTracker 各部分位置计算
- [x] 7.2 编写单元测试验证压缩大小计算正确性
- [x] 7.3 使用实际 AssetBundle 文件测试完整流程
- [x] 7.4 验证误差在合理范围内（通常 < 1%）
- [x] 7.5 对比新旧报告格式，确保向后兼容