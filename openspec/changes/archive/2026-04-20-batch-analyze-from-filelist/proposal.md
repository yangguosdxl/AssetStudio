## Why

当前 CLI 仅支持通过目录路径批量分析（`-s` 参数），但实际场景中需要从文件列表（如 `test/高误差文件.txt`，1834 个文件路径）中批量分析。文件列表中的路径是绝对路径，分散在不同目录中，无法用单一目录覆盖。需要支持从文件列表读取路径、逐个分析、边分析边导出、最终生成汇总报告。

## What Changes

- **新增 `-l` / `--list` 命令行参数**：接受文件列表路径，每行一个 AssetBundle 绝对路径
- **修改 `Program.Main()` 批量分析逻辑**：当使用 `-l` 参数时，从文件列表读取路径，逐个分析并导出到指定输出目录
- **汇总报告格式**：与现有 `reports/summary_report.xlsx` 一致（误差率汇总 + 高误差分析两个 Sheet）

## Capabilities

### New Capabilities
- `filelist-batch-mode`: 支持从文件列表批量分析 AssetBundle，逐个导出报告并生成汇总

### Modified Capabilities
<!-- 无需修改现有 spec -->

## Impact

- `AssetStudio.CLI.Analyzer/Program.cs` — 新增 `-l` 参数解析和文件列表批量分析逻辑
- 命令行帮助信息更新
