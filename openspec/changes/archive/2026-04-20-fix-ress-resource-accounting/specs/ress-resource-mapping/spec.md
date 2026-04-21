## ADDED Requirements

### Requirement: Texture2D 数据来源识别必须考虑同 bundle 内 .resS 文件

当 Texture2D 的 `image_data.Size == 0`（纹理数据不在 SerializedFile 内）且同 bundle 存在 `.resS` Node 时，分析器 SHALL 将其识别为数据存储在同 bundle 内的 `.resS` 文件中，而非 `Embedded`。

#### Scenario: Texture2D 纹理数据在同 bundle .resS 文件中且 m_StreamData 为零值
- **WHEN** Texture2D 对象的 `image_data.Size == 0`
- **AND** `m_StreamData` 存在但 offset=0, size=0, path=''（Unity 2022.3 的零值模式）
- **AND** 同 bundle 的 DirectoryInfo 中存在以 `.resS` 结尾的 Node
- **THEN** 分析器 SHALL 将该 Texture2D 的 DataSource 标记为 `BundleResource`
- **AND** 使用 `.resS` Node 的 offset 作为纹理数据在 blocksStream 中的起始位置
- **AND** 使用 `.resS` Node 的 size 作为未压缩大小

#### Scenario: Texture2D 纹理数据在跨 bundle 外部文件中
- **WHEN** Texture2D 对象的 `m_StreamData.path` 非空且不匹配同 bundle 内的任何 Node
- **THEN** 分析器 SHALL 保持现有 `ExternalTexture` 或 `ExternalMissing` 行为不变

#### Scenario: Texture2D 纹理数据内嵌在 SerializedFile 中
- **WHEN** Texture2D 对象的 `image_data.Size > 0`
- **THEN** 分析器 SHALL 保持现有 `Embedded` 行为不变

### Requirement: 验证汇总必须正确反映所有资源数据

验证汇总 SHALL 将同 bundle 内 `.resS` 文件中的纹理数据正确归属到对应 Texture2D 资源，误差率 SHALL 低于 5%。

#### Scenario: 分析 d1497d267.dat 的验证误差
- **WHEN** 分析器分析包含 Texture2D 纹理数据在 `.resS` 文件中的 bundle
- **THEN** 验证汇总的 RelativeErrorPercent SHALL 低于 5%
- **AND** Texture2D 的 SizeUncompressed SHALL 接近 3,589,680（.resS 文件大小）

### Requirement: ResourceBlockMapper 必须支持同 bundle .resS 资源映射

ResourceBlockMapper SHALL 提供方法计算存储在同 bundle `.resS` Node 中的资源数据的压缩大小贡献。

#### Scenario: 计算 .resS 中纹理数据的压缩大小
- **WHEN** ResourceBlockMapper 接收到 `.resS` Node 引用
- **THEN** SHALL 通过 NodeBlockMapper 计算 `.resS` Node offset 到 Node size 范围内的压缩大小贡献
- **AND** 返回的压缩大小 SHALL 准确反映该纹理数据在压缩块中的实际占比

### Requirement: DataSourceType 枚举必须包含 BundleResource 类型

`DataSourceType` 枚举 SHALL 包含 `BundleResource` 值，表示资源数据存储在同 bundle 内的 `.resS` 文件中。

#### Scenario: BundleResource 类型在报告中显示
- **WHEN** Texture2D 的数据来源被识别为 BundleResource
- **THEN** ResourceInfo 的 DataSource 字段 SHALL 为 "BundleResource"
- **AND** JSON 报告中 data_source 字段 SHALL 为 "BundleResource"
