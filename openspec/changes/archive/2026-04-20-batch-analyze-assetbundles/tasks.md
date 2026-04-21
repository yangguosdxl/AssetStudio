## 1. 数据模型

- [x] 1.1 创建 `BundleSummaryItem` 类，包含文件名、路径、大小、误差率等字段
- [x] 1.2 创建 `VarianceCause` 枚举，定义误差原因类型（ExternalMissing, LowCompression, MetadataBloat, UnidentifiedData）
- [x] 1.3 创建 `VarianceAnalysisResult` 类，包含原因类型、影响比例、置信度、详细信息

## 2. 误差分析器

- [x] 2.1 创建 `VarianceAnalyzer.cs`，实现误差原因分析逻辑
- [x] 2.2 实现 `AnalyzeExternalMissing()` 方法，检测外部资源缺失
- [x] 2.3 实现 `AnalyzeLowCompression()` 方法，检测压缩效率低
- [x] 2.4 实现 `AnalyzeMetadataBloat()` 方法，检测元数据膨胀
- [x] 2.5 实现 `AnalyzeUnidentifiedData()` 方法，检测未识别数据
- [x] 2.6 实现 `Analyze()` 主方法，汇总所有原因并排序

## 3. 汇总导出器

- [x] 3.1 创建 `BatchSummaryExporter.cs`
- [x] 3.2 实现 `WriteSummarySheet()` 方法，生成误差率汇总 Sheet
- [x] 3.3 实现 `WriteAnalysisSheet()` 方法，生成高误差分析 Sheet
- [x] 3.4 实现 `Export()` 主方法，整合两个 Sheet 输出
- [x] 3.5 实现误差率超 10% 行的红色高亮

## 4. CLI 集成

- [x] 4.1 修改 `CliOptions` 类，添加 `Summary` 布尔属性
- [x] 4.2 修改 `ParseArgs()` 方法，解析 `-s` / `--summary` 参数
- [x] 4.3 修改 `Main()` 方法，批量分析后调用汇总导出
- [x] 4.4 更新 `ShowHelp()` 方法，添加汇总参数说明

## 5. 测试与验证

- [x] 5.1 使用目标目录 `C:\Users\Administrator\Downloads\assetbundles.qa` 进行完整测试
- [x] 5.2 验证汇总报告 Excel 格式正确
- [x] 5.3 验证高误差分析原因准确
- [x] 5.4 验证误差率排序正确