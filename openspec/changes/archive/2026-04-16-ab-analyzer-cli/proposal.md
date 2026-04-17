# AssetBundle 分析 CLI 工具

## Why

Unity AssetBundle 多次打包后文件尺寸变化难以追踪，开发者无法快速定位尺寸增加或减少的根本原因。现有 AssetStudioGUI 需要手动操作 UI，不适合批量分析和自动化对比场景。需要一个命令行工具精确分析 AssetBundle 内部各部分的字节分布，输出结构化报告供对比分析。

## What Changes

- 新增独立 CLI 项目 `AssetStudio.CLI.Analyzer`
- 新增独立类库 `AssetStudio.Analyzer.Contracts` 包含 JSON 输出类型定义
- 新增 AssetBundle 分析功能：
  - Bundle 级结构解析（Header、BlocksInfo、DataBlocks）
  - 内部文件级解析（SerializedFile、ResourceFile）
  - 资源级精确定位（ObjectInfo → Node → Block 映射）
  - 按数据块范围计算压缩前后大小
- 输出双格式报告：
  - xlsx（Excel 格式，包含多个 Sheet）
  - json（结构化数据，类型定义独立类库）
- 命令行参数支持：
  - 单文件分析
  - 批量目录分析
  - 输出路径指定

## Capabilities

### New Capabilities

- `bundle-analysis`: AssetBundle 文件内部结构分析，精确到资源级，计算压缩前后大小
- `analysis-report-export`: 分析报告导出，支持 xlsx 和 json 双格式输出

### Modified Capabilities

无现有能力修改。

## Impact

### 新增项目

| 项目 | 类型 | 说明 |
|------|------|------|
| `AssetStudio.Analyzer.Contracts` | 类库 | JSON 输出类型定义，独立类库供其他工具引用 |
| `AssetStudio.CLI.Analyzer` | 控制台应用 | CLI 入口，依赖 Contracts 和 AssetStudio 核心库 |

### 依赖关系

```
AssetStudio.CLI.Analyzer
    ├── AssetStudio.Analyzer.Contracts
    ├── AssetStudio (核心解析库)
    ├── EPPlus (xlsx 输出，NuGet)
    └── Newtonsoft.Json (json 输出，复用现有)
```

### 目标框架

- `net6.0` (CLI 主项目)
- `netstandard2.0` (Contracts 类库，最大化兼容性)

### 输出文件结构

```
输出目录/
├── {bundle名}_analysis.xlsx    # Excel 报告
└── {bundle名}_analysis.json    # JSON 报告
```

### Excel Sheet 结构

1. **Bundle 概览**：总大小、压缩信息、文件数量
2. **内部文件列表**：每个 Node 的路径、大小、类型
3. **资源对象明细**：每个资源的名称、类型、压缩前后大小、Block 分布
4. **类型汇总**：按 ClassIDType 分组的统计
5. **数据块分布**：每个 Block 的压缩前后大小、压缩率