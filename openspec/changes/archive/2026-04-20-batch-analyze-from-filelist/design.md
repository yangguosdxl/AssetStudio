## Context

当前 `Program.Main()` 的批量分析逻辑仅支持目录模式：遍历目录下所有文件，通过 `IsBundleFile()` 过滤后逐个分析。但 `test/高误差文件.txt` 包含 1834 个绝对路径，分散在 `C:/Users/Administrator/Downloads/assetbundles.qa/` 下，无法用单一目录覆盖。

现有批量分析流程：
1. `Directory.GetFiles()` → 过滤 bundle 文件 → 逐个分析
2. 每个文件分析后立即导出 xlsx + json（避免崩溃丢失数据）
3. 全部完成后可选生成 `summary_report.xlsx`

需要新增：从文件列表读取路径的批量分析模式。

## Goals / Non-Goals

**Goals:**
- 支持从文件列表（每行一个绝对路径）批量分析
- 逐个分析并导出，避免崩溃丢失数据
- 全部完成后生成与现有格式一致的汇总报告
- 支持跳过不存在的文件（记录跳过数量）

**Non-Goals:**
- 不修改 BatchSummaryExporter 的格式
- 不修改分析逻辑本身
- 不支持文件列表中的相对路径（要求绝对路径）

## Decisions

### 1. 命令行参数设计

**决策**：新增 `-l <filelist>` / `--list <filelist>` 参数，与现有 `-s` 参数配合使用。

用法：`AssetStudio.CLI.Analyzer -l test/高误差文件.txt -o report2 -s`

**理由**：保持与现有 CLI 风格一致，`-l` 指定文件列表，`-o` 指定输出目录，`-s` 生成汇总报告。

### 2. 文件列表处理逻辑

**决策**：逐行读取文件列表，跳过空行和以 `#` 开头的注释行。对每个路径检查文件是否存在，不存在则跳过并计数。

### 3. 输出目录结构

**决策**：所有分析报告（xlsx + json）输出到 `-o` 指定的目录。汇总报告也输出到同一目录。

## Risks / Trade-offs

- [1834 个文件分析耗时较长] → 逐个分析边导出，已分析的结果不会丢失
- [部分文件可能不存在] → 跳过并记录跳过数量，不影响其他文件分析
- [内存压力] → 每个文件分析完后释放 AssetsManager，汇总报告仅保留 VerificationSummary 级别数据
