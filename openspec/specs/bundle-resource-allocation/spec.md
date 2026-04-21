## MODIFIED Requirements

### Requirement: BundleResource 大小分配
当多个 Texture2D 资源被标记为 BundleResource 时，系统 SHALL 仅将 .resS 文件大小分配给第一个 BundleResource 资源，后续 BundleResource 资源的 SizeUncompressed 和 SizeCompressed SHALL 设为 0。

#### Scenario: 单个 BundleResource
- **WHEN** bundle 中仅有一个 Texture2D 标记为 BundleResource
- **THEN** 该资源 SHALL 保持现有行为，SizeUncompressed = .resS Node.size，SizeCompressed 通过 NodeBlockMapper 计算

#### Scenario: 多个 BundleResource
- **WHEN** bundle 中有 N 个 Texture2D 标记为 BundleResource（N > 1）
- **THEN** 第一个 BundleResource 资源 SHALL 声明 .resS 全部大小，其余 N-1 个 BundleResource 资源的 SizeUncompressed SHALL 为 0，SizeCompressed SHALL 为 0，DataSource SHALL 变为 "BundleResourceShared"

#### Scenario: BundleResource 与 ExternalTexture 共存
- **WHEN** bundle 同时包含 BundleResource 和 ExternalTexture 资源
- **THEN** BundleResource 声明 .resS 全部大小，ExternalTexture 的 offset+size 区域 SHALL NOT 从 .resS 大小中扣除（因为 ExternalTexture 引用的是 .resS 内的子区域，数据不重复）
