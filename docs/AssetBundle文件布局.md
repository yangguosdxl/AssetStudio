# AssetBundle 文件布局说明

本文档详细说明 Unity AssetBundle 文件的内部结构布局。

## 1. AssetBundle 整体结构

AssetBundle 文件由三大部分组成：

```
┌─────────────────────────────────────────────────────────────┐
│                    AssetBundle 文件                          │
├─────────────────────────────────────────────────────────────┤
│  1. Bundle Header (不压缩)                                   │
│     - signature: "UnityFS" / "UnityWeb" / "UnityRaw"         │
│     - version: Bundle 格式版本                               │
│     - unityVersion: Unity 编辑器版本                         │
│     - size: 文件总大小                                       │
│     - compressedBlocksInfoSize: BlocksInfo 压缩后大小        │
│     - uncompressedBlocksInfoSize: BlocksInfo 解压后大小      │
│     - flags: 压缩类型和布局标志                              │
├─────────────────────────────────────────────────────────────┤
│  2. BlocksInfo Metadata (可能压缩)                           │
│     - uncompressedDataHash: 16字节哈希                       │
│     - blocksInfoCount: 数据块数量                            │
│     - StorageBlock[]: 各数据块的压缩/解压大小和标志           │
│     - nodesCount: 内部文件数量                               │
│     - Node[]: 各内部文件的偏移、大小、路径                    │
├─────────────────────────────────────────────────────────────┤
│  3. DataBlocks (可能压缩)                                    │
│     - Block 0: 解压后包含所有内部文件数据                     │
│     - Block 1: (如有)                                        │
│     - ...                                                    │
│                                                              │
│  DataBlocks 解压后的内容:                                    │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  SerializedFile 0 (如 CAB-xxx.sharedAssets)             ││
│  │    - File Header + Metadata                             ││
│  │    - Object Data 0                                      ││
│  │    - Object Data 1                                      ││
│  │    - ...                                                ││
│  ├─────────────────────────────────────────────────────────┤│
│  │  SerializedFile 1 (如 CAB-xxx)                          ││
│  │    - File Header + Metadata                             ││
│  │    - Object Data 0                                      ││
│  │    - ...                                                ││
│  ├─────────────────────────────────────────────────────────┤│
│  │  ResourceFile (如 .resS)                                ││
│  │    - 原始资源数据                                        ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

## 2. Bundle Header 详细结构

Bundle Header 位于文件开头，**不被压缩**，大小固定或可计算。

### UnityFS 格式 (version 7+)

| 字段 | 类型 | 大小 | 说明 |
|------|------|------|------|
| signature | string + null | ~8字节 | "UnityFS" |
| version | uint32 | 4 | Bundle 格式版本，通常为 7 |
| unityVersion | string + null | ~10字节 | Unity 版本如 "5.x.x" |
| unityRevision | string + null | ~10字节 | Unity Git revision |
| size | int64 | 8 | 文件总大小 |
| compressedBlocksInfoSize | uint32 | 4 | BlocksInfo 压缩后大小 |
| uncompressedBlocksInfoSize | uint32 | 4 | BlocksInfo 解压后大小 |
| flags | uint32 | 4 | ArchiveFlags 位标志 |

### ArchiveFlags 位定义

| 位 | 名称 | 说明 |
|----|------|------|
| 0-5 | CompressionTypeMask | 压缩类型：0=None, 1=LZMA, 2=LZ4, 3=LZ4HC |
| 6 | BlocksInfoAtTheEnd | BlocksInfo 位于文件末尾 |
| 7 | BlockInfoNeedPaddingAtStart | Block 需要起始填充 |

### Header 大小计算公式

```
HeaderSize = signature长度 + 1 + 4 + unityVersion长度 + 1 + unityRevision长度 + 1 + 8 + 4 + 4 + 4
```

典型值：约 50 字节

## 3. BlocksInfo Metadata 详细结构

BlocksInfo 包含数据块和文件目录信息，可能被压缩（根据 flags）。

解压后的结构：

```
┌────────────────────────────────────────────────────┐
│ BlocksInfo (解压后)                                │
├────────────────────────────────────────────────────┤
│ uncompressedDataHash (16 bytes)                    │
│ blocksInfoCount (int32)                            │
│ ┌────────────────────────────────────────────────┐ │
│ │ StorageBlock[0]                                │ │
│ │   - uncompressedSize (uint32)                  │ │
│ │   - compressedSize (uint32)                    │ │
│ │   - flags (uint16)                             │ │
│ ├────────────────────────────────────────────────┤ │
│ │ StorageBlock[1]                                │ │
│ │   - ...                                        │ │
│ └────────────────────────────────────────────────┘ │
│ nodesCount (int32)                                 │
│ ┌────────────────────────────────────────────────┐ │
│ │ Node[0]                                        │ │
│ │   - offset (int64): 在解压后 blocksStream 中的偏移│ │
│ │   - size (int64): 文件大小                     │ │
│ │   - flags (uint32)                             │ │
│ │   - path (string): 文件路径如 "CAB-xxx"        │ │
│ ├────────────────────────────────────────────────┤ │
│ │ Node[1]                                        │ │
│ │   - ...                                        │ │
│ └────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────┘
```

### StorageBlockFlags 位定义

| 位 | 名称 | 说明 |
|----|------|------|
| 0-5 | CompressionTypeMask | 压缩类型 |

## 4. SerializedFile 内部结构

每个 SerializedFile (如 CAB-xxx.assets) 包含：

```
┌─────────────────────────────────────────────────────────────┐
│                    SerializedFile                            │
├─────────────────────────────────────────────────────────────┤
│  Metadata (从文件开头到 m_DataOffset)                         │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ 1. File Header                                          ││
│  │    - metadataSize (uint32)                              ││
│  │    - fileSize (uint32/int64)                            ││
│  │    - version (uint32): SerializedFileFormatVersion      ││
│  │    - dataOffset (uint32/int64)                          ││
│  │    - endianess (byte): 0=小端, 1=大端                   ││
│  │    - reserved (3 bytes)                                 ││
│  │    (version >= 22 时有扩展字段)                          ││
│  ├─────────────────────────────────────────────────────────┤│
│  │ 2. Version/Platform                                     ││
│  │    - unityVersion (string)                              ││
│  │    - targetPlatform (int32)                             ││
│  │    - enableTypeTree (bool)                              ││
│  ├─────────────────────────────────────────────────────────┤│
│  │ 3. Types + TypeTree                                     ││
│  │    - typeCount (int32)                                  ││
│  │    - SerializedType[]:                                  ││
│  │      - classID (int32)                                  ││
│  │      - isStrippedType (bool)                            ││
│  │      - scriptTypeIndex (int16)                          ││
│  │      - oldTypeHash (16 bytes)                           ││
│  │      - TypeTree nodes (如启用)                          ││
│  │      - typeDependencies (int32[])                       ││
│  ├─────────────────────────────────────────────────────────┤│
│  │ 4. ObjectDir (Objects 数组)                             ││
│  │    - objectCount (int32)                                ││
│  │    - ObjectInfo[]:                                      ││
│  │      - pathID (int32/int64)                             ││
│  │      - byteStart (uint32/int64)                         ││
│  │      - byteSize (uint32)                                ││
│  │      - typeID (int32)                                   ││
│  │      - classID (uint16) 或 typeIndex                    ││
│  ├─────────────────────────────────────────────────────────┤│
│  │ 5. Externals                                            ││
│  │    - scriptTypesCount + LocalSerializedObjectIdentifier[]││
│  │    - externalsCount + FileIdentifier[]                  ││
│  │      - guid (16 bytes)                                  ││
│  │      - type (int32)                                     ││
│  │      - pathName (string)                                ││
│  │    - refTypesCount + SerializedType[] (如支持)          ││
│  │    - userInformation (string)                           ││
│  └─────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────┤
│  Data Area (从 m_DataOffset 开始)                            │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Object Data 0                                           ││
│  │   - 根据 TypeTree 结构序列化的资源数据                   ││
│  ├─────────────────────────────────────────────────────────┤│
│  │ Object Data 1                                           ││
│  │   - ...                                                 ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### SerializedFileFormatVersion 关键版本

