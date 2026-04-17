# AssetStudio.CLI.Analyzer 报告数据说明

本文档详细说明 AssetStudio.CLI.Analyzer 生成的分析报告中各项数据的含义和计算方式。

## 1. 报告输出格式

分析报告支持两种输出格式：

- **Excel (.xlsx)**: 包含多个 Sheet，适合人工查看
- **JSON (.json)**: 结构化数据，适合程序处理

## 2. 报告结构概览

```
┌─────────────────────────────────────────────────────────────┐
│                    分析报告                                  │
├─────────────────────────────────────────────────────────────┤
│  Sheet 1: Bundle概览                                        │
│    - Bundle 文件的基本信息                                   │
├─────────────────────────────────────────────────────────────┤
│  Sheet 2: 内部文件                                          │
│    - AssetBundle 内包含的所有文件列表                         │
├─────────────────────────────────────────────────────────────┤
│  Sheet 3: 资源明细                                          │
│    - 所有资源和非资源数据的详细列表（按偏移排序）              │
├─────────────────────────────────────────────────────────────┤
│  Sheet 4: 类型汇总                                          │
│    - 按资源类型统计的数量和大小汇总                           │
├─────────────────────────────────────────────────────────────┤
│  Sheet 5: 数据块分布                                        │
│    - AssetBundle 数据块的压缩信息                            │
├─────────────────────────────────────────────────────────────┤
│  Sheet 6: TypeTree结构                                      │
│    - SerializedFile 中定义的类型结构                         │
├─────────────────────────────────────────────────────────────┤
│  Sheet 7: 验证汇总                                          │
│    - 数据完整性验证和误差分析                                 │
└─────────────────────────────────────────────────────────────┘
```

## 3. Bundle概览 (Sheet 1)

### 字段说明

| 字段名 | JSON键名 | 说明 | 计算方式 |
|--------|----------|------|----------|
| 文件名 | file_name | AssetBundle 文件名 | 从文件路径提取 |
| 总大小（压缩后） | total_size_compressed | Bundle 文件实际大小 | 文件系统读取 |
| 总大小（解压后） | total_size_uncompressed | 所有数据块解压后总和 | Σ(uncompressedBlock) + Header + BlocksInfo |
| Header 大小 | header_size | Bundle Header 字节数 | 根据格式计算 |
| BlocksInfo 大小（压缩后） | blocks_info_size_compressed | BlocksInfo 压缩后大小 | header.compressedBlocksInfoSize |
| BlocksInfo 大小（解压后） | blocks_info_size_uncompressed | BlocksInfo 解压后大小 | header.uncompressedBlocksInfoSize |
| DataBlocks 大小（压缩后） | data_blocks_size_compressed | 数据块压缩后总和 | Σ(compressedBlock) |
| DataBlocks 大小（解压后） | data_blocks_size_uncompressed | 数据块解压后总和 | Σ(uncompressedBlock) |
| 压缩类型 | compression_type | 使用的压缩算法 | LZ4 / LZ4HC / LZMA / None |
| Unity 版本 | unity_version | Bundle 中记录的 Unity 版本 | header.unityVersion |
| 内部文件数量 | internal_file_count | Bundle 包含的文件数 | directoryInfo.length |
| 资源对象数量 | resource_count | 所有 SerializedFile 的资源总数 | Σ(Objects.count) |
| 压缩率 | - | 压缩比例百分比 | compressed / uncompressed × 100% |

### 大小关系验证

```
total_size_compressed ≈ header_size + blocks_info_size_compressed + data_blocks_size_compressed
total_size_uncompressed = header_size + blocks_info_size_uncompressed + data_blocks_size_uncompressed
```

## 4. 内部文件 (Sheet 2)

### 字段说明

| 字段名 | JSON键名 | 说明 | 备注 |
|--------|----------|------|------|
| 文件名 | file_name | 内部文件名称 | 如 "CAB-xxx" |
| 大小（解压后） | size_uncompressed | 文件在解压后数据流中的大小 | node.size |
| 文件类型 | file_type | 文件类型分类 | SerializedFile / ResourceFile |
| 偏移 | offset | 在解压后 blocksStream 中的起始位置 | node.offset |
| 元数据大小 | metadata_size | SerializedFile 的 metadata 部分大小 | 仅 SerializedFile 有值 |
| 数据区大小 | data_size | SerializedFile 的数据区大小 | 仅 SerializedFile 有值 |
| 资源数量 | resource_count | 该文件包含的资源对象数 | 仅 SerializedFile 有值 |
| 类型数量 | type_count | 该文件定义的 SerializedType 数 | 仅 SerializedFile 有值 |

### 文件类型判断逻辑

```
FileType = "SerializedFile"  // 文件名含 "CAB-" 或扩展名 ".assets"
FileType = "ResourceFile"    // 其他情况
```

## 5. 资源明细 (Sheet 3)

资源明细是报告的核心 Sheet，包含**资源数据**和**非资源数据**，按偏移混合排序。

### 字段说明

