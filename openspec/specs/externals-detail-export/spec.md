## ADDED Requirements

### Requirement: 命令行开关控制 Externals 详情输出

系统应当提供命令行开关 `-e/--externals` 来启用报告中的 Externals 详情输出。

#### Scenario: 无开关时的默认行为
- **WHEN** 用户运行分析器时不带 `-e` 或 `--externals` 参数
- **THEN** 报告中不包含 Externals 详情 Sheet

#### Scenario: 使用短参数启用
- **WHEN** 用户运行分析器时带 `-e` 参数
- **THEN** 报告中包含四个 Externals 详情 Sheet

#### Scenario: 使用长参数启用
- **WHEN** 用户运行分析器时带 `--externals` 参数
- **THEN** 报告中包含四个 Externals 详情 Sheet

### Requirement: ScriptTypes Sheet 输出

当 externals 导出启用时，系统应当输出包含 LocalSerializedObjectIdentifier 详情的 ScriptTypes Sheet。

#### Scenario: ScriptTypes Sheet 列标题
- **WHEN** externals 导出启用且 SerializedFile 有 ScriptTypes 数据
- **THEN** ScriptTypes Sheet 包含列：所属文件、本地文件索引、本地标识符

#### Scenario: ScriptTypes 为空
- **WHEN** externals 导出启用但 SerializedFile 无 ScriptTypes 数据
- **THEN** ScriptTypes Sheet 为空或不创建

### Requirement: Externals Sheet 输出

当 externals 导出启用时，系统应当输出包含 FileIdentifier 详情的 Externals Sheet。

#### Scenario: Externals Sheet 列标题
- **WHEN** externals 导出启用且 SerializedFile 有 Externals 数据
- **THEN** Externals Sheet 包含列：所属文件、GUID、类型值、类型名、路径、文件名

#### Scenario: GUID 格式
- **WHEN** Externals 数据导出时
- **THEN** GUID 格式化为 32 字符十六进制字符串（无连字符，格式 "N"）

#### Scenario: 类型名称映射
- **WHEN** Externals 数据导出时
- **THEN** type 字段映射为可读名称：0=NonAsset、2=SerializedAsset、3=MetaAsset

### Requirement: RefTypes Sheet 输出

当 externals 导出启用时，系统应当输出包含 SerializedType（isRefType=true）详情的 RefTypes Sheet。

#### Scenario: RefTypes Sheet 列标题
- **WHEN** externals 导出启用且 SerializedFile 有 RefTypes 数据（版本 >= 20）
- **THEN** RefTypes Sheet 包含列：所属文件、类型ID、是否剥离、脚本索引、类名、命名空间、程序集

#### Scenario: RefTypes 为空
- **WHEN** externals 导出启用但 SerializedFile 版本 < 20 或无 RefTypes 数据
- **THEN** RefTypes Sheet 为空或不创建

### Requirement: UserInformation Sheet 输出

当 externals 导出启用时，系统应当输出包含用户自定义信息的 UserInformation Sheet。

#### Scenario: UserInformation Sheet 列标题
- **WHEN** externals 导出启用且 SerializedFile 有非空 userInformation
- **THEN** UserInformation Sheet 包含列：所属文件、用户信息

#### Scenario: UserInformation 为空
- **WHEN** externals 导出启用但所有 SerializedFile 的 userInformation 为空
- **THEN** UserInformation Sheet 为空或不创建

### Requirement: JSON 输出同步

当 externals 导出启用时，系统应当在 JSON 输出中包含 Externals 详情字段。

#### Scenario: JSON 字段名称
- **WHEN** externals 导出启用
- **THEN** JSON 输出包含 `script_types`、`external_details`、`ref_types`、`user_informations` 数组

#### Scenario: JSON 字段结构
- **WHEN** externals 导出启用且数据存在
- **THEN** 每个数组包含符合数据模型结构的对象，使用正确的 JSON 属性名