| 版本值 | 名称 | 说明 |
|--------|------|------|
| 2 | Unknown_2 | 早期格式 |
| 7 | Unknown_7 | 引入 unityVersion, bigIDEnabled |
| 9 | Unknown_9 | 引入 endianess 字段 |
| 14 | Unknown_14 | PathID 改为 int64 + alignment |
| 22 | LargeFilesSupport | fileSize/dataOffset 改为 int64 |

## 5. 数据大小关系

### 压缩前后大小对照

```
Bundle 文件实际大小 = HeaderSize + compressedBlocksInfoSize + Σ(compressedBlock[i])

解压后总大小 = HeaderSize + uncompressedBlocksInfoSize + Σ(uncompressedBlock[i])

压缩率 = compressedSize / uncompressedSize
```

### SerializedFile 大小分布

```
SerializedFile 大小 = MetadataSize + DataSize

MetadataSize = m_DataOffset (精确值)
  = Header + Version/Platform + Types + TypeTree + ObjectDir + Externals

DataSize = fileSize - m_DataOffset
  = Σ(ObjectInfo.byteSize)
```

## 6. 非资源数据分类

在 AssetStudio.CLI.Analyzer 报告中，非资源数据分为以下类别：

| 类别 | 包含内容 | 位置 |
|------|----------|------|
| BundleMeta | Bundle Header + BlocksInfo | Bundle 级 |
| FileHeader | SerializedFile Header + Version/Platform | 每个 SerializedFile |
| TypeTree | Types 数组 + TypeTree 数据 | 每个 SerializedFile |
| ObjectDir | Objects 数组 (ObjectInfo[]) | 每个 SerializedFile |
| Externals | ScriptTypes + Externals + RefTypes + UserInfo | 每个 SerializedFile |

## 7. 常见文件类型

| 文件名模式 | 类型 | 说明 |
|------------|------|------|
| CAB-xxx | SerializedFile | 包含资源对象的主要文件 |
| CAB-xxx.sharedAssets | SerializedFile | 共享类型定义文件 |
| *.resS | ResourceFile | 外部资源数据文件 |
| *.assets | SerializedFile | 场景或预制体文件 |

## 8. SerializedFile 深入分析

SerializedFile 是 Unity 资源系统的核心载体，承载了所有游戏资源的定义和实际数据。

### 8.1 SerializedFile 的核心作用

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SerializedFile 在资源系统中的位置                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   AssetBundle                                                           │
│       │                                                                 │
│       ├── CAB-xxx.sharedAssets (类型定义文件)                            │
│       │       │                                                         │
│       │       ├── SerializedType[]: 定义所有可用类型                     │
│       │       │   ┌─────────────────────────────────────────────────┐   │
│       │       │   │ TypeTree: 描述每个类型的字段布局                   │   │
│       │       │   │   - MonoBehaviour: m_Script, m_Name...           │   │
│       │       │   │   - Texture2D: m_Width, m_Height, m_TextureFormat│   │
│       │       │   │   - Mesh: m_Vertices, m_Normals, m_Triangles...  │   │
│       │       │   └─────────────────────────────────────────────────┘   │
│       │       └── AssetBundle 对象: 定义 Bundle 内容清单                 │
│       │                                                                 │
│       └── CAB-xxx (资源数据文件)                                         │
│       │       │                                                         │
│       │       ├── ObjectInfo[]: 资源对象索引                             │
│       │       │   ┌─────────────────────────────────────────────────┐   │
│       │       │   │ pathID=1 → Mesh, 偏移=100, 大小=5000             │   │
│       │       │   │ pathID=2 → Texture2D, 偏移=5100, 大小=2000       │   │
│       │       │   │ pathID=3 → MonoBehaviour, 偏移=7100, 大小=300    │   │
│       │       │   └─────────────────────────────────────────────────┘   │
│       │       └── Object Data[]: 实际资源数据                            │
│       │           根据 TypeTree 结构反序列化                             │
│       │                                                                 │
│       └── *.resS (外部资源数据文件 - ResourceFile)                       │
│               │                                                         │
│               └── 大型资源原始数据（纹理、音频等）                        │
│                   通过 StreamingInfo 引用                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.2 File Header 深入解析

File Header 是 SerializedFile 的入口，决定了后续数据的解析方式。

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         File Header 结构详解                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  版本 < 9 (早期格式):                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ m_MetadataSize (uint32)      │ 文件元数据部分大小                    │ │
│  │ m_FileSize (uint32)          │ 整个文件的大小                        │ │
│  │ m_Version (uint32)           │ 格式版本号                            │ │
│  │ m_DataOffset (uint32)        │ 数据区起始偏移                        │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  版本 >= 9 (现代格式):                                                   │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ m_MetadataSize (uint32)      │                                      │ │
│  │ m_FileSize (uint32)          │                                      │ │
│  │ m_Version (uint32)           │                                      │ │
│  │ m_DataOffset (uint32)        │                                      │ │
│  │ m_Endianess (byte)           │ 0=小端(Little), 1=大端(Big)          │ │
│  │ m_Reserved[3] (bytes)        │ 保留字段                              │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  版本 >= 22 (LargeFilesSupport, 2020.1+):                               │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ ...基础字段...                                                       │ │
│  │ m_MetadataSize (uint32)      │ 重读，扩展后的元数据大小              │ │
│  │ m_FileSize (int64)           │ 扩展为 64 位，支持超大文件            │ │
│  │ m_DataOffset (int64)         │ 扩展为 64 位                          │ │
│  │ unknown (int64)              │ 未使用字段                            │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Header 字段逻辑含义详解

| 字段 | 逻辑含义 | 使用场景 |
|------|----------|----------|
| **m_MetadataSize** | Metadata 部分的大小（不含 Header 本身） | 用于定位 Metadata 结束位置，计算：`MetadataEnd = HeaderSize + m_MetadataSize` |
| **m_FileSize** | 整个 SerializedFile 的总大小 | 校验文件完整性：读取结束后检查是否到达此位置 |
| **m_Version** | SerializedFileFormatVersion | 决定后续所有字段的读取逻辑（字段是否存在、类型大小等） |
| **m_DataOffset** | 数据区起始位置 | 分隔 Metadata 和 Data Area：Metadata 在 `[HeaderSize, m_DataOffset)` |
| **m_Endianess** | 数据字节序 | **关键**：决定后续所有数值的读取方式（Unity 大多数使用小端序） |
| **m_Reserved** | 保留字段 | 版本升级预留空间，当前未使用 |

