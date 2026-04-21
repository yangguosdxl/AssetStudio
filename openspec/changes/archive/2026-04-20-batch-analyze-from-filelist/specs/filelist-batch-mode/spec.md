## ADDED Requirements

### Requirement: CLI 支持从文件列表批量分析

分析器 SHALL 支持通过 `-l <filelist>` 参数指定文件列表路径，每行一个 AssetBundle 绝对路径，逐个分析并导出。

#### Scenario: 从文件列表批量分析
- **WHEN** 用户执行 `AssetStudio.CLI.Analyzer -l test/高误差文件.txt -o report2 -s`
- **THEN** 分析器 SHALL 逐行读取文件列表中的路径
- **AND** 对每个存在的文件执行分析并导出 xlsx + json 到 report2 目录
- **AND** 全部完成后生成 summary_report.xlsx 到 report2 目录

#### Scenario: 文件列表中部分文件不存在
- **WHEN** 文件列表中包含不存在的路径
- **THEN** 分析器 SHALL 跳过该文件并记录跳过数量
- **AND** 不影响其他文件的分析

#### Scenario: 文件列表中包含空行或注释
- **WHEN** 文件列表中包含空行或以 `#` 开头的行
- **THEN** 分析器 SHALL 跳过这些行
