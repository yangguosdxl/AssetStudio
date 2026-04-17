# AssetStudio.CLI.Analyzer

AssetBundle 分析 CLI 工具，用于分析 Unity AssetBundle 文件内部各部分的字节分布，精确到资源级别。

## 功能特性

- **精确到资源级别**：每个资源对象的大小、类型、位置信息
- **压缩大小计算**：按数据块范围计算压缩前后大小
- **多格式输出**：同时生成 Excel (xlsx) 和 JSON 格式报告
- **批量分析**：支持单文件和目录批量分析
- **独立类型库**：JSON 类型定义在独立类库中，方便其他工具引用
- **精确元数据分析**：准确计算 TypeTree 和 Externals 各部分大小

## 安装构建

### 前置要求

- .NET 6.0 SDK 或更高版本
- Visual Studio 2022 或更高版本（可选）

### 构建

```bash
# 进入项目目录
cd AssetStudio

# 还原依赖
dotnet restore

# 构建项目
dotnet build AssetStudio.CLI.Analyzer/AssetStudio.CLI.Analyzer.csproj

# 或使用 MSBuild
msbuild /t:AssetStudio.CLI.Analyzer /p:Configuration=Release
```

### 输出位置

构建完成后，可执行文件位于：
```
AssetStudio.CLI.Analyzer/bin/Debug/net6.0/AssetStudio.CLI.Analyzer.dll
```

## 使用方法

### 命令行参数

```
AssetStudio.CLI.Analyzer <input> [-o <output>] [-v] [-h]

参数:
  <input>       输入文件或目录路径（必需）
  -o, --output  输出目录（默认：输入文件所在目录）
  -v, --verbose 详细输出模式
  -h, --help    显示帮助信息
```

### 使用示例

```bash
# 单文件分析
dotnet AssetStudio.CLI.Analyzer.dll game.bundle

# 批量目录分析
dotnet AssetStudio.CLI.Analyzer.dll ./bundles/ -o ./reports/

# 详细输出模式
dotnet AssetStudio.CLI.Analyzer.dll game.bundle -v

# 指定输出目录
dotnet AssetStudio.CLI.Analyzer.dll game.bundle -o ./output/
```

### 输出文件

每个 AssetBundle 文件会生成两个报告：

| 文件格式 | 文件名模式 | 说明 |
|---------|-----------|------|
| Excel | `{bundle名}_analysis.xlsx` | 中文列标题，5个Sheet |
| JSON | `{bundle名}_analysis.json` | 英文字段名，结构化数据 |

## 输出格式说明

### Excel 报告结构

Excel 报告包含 6 个 Sheet：

| Sheet名称 | 内容 |
|----------|------|
| Bundle概览 | 文件基本信息、大小、压缩率 |
| 内部文件 | SerializedFile/ResourceFile 列表 |
| 资源明细 | 每个资源对象的详细信息 |
| 类型汇总 | 按类型统计的资源分布 |
| 数据块分布 | StorageBlock 的压缩信息 |
| TypeTree结构 | TypeTree 类型结构概览 |

### JSON 报告结构

```json
{
  "bundle": {
    "file_name": "game.bundle",
    "total_size_compressed": 9828,
    "total_size_uncompressed": 32501,
    "compression_type": "Lz4HC",
    "unity_version": "5.x.x",
    ...
  },
  "internal_files": [...],
  "resources": [...],
  "type_summaries": [...],
  "blocks": [...],
  "type_trees": [...]
}
```

### 数据字段说明

#### Bundle 概览

| 字段 | 说明 |
|-----|------|
| `total_size_compressed` | Bundle 文件总大小（压缩后） |
| `total_size_uncompressed` | Bundle 解压后总大小 |
| `header_size` | Bundle Header 大小 |
| `compression_type` | 压缩类型（Lz4/Lz4HC/Lzma/None） |
| `data_blocks_size_compressed` | 数据块压缩后总大小 |
| `data_blocks_size_uncompressed` | 数据块解压后总大小 |

#### 资源明细

| 字段 | 说明 |
|-----|------|
| `name` | 资源名称 |
| `type_name` | 资源类型（Mesh/Texture2D/AnimationClip等） |
| `type_id` | Unity 类型 ID |
| `path_id` | 资源唯一标识 |
| `data_source` | 数据来源（Embedded/ExternalTexture/ExternalAudio） |
| `size_uncompressed` | 资源解压后大小 |
| `size_compressed` | 资源压缩后大小（估算值） |
| `block_contributions` | Block 贡献明细（按比例计算） |

#### TypeTree 类型结构

| 字段 | 说明 |
|-----|------|
| `type_id` | Unity 类型 ID |
| `type_name` | 类型名称 |
| `is_stripped` | 是否为剥离类型 |
| `script_type_index` | 脚本类型索引（MonoBehaviour） |
| `internal_file_name` | 所属内部文件名 |
| `node_count` | TypeTree 节点数量 |
| `nodes` | TypeTree 节点明细（每个字段的类型、名称、大小等） |