#### 版本演进对 Header 的影响

```
版本演进历史:

v2 (Unity 1.x):  基础 4 字段
v7 (Unity 3.0):  增加 unityVersion 字符串、bigIDEnabled
v9 (Unity 3.5):  增加 endianess + reserved，数据读取开始区分字节序
v14 (Unity 5.0): PathID 从 int32 变为 int64（需要 alignment）
v22 (Unity 2020): fileSize/dataOffset 变为 int64，支持超过 4GB 的文件

影响链：
  m_Version → 决定字段存在与否 → 决定读取逻辑 → 影响解析正确性
```

### 8.3 Version/Platform 部分详解

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Version/Platform 部分结构                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  版本 >= 7:                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ unityVersion (string)       │ 如 "2022.3.50f1c1"                   │ │
│  │                             │ 格式: major.minor.patch[buildType]   │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  版本 >= 8:                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ m_TargetPlatform (int32)    │ BuildTarget 枚举值                   │ │
│  │                             │ 如: StandaloneWindows=5, Android=13  │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  版本 >= 13:                                                            │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ m_EnableTypeTree (bool)     │ 是否包含 TypeTree 数据               │ │
│  │                             │ true: 每个 Type 后有 TypeTree        │ │
│  │                             │ false: 无 TypeTree，需外部 DLL       │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### unityVersion 的解析与用途

```
unityVersion 字符串示例:
  "2022.3.50f1c1"

解析后:
  version = [2022, 3, 50, 1]  // major, minor, patch, buildNumber
  buildType = "f"            // f=Final, a=Alpha, b=Beta, rc=ReleaseCandidate

用途:
  1. 决定 TypeTree 字段版本 (每个字段有 m_Version)
  2. 决定某些类型的新增字段是否存在
  3. API 兼容性判断
```

#### BuildTarget 枚举与平台特性

| 平台 | 枚举值 | 资源特性 |
|------|--------|----------|
| StandaloneWindows | 5 | 纹理格式支持 DXT、BC |
| Android | 13 | 纹理格式支持 ETC、ASTC、PVRTC |
| iOS | 9 | 纹理格式支持 PVRTC |
| WebGL | 20 | 纹理格式受限，需转换 |

### 8.4 Types + TypeTree 深入分析

这是 SerializedFile 最复杂的部分，定义了所有资源的"骨架"。

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   Types + TypeTree 完整结构                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  typeCount (int32): 类型数量                                             │
│                                                                         │
│  SerializedType[0]:                                                     │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ classID (int32)              │ 类型ID，如 1=GameObject, 43=Mesh    │ │
│  │                              │ 114=MonoBehaviour, 28=Texture2D     │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ 版本 >= 16:                                                          │ │
│  │ m_IsStrippedType (bool)      │ 是否为剥离类型（仅保留必要数据）      │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ 版本 >= 17:                                                          │ │
│  │ m_ScriptTypeIndex (int16)    │ 脚本类型索引（MonoBehaviour相关）    │ │
│  │                              │ -1: 非脚本类型                       │ │
│  │                              │ >=0: 对应 m_ScriptTypes[index]      │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ 版本 >= 13:                                                          │ │
│  │ m_ScriptID (bytes[16])       │ 脚本 GUID（仅 MonoBehaviour）       │ │
│  │ m_OldTypeHash (bytes[16])    │ 类型哈希，用于版本兼容性验证         │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ TypeTree (如 m_EnableTypeTree=true):                                │ │
│  │ ┌─────────────────────────────────────────────────────────────────┐│ │
│  │ │ 版本 >= 12 或 == 10 (Blob格式):                                  ││ │
│  │ │   numberOfNodes (int32)     │ TypeTree 节点数量                 ││ │
│  │ │   stringBufferSize (int32)  │ 字符串缓冲区大小                  ││ │
│  │ │   Nodes[numberOfNodes]:     │ 节点数据                          ││ │
│  │ │     ┌───────────────────────────────────────────────────────────┐││ │
│  │ │     │ version >= 19:                                          │││ │
│  │ │     │   m_TypeStrOffset (uint32) │ 类型名在字符串缓冲区的偏移  │││ │
│  │ │     │   m_NameStrOffset (uint32) │ 字段名在字符串缓冲区的偏移  │││ │
│  │ │     │   m_ByteSize (int32)      │ 字段大小（字节）             │││ │
│  │ │     │   m_Index (int32)         │ 索引位置                     │││ │
│  │ │     │   m_TypeFlags (int32)     │ 是否数组等标志               │││ │
│  │ │     │   m_Version (int32)       │ 字段版本                     │││ │
│  │ │     │   m_MetaFlag (int32)      │ 对齐等元数据标志             │││ │
│  │ │     │   m_RefTypeHash (uint64)  │ 引用类型哈希（版本 >= 19）   │││ │
│  │ │     └───────────────────────────────────────────────────────────┘││ │
│  │ │   StringBuffer[stringBufferSize] │ 所有类型名和字段名           ││ │
│  │ │                                                                   ││ │
│  │ │ 版本 < 12 (Legacy格式):                                          ││ │
│  │ │   递归读取节点:                                                   ││ │
│  │ │     m_Type (string)              │ 类型名                        ││ │
│  │ │     m_Name (string)              │ 字段名                        ││ │
│  │ │     m_ByteSize (int32)           │                               ││ │
│  │ │     m_Index (int32)              │                               ││ │
│  │ │     m_TypeFlags (int32)          │                               ││ │
│  │ │     m_Version (int32)            │                               ││ │
│  │ │     m_MetaFlag (int32)           │                               ││ │
│  │ │     childrenCount (int32)        │ 子节点数量                    ││ │
│  │ │     递归读取子节点...                                            ││ │
│  │ └─────────────────────────────────────────────────────────────────┘│ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ 版本 >= 21:                                                          │ │
│  │ m_TypeDependencies (int32[]) │ 该类型依赖的其他类型ID              │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### TypeTree 的作用与重要性

