## 1. 数据模型扩展

- [x] 1.1 在 `NonResourceType` 枚举中新增 `ResourceGap` 值
- [x] 1.2 在 `DataSourceType` 枚举中新增 `BundleResourceShared` 值

## 2. BundleResource 重复计算修正

- [x] 2.1 修改 `BundleAnalyzer.ParseResources` 方法：解析完所有资源后，调用 `FixDuplicateBundleResources` 修正多个 BundleResource 对同一 .resS 的重复声明
- [x] 2.2 `FixDuplicateBundleResources`：按 ExternalFilePath 分组，仅第一个保留 .resS 大小，后续设为 BundleResourceShared（SizeUncompressed=0, SizeCompressed=0）

## 3. .resS 间隙追踪

- [x] 3.1 在 `BundleAnalyzer` 中新增 `CalculateResSGaps` 方法：对每个 .resS Node，收集所有引用它的 ExternalTexture/Audio 的 [offset, offset+size) 区间
- [x] 3.2 实现区间合并算法 `CalculateIntervalGaps`：将重叠区间合并，计算差集
- [x] 3.3 处理 BundleResource + ExternalTexture 共存的重叠：标记 ExternalTexture 为 "ExternalInBundleResS"，在验证汇总中扣除其大小
- [x] 3.4 将间隙区间转换为 ResourceGap 类型的 ResourceInfo，通过 NodeBlockMapper 计算压缩大小
- [x] 3.5 在 `BuildVerificationSummary` 中调用 `CalculateResSGaps`，将结果加入 NonResourceData

## 4. SerializedFile 数据区间隙追踪

- [x] 4.1 在 `BundleAnalyzer` 中新增 `CalculateSFGaps` 方法：对每个 SerializedFile，计算数据区大小 = node.size - m_DataOffset
- [x] 4.2 收集该 SF 中所有 Embedded 资源的 byteSize + ExternalTexture/Audio 在 SF 中的 ObjectInfo.byteSize
- [x] 4.3 若数据区大小 > 资源 byteSize 之和，将差值作为 ResourceGap 计入
- [x] 4.4 通过 NodeBlockMapper 计算间隙的压缩大小
- [x] 4.5 在 `BuildVerificationSummary` 中调用 `CalculateSFGaps`

## 5. VerificationSummary 更新

- [x] 5.1 在 `BuildVerificationSummary` 中新增 ResourceGap 类别行到 CategoryTotals
- [x] 5.2 扣除 ExternalInBundleResS 的重叠大小（从资源数据压缩大小中减去）
- [x] 5.3 确保 ResourceGap 的压缩大小被包含在 TotalCompressed 和 TotalUncompressed 中

## 6. 验证与测试

- [x] 6.1 构建项目，确保编译通过
- [x] 6.2 对 test/高误差文件.txt 中的 942 个文件重新运行分析
- [x] 6.3 验证之前正常的文件无回归
- [x] 6.4 结果：941/942 (99.89%) 误差 <= 10%，1 个文件 (pb3736d3d) = 11.02%（SF 格式固有限制）