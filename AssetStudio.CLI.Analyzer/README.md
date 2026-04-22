# AssetStudio.CLI.Analyzer

AssetBundle 结构分析 CLI 工具，深入解析 Unity AssetBundle 文件的内部字节布局，精确到每一个资源对象和非资源数据区域。

## 功能特性

- **全量字节级分析**：将 Bundle 文件的每一个字节归属到具体数据类别（资源/元数据/间隙）
- **精确资源追踪**：5 种数据来源识别（Embedded / ExternalTexture / ExternalAudio / ExternalVideo / BundleResource）
- **压缩大小估算**：跨 Block 边界按比例计算每个资源的压缩贡献
- **元数据细分**：SerializedFile Metadata 拆分为 Header / Types / ObjectDir / Externals 等部分独立计算大小
- **验证汇总**：自动对比计算总和与文件实际大小，输出绝对/相对误差
- **误差原因诊断**：自动检测外部资源缺失、压缩效率低、元数据膨胀、数据块碎片化等问题
- **多格式输出**：同时生成 Excel (.xlsx) 和 JSON (.json) 报告
- **3 种分析模式**：单文件 / 目录批量 / 文件列表批量
- **汇总报告**：批量分析时生成跨 Bundle 汇总，按误差率排序并高亮异常项
- **Externaal 详情**：可选输出 ScriptTypes / Externals / RefTypes / UserInformation 详情 Sheet
- **独立类库**：数据契约在 `AssetStudio.Analyzer.Contracts` 中定义，方便其他工具引用

## 安装构建

### 前置要求

- .NET 6.0 SDK 或更高版本
- Visual Studio 2022 或更高版本（可选）

### 构建

```bash
# 还原依赖
dotnet restore

# 构建（Debug）
dotnet build AssetStudio.CLI.Analyzer/AssetStudio.CLI.Analyzer.csproj

# 构建（Release）
dotnet build AssetStudio.CLI.Analyzer/AssetStudio.CLI.Analyzer.csproj -c Release
```

输出路径：`AssetStudio.CLI.Analyzer/bin/{Configuration}/net6.0/`

## 使用方法

### 命令行参数

```
AssetStudio.CLI.Analyzer <input> [-o <output>] [-v] [-e] [-s] [-h]
AssetStudio.CLI.Analyzer -l <filelist> [-o <output>] [-s]

参数:
  <input>         输入文件或目录路径
  -l, --list      文件列表路径（每行一个 AssetBundle 绝对路径，# 开头为注释）
  -o, --output    输出目录（默认：输入文件所在目录）
  -v, --verbose   详细输出模式
  -e, --externals 输出 Externals 详情 Sheet（ScriptTypes/Externals/RefTypes/UserInformation）
  -s, --summary   批量分析时生成汇总报告（误差率排序 + 高误差分析）
  -h, --help      显示帮助信息
```

### 使用示例

```bash
# 单文件分析
dotnet AssetStudio.CLI.Analyzer.dll game.bundle

# 批量目录分析 + 汇总报告
dotnet AssetStudio.CLI.Analyzer.dll ./bundles/ -o ./reports/ -s

# 从文件列表批量分析
dotnet AssetStudio.CLI.Analyzer.dll -l bundle_list.txt -o ./reports/ -s

# 详细输出 + Externals 详情
dotnet AssetStudio.CLI.Analyzer.dll game.bundle -v -e

# 指定输出目录
dotnet AssetStudio.CLI.Analyzer.dll game.bundle -o ./output/
```

### 输出文件

| 文件格式 | 文件名模式 | 触发条件 | 说明 |
|---------|-----------|---------|------|
| Excel | `{bundle名}_analysis.xlsx` | 始终生成 | 中文列标题，7+ Sheet |
| JSON | `{bundle名}_analysis.json` | 始终生成 | 英文字段名，结构化数据 |
| 汇总 Excel | `summary_report.xlsx` | `-s` 参数 | 误差率排序 + 高误差分析 |

## 输出格式说明

### Excel 报告结构

#### 基础 Sheet（始终生成）

| Sheet | 说明 |
|-------|------|
| Bundle概览 | 文件基本信息、大小、压缩率、内部文件/资源数量 |
| 内部文件 | SerializedFile / ResourceFile 列表，含缩 metadata/data 大小 |
| 资源明细 | 所有资源对象和非资源数据行，按偏移排序，含数据类别列 |
| 类型汇总 | 按类型分组统计资源数量、大小、占比 |
| 数据块分布 | 每个 StorageBlock 的压缩类型、大小、包含文件列表 |
| TypeTree结构 | 类型定义概览，含节点数量 |
| 验证汇总 | 数据类别汇总表 + 计算总计 vs 文件大小的误差信息 |

#### 扩展 Sheet（`-e` 参数启用）