```
TypeTree 是 Unity 序列化的核心机制：

┌─────────────────────────────────────────────────────────────────────────┐
│  为什么需要 TypeTree？                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. 版本兼容性                                                           │
│     ┌─────────────────────────────────────────────────────────────────┐ │
│     │ Unity 2020 打包的 Bundle:                                       │ │
│     │   Mesh.m_Vertices: m_Version=2200                              │ │
│     │                                                                 │ │
│     │ Unity 2022 加载时:                                              │ │
│     │   发现 Mesh.m_Vertices.m_Version > 当前版本                    │ │
│     │   知道这是新字段，可以安全跳过                                   │ │
│     │                                                                 │ │
│     │ Unity 2019 加载时:                                              │ │
│     │   Mesh.m_Vertices.m_Version < 当前版本                          │ │
│     │   旧数据，使用默认值填充缺失字段                                 │ │
│     └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  2. 无需 DLL 反序列化                                                    │
│     ┌─────────────────────────────────────────────────────────────────┐ │
│     │ 传统序列化: 需要 DLL 才知道 Mesh.m_Vertices 的位置和大小        │ │
│     │                                                                 │ │
│     │ TypeTree 序列化: 自包含，TypeTree 告诉你每个字段的位置          │ │
│     │   m_Vertices: 偏移=0, 大小=数组                                  │ │
│     │   m_Normals: 偏移=vertices后, 大小=数组                         │ │
│     │   ...                                                            │ │
│     └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  3. 跨平台一致性                                                         │
│     ┌─────────────────────────────────────────────────────────────────┐ │
│     │ Windows 打包 → Android 加载                                     │ │
│     │ Android 打包 → iOS 加载                                          │ │
│     │ TypeTree 保证数据结构一致，无需平台特定 DLL                      │ │
│     └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### TypeTree 实例分析 - Texture2D 类型

```
Texture2D 的 TypeTree 结构（简化版）:

┌─────────────────────────────────────────────────────────────────────────┐
│ Texture2D (classID=28)                                                  │
├─────────────────────────────────────────────────────────────────────────┤
│ Level 0:                                                                │
│   m_Type="Texture2D", m_Name="Base"                                     │
│                                                                         │
│ Level 1 (子字段):                                                        │
│   m_Type="int",    m_Name="m_Width"           → 偏移=0,  大小=4        │
│   m_Type="int",    m_Name="m_Height"          → 偏移=4,  大小=4        │
│   m_Type="int",    m_Name="m_CompleteImageSize" → 偏移=8, 大小=4       │
│   m_Type="int",    m_Name="m_TextureFormat"   → 偏移=12, 大小=4        │
│   m_Type="bool",   m_Name="m_MipMap"          → 偏移=16, 大小=1        │
│   m_Type="bool",   m_Name="m_IsReadable"      → 偏移=17, 大小=1        │
│   m_Type="int",    m_Name="m_ReadCount"       → 偏移=18, 大小=4        │
│   ...                                                                   │
│   m_Type="Array",  m_Name="image_data"        → 数组类型               │
│     Level 2:                                                            │
│       m_Type="char", m_Name="data"              → 每个元素=1字节       │
│   ...                                                                   │
│   m_Type="StreamingInfo", m_Name="m_StreamData" → 外部资源引用         │
│     Level 2:                                                            │
│       m_Type="ulong",  m_Name="offset"          → 偏移=8字节           │
│       m_Type="uint",   m_Name="size"            → 偏移=16字节          │
│       m_Type="string", m_Name="path"            → 偏移=20字节          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

解析示例数据流:
  byteStart = 100 (数据区开始位置)
  
  读取 m_Width:   Position=100+0=100,   ReadInt32() → 1024
  读取 m_Height:  Position=100+4=104,   ReadInt32() → 768
  读取 m_Format:  Position=100+12=112,  ReadInt32() → 28 (DXT5)
  
  读取 image_data:
    先读数组长度 → 50000
    Position 移动到数组数据开始
    读取 50000 字节的纹理数据
  
  或读取 m_StreamData:
    发现 path="sharedassets.assets.resS"
    offset=0, size=50000
    → 数据在外部文件，需要从 resS 文件读取
```

### 8.5 ObjectDir (Objects 数组) 详解

ObjectDir 是资源对象的"索引表"，记录了每个资源的位置和元信息。

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       ObjectDir 结构详解                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  objectCount (int32): 本 SerializedFile 包含的资源对象数量               │
│                                                                         │
│  ObjectInfo[0]:                                                         │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ 版本 >= 7 且 < 14:                                                   │ │
│  │   bigIDEnabled (int32)       │ 决定 PathID 的读取方式               │ │
│  │                             │ 0: int32 PathID                       │ │
│  │                             │ 非0: int64 PathID                     │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ PathID:                                                              │ │
│  │   版本 < 14 且 bigIDEnabled=0: m_PathID (int32)                     │ │
│  │   版本 < 14 且 bigIDEnabled≠0: m_PathID (int64)                     │ │
│  │   版本 >= 14: Align(4) + m_PathID (int64)                           │ │
│  │                             │ PathID 是对象的唯一标识               │ │
│  │                             │ 同一 SerializedFile 内不重复          │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ byteStart:                                                           │ │
│  │   版本 < 22: byteStart (uint32)                                     │ │
│  │   版本 >= 22: byteStart (int64)                                     │ │
│  │   实际位置 = byteStart + m_DataOffset                               │ │
│  │                             │ 对象数据在数据区的起始位置             │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ byteSize (uint32)           │ 对象数据的字节大小                    │ │
│  │                             │ 精确值，不含 alignment                │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ typeID (int32)              │ 类型索引                               │ │
│  │   版本 < 16: typeID=classID │ 直接是类型ID                          │ │
│  │   版本 >= 16: typeID=index  │ 是 m_Types 数组的索引                 │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ classID:                                                             │ │
│  │   版本 < 16: classID (uint16)                                       │ │
│  │   版本 >= 16: 从 m_Types[typeID].classID 获取                       │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ 版本 < 11:                                                           │ │
│  │   isDestroyed (uint16)       │ 已废弃                               │ │
│  ├────────────────────────────────────────────────────────────────────┤ │
│  │ 版本 == 15 或 == 16:                                                │ │
│  │   stripped (byte)           │ 是否为剥离对象                        │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### ObjectInfo 各字段的逻辑含义

| 字段 | 逻辑含义 | 实际用途 |
|------|----------|----------|
| **PathID** | 对象唯一标识 | 用于引用查找：PPtr 通过 `(fileID, pathID)` 定位对象 |
| **byteStart** | 数据区偏移 | 定位对象数据：`Position = m_DataOffset + byteStart` |
| **byteSize** | 数据大小 | 数据边界：`Position + byteSize` 是对象结束位置 |
| **typeID** | 类型标识 | 决定如何解析：根据 `m_Types[typeID]` 的 TypeTree 结构读取 |
| **classID** | 类型ID | 类型分类：1=GameObject, 43=Mesh, 28=Texture2D 等 |
| **stripped** | 剥离标志 | 表示对象已被精简（删除不必要的字段） |

#### ObjectInfo 与 PPtr 的关系

```
PPtr (Persistent Pointer) 是 Unity 资源引用的核心机制：

┌─────────────────────────────────────────────────────────────────────────┐
│  PPtr<T> 结构                                                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  m_FileID (int32):                                                      │
│    0     → 本 SerializedFile                                            │
│    > 0   → 引用 m_Externals[m_FileID-1] 指定的外部文件                  │
│                                                                         │
│  m_PathID (int32/int64):                                                │
│    在目标 SerializedFile 中查找 ObjectsDic[m_PathID]                    │
│                                                                         │
│  解析示例:                                                               │
│                                                                         │
│  PPtr<Mesh> pptr;                                                       │
│  pptr.m_FileID = 0;                                                     │
│  pptr.m_PathID = 5;                                                     │
│                                                                         │
│  → 在本 SerializedFile.m_Objects 中找到 pathID=5 的 ObjectInfo          │
│  → ObjectInfo.classID = 43 (Mesh)                                       │
│  → Position = m_DataOffset + ObjectInfo.byteStart                       │
│  → 根据 m_Types[Mesh].TypeTree 解析 Mesh 数据                           │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

