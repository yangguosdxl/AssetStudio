## Context

AssetStudio.CLI.Analyzer 是一个 AssetBundle 分析工具，输出 Excel 和 JSON 格式的分析报告。当前 `SerializedFile` 包含 Externals 相关数据（ScriptTypes、Externals、RefTypes、UserInformation），但这些数据只在"验证汇总"中作为大小统计展示，未解析具体内容。

数据来源：
- `SerializedFile.m_ScriptTypes` - `List<LocalSerializedObjectIdentifier>`（版本 >= 11）
- `SerializedFile.m_Externals` - `List<FileIdentifier>`（已有）
- `SerializedFile.m_RefTypes` - `List<SerializedType>`（版本 >= 20，isRefType=true）
- `SerializedFile.userInformation` - `string`（版本 >= 5）

## Goals / Non-Goals

**Goals:**
- 提供命令行开关控制 Externals 详情输出
- 输出四部分详情数据到独立的 Excel Sheet
- 保持向后兼容（默认关闭）
- JSON 输出同步新增字段

**Non-Goals:**
- 不修改 AssetStudio 核心库的解析逻辑
- 不影响默认输出格式
- 不新增外部依赖

## Decisions

### 1. 数据模型设计

**决策**: 新建独立文件 `ExternalDetail.cs` 包含四个类

**理由**:
- 遵循现有 Contracts 项目结构（一个文件一个主要类型）
- 四个类关联性强，适合放在同一文件
- 使用 `[JsonProperty]` 标注确保 JSON 序列化一致性

**数据模型**:
```csharp
// ScriptTypeInfo - ScriptTypes 详情
public class ScriptTypeInfo
{
    [JsonProperty("internal_file_name")]
    public string InternalFileName { get; set; }

    [JsonProperty("local_serialized_file_index")]
    public int LocalSerializedFileIndex { get; set; }

    [JsonProperty("local_identifier_in_file")]
    public long LocalIdentifierInFile { get; set; }
}

// ExternalDetailInfo - Externals 详情
public class ExternalDetailInfo
{
    [JsonProperty("internal_file_name")]
    public string InternalFileName { get; set; }

    [JsonProperty("guid")]
    public string Guid { get; set; }  // hex format: "a1b2c3d4..."

    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("type_name")]
    public string TypeName { get; set; }

    [JsonProperty("path_name")]
    public string PathName { get; set; }

    [JsonProperty("file_name")]
    public string FileName { get; set; }
}

// RefTypeInfo - RefTypes 详情
public class RefTypeInfo
{
    [JsonProperty("internal_file_name")]
    public string InternalFileName { get; set; }

    [JsonProperty("class_id")]
    public int ClassID { get; set; }

    [JsonProperty("is_stripped_type")]
    public bool IsStrippedType { get; set; }

    [JsonProperty("script_type_index")]
    public short ScriptTypeIndex { get; set; }

    [JsonProperty("klass_name")]
    public string KlassName { get; set; }

    [JsonProperty("name_space")]
    public string NameSpace { get; set; }

    [JsonProperty("asm_name")]
    public string AsmName { get; set; }
}

// UserInformationInfo - UserInformation 详情
public class UserInformationInfo
{
    [JsonProperty("internal_file_name")]
    public string InternalFileName { get; set; }

    [JsonProperty("user_information")]
    public string UserInformation { get; set; }
}
```

### 2. 命令行参数设计

**决策**: 使用 `-e/--externals` 短参数和长参数

**理由**:
- 遵循现有参数风格（`-v/--verbose`, `-o/--output`)
- `-e` 简洁易记（externals）
- bool 类型开关，无参数值

### 3. 数据收集时机

**决策**: 在 `AssetsManager.LoadFiles()` 后收集，与 TypeTree 解析同时进行

**理由**:
- 此时 `SerializedFile` 已完整解析，所有字段可用
- 复用已有的 `assetsManager.assetsFileList`
- 避免重复解析

### 4. Excel Sheet 输出条件

**决策**: 仅当开关开启且有数据时写入 Sheet

**理由**:
- 避免空 Sheet 混淆用户
- 减少文件大小
- 保持报告简洁

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| 数据量可能较大（大型 Bundle 有数百 Externals） | 限制行数或分页（当前不做，观察实际使用情况） |
| GUID 格式不一致（Guid.ToString 有多种格式） | 统一使用 `"N"` 格式（无连字符，32字符） |
| RefTypes 可能为空（版本 < 20） | 使用空列表处理，不抛异常 |
| userInformation 通常为空 | 仅输出非空值 |