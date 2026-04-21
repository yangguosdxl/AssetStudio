## Why

分析 `d1497d267.dat` 时，验证汇总显示资源压缩总计仅 1,690 字节，而实际文件大小 1,358,257 字节，误差达 99.88%。

**深入诊断结果**：

该 bundle 包含一个 2048x2048 ASTC_RGB_5x5 格式的 Texture2D（完整 mipmap chain = 3,589,680 字节），其数据存储在同 bundle 内的 `.resS` 文件中。但 Unity 2022.3 中该纹理的 `m_StreamData` 所有字段均为零值/空：
- `m_StreamData.offset = 0`
- `m_StreamData.size = 0`
- `m_StreamData.path = ''`（空字符串）

这导致 AssetStudio 核心 `Texture2D` 构造函数走 else 分支（`!string.IsNullOrEmpty(path)` 为 false），创建 `ResourceReader(reader, position, 0)`，`image_data.Size = 0`。

分析器进而：
1. `DetermineDataSourceType()` 检查 `m_StreamData.path` 为空 → 判定为 `Embedded`
2. `ParseSingleResource()` 使用 `objectInfo.byteSize = 232`（仅元数据）→ 压缩大小仅 32 字节
3. `.resS` Node 的全部 3,589,680 字节数据无人认领

**.resS 文件完整性验证**：3,589,680 字节 = ASTC 5x5 格式 2048x2048 完整 12 级 mipmap chain，精确匹配。`.resS` 中没有其他资源的数据。

## What Changes

- **修复 `ResourceBlockMapper.DetermineDataSourceType()`**：当 Texture2D 的 `image_data.Size == 0`（即纹理数据不在 SerializedFile 内）且 bundle 中存在 `.resS` Node 时，识别为 `BundleResource` 类型
- **修复 `BundleAnalyzer.ParseSingleResource()`**：新增 `BundleResource` 分支，将 `.resS` Node 的数据归属到对应 Texture2D，正确计算压缩大小贡献
- **修复 `ResourceBlockMapper`**：新增 `CalculateBundleResourceCompressedSize()` 方法，处理 `.resS` Node 内资源数据的压缩大小映射
- **修复验证汇总计算**：确保 `.resS` 文件的数据被正确归属到对应资源

## Capabilities

### New Capabilities
- `ress-resource-mapping`: 处理 Texture2D 纹理数据存储在同 bundle 内 `.resS` 文件中（`m_StreamData` 字段为零值/空）的情况，正确映射资源到压缩块

### Modified Capabilities
<!-- 无需修改现有 spec -->

## Impact

- `AssetStudio.CLI.Analyzer/ResourceBlockMapper.cs` — 新增 `BundleResource` 枚举值和 `.resS` 映射方法
- `AssetStudio.CLI.Analyzer/BundleAnalyzer.cs` — 修改 `ParseSingleResource()` 中 Texture2D 的处理逻辑，新增 `BundleResource` case
- 验证汇总误差率将从 99.88% 降至 < 5%