| Sheet | 说明 |
|-------|------|
| ScriptTypes | LocalSerializedObjectIdentifier 列表 |
| Externals | FileIdentifier 列表（GUID / 类型 / 路径） |
| RefTypes | 引用类型详情（类名 / 命名空间 / 程序集） |
| UserInformation | 用户自定义信息字符串 |

### 资源明细字段

| 列 | 说明 |
|----|------|
| 资源名称 | 资源对象名称 |
| 数据类别 | 资源 / 非资源 |
| 类型名称 | Unity 类型名（Mesh / Texture2D / AnimationClip 等） |
| 类型ID | Unity ClassID |
| PathID | 资源唯一标识 |
| 数据来源 | Embedded / ExternalTexture / ExternalAudio / ExternalVideo / BundleResource / BundleResourceShared / ExternalMissing |
| 大小（解压后） | 原始字节大小 |
| 大小（压缩后） | 按比例估算的压缩大小 |
| 数据偏移 | 在解压数据流中的绝对偏移 |
| 外部文件路径 | .resource / .resS 文件路径（如有） |
| 所属内部文件 | 所属 SerializedFile 文件名 |

### 非资源数据类型

| 类型 | 说明 |
|-----|------|
| BundleMeta | Bundle Header + BlocksInfo（不压缩，大小固定） |
| FileHeader | SerializedFile Header + Version/Platform |
| TypeTree | Types 数组 + TypeTree 数据（节点 + 字符串缓冲区） |
| ObjectDir | Objects 目录数组（ObjectInfo 条目） |
| Externals | 外部引用区域（FileIdentifier 列表） |
| ScriptTypes | LocalSerializedObjectIdentifier 数组 |
| RefTypes | 引用类型 SerializedType 数组（含 TypeTree） |
| UserInformation | 用户自定义信息字符串 |
| ResourceGap | 资源之间的间隙（未归属的空白区域） |

### 数据来源说明

资源对象的数据可能存储在不同位置，工具会自动识别并标注：

| 数据来源 | 说明 |
|---------|------|
| `Embedded` | 资源数据内嵌在 SerializedFile 数据区 |
| `ExternalTexture` | Texture2D 数据在外部 .resource 文件 |
| `ExternalAudio` | AudioClip 数据在外部 .resource 文件 |
| `ExternalVideo` | VideoClip 数据在外部文件 |
| `BundleResource` | 数据在同 Bundle 的 .resS 文件中 |
| `BundleResourceShared` | 多个资源共享同一 .resS（仅首个声明大小） |
| `ExternalMissing` | 外部文件缺失（引用的 .resource 未找到） |

### Block 贡献计算

资源在压缩数据块中的大小按比例计算：

```
压缩贡献 = Block压缩大小 × (资源占用区间 ∩ Block区间长度 / Block解压大小)
```

跨 Block 边界的资源会被拆分为多个贡献项，汇总后得到总压缩大小。

### 验证汇总

验证汇总对比计算总和与文件实际大小：

| 指标 | 说明 |
|------|------|
| BundleMeta | Header + BlocksInfo 大小 |
| FileHeader | SerializedFile Header 区域 |
| TypeTree | 类型定义区域 |
| ObjectDir | 对象目录区域 |
| Externals | 外部引用区域 |
| 资源数据 | 所有资源对象大小 |
| ResourceGap | 资源间未归属区域 |
| 计算总计 | 上述各项之和 |
| 文件实际大小 | 文件系统上的大小 |
| 绝对误差 | \|计算总计 - 文件实际大小\| |
| 相对误差 | 绝对误差 / 文件实际大小 × 100% |

### 汇总报告（`-s` 参数）

批量分析时生成的汇总报告包含两个 Sheet：

1. **误差率汇总** — 所有 Bundle 按误差率降序排列，高误差项（>10%）红色高亮
2. **高误差分析** — 对每个高误差 Bundle 自动分析可能原因：
   - 外部资源缺失
   - 压缩效率低
   - 元数据膨胀
   - 未识别数据
   - 数据块碎片化

## 技术原理

### AssetBundle 结构

```
┌─────────────────────────────────────┐
│           Bundle Header             │  ← 文件签名、版本、标志
├─────────────────────────────────────┤
│         BlocksInfo (压缩)           │  ← 描述数据块的元数据
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐    │
│  │    Block 0 (LZ4/LZ4HC)     │    │  ← 解压后拼接为 blocksStream
│  ├─────────────────────────────┤    │
│  │    Block 1 (LZ4/LZ4HC)     │    │  ← 包含内部文件数据
│  ├─────────────────────────────┤    │
│  │    Block N (None)          │    │  ← 可能不压缩
│  └─────────────────────────────┘    │
└─────────────────────────────────────┘

blocksStream 解压后:
┌────────────────┬─────────────────┬──────────────┐
│  Node 0 (SF)   │  Node 1 (.resS)  │  Node 2 (SF)  │
│  offset/size   │  offset/size     │  offset/size │
└────────────────┴─────────────────┴──────────────┘
```

### SerializedFile 结构