跨文件引用示例:

  SerializedFile A (CAB-xxx):
    m_Externals[0] = { guid=..., pathName="CAB-yyy" }
    
  Object A.Mesh → m_Material (PPtr<Material>):
    m_FileID = 1       → 引用 m_Externals[0] = CAB-yyy
    m_PathID = 10      → 在 CAB-yyy 中找 pathID=10
    
  → 需要先加载 CAB-yyy
  → 在 CAB-yyy.m_Objects 中找到 pathID=10
  → 解析 Material 数据
```

### 8.6 Externals 部分详解

Externals 记录了本 SerializedFile 依赖的外部文件信息。

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       Externals 完整结构                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ScriptTypes 部分 (版本 >= 11):                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ scriptTypesCount (int32)                                            │ │
│  │                                                                     │ │
│  │ LocalSerializedObjectIdentifier[i]:                                │ │
│  │   localSerializedFileIndex (int32)  │ 引用的本地文件索引            │ │
│  │   localIdentifierInFile (int32/int64) │ 对象 PathID                 │ │
│  │                                                                     │ │
│  │ 作用: 记录本 Bundle 内的 MonoBehaviour 脚本类型引用                 │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  Externals 部分:                                                         │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ externalsCount (int32)                                              │ │
│  │                                                                     │ │
│  │ FileIdentifier[i]:                                                  │ │
│  │   版本 >= 6:                                                        │ │
│  │     tempEmpty (string)      │ 空字符串（废弃）                      │ │
│  │   版本 >= 5:                                                        │ │
│  │     guid (bytes[16])        │ 文件的 GUID 标识                      │ │
│  │     type (int32)            │ 文件类型                              │ │
│  │                               0 = NonAssetFile                      │ │
│  │                               2 = SerializedAssetType               │ │
│  │                               3 = MetaAssetType                     │ │
│  │   pathName (string)         │ 文件路径                              │ │
│  │                               如 "Library/assetBundleAssets/xxx"    │ │
│  │   fileName                   │ 提取的文件名                          │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  RefTypes 部分 (版本 >= 20):                                             │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ refTypesCount (int32)                                               │ │
│  │                                                                     │ │
│  │ SerializedType[i] (isRefType=true):                                │ │
│  │   同普通 SerializedType，但额外有:                                  │ │
│  │   m_KlassName (string)      │ 类名                                  │ │
│  │   m_NameSpace (string)      │ 命名空间                              │ │
│  │   m_AsmName (string)        │ 程序集名                              │ │
│  │                                                                     │ │
│  │ 作用: 记录外部 Assembly 的类型引用（用于 IL2CPP 反序列化）           │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  UserInformation 部分 (版本 >= 5):                                       │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ userInformation (string)    │ 用户自定义信息                        │ │
│  │                               通常为空或包含打包信息                 │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### FileIdentifier 的作用

```
FileIdentifier 用于解决跨 Bundle 引用:

┌─────────────────────────────────────────────────────────────────────────┐
│  跨 Bundle 引用示例                                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Bundle A (materials.assetbundle):                                      │
│    SerializedFile "CAB-materials":                                      │
│      m_Externals[0] = {                                                 │
│        guid = "a1b2c3d4e5f6...",                                        │
│        type = 3,                                                        │
│        pathName = "textures.assetbundle"                                │
│      }                                                                  │
│                                                                         │
│      Material 对象:                                                      │
│        m_Texture (PPtr<Texture2D>):                                     │
│          m_FileID = 1    → 引用 m_Externals[0]                          │
│          m_PathID = 5    → textures.assetbundle 中的 pathID=5           │
│                                                                         │
│  解析流程:                                                               │
│    1. 检查 m_FileID = 1                                                 │
│    2. 查找 m_Externals[0]，发现依赖 textures.assetbundle               │
│    3. 加载 textures.assetbundle                                         │
│    4. 在 textures.assetbundle 的 m_Objects 中找 pathID=5               │
│    5. 解析 Texture2D 数据                                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

GUID 的作用:
  Unity 使用 GUID 标识文件，而非路径名
  路径名可能变化，GUID 保持不变
  Build 时 GUID 映射到实际的 Bundle 文件名
