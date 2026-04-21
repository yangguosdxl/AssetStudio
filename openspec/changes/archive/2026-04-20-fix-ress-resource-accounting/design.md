## Context

**运行时诊断数据**（通过反射获取 Texture2D 内部字段）：

```
Texture2D: ChooseRace_lihui_monster01
  Width=2048, Height=2048, Format=ASTC_RGB_5x5
  m_StreamData: offset=0, size=0, path=''  (全部零值/空)
  image_data: needSearch=False, path='', offset=8924, size=0, Size=0
  byteSize=232 (仅元数据)
```

**Bundle 结构**：
- 2 个 Node: CAB-xxx (SerializedFile, 10,076 bytes) + CAB-xxx.resS (ResourceFile, 3,589,680 bytes)
- 28 个 LZ4HC 压缩 Block，blocksStream 总大小 3,599,756 bytes
- 3 个资源对象：AssetBundle(480B) + Texture2D(232B) + Material(1,060B)，全部在 CAB SerializedFile 内

**根因**：Unity 2022.3.62f3 中，当纹理数据在同 bundle 的 `.resS` 文件时，`m_StreamData` 的 offset/size/path 全部为零值/空。AssetStudio 核心 `Texture2D` 构造函数判断 `!string.IsNullOrEmpty(m_StreamData?.path)` 为 false，走内嵌分支创建 size=0 的 ResourceReader。分析器继承了这个限制，进一步将纹理判定为 Embedded 且仅计入元数据大小。

**.resS 完整性**：3,589,680 bytes = ASTC 5x5 格式 2048x2048 完整 12 级 mipmap chain，精确匹配。`.resS` 中不含其他资源数据。

**当前数据流**（错误的）：
1. `DetermineDataSourceType(Texture2D)` → `m_StreamData.path` 为空 → `Embedded`
2. `ParseSingleResource(Embedded case)` → `objectInfo.byteSize = 232` → 压缩大小 32B
3. `.resS` Node 的 3,589,680B 无人认领 → 验证误差 99.88%

## Goals / Non-Goals

**Goals:**
- 正确识别纹理数据在同 bundle `.resS` 文件中但 `m_StreamData` 字段为零值/空的 Texture2D
- 将 `.resS` 文件中的纹理数据正确归属到对应 Texture2D 资源
- 验证汇总误差率降至 < 5%
- 保持对正常 `Embedded` / `ExternalTexture` / `ExternalMissing` 分支的兼容

**Non-Goals:**
- 不修复 AssetStudio 核心库的 Texture2D 解析（AssetStudio 本身也无法导出这种纹理）
- 不处理跨 bundle 外部 `.resS` 引用（已有 ExternalMissing 处理）
- 不处理 AudioClip / VideoClip 的类似情况（测试数据未涉及）
- 不精确计算 ASTC mipmap chain 大小（直接用 .resS Node.size）

## Decisions

### 1. 判断条件：使用 `image_data.Size == 0` 而非检查 m_StreamData

**决策**：在 `ParseSingleResource()` 中，当对象为 Texture2D 且 `image_data.Size == 0` 时，检查是否存在同 bundle `.resS` Node。

**理由**：
- `image_data.Size == 0` 直接表明纹理数据不在 SerializedFile 内（image_data_size=0）
- 比 `m_StreamData.path` 检查更可靠，因为某些版本 `m_StreamData` 字段可能都为零
- 无需访问 Texture2D 的私有字段

**替代方案**：检查 `m_StreamData != null && m_StreamData.size == 0` → 但 m_StreamData 在 image_data_size > 0 时为 null，需要额外判空

### 2. 数据归属策略：整个 .resS Node 归属到 Texture2D

**决策**：当只有一个 Texture2D 的 `image_data.Size == 0` 且 `.resS` Node 存在时，将整个 `.resS` Node 的数据归属到该 Texture2D。

**理由**：
- 当前测试数据中 `.resS` 仅被一个 Texture2D 使用
- 计算验证：.resS 大小 = ASTC mipmap chain 总大小，精确匹配
- 对于多个 Texture2D 共享 .resS 的情况，可以后续通过 m_StreamData.offset 做更精细的划分

**替代方案**：通过 Width/Height/Format 计算精确纹理大小 → 需要实现所有纹理格式的大小计算，复杂度高且不必要

### 3. .resS Node 查找逻辑

**决策**：在 `ResourceBlockMapper` 构造时建立 `.resS` 文件名到 Node 的映射。查找时：
1. 使用对应 SerializedFile 的 Node path + `.resS` 后缀匹配
2. 或直接查找 path 以 `.resS` 结尾的 Node

**理由**：`.resS` 文件通常与 SerializedFile 同名（如 `CAB-xxx` + `CAB-xxx.resS`），但也有独立命名的可能。

## Risks / Trade-offs

- [多个 Texture2D 共享 .resS] → 当前直接将整个 .resS 归属到第一个 Texture2D，后续可通过 m_StreamData.offset 做精确划分
- [AudioClip 在 .resS 中] → 当前方案仅处理 Texture2D，如需支持 AudioClip 需额外开发
- [.resS 包含非纹理数据] → 理论上 .resS 可包含多种资源数据，当前全归属到 Texture2D 可能导致大小略高