| 字段名 | JSON键名 | 说明 | 计算方式 |
|--------|----------|------|----------|
| 资源名称 | name | 资源或非资源数据项名称 | Object.m_Name 或类型描述 |
| 数据类别 | data_category | 资源 / 非资源 | 枚举值: Resource=0, NonResource=1 |
| 类型名称 | type_name | Unity 类型名 | 如 Mesh, Texture2D, [BundleMeta] |
| 类型ID | type_id | Unity 类型ID | classID 值，非资源为 -1 |
| PathID | path_id | 对象唯一标识 | ObjectInfo.pathID |
| 数据来源 | data_source | 数据存储位置 | Embedded / External / BundleHeader 等 |
| 大小（解压后） | size_uncompressed | 原始大小 | byteSize 或计算值 |
| 大小（压缩后） | size_compressed | 压缩后大小贡献 | 比例法计算 |
| 数据偏移 | data_offset | 在解压后数据流中的位置 | byteStart + dataOffset |
| 外部文件路径 | external_file_path | 外部资源文件路径 | 仅 External 资源有值 |
| Container路径 | container_path | AssetBundle 中的容器路径 | AssetBundle.m_Container |
| 所属内部文件 | internal_file_name | 所在的 SerializedFile 名 | assetsFile.fileName |

### 数据类别详解

| 类别值 | 类别名称 | 包含的数据 |
|--------|----------|------------|
| 0 | 资源 | Unity 游戏资源对象（Mesh, Texture2D, MonoBehaviour 等） |
| 1 | 非资源 | AssetBundle 结构数据（Header, Metadata 等） |

### 非资源数据类型 (non_resource_type)

| 类型值 | 类型名称 | 说明 |
|--------|----------|------|
| 0 | BundleMeta | Bundle Header + BlocksInfo（Bundle 级） |
| 1 | FileHeader | SerializedFile Header + Version/Platform |
| 2 | TypeTree | Types 数组 + TypeTree 数据 |
| 3 | ObjectDir | Objects 数组（对象目录） |
| 4 | Externals | ScriptTypes + Externals + RefTypes + UserInfo |

### 数据来源详解

| 来源值 | 说明 |
|--------|------|
| Embedded | 数据内嵌在 SerializedFile 中 |
| External | 数据存储在独立的 .resS 文件中 |
| BundleHeader | Bundle 级 Header 数据 |
| BlocksInfo | Bundle 级 BlocksInfo 数据 |
| SerializedFileHeader | SerializedFile Header 部分 |
| SerializedFileTypes | SerializedFile Types 部分 |
| SerializedFileObjects | SerializedFile ObjectDir 部分 |
| SerializedFileExternals | SerializedFile Externals 部分 |

### 压缩大小计算（比例法）

由于压缩算法的特性，无法精确计算单个资源在压缩流中的大小。采用**比例法**估算：

```
compressed_contribution = resource_size / block_size × block_compressed_size

例如：
Block 解压后大小: 32340 bytes
Block 压缩后大小: 9652 bytes
资源大小: 217 bytes

资源压缩贡献 = 217 / 32340 × 9652 ≈ 64 bytes
```

### BlockContributions 结构

每个资源可能跨多个 Block 存储，`block_contributions` 记录各 Block 的贡献：

```json
{
  "block_index": 0,
  "intersect_start": 3360,
  "intersect_end": 3577,
  "intersect_size": 217,
  "ratio": 0.0067,
  "compressed_contribution": 64
}
```

## 6. 类型汇总 (Sheet 4)

按 Unity 类型统计资源数量和大小。

### 字段说明

| 字段名 | 说明 | 计算方式 |
|--------|------|----------|
| 类型名称 | Unity 类型名 | 如 Mesh, Texture2D |
| 类型ID | Unity 类型ID | classID 值 |
| 资源数量 | 该类型的资源总数 | count(resources where typeID = X) |
| 总大小（解压后） | 该类型所有资源解压后总和 | Σ(size_uncompressed) |
| 总大小（压缩后） | 该类型所有资源压缩后总和 | Σ(size_compressed) |
| 平均大小 | 平均每个资源的大小 | total / count |
| 最大资源名称 | 最大的资源名称 | max by size_uncompressed |
| 最大资源大小 | 最大资源的大小 | max(size_uncompressed) |
| 占Bundle比例 | 占 Bundle 总大小的比例 | total_uncompressed / bundle_total × 100% |

## 7. 数据块分布 (Sheet 5)

### 字段说明

| 字段名 | 说明 |
|--------|------|
| Block序号 | 数据块索引 |
| 压缩类型 | 该块使用的压缩算法 |
| 压缩大小 | 压缩后字节数 |
| 解压大小 | 解压后字节数 |
| 压缩率 | 压缩比例 |
| 包含文件数 | 该块中包含的内部文件数 |
| 包含的文件列表 | 文件名列表 |

### Block 与文件的关系

```
所有内部文件按 Node.offset 和 Node.size 分布在解压后的 blocksStream 中：

Block 解压后内容 = [SerializedFile 0][SerializedFile 1][ResourceFile]
                   ↑offset=0         ↑offset=X         ↑offset=Y
```

## 8. TypeTree结构 (Sheet 6)

### 字段说明