```

### 8.7 Data Area 数据解析流程

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    数据区解析流程详解                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  输入: ObjectInfo 对象                                                   │
│    - pathID = 10                                                        │
│    - byteStart = 1000                                                   │
│    - byteSize = 500                                                     │
│    - typeID = 2                                                         │
│    - classID = 43 (Mesh)                                                │
│                                                                         │
│  步骤 1: 获取 TypeTree                                                   │
│    serializedType = m_Types[typeID]                                     │
│    typeTree = serializedType.m_Type                                     │
│                                                                         │
│  步骤 2: 定位数据                                                         │
│    reader.Position = m_DataOffset + byteStart                           │
│    // m_DataOffset 是 Metadata 结束位置                                 │
│    // byteStart 是相对于数据区的偏移                                     │
│                                                                         │
│  步骤 3: 按 TypeTree 结构读取                                             │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ Mesh 的 TypeTree 字段:                                          │ │
│    │                                                                 │ │
│    │ m_Name (string):                                                │ │
│    │   // 读取对齐字符串                                              │ │
│    │   name = reader.ReadAlignedString()                             │ │
│    │                                                                 │ │
│    │ m_Vertices (Vector3[]):                                         │ │
│    │   // 数组读取                                                    │ │
│    │   count = reader.ReadInt32()                                    │ │
│    │   vertices = new Vector3[count]                                 │ │
│    │   for (i = 0; i < count; i++)                                   │ │
│    │     vertices[i] = reader.ReadVector3()  // 3 * float = 12 bytes │ │
│    │                                                                 │ │
│    │ m_Normals (Vector3[]):                                          │ │
│    │   // 同上                                                        │ │
│    │                                                                 │ │
│    │ m_Triangles (int[]):                                            │ │
│    │   count = reader.ReadInt32()                                    │ │
│    │   triangles = reader.ReadInt32Array(count)                      │ │
│    │                                                                 │ │
│    │ ... 其他字段                                                     │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  步骤 4: 验证读取边界                                                     │
│    currentPosition = reader.Position                                    │
│    expectedEnd = m_DataOffset + byteStart + byteSize                    │
│    if (currentPosition > expectedEnd)                                   │
│      → 解析错误，数据溢出                                                │
│    if (currentPosition < expectedEnd)                                   │
│      → 有填充字节或未读字段                                              │
│                                                                         │
│  步骤 5: 构造对象                                                         │
│    mesh = new Mesh()                                                    │
│    mesh.m_Name = name                                                   │
│    mesh.m_Vertices = vertices                                           │
│    mesh.m_Normals = normals                                             │
│    mesh.m_Triangles = triangles                                         │
│    ...                                                                  │
│                                                                         │
│  输出: Mesh 对象实例                                                      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 8.8 实际数据解析示例

以下是一个完整的 Mesh 对象解析示例：

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Mesh 对象解析示例                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ObjectInfo:                                                             │
│    pathID = 1                                                            │
│    byteStart = 100                                                       │
│    byteSize = 1500                                                       │
│    classID = 43 (Mesh)                                                   │
│                                                                         │
│  数据区位置: m_DataOffset + 100 = 26064 + 100 = 26164                   │
│                                                                         │
│  TypeTree (Mesh):                                                        │
│    0: Mesh Base                                                          │
│    1: string m_Name                                                      │
│    1: Vector3[] m_Vertices                                               │
│    2: int size → 3: Vector3 data                                         │
│    1: Vector3[] m_Normals                                                │
│    1: Vector3[] m_Tangents                                               │
│    1: int[] m_Triangles                                                  │
│    ...                                                                   │
│                                                                         │
│  原始数据流 (十六进制):                                                   │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ 位置 26164:                                                         │ │
│  │ 00 00 00 10          → int32, m_Name 长度 = 16                     │ │
│  │ 45 66 66 65 63...    → UTF-8 字符串 "EffectMesh"                   │ │
│  │ 00                   → null terminator                             │ │
│  │ (alignment padding)  → 对齐到 4 字节                                │ │
│  │                                                                     │ │
│  │ 位置 26184:                                                         │ │
│  │ 00 00 00 64          → int32, m_Vertices 数量 = 100                │ │
│  │                                                                     │ │
│  │ 位置 26188 (每个 Vector3 = 12 bytes):                              │ │
│  │ 00 00 80 3F          → float 1.0  (x)                              │ │
│  │ 00 00 00 00          → float 0.0  (y)                              │ │
│  │ 00 00 00 00          → float 0.0  (z)                              │ │
│  │ ... (99 more Vector3s = 1188 bytes)                                │ │
│  │                                                                     │ │
│  │ 位置 26376:                                                         │ │
│  │ 00 00 00 64          → int32, m_Normals 数量 = 100                 │ │
│  │ ... (100 Vector3s = 1200 bytes)                                    │ │
│  │                                                                     │ │
│  │ 位置 26580:                                                         │ │
│  │ 00 00 00 64          → int32, m_Tangents 数量 = 100                │ │
│  │ ...                                                                 │ │
│  │                                                                     │ │
│  │ 位置 ...:                                                           │ │
│  │ 00 00 00 C0          → int32, m_Triangles 数量 = 192               │ │
│  │ ...                                                                 │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  解析结果:                                                               │
│    Mesh {                                                                │
│      m_Name = "EffectMesh"                                              │
│      m_Vertices = [100 个 Vector3]                                      │
│      m_Normals = [100 个 Vector3]                                       │
│      m_Tangents = [100 个 Vector4]                                      │
│      m_Triangles = [192 个 int]                                         │
│      ...                                                                 │
│    }                                                                     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## 9. ResourceFile 深入分析

ResourceFile 是存储大型资源原始数据的独立文件，通常与 SerializedFile 配合使用。

### 9.1 ResourceFile 的作用与定位

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ResourceFile 在资源系统中的作用                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  为什么需要 ResourceFile？                                               │
│                                                                         │
│  问题: 大型纹理/音频直接存入 SerializedFile                              │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ 纹理数据: 10MB                                                  │ │
│    │ 音频数据: 5MB                                                   │ │
│    │                                                                 │ │
│    │ 全部嵌入 SerializedFile →                                      │ │
│    │   - 加载时必须全部解压                                          │ │
│    │   - 内存占用大                                                  │ │
│    │   - 无法按需加载                                                │ │
│    │   - Bundle 体積过大                                             │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  解决: 外部 ResourceFile (.resS 文件)                                    │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ SerializedFile 只存元数据:                                      │ │
│    │   Texture2D.m_Width = 1024                                      │ │
│    │   Texture2D.m_Height = 768                                      │ │
│    │   Texture2D.m_StreamData.offset = 0                             │ │
│    │   Texture2D.m_StreamData.size = 10MB                            │ │
│    │   Texture2D.m_StreamData.path = "archive.resS"                  │ │
│    │                                                                 │ │
│    │ 实际纹理数据 → archive.resS 文件                                │ │
│    │   按需加载（StreamingAssets）                                   │ │
│    │   不必全部解压                                                  │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.2 ResourceFile 在 AssetBundle 中的位置

```
┌─────────────────────────────────────────────────────────────────────────┐
│                ResourceFile 与 AssetBundle 的关系                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  方式 1: 内嵌在 Bundle 的 DataBlock 中                                   │
│                                                                         │
│  AssetBundle 文件:                                                       │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ Bundle Header                                                   │ │
│    │ BlocksInfo                                                      │ │
│    │ DataBlock (压缩):                                               │ │
│    │   ├─ SerializedFile "CAB-xxx"                                   │ │
│    │   │    ├─ Metadata                                              │ │
│    │   │    ├─ Object Data (小型资源)                                │ │
│    │   │    │    ├─ MonoBehaviour                                    │ │
│    │   │    │    ├─ AssetBundle                                      │ │
│    │   │    │    ├─ GameObject                                       │ │
│    │   │    └────────────────────────                                │ │
│    │   │    └────────────────────────                                │ │
│    │   ├─ ResourceFile "CAB-xxx.resS"                                │ │
│    │   │    ├─ Texture2D 原始数据                                    │ │
│    │   │    ├─ AudioClip 原始数据                                    │ │
│    │   │    ├─ Mesh 原始数据                                         │ │
│    │   │    └────────────────────────                                │ │
│    │   └────────────────────────────────────────────────────────────┘ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  方式 2: 独立的外部文件 (StreamingAssets)                                │
│                                                                         │
│  游戏目录:                                                               │
│    StreamingAssets/                                                     │
│      ├─ archive.resS          ← ResourceFile                           │
│      ├─ textures.resS         ← ResourceFile                           │
│      └────────────────────────                                         │
│                                                                         │
│  AssetBundle:                                                           │
│    SerializedFile.m_Externals 引用 →                                    │
│      { pathName = "StreamingAssets/archive.resS" }                     │
│                                                                         │
│    Texture2D.m_StreamData:                                              │
│      { offset = 0, size = 10MB, path = "archive.resS" }                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.3 StreamingInfo 结构详解

StreamingInfo 是 SerializedFile 中引用 ResourceFile 的结构：

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    StreamingInfo 结构                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  版本 < 2020:                                                            │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ offset (uint32)             │ 在 ResourceFile 中的偏移             │ │
│  │ size (uint32)               │ 数据大小                             │ │
│  │ path (string)               │ ResourceFile 文件名                  │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  版本 >= 2020:                                                           │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ offset (int64)              │ 扩展为 64 位，支持大文件              │ │
│  │ size (uint32)               │                                      │ │
│  │ path (string)               │                                      │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### StreamingInfo 使用示例