#### TypeTree 节点信息

| 字段 | 说明 |
|-----|------|
| `type` | 字段类型（int, string, Vector3 等） |
| `name` | 字段名称 |
| `level` | 层级深度（0=根节点） |
| `byte_size` | 字段字节大小 |
| `type_flags` | 类型标志（是否数组等） |
| `version` | 版本 |
| `meta_flag` | 元数据标志 |
| `ref_type_hash` | 引用类型哈希 |

#### Block 贡献计算

资源在压缩数据块中的大小按比例计算：

```
压缩贡献 = Block压缩大小 × (资源占用比例)
         = Block压缩大小 × (资源数据大小 / Block解压大小)
```

#### 非资源数据类型（Non-Resource Types）

除了资源对象外，报告还包含 SerializedFile 元数据各部分的大小：

| 类型 | 说明 |
|-----|------|
| `BundleMeta` | Bundle Header + BlocksInfo |
| `FileHeader` | SerializedFile Header + Version/Platform 信息 |
| `TypeTree` | Types 数组 + TypeTree 数据（节点 + 字符串缓冲区） |
| `ObjectDir` | Objects 目录数组（ObjectInfo 条目） |
| `Externals` | 外部引用区域汇总（ScriptTypes + FileIdentifier + RefTypes + UserInformation） |
| `ScriptTypes` | LocalSerializedObjectIdentifier 数组 |
| `RefTypes` | 引用类型 SerializedType 数组（含 TypeTree） |
| `UserInformation` | 用户自定义信息字符串 |

**TypeTree 大小计算**：
- 使用实际节点数量计算，每个节点 24-32 bytes（取决于版本）
- 公式：`4 (nodeCount) + 4 (stringBufferSize) + nodeCount × nodeSize + stringBuffer`

**Externals 大小计算**：
- 各子部分独立追踪后汇总
- 典型大小：ScriptTypes (~几百 bytes), FileIdentifier (1-5 KB), RefTypes (0-数十 KB), UserInformation (~几十 bytes)

## 项目结构

```
AssetStudio.CLI.Analyzer/
├── Program.cs              - CLI 入口，参数解析
├── BundleAnalyzer.cs       - 分析器主逻辑
├── MetadataPositionTracker.cs - SerializedFile 元数据位置精确追踪
├── BlockOffsetCalculator.cs - Block 累积偏移计算
├── NodeBlockMapper.cs      - Node 到 Block 映射
├── ResourceBlockMapper.cs  - 资源到 Block 映射
├── ExcelReportExporter.cs  - Excel 报告导出
└── JsonReportExporter.cs   - JSON 报告导出

AssetStudio.Analyzer.Contracts/
├── AnalysisReport.cs       - 顶层报告结构
├── BundleSummary.cs        - Bundle 概览信息
├── InternalFileInfo.cs     - 内部文件信息
├── ResourceInfo.cs         - 资源对象明细
├── BlockContribution.cs    - Block 贡献明细
├── TypeSummary.cs          - 类型汇总统计
├── BlockInfo.cs            - 数据块信息
├── TypeTreeInfo.cs         - TypeTree 类型结构信息
└── NonResourceType.cs      - 非资源数据类型枚举
```

## 技术原理

### AssetBundle 结构

```
┌─────────────────────────────────────┐
│           Bundle Header             │  ← 文件签名、版本信息
├─────────────────────────────────────┤
│         BlocksInfo (压缩)           │  ← 描述数据块的元数据
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐    │
│  │    Block 0 (LZ4 压缩)       │    │  ← 包含内部文件数据
│  └─────────────────────────────┘    │
│  ┌─────────────────────────────┐    │
│  │    Block 1 (LZ4 压缩)       │    │  ← 可能包含更多文件
│  └─────────────────────────────┘    │
│  ...                                │
└─────────────────────────────────────┘
```

### 数据映射链

```
资源对象 (ObjectInfo)
    ↓ byteStart/byteSize
SerializedFile
    ↓ offset/size
Node (DirectoryInfo)
    ↓ 映射到 Block 范围
StorageBlock
    ↓ 计算压缩贡献
压缩大小
```

## 应用场景

- **打包优化**：分析 AssetBundle 大小变化原因
- **版本对比**：比较不同打包版本的资源分布差异
- **资源审计**：找出占用空间最大的资源类型
- **压缩分析**：评估不同压缩算法的效果

## 依赖项

- [EPPlus 7.0.0](https://github.com/EPPlusSoftware/EPPlus) - Excel 文件生成
- [Newtonsoft.Json 13.0.1](https://www.newtonsoft.com/json) - JSON 序列化
- [AssetStudio](../AssetStudio/) - AssetBundle 解析核心库

## 许可证

本项目为 AssetStudio 的扩展工具，遵循 AssetStudio 的许可证条款。
EPPlus 使用 NonCommercial 许可证上下文（仅限非商业用途）。