| 字段名 | 说明 |
|--------|------|
| 类型ID | SerializedType 的 classID |
| 类型名称 | Unity 类型名 |
| 是否剥离 | m_IsStrippedType 标志 |
| 脚本类型索引 | m_ScriptTypeIndex（MonoBehaviour 相关） |
| 所属内部文件 | 定义该类型的 SerializedFile |
| 节点数量 | TypeTree 中的节点数 |

### TypeTree 作用

TypeTree 定义了 Unity 类的字段布局，用于：
- 反序列化资源数据
- 跨版本兼容性处理
- MonoBehaviour 脚本反序列化

## 9. 验证汇总 (Sheet 7)

验证汇总用于检验分析数据的完整性。

### 结构说明

```
┌─────────────────────────────────────────────────────────────┐
│ 数据类别汇总                                                 │
├─────────────────────────────────────────────────────────────┤
│ BundleMeta: 压缩前总计 / 压缩后总计 / 占文件比例 / 行数      │
│ FileHeader: ...                                             │
│ TypeTree: ...                                               │
│ ObjectDir: ...                                              │
│ Externals: ...                                              │
│ 资源数据: ...                                               │
├─────────────────────────────────────────────────────────────┤
│ 计算总计: 所有类别压缩后总和                                  │
├─────────────────────────────────────────────────────────────┤
│ 文件验证                                                    │
├─────────────────────────────────────────────────────────────┤
│ 文件实际大小: Bundle 文件字节数                              │
│ 计算总计: Σ(compressed_size)                                │
│ 绝对误差: |实际 - 计算|                                      │
│ 相对误差: 绝对误差 / 实际 × 100%                             │
└─────────────────────────────────────────────────────────────┘
```

### 字段说明

| 字段名 | JSON键名 | 说明 |
|--------|----------|------|
| 数据类别 | category_name | 类别名称 |
| 压缩前总计 | size_uncompressed | 该类别所有项解压后总和 |
| 压缩后总计 | size_compressed | 该类别所有项压缩后总和 |
| 占文件比例 | percentage | 占 Bundle 文件的比例 |
| 行数 | item_count | 该类别的数据行数 |

### 误差来源分析

实际文件大小与计算总计的误差来源：

1. **估算误差**: SerializedFile Metadata 各部分大小为估算值
2. **比例法误差**: 压缩大小使用比例法，无法精确计算单个资源
3. **填充字节**: 某些版本有 alignment 填充字节
4. **未知字段**: 部分 Unity 版本有未记录的字段

### 合理误差范围

通常相对误差应 < 1%，超过此范围可能存在：
- 文件损坏
- 版本格式不兼容
- 计算逻辑错误

## 10. JSON 报告完整结构

```json
{
  "bundle": {
    "file_name": "xxx.dat",
    "total_size_compressed": 9828,
    "total_size_uncompressed": 32501,
    "header_size": 50,
    "blocks_info_size_compressed": 97,
    "blocks_info_size_uncompressed": 161,
    "data_blocks_size_compressed": 9652,
    "data_blocks_size_uncompressed": 32340,
    "compression_type": "Lz4HC",
    "unity_version": "5.x.x",
    "internal_file_count": 2,
    "resource_count": 27
  },
  "internal_files": [...],
  "resources": [...],
  "non_resource_data": [...],
  "type_summaries": [...],
  "blocks": [...],
  "type_trees": [...],
  "verification_summary": {
    "category_totals": [...],
    "total_uncompressed": 32501,
    "total_compressed": 9763,
    "error_info": {
      "actual_file_size": 9828,
      "calculated_total": 9763,
      "absolute_error": 65,
      "relative_error_percent": 0.66
    }
  }
}
```

## 11. 使用场景

### 场景 1: 验证 AssetBundle 完整性

查看「验证汇总」Sheet：
- 相对误差 < 1% 表示分析成功
- 误差过大可能文件损坏

### 场景 2: 分析资源占比

查看「类型汇总」Sheet：
- 找出占用空间最大的类型
- 优化资源打包策略

### 场景 3: 定位资源位置

查看「资源明细」Sheet：
- 查找特定资源的偏移和大小
- 分析资源在 Bundle 中的分布

### 场景 4: 对比压缩效果

对比「数据块分布」Sheet：
- 不同压缩类型的压缩率对比
- 评估压缩算法选择

## 12. 命令行用法

```bash
# 分析单个 Bundle
AssetStudio.CLI.Analyzer --bundle path/to/bundle --output output/dir

# 分析多个 Bundle
AssetStudio.CLI.Analyzer --bundle path/to/bundle1 --bundle path/to/bundle2 --output output/dir

# 详细模式（显示处理日志）
AssetStudio.CLI.Analyzer --bundle path/to/bundle --output output/dir --verbose
```

## 13. 注意事项

1. **压缩大小为估算值**: 由于压缩算法特性，单个资源的压缩大小无法精确计算
2. **Metadata 大小为估算值**: SerializedFile Metadata 各部分大小通过估算得出
3. **版本兼容性**: 不同 Unity 版本的格式可能有差异
4. **外部资源**: External 类型资源的实际数据在独立文件中，不在 Bundle 内