```
┌─────────────────────────────────────────────────────────────────────────┐
│                Texture2D 引用 ResourceFile 示例                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  SerializedFile 中的 Texture2D 对象:                                     │
│                                                                         │
│  Texture2D {                                                             │
│    m_Name = "character_texture"                                         │
│    m_Width = 2048                                                        │
│    m_Height = 2048                                                       │
│    m_TextureFormat = DXT5                                               │
│    m_MipCount = 10                                                       │
│    // 数据不在本文件中                                                   │
│    m_StreamData = {                                                      │
│      offset = 5242880    // 5MB，前面是其他纹理                          │
│      size = 4194304      // 4MB                                          │
│      path = "textures.resS"                                              │
│    }                                                                     │
│  }                                                                       │
│                                                                         │
│  ResourceFile "textures.resS" 的布局:                                    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ 偏移 0:                                                            │ │
│  │   Texture "background_texture"                                    │ │
│  │   size = 5MB                                                       │ │
│  │                                                                    │ │
│  │ 偏移 5242880 (5MB):                                                │ │
│  │   Texture "character_texture"                                     │ │
│  │   size = 4MB                                                       │ │
│  │   ┌─────────────────────────────────────────────────────────────┐ │ │
│  │   │ DXT5 压缩纹理数据                                            │ │ │
│  │   │ 2048x2048 像素                                               │ │ │
│  │   │ 10 个 Mipmap 层级                                            │ │ │
│  │   │ ┌─────────────────────────────────────────────────────────┐ │ │ │
│  │   │ │ Mip 0: 2048x2048, 4MB                                    │ │ │ │
│  │   │ │ Mip 1: 1024x1024, 1MB                                    │ │ │ │
│  │   │ │ Mip 2: 512x512, 256KB                                    │ │ │ │
│  │   │ │ ...                                                       │ │ │ │
│  │   │ │ Mip 9: 2x2, 16 bytes                                     │ │ │ │
│  │   │ └─────────────────────────────────────────────────────────┘ │ │ │
│  │   └─────────────────────────────────────────────────────────────┘ │ │
│  │                                                                    │ │
│  │ 偏移 9437184 (9MB):                                                │ │
│  │   Texture "ui_texture"                                            │ │
│  │   ...                                                              │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  加载流程:                                                               │
│    1. 解析 Texture2D 对象，发现 m_StreamData.path = "textures.resS"     │
│    2. 打开 textures.resS 文件                                           │
│    3. Position = m_StreamData.offset = 5MB                              │
│    4. Read(m_StreamData.size = 4MB)                                     │
│    5. 解压缩/转换 DXT5 数据为可渲染格式                                  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.4 ResourceFile 内容格式

ResourceFile 通常存储以下类型的数据：

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ResourceFile 存储的数据类型                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. Texture2D 原始数据                                                   │
│     ┌─────────────────────────────────────────────────────────────────┐ │
│     │ 格式: DXT1/3/5, ETC1/2, PVRTC, ASTC, etc.                       │ │
│     │ 特点: 压缩格式，需 GPU 解码                                      │ │
│     │ 大小: 取决于分辨率和格式                                         │ │
│     │ 例: 2048x2048 DXT5 = 4MB                                        │ │
│     └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  2. AudioClip 原始数据                                                   │
│     ┌─────────────────────────────────────────────────────────────────┐ │
│     │ 格式: OGG Vorbis, FSB (FMOD)                                    │ │
│     │ 特点: 音频压缩格式                                               │ │
│     │ 大小: 取决于时长和质量                                           │ │
│     │ 例: 3分钟 128kbps OGG = ~3MB                                    │ │
│     └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  3. VideoClip 原始数据                                                   │
│     ┌─────────────────────────────────────────────────────────────────┐ │
│     │ 格式: VP8, H.264                                                │ │
│     │ 特点: 视频编码                                                   │ │
│     │ 大小: 较大                                                       │ │
│     └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  4. Mesh 原始数据 (较少见)                                               │
│     ┌─────────────────────────────────────────────────────────────────┐ │
│     │ 格式: 原始顶点数据                                               │ │
│     │ 特点: 可能压缩                                                   │ │
│     └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  数据布局:                                                               │
│     所有数据按偏移顺序紧密排列                                            │
│     无分隔符或标记                                                       │
│     偏移由 SerializedFile 中的 StreamingInfo 确定                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.5 ResourceFile 与 SerializedFile 的协同工作

```
┌─────────────────────────────────────────────────────────────────────────┐
│              SerializedFile + ResourceFile 协同加载流程                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  加载一个包含外部纹理的 AssetBundle:                                      │
│                                                                         │
│  步骤 1: 加载 AssetBundle                                                │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ FileReader.ReadBundleHeader()                                    │ │
│    │ FileReader.ReadBlocksInfo()                                      │ │
│    │ FileReader.DecompressBlocks()                                    │ │
│    │ → 得到 blocksStream                                              │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  步骤 2: 解析 SerializedFile                                             │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ 从 blocksStream 中读取 SerializedFile                            │ │
│    │ 解析 Metadata: Header, Types, ObjectDir                          │ │
│    │ 得到 ObjectInfo 列表                                             │ │
│    │                                                                   │ │
│    │ ObjectInfo[5]:                                                   │ │
│    │   pathID = 5                                                     │ │
│    │   classID = 28 (Texture2D)                                       │ │
│    │   byteStart = 100                                                │ │
│    │   byteSize = 100                                                 │ │
│    │   // 注意：byteSize 很小，只存元数据                              │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  步骤 3: 解析 Texture2D 元数据                                           │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ Position = m_DataOffset + byteStart                              │ │
│    │ 根据 Texture2D 的 TypeTree 读取:                                 │ │
│    │   m_Width = 2048                                                 │ │
│    │   m_Height = 2048                                                │ │
│    │   m_TextureFormat = DXT5                                         │ │
│    │   m_StreamData.offset = 0                                        │ │
│    │   m_StreamData.size = 4194304                                    │ │
│    │   m_StreamData.path = "textures.resS"                            │ │
│    │                                                                   │ │
│    │ // 发现数据在外部文件                                             │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  步骤 4: 加载 ResourceFile                                               │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ 检查 Bundle 是否包含 "textures.resS" Node                        │ │
│    │   → 如果包含：从 blocksStream 对应位置读取                        │ │
│    │   → 如果不包含：从文件系统 StreamingAssets 读取                   │ │
│    │                                                                   │ │
│    │ 打开 ResourceFile                                                │ │
│    │ Position = m_StreamData.offset                                   │ │
│    │ ReadBytes(m_StreamData.size)                                     │ │
│    │ → 得到 4MB DXT5 数据                                             │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  步骤 5: 构造完整 Texture2D                                               │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ Texture2D {                                                      │ │
│    │   m_Width = 2048                                                 │ │
│    │   m_Height = 2048                                                │ │
│    │   m_TextureFormat = DXT5                                         │ │
│    │   image_data = [4MB DXT5 数据]                                   │ │
│    │ }                                                                 │ │
│    │                                                                   │ │
│    │ // 元数据 + 原始数据 组合完成                                     │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  步骤 6: 渲染                                                            │
│    ┌─────────────────────────────────────────────────────────────────┐ │
│    │ GPU 解码 DXT5                                                    │ │
│    │ 创建 Texture 对象                                                │ │
│    │ 应用到 Material                                                   │ │
│    └─────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## 10. 完整资源解析流程总结

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    从 AssetBundle 到游戏对象的完整流程                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. 文件识别                                                             │
│    FileReader → 检测 signature → BundleFile                            │
│                                                                         │
│  2. Bundle 解析                                                          │
│    BundleFile → Header + BlocksInfo + Blocks                            │
│    Decompress Blocks → blocksStream                                    │
│                                                                         │
│  3. 内部文件提取                                                          │
│    blocksStream → Node[] → SerializedFile + ResourceFile               │
│                                                                         │
│  4. SerializedFile 解析                                                  │
│    SerializedFile → Header + Metadata + DataArea                       │
│    Metadata → Types + TypeTree + ObjectDir                             │
│                                                                         │
│  5. 对象定位                                                             │
│    ObjectDir → ObjectInfo → (pathID, byteStart, typeID)                │
│                                                                         │
│  6. 类型获取                                                             │
│    typeID → m_Types[typeID] → TypeTree                                 │
│                                                                         │
│  7. 数据解析                                                             │
│    DataArea + TypeTree → 反序列化 → Unity Object                       │
│                                                                         │
│  8. 外部引用处理                                                          │
│    PPtr → (fileID, pathID) → 跨文件查找                                │
│    StreamingInfo → ResourceFile 加载                                   │
│                                                                         │
│  9. 对象构造                                                             │
│    Unity Object → Mesh, Texture2D, MonoBehaviour, etc.                 │
│                                                                         │
│  10. 游戏使用                                                            │
│    Mesh → MeshRenderer                                                  │
│    Texture2D → Material                                                 │
│    MonoBehaviour → Script Instance                                     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## 11. SerializedFile Externals 结构详解

