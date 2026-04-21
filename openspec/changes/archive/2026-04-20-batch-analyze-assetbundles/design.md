## Context

AssetStudio.CLI.Analyzer 当前已支持批量分析目录中的多个 AssetBundle 文件，并为每个文件生成独立的 Excel 和 JSON 报告。报告包含验证汇总信息，其中有误差率数据（相对误差百分比）。

**当前状态**:
- `BundleAnalyzer.AnalyzeDirectory()` 返回 `Dictionary<string, AnalysisReport>`
- 每个 `AnalysisReport` 包含 `VerificationSummary.ErrorInfo.RelativeErrorPercent`
- 现有 `ExcelReportExporter` 支持多 Sheet 输出

**约束**:
- 目标目录：`C:\Users\Administrator\Downloads\assetbundles.qa`
- 误差阈值：10%
- 输出格式：Excel
- 需要自动分析高误差原因

## Goals / Non-Goals

**Goals:**
- 批量分析完成后自动生成汇总报告
- 汇总报告按误差率降序排列所有 AssetBundle
- 对误差超过 10% 的 AssetBundle 自动分析原因
- 提供清晰的 Excel 汇总报告，便于快速定位问题

**Non-Goals:**
- 不修改单个 Bundle 分析逻辑
- 不改变现有报告格式
- 不实现实时监控或增量分析

## Decisions

### 1. 汇总报告结构

**决定**: 使用单一 Excel 文件，包含两个 Sheet：
1. **误差率汇总** - 所有 AssetBundle 按误差率降序排列
2. **高误差分析** - 对误差 > 10% 的 Bundle 进行原因分析

**替代方案**: 
- 多文件输出（JSON + Excel）→ 拒绝，用户明确要求 Excel 格式
- 单 Sheet 包含所有信息 → 拒绝，分离关注点更清晰

### 2. 误差原因分析策略

**决定**: 实现规则化的原因分析器，检查以下常见问题：

| 原因类型 | 检测条件 | 说明 |
|---------|---------|------|
| 外部资源缺失 | DataSource == "ExternalMissing" | .resource 文件未找到 |
| 压缩效率低 | 压缩率 > 90% | 数据几乎未被压缩 |
| 元数据膨胀 | 元数据占比 > 30% | TypeTree 或类型信息过大 |
| 数据块对齐 | 存在大量小块 | Block 碎片化严重 |
| 未识别数据 | 非资源数据占比高 | 存在未解析的数据区域 |

**替代方案**:
- ML 模型分析 → 拒绝，规则化足够且可解释
- 仅列出数据让用户判断 → 拒绝，用户明确要求自动分析原因

### 3. CLI 参数扩展

**决定**: 新增 `-s` / `--summary` 参数启用汇总报告模式

```
AssetStudio.CLI.Analyzer <目录> -o <输出目录> -s
```

**理由**: 保持向后兼容，不改变现有批量分析行为

## Risks / Trade-offs

| 风险 | 缓解措施 |
|-----|---------|
| 大目录分析耗时长 | 显示进度条，支持 Ctrl+C 中断 |
| 误差原因分析不准确 | 提供置信度评分，低置信度时标注"需人工确认" |
| Excel 文件过大 | 汇总报告仅包含关键指标，不含完整数据 |