```
┌────────────────┬──────────┬───────────┬───────────┬───────────┐
│  FileHeader    │ TypeTree │ ObjectDir │ Externals │ Data Area │
│  (metadata)    │ (types)  │ (objects) │ (refs)    │ (resources)│
└────────────────┴──────────┴───────────┴───────────┴───────────┘
                                 ↑ m_DataOffset           ↑ m_FileSize
```

### 数据映射链

```
资源对象 (ObjectInfo.byteStart/byteSize)
    ↓ 资源在 SerializedFile 数据区的偏移
SerializedFile (Node.offset + m_DataOffset + byteStart)
    ↓ 在 blocksStream 中的绝对位置
StorageBlock[] (NodeBlockMapper 查找交集 Block)
    ↓ 按比例计算压缩贡献
压缩大小 (SumCompressedContributions)
```

### 核心算法

1. **BlockOffsetCalculator** — 计算 StorageBlock 累积偏移表，建立解压位置到 Block 的映射
2. **NodeBlockMapper** — 将任一解压区间映射到涉及的 Block 列表，按交集比例计算压缩贡献
3. **ResourceBlockMapper** — 根据资源类型（Embedded/External/BundleResource）计算不同来源资源的压缩大小
4. **MetadataPositionTracker** — 逐字节追踪 SerializedFile 元数据各部分的位置和大小
5. **NonResourceDataCalculator** — 生成 Bundle 级和 SerializedFile 级非资源数据行
6. **VarianceAnalyzer** — 分析误差原因，检测 5 类常见问题

## 项目结构

```
AssetStudio.CLI.Analyzer/
├── Program.cs                   - CLI 入口，参数解析，三种运行模式
├── BundleAnalyzer.cs            - 分析器主逻辑，资源解析和验证
├── MetadataPositionTracker.cs  - SerializedFile 元数据位置精确追踪
├── BlockOffsetCalculator.cs     - Block 累积偏移计算
├── NodeBlockMapper.cs           - Node 到 Block 映射和压缩贡献计算
├── ResourceBlockMapper.cs       - 资源到 Block 映射，5 种数据来源识别
├── NonResourceDataCalculator.cs - 非资源数据行生成
├── VarianceAnalyzer.cs          - 误差原因分析器
├── ExcelReportExporter.cs       - Excel 报告导出（7+ Sheet）
├── JsonReportExporter.cs        - JSON 报告导出（支持导入回读）
└── BatchSummaryExporter.cs      - 批量汇总报告导出

AssetStudio.Analyzer.Contracts/
├── AnalysisReport.cs            - 顶层报告结构
├── BundleSummary.cs             - Bundle 概览信息
├── InternalFileInfo.cs          - 内部文件信息
├── ResourceInfo.cs              - 资源对象明细（含非资源数据）
├── BlockContribution.cs         - Block 贡献明细
├── TypeSummary.cs               - 类型汇总统计
├── BlockInfo.cs                 - 数据块信息
├── TypeTreeInfo.cs               - TypeTree 类型和节点信息
├── CategoryTotal.cs             - 数据类别汇总行
├── VerificationSummary.cs       - 验证汇总（含 ErrorInfo）
├── NonResourceType.cs           - 非资源数据类型枚举
├── DataCategory.cs              - 数据类别枚举
├── ScriptTypeInfo.cs            - ScriptType 详情
├── ExternalDetail.cs            - External 详情
├── RefTypeInfo.cs               - RefType 详情
├── UserInformationInfo.cs       - UserInformation 详情
├── BundleSummaryItem.cs         - 批量汇总行
├── VarianceAnalysisResult.cs    - 误差分析结果
└── VarianceCause.cs             - 误差原因枚举
```

## 依赖项

| 包 | 版本 | 用途 |
|---|------|------|
| [EPPlus](https://github.com/EPPlusSoftware/EPPlus) | 7.0.0 | Excel 文件生成 |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13.0.1 | JSON 序列化 |
| [AssetStudio](../AssetStudio/) | - | AssetBundle 解析核心库 |
| [AssetStudio.Analyzer.Contracts](../AssetStudio.Analyzer.Contracts/) | - | 数据契约类库 |

## 注意事项

- EPPlus 使用 `NonCommercial` 许可证上下文
- 压缩大小为**比例估算值**，非精确测量；跨 Block 边界的资源按交集比例拆分
- 资源间隙（ResourceGap）表示 .resS 文件或 SerializedFile 数据区中未被任何资源声明的区域
- `ExternalMissing` 表示引用的外部 .resource 文件不在同 Bundle 中，无法计算压缩大小
- 多个 BundleResource 共享同一 .resS 时，仅首个资源声明大小，后续标记为 `BundleResourceShared`（大小为 0，避免重复计算）

## 许可证

本项目为 AssetStudio 的扩展工具，遵循 AssetStudio 的许可证条款。
EPPlus 使用 NonCommercial 许可证上下文（仅限非商业用途）。