Externals 部分位于 SerializedFile Metadata 的末尾（ObjectDir 之后），由多个子部分组成。
从 Analyzer 报告角度看，Externals 现已拆分为四个独立的报告条目。

### 11.1 Externals 的四个组成部分

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Externals 结构详解                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. ScriptTypes (version >= HasScriptTypeIndex)                         │
│     ┌───────────────────────────────────────────────────────────────┐   │
│     │ int32 scriptCount                                            │   │
│     │ LocalSerializedObjectIdentifier[scriptCount]:                │   │
│     │   - int32 localSerializedFileIndex                           │   │
│     │   - int32/int64 localIdentifierInFile (取决于 version)       │   │
│     └───────────────────────────────────────────────────────────────┘   │
│     固定大小: scriptCount × (8 或 12) bytes                             │
│                                                                         │
│  2. FileIdentifier (真正的 Externals)                                    │
│     ┌───────────────────────────────────────────────────────────────┐   │
│     │ int32 externalsCount                                         │   │
│     │ FileIdentifier[externalsCount]:                              │   │
│     │   [version >= 6] string tempEmpty (空字符串，通常 1 byte)    │   │
│     │   [version >= 5] Guid guid (16 bytes)                        │   │
│     │   [version >= 5] int32 type                                  │   │
│     │   string pathName                                            │   │
│     └───────────────────────────────────────────────────────────────┘   │
│     固定部分: externalsCount × 20 bytes (version >= 5)                  │
│     动态部分: pathName 长度不定                                          │
│                                                                         │
│  3. RefTypes (version >= SupportsRefObject)                             │
│     ┌───────────────────────────────────────────────────────────────┐   │
│     │ int32 refTypesCount                                          │   │
│     │ SerializedType[refTypesCount]:                               │   │
│     │   - 与普通 SerializedType 结构相同                            │   │
│     │   - 包含完整的 TypeTree 数据                                  │   │
│     │   - 额外字段 (version >= 21):                                 │   │
│     │       string m_KlassName                                     │   │
│     │       string m_NameSpace                                     │   │
│     │       string m_AsmName                                       │   │
│     └───────────────────────────────────────────────────────────────┘   │
│     大小: 取决于 TypeTree 内容，可能较大                                 │
│                                                                         │
│  4. UserInformation (version >= Unknown_5)                              │
│     ┌───────────────────────────────────────────────────────────────┐   │
│     │ string userInformation (null-terminated)                     │   │
│     └───────────────────────────────────────────────────────────────┘   │
│     大小: strlen + 1 (通常很小，几十字节)                                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 11.2 大小计算公式

| 子部分 | 公式 | 备注 |
|--------|------|------|
| ScriptTypes | 4 + count × (8 或 12) | version >= 14 时含 4 字节对齐 + 8 字节 int64 |
| FileIdentifier | 4 + Σ(20 + pathName_len + 1) | version >= 5 时含 guid(16) + type(4) |
| RefTypes | 4 + Σ(SerializedType 大小) | 每个 RefType 含完整 TypeTree blob |
| UserInformation | strlen + 1 | null-terminated 字符串 |

**Externals 总大小** = ScriptTypes + FileIdentifier + RefTypes + UserInformation

### 11.3 Analyzer 报告中的数据源标识

| 数据源 | 对应部分 | NonResourceType 值 |
|--------|----------|---------------------|
| SerializedFileScriptTypes | ScriptTypes 部分 | ScriptTypes (5) |
| SerializedFileFileIdentifier | FileIdentifier 部分 | Externals (4) |
| SerializedFileRefTypes | RefTypes 部分 | RefTypes (6) |
| SerializedFileUserInformation | UserInformation 部分 | UserInformation (7) |
| SerializedFileExternals | 四部分汇总 | Externals (4) |

### 11.4 常见大小分布

典型 AssetBundle 中 Externals 各部分的典型大小：

| 子部分 | 典型大小 | 说明 |
|--------|----------|------|
| ScriptTypes | 0-几百 bytes | 通常很少使用，count = 0 时仅 4 bytes |
| FileIdentifier | 1-5 KB | 引用外部资源文件（其他 CAB、Library 等） |
| RefTypes | 0-数十 KB | MonoBehaviour 引用类型，含完整 TypeTree |
| UserInformation | 几十 bytes | 用户自定义信息，通常为空或短字符串 |

### 11.5 重要注意事项

**TypeTree 数据不在 Externals 中**

TypeTree 数据属于 Types 部分（SerializedFileTypes），不在 Externals 中。
如果 Analyzer 报告显示 Externals 占用大量空间（>10KB），请检查：

1. **TypeTree 大小是否正确** - TypeTree blob 应包含所有 nodes 数据
2. **RefTypes 内容** - RefTypes 可能包含大量 TypeTree 数据
3. **nodeSize 计算** - 每个 TypeTree node 为 24 bytes（基础）或 32 bytes（含 RefTypeHash）

**历史问题说明**

在早期版本的 Analyzer 中，由于 TypeTree 大小估算偏低（假设每个 type 100 bytes），
导致大量 TypeTree 数据被错误归类到 Externals（通过减法计算剩余部分）。
现已修正：使用 MetadataPositionTracker 精确追踪各部分边界。

## 12. 参考链接

- [Unity AssetBundle 格式分析](https://docs.unity3d.com/Manual/AssetBundlesIntro.html)
- AssetStudio 源码: `BundleFile.cs`, `SerializedFile.cs`, `ObjectReader.cs`
- Unity 类型定义: `AssetStudio/Classes/*.cs`