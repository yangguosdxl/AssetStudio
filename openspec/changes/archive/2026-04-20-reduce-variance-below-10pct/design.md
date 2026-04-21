## Context

当前 `AssetStudio.CLI.Analyzer` 的资源会计系统通过累加各资源的压缩大小来验证与文件实际大小的一致性。对 1095 个高误差文件的根因分析揭示了三大系统性误差源：

| 误差类型 | 受影响文件数 | 特征 |
|----------|-------------|------|
| .resS 完全未计入 | 605 | Mesh/Cubemap 等类型的顶点/像素数据在 .resS 中但无 m_StreamData 引用 |
| ExternalTexture 部分覆盖 | 288 | 纹理的 offset+size 未覆盖 .resS 全部数据，存在间隙 |
| BundleResource 重复计算 | 5 | 多个 Texture2D 标记为 BundleResource 时各自声称拥有整个 .resS |
| 无 .resS 但仍有误差 | 20 | TypeTree 估算偏差导致 SF 数据区未被完全覆盖 |

当前架构：
- `ResourceBlockMapper` 负责将资源映射到压缩 Block
- `BundleAnalyzer.BuildVerificationSummary` 按类别汇总大小
- 间隙数据无归属机制

## Goals / Non-Goals

**Goals:**
- 将所有 1095 个高误差文件的误差率降至 10% 以下
- 将 .resS 文件中未被任何资源引用的数据正确归类为"资源间隙"
- 修正 BundleResource 重复计算问题
- 将 ExternalTexture 间隙纳入追踪
- 将 SerializedFile 数据区间隙纳入追踪

**Non-Goals:**
- 不修改 AssetStudio 核心库的解析逻辑
- 不改变现有的 JSON/Excel 输出格式（仅新增字段）
- 不处理 ExternalMissing 类型（外部文件不在当前 bundle 内的数据）
- 不追求 0% 误差率（压缩数据比例法存在固有估算误差）

## Decisions

### Decision 1: 在 BuildVerificationSummary 中计算间隙数据

**选择**: 在验证汇总阶段统一计算间隙，而非在资源解析阶段

**理由**:
- 资源解析阶段的职责是解析单个资源，不应关心全局会计
- 验证汇总阶段已有所有数据（resources、nonResourceData、blocks、directoryInfo）
- 间隙计算需要知道所有资源已声明的范围，属于后处理逻辑

**备选方案**: 在 ResourceBlockMapper 中追踪 → 拒绝，因为需要全局视角

### Decision 2: 间隙数据作为 ResourceGap 类别

**选择**: 在 `NonResourceType` 中新增 `ResourceGap` 枚举值

**理由**:
- 间隙数据不是传统意义上的资源，但也不属于 Bundle/SerializedFile 元数据
- 归入 `NonResourceData` 列表，使用 `DataCategory.NonResource`
- 在 `CategoryTotals` 中单独一行展示

### Decision 3: BundleResource 按实际覆盖范围分配

**选择**: 当有多个 BundleResource 时，仅让第一个资源声明 .resS 全部大小，后续 BundleResource 资源将 byteSize 设为 0（视为共享引用）

**理由**:
- BundleResource 本质上是指向整个 .resS 的引用，多个纹理共享同一 .resS
- 只有第一个声明的纹理负责"拥有"这块数据
- 其余 BundleResource 纹理的像素数据实际已在 .resS 中被覆盖

**备选方案**: 按纹理大小比例分摊 → 拒绝，因为无法精确知道每个纹理在 .resS 中的确切位置

### Decision 4: 间隙计算算法

**选择**: 区间合并法 - 收集所有资源已声明的 [offset, offset+size) 区间，与 .resS / SF 数据区取差集

**理由**:
- 精确计算未覆盖区域
- 可处理多个资源交叉引用同一区间的复杂情况
- 时间复杂度 O(n log n)，n 为资源数量

## Risks / Trade-offs

- **[风险] 压缩大小估算** → 间隙的压缩大小通过 NodeBlockMapper 按比例计算，存在固有误差。缓解：对于 LZ4 压缩，比例法误差通常 < 5%
- **[风险] .resS 中合法的 padding/alignment** → 某些 .resS 文件可能有对齐 padding。缓解：padding 通常很小（< 1%），不影响 10% 阈值
- **[取舍] TypeTree 估算偏差** → 无 .resS 的 SF 文件中，TypeTree 大小通过估算得出，可能不完全准确。缓解：这部分影响仅 20 个文件，且估算偏差通常 < 10%
