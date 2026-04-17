## 1. 数据模型创建

- [x] 1.1 创建 `AssetStudio.Analyzer.Contracts/ExternalDetail.cs`，包含四个类：ScriptTypeInfo、ExternalDetailInfo、RefTypeInfo、UserInformationInfo
- [x] 1.2 修改 `AssetStudio.Analyzer.Contracts/AnalysisReport.cs`，添加四个新列表字段

## 2. 命令行参数处理

- [x] 2.1 修改 `AssetStudio.CLI.Analyzer/Program.cs`，在 CliOptions 类添加 ShowExternals 属性
- [x] 2.2 在 ParseArgs 方法添加 `-e/--externals` 参数解析
- [x] 2.3 在 ShowHelp 方法添加参数说明
- [x] 2.4 修改 BundleAnalyzer 实例化，传递 showExternals 参数

## 3. 数据收集逻辑

- [x] 3.1 修改 `BundleAnalyzer.cs` 构造函数，添加 _showExternals 字段
- [x] 3.2 新增 ParseExternalsDetails 方法，遍历 assetsFileList 收集四部分数据
- [x] 3.3 新增 GetExternalTypeName 辅助方法，映射 type 值到可读名称
- [x] 3.4 在 AnalyzeBundleFile 方法中调用 ParseExternalsDetails（条件调用）

## 4. Excel 输出

- [x] 4.1 修改 `ExcelReportExporter.cs` Export 方法，添加条件 Sheet 写入
- [x] 4.2 新增 WriteScriptTypesSheet 方法
- [x] 4.3 新增 WriteExternalsSheet 方法
- [x] 4.4 新增 WriteRefTypesSheet 方法
- [x] 4.5 新增 WriteUserInformationSheet 方法

## 5. JSON 输出

- [x] 5.1 修改 `JsonReportExporter.cs`，确保新字段正确序列化（无需修改，自动处理）

## 6. 验证

- [x] 6.1 构建项目验证编译通过
- [x] 6.2 测试不带 `-e` 参数，验证无新 Sheet 输出（JSON 新字段为空数组）
- [x] 6.3 测试带 `-e` 参数，验证四个新 Sheet 正确输出
  - ue5166d82.dat: ScriptTypes=4, Externals=8
  - uf6c4d88c.dat: ScriptTypes=4, Externals=14
- [x] 6.4 验证 JSON 输出包含新字段，数据结构正确