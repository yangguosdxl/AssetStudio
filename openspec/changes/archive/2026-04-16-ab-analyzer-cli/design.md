# AssetBundle 分析 CLI 工具 - 技术设计

## Context

### 背景

AssetStudio 是 Unity 资源提取工具，已实现 BundleFile 和 SerializedFile 的解析逻辑。现有解析代码能够：
- 解析 AssetBundle 文件结构（Header、StorageBlock、Node）
- 解压数据块并提取内部文件
- 解析 SerializedFile 的元数据和资源对象

但现有代码**未追踪**资源在压缩数据块中的精确位置，无法计算单个资源的压缩后大小。

### 现有代码结构

```
BundleFile
    m_Header         → Bundle 头信息
    m_BlocksInfo[]   → StorageBlock（压缩前后大小）
    m_DirectoryInfo[] → Node（在解压流中的位置）
    fileList[]       → StreamFile（解压后的文件内容）

SerializedFile
    header           → 文件头（数据区偏移）
    m_Types[]        → 类型定义
    m_Objects[]      → ObjectInfo（资源定位）
    m_Externals[]    → 外部引用
```

### 关键数据映射链路

```
资源 ObjectInfo.byteStart/byteSize
    ↓
SerializedFile 在 Node 中的位置
    ↓
Node.offset/size → blocksStream 中的绝对位置
    ↓
blocksStream → StorageBlock[] 的映射
    ↓
按比例计算压缩贡献
```

### 约束

- 不能修改现有 AssetStudio 核心库的解析逻辑
- CLI 项目需要复用现有解析代码
- JSON 类型类库需要独立，不依赖 CLI 实现

## Goals / Non-Goals

**Goals:**

1. 精确计算每个资源在 AssetBundle 中的压缩前后大小
2. 建立 Object → Node → Block 的完整映射链路
3. 输出结构化报告（xlsx + json）供对比分析
4. CLI 支持单文件和批量分析
5. JSON 类型类库独立，方便其他工具反序列化

**Non-Goals:**

1. 不修改 AssetStudio 核心库的现有解析逻辑
2. 不支持实时监控或增量分析
3. 不支持 AssetBundle 内容修改
4. 不支持加密 AssetBundle 的分析

## Decisions

### D1: 资源压缩大小计算方法

**决策**: 按数据块范围比例计算

**算法**:
```
1. 计算资源在 blocksStream 中的绝对位置
   absoluteStart = Node.offset + ObjectInfo.byteStart
   absoluteEnd = absoluteStart + ObjectInfo.byteSize

2. 构建 Block 累积偏移表
   BlockStart[i] = Σ(BlocksInfo[0..i-1].uncompressedSize)

3. 找出涉及的 Block
   遍历每个 Block，检查资源范围是否与 Block 范围重叠

4. 按比例计算每个 Block 的压缩贡献
   intersectSize = min(absoluteEnd, BlockEnd) - max(absoluteStart, BlockStart)
   ratio = intersectSize / Block.uncompressedSize
   contribution = Block.compressedSize × ratio

5. 汇总压缩大小
   compressedSize = Σ(contributions)
```

**替代方案考虑**:
- 整体压缩率估算：不准确，不同 Block 压缩率可能不同
- 精确字节追踪：需要深入压缩算法，复杂度高且不可行

**选择原因**: 比例法合理且实用，Block 是压缩的基本单位

### D2: 外部资源处理

**决策**: 关联外部资源文件与 Node

**场景**: Texture2D/AudioClip/VideoClip 数据在 .resource 文件中

**处理流程**:
```
1. 识别外部资源类型
   Texture2D → StreamingInfo.path/offset/size
   AudioClip → m_Source/m_Offset/m_Size

2. 找到外部文件对应的 Node
   匹配 Node.path 与外部文件名

3. 计算外部数据在 blocksStream 中的位置
   absoluteStart = Node.offset + StreamingInfo.offset
   absoluteSize = StreamingInfo.size

4. 同样用 Block 比例法计算压缩大小
```

### D3: 项目结构设计

**决策**: 三项目架构

```
AssetStudio.Analyzer.Contracts (netstandard2.0)
    ├── 定义 JSON 输出的数据模型
    ├── 无外部依赖（仅 Newtonsoft.Json 可选）
    └── 可被其他工具引用

AssetStudio.CLI.Analyzer (net6.0)
    ├── 复用 AssetStudio 核心解析库
    ├── 实现 Block 映射算法
    ├── 输出 xlsx (EPPlus) + json
    └── 命令行参数处理

AssetStudio (现有核心库)
    └── 不修改，仅引用
```

**选择原因**:
- Contracts 独立：最大化兼容性，其他工具可直接引用
- CLI 复用核心库：避免重复实现解析逻辑
- 不修改核心库：保持稳定性

