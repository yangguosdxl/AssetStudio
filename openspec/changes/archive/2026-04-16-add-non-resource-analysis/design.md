## Context

### 背景

AssetStudio CLI Analyzer 已实现 AssetBundle 文件结构分析，能够精确计算每个资源对象的压缩前后大小。但当前实现仅追踪资源数据（Object Data Area），缺少对非资源数据（Metadata）的统计。

### AssetBundle 数据结构

```
AssetBundle 文件
├── Bundle Header (不压缩)
│   signature, version, flags, size...
├── BlocksInfo Metadata (压缩)
│   StorageBlock[] + Node[]
├── DataBlocks (压缩)
│   ├── SerializedFile (.assets)
│   │   ├── Metadata (非资源)
│   │   │   ├── Header + Version/Platform
│   │   │   ├── Types[] + TypeTree
│   │   │   ├── Objects[] (目录)
│   │   │   ├── Externals + RefTypes + UserInfo
│   │   │   └── ────────────────────────
│   │   │       总大小 = m_DataOffset
│   │   ├── Object Data Area (资源)
│   │   │   Mesh, Texture2D, AnimationClip...
│   │   └── └─────────────────────────────
│   │       总大小 = m_FileSize
│   └── ResourceFile (.resource)
│       全部为资源数据（无 Metadata）
```

### 约束

- 不修改 AssetStudio 核心库的现有解析逻辑
- 复用现有的 NodeBlockMapper 压缩大小计算方法
- 非资源数据与资源数据按偏移混合排序

## Goals / Non-Goals

**Goals:**

1. 精确追踪 SerializedFile Metadata 各部分的位置和大小
2. 使用比例法计算非资源数据的压缩后大小（与资源数据方法一致）
3. 在报告中将非资源数据与资源数据按偏移排序显示
4. 提供验证汇总，对比计算总和与文件实际大小

**Non-Goals:**

1. 不支持实时监控或增量分析
2. 不支持加密 AssetBundle 的分析
3. 不拆分单个 TypeTree（Types + TypeTree 汇总为一行）
4. 不修改 AssetStudio 核心库

## Decisions

### D1: Metadata 位置追踪方法

**决策**: 创建 MetadataPositionTracker 类重新解析 SerializedFile stream

**算法**:
```
1. 获取 SerializedFile 的 stream（通过反射或重新打开文件）
2. 按解析顺序追踪每个部分的位置：
   - Header: 从 stream 开头到 version/platform 结束
   - Types: types 数组的完整范围
   - ObjectDir: objects 数组的完整范围
   - Externals: ScriptTypes + Externals + RefTypes + UserInfo
3. 记录每个部分的 start/end position
4. 计算大小 = end - start
5. 验证: Header + Types + ObjectDir + Externals ≈ m_DataOffset
```

**替代方案考虑**:
- 修改核心库添加位置追踪：违反约束
- 使用固定大小估算：不同版本格式不同，不准确
- 基于现有 ObjectInfo 计算：缺少 Types 和 Externals 信息

**选择原因**: 重新解析最精确，且不违反"不修改核心库"约束

### D2: 压缩大小计算方法

**决策**: 使用 NodeBlockMapper 比例计算法

**算法**:
```
1. 计算 Metadata 各部分在 blocksStream 中的绝对位置
   absoluteStart = Node.offset + partStart
   absoluteEnd = Node.offset + partEnd

2. 使用现有的 NodeBlockMapper.CalculateCompressedSize()
   - 找出涉及的 StorageBlock
   - 按比例计算每个 Block 的压缩贡献
   - 汇总得到压缩后大小

3. 与资源数据使用相同的算法，确保一致性
```

### D3: 数据展示格式

**决策**: 非资源数据与资源数据混合，按偏移排序

**Excel 资源明细 Sheet 格式**:
```
偏移     │ 名称               │ 数据类别 │ 类型          │ 解压后 │ 压缩后 │ ...
0        │ Bundle Header      │ 非资源   │ [BundleMeta]  │ 50 B   │ 50 B   │ ...
50       │ BlocksInfo         │ 非资源   │ [BundleMeta]  │ 2 KB   │ 1 KB   │ ...
200      │ CAB-xxx Header     │ 非资源   │ [FileHeader]  │ 100 B  │ 50 B   │ ...
300      │ CAB-xxx Types      │ 非资源   │ [TypeTree]    │ 5 KB   │ 2 KB   │ ...
800      │ Mesh_001           │ 资源     │ Mesh          │ 1.5 MB │ 0.8 MB │ ...
```

**验证汇总 Sheet 格式**:
```
数据类别      │ 压缩前总计  │ 压缩后总计  │ 占比     │
BundleMeta   │ 2 KB       │ 1 KB       │ 0.1%    │
FileHeader   │ 500 B      │ 250 B      │ 0.05%   │
TypeTree     │ 15 KB      │ 7 KB       │ 0.3%    │
ObjectDir    │ 5 KB       │ 2 KB       │ 0.1%    │
Externals    │ 1 KB       │ 500 B      │ 0.05%   │
资源数据     │ 15 MB      │ 8 MB       │ 99.4%   │
────────────────────────────────────────────────────
计算总计     │ 15.02 MB   │ 8.01 MB    │ 100%    │
文件实际大小 │ -          │ 8.05 MB    │ -       │
误差        │ -          │ 40 KB      │ 0.5%    │
```

### D4: 数据模型扩展

**决策**: 扩展 ResourceInfo，添加枚举区分数据类别

```csharp
// 新增枚举
public enum DataCategory
{
    Resource,     // 资源数据
    NonResource   // 非资源数据
}

// 新增枚举（仅用于非资源数据）
public enum NonResourceType
{
    BundleMeta,   // Bundle Header + BlocksInfo
    FileHeader,   // SerializedFile Header + Version/Platform
    TypeTree,     // Types[] + TypeTree 数据
    ObjectDir,    // Objects[] 目录
    Externals     // ScriptTypes + Externals + RefTypes + UserInfo
}

// 扩展 ResourceInfo
public class ResourceInfo
{
    // ... 现有字段 ...
    public DataCategory DataCategory { get; set; }
    public NonResourceType? NonResourceType { get; set; } // 仅非资源数据有值
}
```

## Risks / Trade-offs

### R1: Metadata 位置计算误差

**风险**: Metadata 各部分边界计算可能因 Unity 版本差异产生误差

**缓解**:
- 使用 SerializedFileFormatVersion 判断版本，应用正确的解析逻辑
- 验证: Header + Types + ObjectDir + Externals 应等于 m_DataOffset
- 若不等，记录警告并在报告中标注为"估算值"

### R2: 块内压缩不均匀

**风险**: 比例计算假设 Block 内压缩均匀，Metadata 可能与资源数据压缩率不同

**缓解**:
- 这是不可避免的近似，与资源数据使用相同方法
- 在验证汇总中显示误差，帮助用户理解精度限制

### R3: 大文件内存占用

**风险**: 重新解析 SerializedFile stream 可能增加内存占用

**缓解**:
- 复用现有的 stream，不重复加载文件内容
- 只追踪位置信息，不解析完整数据

### R4: 反射访问私有字段

**风险**: 使用反射获取 SerializedFile 的 stream 可能受版本影响

**缓解**:
- 在 MetadataPositionTracker 中封装反射逻辑
- 若反射失败，使用重新打开文件的方式