## 1. 数据源类型扩展

- [ ] 1.1 在 `DataSourceType` 枚举中新增 `BundleResource` 类型，表示资源数据在同 bundle 内的 `.resS` 文件中
- [ ] 1.2 在 `ResourceBlockMapper` 构造函数中建立 `.resS` 文件名到 Node 的映射缓存（查找 path 以 `.resS` 结尾的 Node）

## 2. ResourceBlockMapper 扩展

- [ ] 2.1 新增 `CalculateBundleResourceCompressedSize()` 方法 — 接收 `.resS` Node 引用，通过 NodeBlockMapper 计算该 Node 在 blocksStream 中的压缩大小贡献
- [ ] 2.2 新增 `FindResSNode()` 方法 — 查找同 bundle 内的 `.resS` Node

## 3. BundleAnalyzer.ParseSingleResource() 修复

- [ ] 3.1 在 Texture2D 的 Embedded case 中增加 `image_data.Size == 0` 的判断 — 当纹理数据不在 SerializedFile 内时，检查是否存在 `.resS` Node
- [ ] 3.2 新增 `DataSourceType.BundleResource` 的 case 分支 — 查找同 bundle 内的 `.resS` Node，使用 Node offset 和 size 计算压缩大小
- [ ] 3.3 设置 `SizeUncompressed` 为 `.resS` Node 的 size（而非 objectInfo.byteSize）
- [ ] 3.4 设置 `DataOffset` 和 `ExternalFilePath` 反映 `.resS` 内的实际位置

## 4. 验证与测试

- [ ] 4.1 使用 `test/d1497d267.dat` 运行分析器，确认误差率低于 5%
- [ ] 4.2 确认 Texture2D 资源的 DataSource 显示为 `BundleResource`，SizeUncompressed 接近 3,589,680
- [ ] 4.3 验证其他测试文件（`ue5166d82.dat`, `uf6c4d88c.dat`）分析结果未退化