### D4: JSON 类型设计

**决策**: 分层类型结构

```csharp
// 顶层报告
public class AnalysisReport
{
    public BundleSummary Bundle { get; set; }
    public List<InternalFileInfo> InternalFiles { get; set; }
    public List<ResourceInfo> Resources { get; set; }
    public List<TypeSummary> TypeSummaries { get; set; }
    public List<BlockInfo> Blocks { get; set; }
}

// Bundle 概览
public class BundleSummary
{
    public string FileName { get; set; }
    public long TotalSizeCompressed { get; set; }
    public long TotalSizeUncompressed { get; set; }
    public long HeaderSize { get; set; }
    public long BlocksInfoSizeCompressed { get; set; }
    public long BlocksInfoSizeUncompressed { get; set; }
    public long DataBlocksSizeCompressed { get; set; }
    public long DataBlocksSizeUncompressed { get; set; }
    public string CompressionType { get; set; }
    public string UnityVersion { get; set; }
    public int InternalFileCount { get; set; }
    public int ResourceCount { get; set; }
}

// 内部文件
public class InternalFileInfo
{
    public string FileName { get; set; }
    public long SizeUncompressed { get; set; }
    public string FileType { get; set; } // SerializedFile/ResourceFile
    public long Offset { get; set; }
    public long? MetadataSize { get; set; }
    public long? DataSize { get; set; }
    public int? ResourceCount { get; set; }
    public int? TypeCount { get; set; }
}

// 资源明细
public class ResourceInfo
{
    public string Name { get; set; }
    public string TypeName { get; set; }
    public int TypeId { get; set; }
    public long PathId { get; set; }
    public string DataSource { get; set; } // Embedded/ExternalTexture/ExternalAudio/ExternalVideo
    public long SizeUncompressed { get; set; }
    public long SizeCompressed { get; set; }
    public long DataOffset { get; set; }
    public string? ExternalFilePath { get; set; }
    public string? ContainerPath { get; set; }
    public string InternalFileName { get; set; }
    public List<BlockContribution> BlockContributions { get; set; }
}

// Block 贡献
public class BlockContribution
{
    public int BlockIndex { get; set; }
    public long IntersectStart { get; set; }
    public long IntersectEnd { get; set; }
    public long IntersectSize { get; set; }
    public double Ratio { get; set; }
    public long CompressedContribution { get; set; }
}

// 类型汇总
public class TypeSummary
{
    public string TypeName { get; set; }
    public int TypeId { get; set; }
    public int ResourceCount { get; set; }
    public long TotalSizeUncompressed { get; set; }
    public long TotalSizeCompressed { get; set; }
    public long AverageSize { get; set; }
    public string LargestResourceName { get; set; }
    public long LargestSize { get; set; }
    public double BundleRatio { get; set; }
}

// Block 信息
public class BlockInfo
{
    public int Index { get; set; }
    public string CompressionType { get; set; }
    public long SizeCompressed { get; set; }
    public long SizeUncompressed { get; set; }
    public double CompressionRatio { get; set; }
    public List<string> ContainedFiles { get; set; }
}
```

### D5: CLI 参数设计

**决策**: 简洁命令行参数

```
AssetStudio.CLI.Analyzer.exe <input> [-o <output>] [-v]

参数:
  <input>     输入文件或目录路径
  -o, --output  输出目录（默认：输入文件所在目录）
  -v, --verbose 详细输出
  -h, --help    显示帮助
```

**使用示例**:
```bash
# 单文件分析
AssetStudio.CLI.Analyzer.exe game.bundle

# 批量目录分析
AssetStudio.CLI.Analyzer.exe ./bundles/ -o ./reports/

# 详细模式
AssetStudio.CLI.Analyzer.exe game.bundle -v
```

## Risks / Trade-offs

### R1: 块内压缩不均匀

**风险**: 比例计算假设 Block 内压缩均匀，实际可能不均匀

**缓解**:
- 这是不可避免的近似
- Block 级别的比例计算是合理的单位
- 在报告中标注为"估算值"

### R2: 跨 Block 资源精度

**风险**: 资源跨多个 Block 时，不同 Block 压缩率不同影响精度

**缓解**:
- 按每个 Block 的实际压缩率分别计算贡献
- 比单一整体压缩率更精确

### R3: 大文件内存占用

**风险**: 大型 AssetBundle 解压时内存占用高

**缓解**:
- 复用现有代码的流式处理
- 不一次性加载全部数据
- BlocksInfo 使用 MemoryStream，大文件使用临时文件

### R4: 外部资源文件缺失

**风险**: 分析时 .resource 文件可能不在同目录

**缓解**:
- 报告中标注外部资源为"无法定位"
- 仍记录资源元数据信息
- sizeUncompressed 记录元数据中声明的大小