## Why

当前 `AssetStudio.CLI.Analyzer` 对 1095 个高误差 AssetBundle 文件的分析中，**942 个文件**的误差超过 10%。根因分析表明三大系统性误差源导致资源汇总与文件原始大小不一致：
1. **.resS 文件数据未被计入**（605 个文件）：Mesh、Cubemap 等类型的顶点/像素数据存放在 .resS 中，但没有被任何资源通过 `m_StreamData` 或 `m_Source` 引用，导致整块数据缺失
2. **ExternalTexture 部分覆盖**（288 个文件）：纹理的 `offset+size` 未覆盖 .resS 全部数据，存在间隙
3. **BundleResource 重复计算**（5 个文件）：多个 Texture2D 被标记为 `BundleResource` 时，每个都声称拥有整个 .resS，导致膨胀

需要修正资源会计逻辑，确保所有字节数据都被正确归属，使误差率降至 10% 以下。

## What Changes

- **新增 .resS 间隙追踪**：在 `BuildVerificationSummary` 中，计算 .resS 文件未被任何资源引用的区域，将其归类为"资源间隙"（ResourceGap）
- **修正 BundleResource 重复计算**：当多个资源标记为 `BundleResource` 时，按比例分摊 .resS 大小，而非每个资源都计算整个 .resS
- **修正 ExternalTexture 间隙追踪**：当 ExternalTexture 的 `offset+size` 未覆盖对应 Node 的全部数据时，将未覆盖部分归为"资源间隙"
- **新增数据区间隙追踪**：计算 SerializedFile 中数据区（data area）被所有 Embedded 资源 byteSize 之和未覆盖的区域
- **更新 VerificationSummary**：新增 `ResourceGap` 类别，将所有未归属数据纳入计算

## Capabilities

### New Capabilities
- `resource-gap-tracking`: 追踪和归属 .resS 文件及 SerializedFile 数据区中未被任何资源引用的间隙数据

### Modified Capabilities
- `bundle-resource-allocation`: 修正多个 BundleResource 纹理对 .resS 大小的重复分配逻辑

## Impact

- **BundleAnalyzer.cs**：修改 `BuildVerificationSummary` 方法，新增间隙计算逻辑
- **ResourceBlockMapper.cs**：修改 `CalculateBundleResourceCompressedSize`，支持多 BundleResource 按比例分配
- **AssetStudio.Analyzer.Contracts**：`CategoryTotal` 和 `NonResourceType` 可能需要新增 `ResourceGap` 类型
- **BatchSummaryExporter.cs**：汇总报告中新增 ResourceGap 列显示
