## ADDED Requirements

### Requirement: 计算并追踪 .resS 文件未归属数据
系统 SHALL 在 `BuildVerificationSummary` 阶段，对每个 .resS 类型的内部文件，计算其未被任何 ExternalTexture / ExternalAudio / BundleResource 资源引用的数据区域，将其归类为 ResourceGap。

#### Scenario: .resS 完全无资源引用
- **WHEN** bundle 包含 .resS 内部文件且无任何资源通过 m_StreamData / m_Source 引用它
- **THEN** 系统 SHALL 将整个 .resS 的解压大小作为 ResourceGap 计入 NonResourceData，并通过 NodeBlockMapper 计算其压缩大小

#### Scenario: .resS 部分被 ExternalTexture 覆盖
- **WHEN** ExternalTexture 的 offset+size 仅覆盖 .resS Node 的部分区域
- **THEN** 系统 SHALL 计算 .resS 中未被覆盖的间隙，将间隙数据作为 ResourceGap 计入

#### Scenario: 无 .resS 的 SerializedFile 数据区间隙
- **WHEN** SerializedFile 的数据区（m_DataOffset 到 m_FileSize）中，所有 Embedded 资源的 byteSize 之和小于数据区大小
- **THEN** 系统 SHALL 将差值作为 ResourceGap 计入，归属到对应 SerializedFile

### Requirement: ResourceGap 在验证汇总中展示
系统 SHALL 在 VerificationSummary 的 CategoryTotals 中新增 "ResourceGap" 类别行，显示未归属数据的解压大小、压缩大小和占比。

#### Scenario: 汇总报告包含 ResourceGap
- **WHEN** 分析的 bundle 存在 ResourceGap 数据
- **THEN** CategoryTotals SHALL 包含 category_name="ResourceGap" 的条目，且其大小被包含在 TotalCompressed 和 TotalUncompressed 中

#### Scenario: 无 ResourceGap 时不显示
- **WHEN** 分析的 bundle 无 ResourceGap 数据
- **THEN** CategoryTotals 中 SHALL NOT 包含 ResourceGap 条目

### Requirement: 间隙计算不影响已有资源数据
系统 SHALL NOT 修改已有资源的 SizeUncompressed、SizeCompressed 或 BlockContributions。间隙数据作为独立的 NonResourceData 条目添加。

#### Scenario: 已有资源数据保持不变
- **WHEN** 对一个已有正常报告（误差 < 10%）的 bundle 重新分析
- **THEN** 所有已有资源的 SizeUncompressed、SizeCompressed SHALL 保持原值不变
