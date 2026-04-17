using AssetStudio;
using AssetStudio.Analyzer.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// Bundle 分析器 - 分析入口
    /// 使用反射访问 BundleFile 的私有字段
    /// </summary>
    public class BundleAnalyzer
    {
        private readonly bool _verbose;
        private readonly bool _showExternals;

        // 反射获取 BundleFile 的私有字段
        private static readonly FieldInfo? BlocksInfoField;
        private static readonly FieldInfo? DirectoryInfoField;

        // 反射获取 SerializedFile 的私有字段
        private static readonly FieldInfo? ScriptTypesField;

        static BundleAnalyzer()
        {
            var bundleType = typeof(BundleFile);
            BlocksInfoField = bundleType.GetField("m_BlocksInfo", BindingFlags.NonPublic | BindingFlags.Instance);
            DirectoryInfoField = bundleType.GetField("m_DirectoryInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            var serializedFileType = typeof(SerializedFile);
            ScriptTypesField = serializedFileType.GetField("m_ScriptTypes", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="verbose">是否输出详细日志</param>
        /// <param name="showExternals">是否输出 Externals 详情</param>
        public BundleAnalyzer(bool verbose = false, bool showExternals = false)
        {
            _verbose = verbose;
            _showExternals = showExternals;
        }

        /// <summary>
        /// 获取 BundleFile 的 BlocksInfo
        /// </summary>
        private BundleFile.StorageBlock[] GetBlocksInfo(BundleFile bundleFile)
        {
            if (BlocksInfoField == null) return Array.Empty<BundleFile.StorageBlock>();
            return (BundleFile.StorageBlock[]?)BlocksInfoField.GetValue(bundleFile) ?? Array.Empty<BundleFile.StorageBlock>();
        }

        /// <summary>
        /// 获取 BundleFile 的 DirectoryInfo
        /// </summary>
        private BundleFile.Node[] GetDirectoryInfo(BundleFile bundleFile)
        {
            if (DirectoryInfoField == null) return Array.Empty<BundleFile.Node>();
            return (BundleFile.Node[]?)DirectoryInfoField.GetValue(bundleFile) ?? Array.Empty<BundleFile.Node>();
        }

        /// <summary>
        /// 分析单个 AssetBundle 文件
        /// </summary>
        /// <param name="bundlePath">AssetBundle 文件路径</param>
        /// <returns>分析报告</returns>
        public AnalysisReport AnalyzeBundleFile(string bundlePath)
        {
            if (_verbose)
            {
                Console.WriteLine($"[分析] 正在加载: {bundlePath}");
            }

            var report = new AnalysisReport();

            // 读取 Bundle 文件
            using var reader = new FileReader(bundlePath);
            if (reader.FileType != FileType.BundleFile)
            {
                throw new InvalidOperationException($"文件 {bundlePath} 不是 AssetBundle 文件，类型: {reader.FileType}");
            }

            var bundleFile = new BundleFile(reader);

            // 获取私有字段数据
            var blocksInfo = GetBlocksInfo(bundleFile);
            var directoryInfo = GetDirectoryInfo(bundleFile);

            // 构建 Bundle 概览
            report.Bundle = BuildBundleSummary(bundleFile, blocksInfo, directoryInfo, bundlePath);

            // 创建 Block 映射器
            var nodeMapper = new NodeBlockMapper(blocksInfo);
            var resourceMapper = new ResourceBlockMapper(nodeMapper, directoryInfo);

            // 【关键】先追踪 Metadata，再解析内部文件（ParseInternalFiles 会 disposed stream）
            report.NonResourceData = ParseNonResourceDataBeforeLoad(bundleFile, blocksInfo, directoryInfo, nodeMapper);

            // 解析内部文件（注意：此方法会 disposed bundleFile.fileList 的 stream）
            report.InternalFiles = ParseInternalFiles(bundleFile, directoryInfo);

            // 使用 AssetsManager 来完整解析资源（一次加载，共享使用）
            var assetsManager = new AssetsManager();
            assetsManager.LoadFiles(bundlePath);

            // 在 LoadFiles 后生成 SerializedFile 级非资源数据
            var serializedFileNonResourceData = ParseNonResourceDataAfterLoad(assetsManager, directoryInfo, nodeMapper);
            report.NonResourceData.AddRange(serializedFileNonResourceData);

            // 解析资源对象
            report.Resources = ParseResources(assetsManager, directoryInfo, nodeMapper, resourceMapper);

            // 计算类型汇总
            report.TypeSummaries = CalculateTypeSummaries(report.Resources, report.Bundle.TotalSizeUncompressed);

            // 构建数据块分布
            report.Blocks = BuildBlockInfos(blocksInfo, nodeMapper, directoryInfo);

            // 解析 TypeTree 类型结构（使用已加载的 AssetsManager）
            report.TypeTrees = ParseTypeTrees(assetsManager);

            // 解析 Externals 详情（可选）
            if (_showExternals)
            {
                ParseExternalsDetails(assetsManager, report);
            }

            // 构建验证汇总
            report.VerificationSummary = BuildVerificationSummary(report, bundlePath);

            // 更新资源数量
            report.Bundle.ResourceCount = report.Resources.Count;

            if (_verbose)
            {
                Console.WriteLine($"[分析] 完成: {bundlePath}");
                Console.WriteLine($"  - 内部文件: {report.InternalFiles.Count}");
                Console.WriteLine($"  - 资源对象: {report.Resources.Count}");
                Console.WriteLine($"  - 非资源数据: {report.NonResourceData.Count}");
                Console.WriteLine($"  - 数据块: {report.Blocks.Count}");
                Console.WriteLine($"  - TypeTree类型: {report.TypeTrees.Count}");
            }

            return report;
        }

        /// <summary>
        /// 分析目录中的所有 AssetBundle 文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>每个 Bundle 的分析报告</returns>
        public Dictionary<string, AnalysisReport> AnalyzeDirectory(string directoryPath)
        {
            var results = new Dictionary<string, AnalysisReport>();

            // 查找所有可能的 AssetBundle 文件
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                .Where(f => IsBundleFile(f))
                .ToList();

            if (_verbose)
            {
                Console.WriteLine($"[批量分析] 找到 {files.Count} 个 AssetBundle 文件");
            }

            foreach (var file in files)
            {
                try
                {
                    var report = AnalyzeBundleFile(file);
                    results[file] = report;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[错误] 分析 {file} 失败: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// 判断文件是否为 AssetBundle 文件
        /// </summary>
        private bool IsBundleFile(string filePath)
        {
            try
            {
                using var reader = new FileReader(filePath);
                return reader.FileType == FileType.BundleFile;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 构建 Bundle 概览
        /// </summary>
        private BundleSummary BuildBundleSummary(
            BundleFile bundleFile,
            BundleFile.StorageBlock[] blocksInfo,
            BundleFile.Node[] directoryInfo,
            string bundlePath)
        {
            var header = bundleFile.m_Header;

            // 计算各部分大小
            long dataBlocksCompressed = blocksInfo.Sum(b => b.compressedSize);
            long dataBlocksUncompressed = blocksInfo.Sum(b => b.uncompressedSize);

            // 确定主要压缩类型
            var compressionTypes = blocksInfo.Select(b => (CompressionType)(b.flags & StorageBlockFlags.CompressionTypeMask)).Distinct().ToList();
            string compressionTypeStr = compressionTypes.Count == 1
                ? compressionTypes[0].ToString()
                : $"Mixed ({string.Join(", ", compressionTypes)})";

            return new BundleSummary
            {
                FileName = Path.GetFileName(bundlePath),
                TotalSizeCompressed = new FileInfo(bundlePath).Length,
                TotalSizeUncompressed = dataBlocksUncompressed + header.uncompressedBlocksInfoSize,
                HeaderSize = CalculateHeaderSize(header),
                BlocksInfoSizeCompressed = header.compressedBlocksInfoSize,
                BlocksInfoSizeUncompressed = header.uncompressedBlocksInfoSize,
                DataBlocksSizeCompressed = dataBlocksCompressed,
                DataBlocksSizeUncompressed = dataBlocksUncompressed,
                CompressionType = compressionTypeStr,
                UnityVersion = header.unityVersion ?? header.unityRevision ?? "Unknown",
                InternalFileCount = directoryInfo.Length,
                ResourceCount = 0 // 后面更新
            };
        }

        /// <summary>
        /// 计算 Bundle Header 大小
        /// </summary>
        private long CalculateHeaderSize(BundleFile.Header header)
        {
            long size = 0;
            size += (header.signature?.Length ?? 0) + 1;
            size += 4; // version (uint32)
            size += (header.unityVersion?.Length ?? 0) + 1;
            size += (header.unityRevision?.Length ?? 0) + 1;
            size += 8; // size (int64)
            size += 4; // compressedBlocksInfoSize (uint32)
            size += 4; // uncompressedBlocksInfoSize (uint32)
            size += 4; // flags (uint32)
            return size;
        }

        /// <summary>
        /// 解析内部文件列表
        /// </summary>
        private List<InternalFileInfo> ParseInternalFiles(BundleFile bundleFile, BundleFile.Node[] directoryInfo)
        {
            var result = new List<InternalFileInfo>();

            foreach (var node in directoryInfo)
            {
                var info = new InternalFileInfo
                {
                    FileName = node.path,
                    SizeUncompressed = node.size,
                    FileType = DetermineFileType(node.path),
                    Offset = node.offset
                };

                // 如果是 SerializedFile，尝试解析更多信息
                var streamFile = bundleFile.fileList.FirstOrDefault(f => f.path == node.path);
                if (streamFile != null && IsSerializedFile(streamFile))
                {
                    try
                    {
                        var serializedFile = ParseSerializedFile(streamFile);
                        if (serializedFile != null)
                        {
                            info.MetadataSize = serializedFile.header.m_DataOffset;
                            info.DataSize = serializedFile.header.m_FileSize - serializedFile.header.m_DataOffset;
                            info.ResourceCount = serializedFile.m_Objects.Count;
                            info.TypeCount = serializedFile.m_Types.Count;
                        }
                    }
                    catch
                    {
                        // 解析失败时保持默认值
                    }
                }

                result.Add(info);
            }

            return result;
        }

        /// <summary>
        /// 判断文件类型
        /// </summary>
        private string DetermineFileType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".assets" || path.Contains("CAB-"))
            {
                return "SerializedFile";
            }
            if (ext == ".resource" || ext == ".ress")
            {
                return "ResourceFile";
            }
            return "Unknown";
        }

        /// <summary>
        /// 判断是否为 SerializedFile
        /// </summary>
        private bool IsSerializedFile(StreamFile streamFile)
        {
            try
            {
                streamFile.stream.Position = 0;
                using var subReader = new FileReader(streamFile.fileName, streamFile.stream);
                return subReader.FileType == FileType.AssetsFile;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析 SerializedFile
        /// </summary>
        private SerializedFile? ParseSerializedFile(StreamFile streamFile)
        {
            streamFile.stream.Position = 0;
            var assetsManager = new AssetsManager();
            using var subReader = new FileReader(streamFile.fileName, streamFile.stream);
            if (subReader.FileType == FileType.AssetsFile)
            {
                return new SerializedFile(subReader, assetsManager);
            }
            return null;
        }

        /// <summary>
        /// 解析资源对象明细
        /// </summary>
        private List<ResourceInfo> ParseResources(
            AssetsManager assetsManager,
            BundleFile.Node[] directoryInfo,
            NodeBlockMapper nodeMapper,
            ResourceBlockMapper resourceMapper)
        {
            var result = new List<ResourceInfo>();
            var nodeByFileName = directoryInfo.ToDictionary(n => Path.GetFileName(n.path), n => n);

            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                // 找到对应的 Node
                var node = nodeByFileName.TryGetValue(assetsFile.fileName, out var foundNode) ? foundNode : null;
                if (node == null) continue;

                foreach (var obj in assetsFile.Objects)
                {
                    var resourceInfo = ParseSingleResource(obj, node, nodeMapper, resourceMapper, assetsFile);
                    if (resourceInfo != null)
                    {
                        result.Add(resourceInfo);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 解析单个资源
        /// </summary>
        private ResourceInfo? ParseSingleResource(
            Object obj,
            BundleFile.Node node,
            NodeBlockMapper nodeMapper,
            ResourceBlockMapper resourceMapper,
            SerializedFile assetsFile)
        {
            var dataSourceType = ResourceBlockMapper.DetermineDataSourceType(obj);
            List<BlockContribution> contributions;
            long uncompressedSize;
            string? externalFilePath = null;
            long dataOffset;

            // 获取资源名称
            string name = GetResourceName(obj);

            switch (dataSourceType)
            {
                case DataSourceType.Embedded:
                    {
                        var objInfo = assetsFile.m_Objects.FirstOrDefault(o => o.m_PathID == obj.m_PathID);
                        if (objInfo != null)
                        {
                            contributions = resourceMapper.CalculateEmbeddedResourceCompressedSize(objInfo, node);
                            uncompressedSize = objInfo.byteSize;
                            dataOffset = node.offset + objInfo.byteStart;
                        }
                        else
                        {
                            contributions = new List<BlockContribution>();
                            uncompressedSize = obj.byteSize;
                            dataOffset = 0;
                        }
                        break;
                    }

                case DataSourceType.ExternalTexture:
                    {
                        var texture = obj as Texture2D;
                        if (texture?.m_StreamData != null)
                        {
                            var (contribs, found, extNode) = resourceMapper.CalculateExternalTextureCompressedSize(texture.m_StreamData);
                            contributions = contribs;
                            uncompressedSize = texture.m_StreamData.size;
                            externalFilePath = texture.m_StreamData.path;
                            dataOffset = extNode != null ? extNode.offset + texture.m_StreamData.offset : 0;

                            if (!found)
                            {
                                dataSourceType = DataSourceType.ExternalMissing;
                            }
                        }
                        else
                        {
                            contributions = new List<BlockContribution>();
                            uncompressedSize = 0;
                            dataOffset = 0;
                            dataSourceType = DataSourceType.ExternalMissing;
                        }
                        break;
                    }

                case DataSourceType.ExternalAudio:
                    {
                        var audio = obj as AudioClip;
                        if (audio != null && !string.IsNullOrEmpty(audio.m_Source))
                        {
                            var (contribs, found, extNode) = resourceMapper.CalculateExternalAudioCompressedSize(audio.m_Source, audio.m_Offset, audio.m_Size);
                            contributions = contribs;
                            uncompressedSize = audio.m_Size;
                            externalFilePath = audio.m_Source;
                            dataOffset = extNode != null ? extNode.offset + audio.m_Offset : 0;

                            if (!found)
                            {
                                dataSourceType = DataSourceType.ExternalMissing;
                            }
                        }
                        else
                        {
                            contributions = new List<BlockContribution>();
                            uncompressedSize = 0;
                            dataOffset = 0;
                            dataSourceType = DataSourceType.ExternalMissing;
                        }
                        break;
                    }

                case DataSourceType.ExternalVideo:
                    {
                        var video = obj as VideoClip;
                        if (video != null)
                        {
                            contributions = new List<BlockContribution>();
                            uncompressedSize = (long)video.m_ExternalResources.m_Size;
                            externalFilePath = video.m_OriginalPath;
                            dataOffset = 0;
                        }
                        else
                        {
                            contributions = new List<BlockContribution>();
                            uncompressedSize = 0;
                            dataOffset = 0;
                            dataSourceType = DataSourceType.ExternalMissing;
                        }
                        break;
                    }

                default:
                    contributions = new List<BlockContribution>();
                    uncompressedSize = obj.byteSize;
                    dataOffset = 0;
                    break;
            }

            return new ResourceInfo
            {
                Name = name,
                TypeName = obj.type.ToString(),
                TypeId = (int)obj.type,
                PathId = obj.m_PathID,
                DataSource = dataSourceType.ToString(),
                SizeUncompressed = uncompressedSize,
                SizeCompressed = NodeBlockMapper.SumCompressedContributions(contributions),
                DataOffset = dataOffset,
                ExternalFilePath = externalFilePath,
                ContainerPath = null,
                InternalFileName = Path.GetFileName(node.path),
                BlockContributions = contributions
            };
        }

        /// <summary>
        /// 获取资源名称
        /// </summary>
        private string GetResourceName(Object obj)
        {
            if (obj is NamedObject named)
            {
                return named.m_Name;
            }
            if (obj is GameObject go)
            {
                return go.m_Name;
            }
            if (obj is Shader shader)
            {
                return shader.m_ParsedForm?.m_Name ?? shader.m_Name ?? "Shader";
            }
            return $"{obj.type}_{obj.m_PathID}";
        }

        /// <summary>
        /// 计算类型汇总
        /// </summary>
        private List<TypeSummary> CalculateTypeSummaries(List<ResourceInfo> resources, long bundleTotalSize)
        {
            var grouped = resources.GroupBy(r => r.TypeId).ToList();
            var result = new List<TypeSummary>();

            foreach (var group in grouped)
            {
                var typeResources = group.ToList();
                var largest = typeResources.OrderByDescending(r => r.SizeUncompressed).FirstOrDefault();

                var summary = new TypeSummary
                {
                    TypeName = typeResources.First().TypeName,
                    TypeId = group.Key,
                    ResourceCount = typeResources.Count,
                    TotalSizeUncompressed = typeResources.Sum(r => r.SizeUncompressed),
                    TotalSizeCompressed = typeResources.Sum(r => r.SizeCompressed),
                    AverageSize = typeResources.Count > 0 ? typeResources.Sum(r => r.SizeUncompressed) / typeResources.Count : 0,
                    LargestResourceName = largest?.Name ?? "",
                    LargestSize = largest?.SizeUncompressed ?? 0,
                    BundleRatio = bundleTotalSize > 0
                        ? (double)typeResources.Sum(r => r.SizeUncompressed) / bundleTotalSize
                        : 0
                };

                result.Add(summary);
            }

            return result.OrderByDescending(s => s.TotalSizeUncompressed).ToList();
        }

        /// <summary>
        /// 构建数据块分布信息
        /// </summary>
        private List<BlockInfo> BuildBlockInfos(
            BundleFile.StorageBlock[] blocksInfo,
            NodeBlockMapper nodeMapper,
            BundleFile.Node[] directoryInfo)
        {
            var result = new List<BlockInfo>();
            var containedFiles = nodeMapper.GetBlockContainedFiles(directoryInfo);

            for (int i = 0; i < blocksInfo.Length; i++)
            {
                var block = blocksInfo[i];
                var compressionType = (CompressionType)(block.flags & StorageBlockFlags.CompressionTypeMask);

                var info = new BlockInfo
                {
                    Index = i,
                    CompressionType = compressionType.ToString(),
                    SizeCompressed = block.compressedSize,
                    SizeUncompressed = block.uncompressedSize,
                    CompressionRatio = block.uncompressedSize > 0
                        ? (double)block.compressedSize / block.uncompressedSize
                        : 0,
                    ContainedFiles = containedFiles.TryGetValue(i, out var files) ? files : new List<string>()
                };

                result.Add(info);
            }

            return result;
        }

        /// <summary>
        /// 解析 TypeTree 类型结构信息
        /// </summary>
        private List<TypeTreeInfo> ParseTypeTrees(AssetsManager assetsManager)
        {
            var result = new List<TypeTreeInfo>();

            // 使用反射获取 SerializedFile 的 m_Types 私有字段
            var serializedFileType = typeof(SerializedFile);
            var typesField = serializedFileType.GetField("m_Types", BindingFlags.Public | BindingFlags.Instance);

            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                try
                {
                    // 获取 m_Types
                    var types = typesField?.GetValue(assetsFile) as List<SerializedType>;
                    if (types == null) continue;

                    foreach (var serializedType in types)
                    {
                        var typeTreeInfo = new TypeTreeInfo
                        {
                            TypeId = serializedType.classID,
                            TypeName = GetTypeNameFromClassID(serializedType.classID),
                            IsStripped = serializedType.m_IsStrippedType,
                            ScriptTypeIndex = serializedType.m_ScriptTypeIndex >= 0 ? serializedType.m_ScriptTypeIndex : null,
                            InternalFileName = assetsFile.fileName,
                            NodeCount = serializedType.m_Type?.m_Nodes?.Count ?? 0
                        };

                        // 解析 TypeTree 节点
                        if (serializedType.m_Type?.m_Nodes != null)
                        {
                            foreach (var treeNode in serializedType.m_Type.m_Nodes)
                            {
                                typeTreeInfo.Nodes.Add(new TypeTreeNodeInfo
                                {
                                    Type = treeNode.m_Type,
                                    Name = treeNode.m_Name,
                                    Level = treeNode.m_Level,
                                    ByteSize = treeNode.m_ByteSize,
                                    Index = treeNode.m_Index,
                                    TypeFlags = treeNode.m_TypeFlags,
                                    Version = treeNode.m_Version,
                                    MetaFlag = treeNode.m_MetaFlag,
                                    RefTypeHash = treeNode.m_RefTypeHash != 0 ? treeNode.m_RefTypeHash : null
                                });
                            }
                        }

                        result.Add(typeTreeInfo);
                    }
                }
                catch
                {
                    // 解析失败时跳过
                }
            }

            return result;
        }

        /// <summary>
        /// 解析非资源数据（在 AssetsManager.LoadFiles 之前调用）
        /// 包括 Bundle Header、BlocksInfo、SerializedFile Metadata 各部分
        /// </summary>
        private List<ResourceInfo> ParseNonResourceDataBeforeLoad(
            BundleFile bundleFile,
            BundleFile.StorageBlock[] blocksInfo,
            BundleFile.Node[] directoryInfo,
            NodeBlockMapper nodeMapper)
        {
            var result = new List<ResourceInfo>();
            var bundleFileName = bundleFile.m_Header.signature ?? "bundle";

            // Bundle 级非资源数据（不需要 SerializedFile 信息）
            result.AddRange(NonResourceDataCalculator.GenerateBundleNonResourceData(
                bundleFile.m_Header, blocksInfo, bundleFileName));

            // SerializedFile 级非资源数据将在 LoadFiles 后从 AssetsManager 获取
            // 因为 fileList.stream 的内容格式问题，直接手动解析不可靠

            return result;
        }

        /// <summary>
        /// 在 AssetsManager.LoadFiles 后生成 SerializedFile 级非资源数据
        /// 从已加载的 SerializedFile 对象获取 metadata 信息
        /// </summary>
        private List<ResourceInfo> ParseNonResourceDataAfterLoad(
            AssetsManager assetsManager,
            BundleFile.Node[] directoryInfo,
            NodeBlockMapper nodeMapper)
        {
            var result = new List<ResourceInfo>();
            var nodeByFileName = directoryInfo.ToDictionary(n => Path.GetFileName(n.path), n => n);

            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                var fileName = assetsFile.fileName;
                if (!nodeByFileName.TryGetValue(fileName, out var node))
                {
                    if (_verbose) Console.WriteLine($"[跳过] {fileName} 未找到对应 Node");
                    continue;
                }

                if (_verbose)
                {
                    Console.WriteLine($"[SerializedFile] {fileName}: version={assetsFile.header.m_Version}, dataOffset={assetsFile.header.m_DataOffset}, metadataSize={assetsFile.header.m_MetadataSize}");
                }

                // 计算 SerializedFile metadata 各部分大小
                var metadataParts = CalculateSerializedFileMetadataParts(assetsFile, node, nodeMapper);
                result.AddRange(metadataParts);
            }

            return result;
        }

        /// <summary>
        /// 计算 SerializedFile metadata 各部分大小
        /// </summary>
        private List<ResourceInfo> CalculateSerializedFileMetadataParts(
            SerializedFile serializedFile,
            BundleFile.Node node,
            NodeBlockMapper nodeMapper)
        {
            var result = new List<ResourceInfo>();
            var fileName = serializedFile.fileName;

            // 尝试使用 MetadataPositionTracker 获取精确大小
            // 注意：当前由于 assetsManager.LoadFiles 重新创建 FileReader，
            // stream 内容可能有问题，暂时降级到估算方案
            // TODO: 修复精确追踪问题
            return CalculateSerializedFileMetadataPartsByEstimation(serializedFile, node, nodeMapper);
        }

        /// <summary>
        /// 根据部分名称获取数据源标识
        /// </summary>
        private string GetDataSourceFromPartName(string partName)
        {
            return partName switch
            {
                "Header" => "SerializedFileHeader",
                "Types" => "SerializedFileTypes",
                "ObjectDir" => "SerializedFileObjects",
                "ScriptTypes" => "SerializedFileScriptTypes",
                "FileIdentifier" => "SerializedFileFileIdentifier",
                "RefTypes" => "SerializedFileRefTypes",
                "UserInformation" => "SerializedFileUserInformation",
                "Externals" => "SerializedFileExternals",
                _ => $"SerializedFile{partName}"
            };
        }

        /// <summary>
        /// 使用估算方案计算 SerializedFile metadata 各部分大小（降级方案）
        /// </summary>
        [Obsolete("Use MetadataPositionTracker instead")]
        private List<ResourceInfo> CalculateSerializedFileMetadataPartsByEstimation(
            SerializedFile serializedFile,
            BundleFile.Node node,
            NodeBlockMapper nodeMapper)
        {
            var result = new List<ResourceInfo>();
            var fileName = serializedFile.fileName;
            var version = serializedFile.header.m_Version;

            // 计算 Header 大小
            long headerSize = CalculateSerializedFileHeaderSize(version);

            // metadata 总大小 = m_DataOffset（从文件开头到数据区开始）
            long totalMetadataSize = serializedFile.header.m_DataOffset;

            // 计算 ObjectDir 大小
            long objectDirSize = EstimateObjectDirSize(serializedFile);

            // 计算 Types + TypeTree 大小
            long typesSizeEstimate = EstimateTypesSize(serializedFile);

            // 确保各部分不会超过 metadata 总大小
            // Types 大小上限 = totalMetadataSize - headerSize - objectDirSize - 最小 Externals(4 bytes)
            long typesSizeMax = totalMetadataSize - headerSize - objectDirSize - 4;
            if (typesSizeEstimate > typesSizeMax)
            {
                // 估算过大，使用上限
                typesSizeEstimate = typesSizeMax > 0 ? typesSizeMax : 0;
            }

            // Externals 大小 = 剩余部分
            long externalsSize = totalMetadataSize - headerSize - typesSizeEstimate - objectDirSize;
            if (externalsSize < 0) externalsSize = 0;

            // 计算各部分的压缩大小（使用比例法）
            long nodeAbsoluteStart = node.offset;

            // Header 部分
            var headerContributions = nodeMapper.CalculateCompressedSize(nodeAbsoluteStart, headerSize);
            long headerCompressed = NodeBlockMapper.SumCompressedContributions(headerContributions);

            result.Add(new ResourceInfo
            {
                Name = $"{fileName} FileHeader",
                DataCategory = DataCategory.NonResource,
                NonResourceType = NonResourceType.FileHeader,
                TypeName = "[FileHeader]",
                TypeId = -1,
                PathId = 0,
                DataSource = "SerializedFileHeader",
                SizeUncompressed = headerSize,
                SizeCompressed = headerCompressed,
                DataOffset = nodeAbsoluteStart,
                InternalFileName = fileName,
                BlockContributions = headerContributions
            });

            // Types 部分
            long typesStart = nodeAbsoluteStart + headerSize;
            var typesContributions = nodeMapper.CalculateCompressedSize(typesStart, typesSizeEstimate);
            long typesCompressed = NodeBlockMapper.SumCompressedContributions(typesContributions);

            result.Add(new ResourceInfo
            {
                Name = $"{fileName} TypeTree",
                DataCategory = DataCategory.NonResource,
                NonResourceType = NonResourceType.TypeTree,
                TypeName = "[TypeTree]",
                TypeId = -1,
                PathId = 0,
                DataSource = "SerializedFileTypes",
                SizeUncompressed = typesSizeEstimate,
                SizeCompressed = typesCompressed,
                DataOffset = typesStart,
                InternalFileName = fileName,
                BlockContributions = typesContributions
            });

            // ObjectDir 部分
            long objectDirStart = typesStart + typesSizeEstimate;
            var objectDirContributions = nodeMapper.CalculateCompressedSize(objectDirStart, objectDirSize);
            long objectDirCompressed = NodeBlockMapper.SumCompressedContributions(objectDirContributions);

            result.Add(new ResourceInfo
            {
                Name = $"{fileName} ObjectDir",
                DataCategory = DataCategory.NonResource,
                NonResourceType = NonResourceType.ObjectDir,
                TypeName = "[ObjectDir]",
                TypeId = -1,
                PathId = 0,
                DataSource = "SerializedFileObjects",
                SizeUncompressed = objectDirSize,
                SizeCompressed = objectDirCompressed,
                DataOffset = objectDirStart,
                InternalFileName = fileName,
                BlockContributions = objectDirContributions
            });

            // Externals 部分
            long externalsStart = objectDirStart + objectDirSize;
            var externalsContributions = nodeMapper.CalculateCompressedSize(externalsStart, externalsSize);
            long externalsCompressed = NodeBlockMapper.SumCompressedContributions(externalsContributions);

            result.Add(new ResourceInfo
            {
                Name = $"{fileName} Externals",
                DataCategory = DataCategory.NonResource,
                NonResourceType = NonResourceType.Externals,
                TypeName = "[Externals]",
                TypeId = -1,
                PathId = 0,
                DataSource = "SerializedFileExternals",
                SizeUncompressed = externalsSize,
                SizeCompressed = externalsCompressed,
                DataOffset = externalsStart,
                InternalFileName = fileName,
                BlockContributions = externalsContributions
            });

            return result;
        }

        /// <summary>
        /// 计算 SerializedFile Header 大小（根据版本）
        /// </summary>
        private long CalculateSerializedFileHeaderSize(SerializedFileFormatVersion version)
        {
            long size = 0;
            // 基础 header: metadataSize(4) + fileSize(4) + version(4) + dataOffset(4) = 16
            size += 16;

            if (version >= SerializedFileFormatVersion.Unknown_9)
            {
                // endianess(1) + reserved(3) = 4
                size += 4;
            }

            if (version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                // metadataSize(4) + fileSize(8) + dataOffset(8) + unknown(8) = 28
                size += 28;
            }

            // unityVersion string (估算平均长度 ~20 字节 + null terminator)
            if (version >= SerializedFileFormatVersion.Unknown_7)
            {
                size += 21; // 估算值
            }

            // m_TargetPlatform (4)
            if (version >= SerializedFileFormatVersion.Unknown_8)
            {
                size += 4;
            }

            // m_EnableTypeTree (1)
            if (version >= SerializedFileFormatVersion.HasTypeTreeHashes)
            {
                size += 1;
            }

            return size;
        }

        /// <summary>
        /// 估算 Types + TypeTree 大小（改进版：使用实际节点数）
        /// </summary>
        [Obsolete("Use MetadataPositionTracker instead")]
        private long EstimateTypesSize(SerializedFile serializedFile)
        {
            var version = serializedFile.header.m_Version;
            var types = serializedFile.m_Types;
            long size = 4; // typeCount (int32)

            // 每个 node 的大小
            int nodeSize = 24; // 基础版本
            if (version >= SerializedFileFormatVersion.TypeTreeNodeWithTypeFlags)
            {
                nodeSize = 32; // 含 RefTypeHash
            }

            foreach (var type in types)
            {
                // SerializedType 基础字段
                size += 4; // classID

                if (version >= SerializedFileFormatVersion.RefactoredClassId)
                {
                    size += 1; // m_IsStrippedType
                }

                if (version >= SerializedFileFormatVersion.RefactorTypeData)
                {
                    size += 2; // m_ScriptTypeIndex
                }

                if (version >= SerializedFileFormatVersion.HasTypeTreeHashes)
                {
                    // 条件性 m_ScriptID (16 bytes)
                    if (version >= SerializedFileFormatVersion.RefactoredClassId && type.classID == 114)
                    {
                        size += 16;
                    }
                    // m_OldTypeHash (16)
                    size += 16;
                }

                // TypeTree blob（如果存在）
                // Blob 结构: numberOfNodes(4) + stringBufferSize(4) + nodes × nodeSize + stringBuffer
                int nodeCount = type.m_Type?.m_Nodes?.Count ?? 0;
                if (nodeCount > 0)
                {
                    size += 4; // numberOfNodes
                    size += 4; // stringBufferSize（估算）

                    // node 数据
                    size += (long)nodeCount * nodeSize;

                    // stringBuffer（估算：平均每个节点约 20 bytes 的字符串）
                    // 使用保守估算，避免低估
                    size += nodeCount * 20;
                }

                // TypeDependencies（version >= StoresTypeDependencies 且非 RefType）
                if (version >= SerializedFileFormatVersion.StoresTypeDependencies)
                {
                    // 估算 4 + 4 个依赖 = 20 bytes
                    size += 20;
                }
            }

            // bigIDEnabled (version 7-13)
            if (version >= SerializedFileFormatVersion.Unknown_7 && version < SerializedFileFormatVersion.Unknown_14)
            {
                size += 4;
            }

            return size;
        }

        /// <summary>
        /// 估算 ObjectDir 大小
        /// </summary>
        private long EstimateObjectDirSize(SerializedFile serializedFile)
        {
            var version = serializedFile.header.m_Version;
            int objectCount = serializedFile.m_Objects.Count;

            // objectCount (4)
            long size = 4;

            // 每个 ObjectInfo 的大小估算
            long perObjectSize = 0;

            // PathID (8 或 4)
            if (version >= SerializedFileFormatVersion.Unknown_14)
            {
                perObjectSize += 8; // int64 + alignment
            }
            else
            {
                perObjectSize += 4; // int32
            }

            // byteStart (8 或 4)
            if (version >= SerializedFileFormatVersion.LargeFilesSupport)
            {
                perObjectSize += 8;
            }
            else
            {
                perObjectSize += 4;
            }

            // byteSize (4)
            perObjectSize += 4;

            // typeID (4)
            perObjectSize += 4;

            // classID (2) 或 type index reference
            if (version < SerializedFileFormatVersion.RefactoredClassId)
            {
                perObjectSize += 2;
            }

            return size + objectCount * perObjectSize;
        }

        /// <summary>
        /// 构建验证汇总
        /// 对比计算总和与文件实际大小
        /// </summary>
        private VerificationSummary BuildVerificationSummary(AnalysisReport report, string bundlePath)
        {
            var summary = new VerificationSummary();
            var actualFileSize = new FileInfo(bundlePath).Length;

            // 按类别汇总
            var categoryTotals = new List<CategoryTotal>();

            // BundleMeta (Bundle Header + BlocksInfo)
            var bundleMeta = report.NonResourceData
                .Where(r => r.NonResourceType == NonResourceType.BundleMeta)
                .ToList();
            categoryTotals.Add(new CategoryTotal
            {
                CategoryName = "BundleMeta",
                NonResourceType = NonResourceType.BundleMeta,
                SizeUncompressed = bundleMeta.Sum(r => r.SizeUncompressed),
                SizeCompressed = bundleMeta.Sum(r => r.SizeCompressed),
                ItemCount = bundleMeta.Count,
                Percentage = actualFileSize > 0 ? (double)bundleMeta.Sum(r => r.SizeCompressed) / actualFileSize * 100 : 0
            });

            // FileHeader
            var fileHeaders = report.NonResourceData
                .Where(r => r.NonResourceType == NonResourceType.FileHeader)
                .ToList();
            categoryTotals.Add(new CategoryTotal
            {
                CategoryName = "FileHeader",
                NonResourceType = NonResourceType.FileHeader,
                SizeUncompressed = fileHeaders.Sum(r => r.SizeUncompressed),
                SizeCompressed = fileHeaders.Sum(r => r.SizeCompressed),
                ItemCount = fileHeaders.Count,
                Percentage = actualFileSize > 0 ? (double)fileHeaders.Sum(r => r.SizeCompressed) / actualFileSize * 100 : 0
            });

            // TypeTree
            var typeTrees = report.NonResourceData
                .Where(r => r.NonResourceType == NonResourceType.TypeTree)
                .ToList();
            categoryTotals.Add(new CategoryTotal
            {
                CategoryName = "TypeTree",
                NonResourceType = NonResourceType.TypeTree,
                SizeUncompressed = typeTrees.Sum(r => r.SizeUncompressed),
                SizeCompressed = typeTrees.Sum(r => r.SizeCompressed),
                ItemCount = typeTrees.Count,
                Percentage = actualFileSize > 0 ? (double)typeTrees.Sum(r => r.SizeCompressed) / actualFileSize * 100 : 0
            });

            // ObjectDir
            var objectDirs = report.NonResourceData
                .Where(r => r.NonResourceType == NonResourceType.ObjectDir)
                .ToList();
            categoryTotals.Add(new CategoryTotal
            {
                CategoryName = "ObjectDir",
                NonResourceType = NonResourceType.ObjectDir,
                SizeUncompressed = objectDirs.Sum(r => r.SizeUncompressed),
                SizeCompressed = objectDirs.Sum(r => r.SizeCompressed),
                ItemCount = objectDirs.Count,
                Percentage = actualFileSize > 0 ? (double)objectDirs.Sum(r => r.SizeCompressed) / actualFileSize * 100 : 0
            });

            // Externals
            var externals = report.NonResourceData
                .Where(r => r.NonResourceType == NonResourceType.Externals)
                .ToList();
            categoryTotals.Add(new CategoryTotal
            {
                CategoryName = "Externals",
                NonResourceType = NonResourceType.Externals,
                SizeUncompressed = externals.Sum(r => r.SizeUncompressed),
                SizeCompressed = externals.Sum(r => r.SizeCompressed),
                ItemCount = externals.Count,
                Percentage = actualFileSize > 0 ? (double)externals.Sum(r => r.SizeCompressed) / actualFileSize * 100 : 0
            });

            // 资源数据
            categoryTotals.Add(new CategoryTotal
            {
                CategoryName = "资源数据",
                SizeUncompressed = report.Resources.Sum(r => r.SizeUncompressed),
                SizeCompressed = report.Resources.Sum(r => r.SizeCompressed),
                ItemCount = report.Resources.Count,
                Percentage = actualFileSize > 0 ? (double)report.Resources.Sum(r => r.SizeCompressed) / actualFileSize * 100 : 0
            });

            summary.CategoryTotals = categoryTotals;

            // 计算总计
            summary.TotalUncompressed = categoryTotals.Sum(c => c.SizeUncompressed);
            summary.TotalCompressed = categoryTotals.Sum(c => c.SizeCompressed);

            // 计算误差
            summary.ErrorInfo = new ErrorInfo
            {
                ActualFileSize = actualFileSize,
                CalculatedTotal = summary.TotalCompressed,
                AbsoluteError = Math.Abs(summary.TotalCompressed - actualFileSize),
                RelativeErrorPercent = actualFileSize > 0
                    ? Math.Abs(summary.TotalCompressed - actualFileSize) / (double)actualFileSize * 100
                    : 0
            };

            return summary;
        }

        /// <summary>
        /// 根据 ClassID 获取类型名称
        /// </summary>
        private string GetTypeNameFromClassID(int classID)
        {
            // 常见 Unity 类型 ID 映射
            var typeNames = new Dictionary<int, string>
            {
                { 1, "GameObject" },
                { 2, "Component" },
                { 3, "ManagedReference" },
                { 4, "Transform" },
                { 5, "RectTransform" },
                { 8, "Behaviour" },
                { 9, " MonoBehaviour" },
                { 11, "MonoScript" },
                { 12, "MonoBehaviour" },
                { 15, "Animation" },
                { 17, "AudioFilter" },
                { 18, "AudioListener" },
                { 19, "AudioSource" },
                { 20, "Camera" },
                { 21, "Collider" },
                { 23, "MeshFilter" },
                { 24, "MeshRenderer" },
                { 25, "Renderer" },
                { 26, "ParticleSystem" },
                { 28, "Mesh" },
                { 33, "MeshFilter" },
                { 34, "GameObject" },
                { 41, "OcclusionPortal" },
                { 43, "Mesh" },
                { 45, "Skybox" },
                { 47, "LightProbes" },
                { 48, "LightProbeGroup" },
                { 49, "LightProbeProxyVolume" },
                { 50, "Rigidbody" },
                { 51, "Rigidbody2D" },
                { 52, "Collider2D" },
                { 53, "Joint" },
                { 54, "Joint2D" },
                { 56, "ConstantForce" },
                { 57, "ConstantForce2D" },
                { 58, "WorldAnchor" },
                { 60, "ParticleSystemRenderer" },
                { 62, "PhysicsMaterial2D" },
                { 64, "BoxCollider" },
                { 65, "BoxCollider2D" },
                { 66, "CircleCollider2D" },
                { 68, "EdgeCollider2D" },
                { 70, "CapsuleCollider" },
                { 71, "CapsuleCollider2D" },
                { 72, "SphereCollider" },
                { 73, "TerrainCollider" },
                { 74, "WheelCollider" },
                { 75, "HingeJoint" },
                { 76, "HingeJoint2D" },
                { 83, "AudioClip" },
                { 84, "AudioReverbFilter" },
                { 85, "AudioHighPassFilter" },
                { 86, "AudioChorusFilter" },
                { 87, "AudioDistortionFilter" },
                { 88, "AudioEchoFilter" },
                { 89, "AudioLowPassFilter" },
                { 90, "AudioBehaviour" },
                { 91, "AudioManager" },
                { 92, "Analytics" },
                { 93, "AnalyticsTracker" },
                { 95, "Terrain" },
                { 96, "TerrainData" },
                { 98, "Light" },
                { 100, "GraphicsSettings" },
                { 101, "PhysicsManager" },
                { 102, "NavMeshAreas" },
                { 104, "QualitySettings" },
                { 108, "Light" },
                { 109, "LightingSettings" },
                { 110, "PlayerSettings" },
                { 111, "ProjectSettings" },
                { 112, "RuntimeInitializeOnLoadManager" },
                { 114, "MonoBehaviour" },
                { 115, "MonoScript" },
                { 117, "Shader" },
                { 119, "ShaderVariantCollection" },
                { 121, "Material" },
                { 128, "Font" },
                { 129, "FontSettings" },
                { 132, "Texture" },
                { 133, "Texture2D" },
                { 134, "Texture2DArray" },
                { 135, "Texture3D" },
                { 136, "TextureCubeArray" },
                { 137, "Cubemap" },
                { 138, "CubemapArray" },
                { 141, "RenderTexture" },
                { 142, "AssetBundle" },
                { 143, "AssetBundleManifest" },
                { 148, "PrefabInstance" },
                { 150, "PreloadData" },
                { 152, "DelayedCallManager" },
                { 153, "TextAsset" },
                { 156, "StreamingManager" },
                { 157, "LightmapSettings" },
                { 158, "LightmapData" },
                { 159, "LightProbes" },
                { 162, "Avatar" },
                { 164, "Animator" },
                { 165, "AnimatorController" },
                { 168, "AnimationClip" },
                { 170, "RuntimeAnimatorController" },
                { 171, "AnimatorOverrideController" },
                { 174, "RuntimeAnimatorController" },
                { 176, "AnimatorStateMachine" },
                { 177, "AnimatorState" },
                { 178, "AnimatorStateTransition" },
                { 179, "AnimatorTransition" },
                { 180, "AnimatorTransitionBase" },
                { 182, "BlendTree" },
                { 183, "Motion" },
                { 184, "Clip" },
                { 185, "AnimClip" },
                { 187, "AnimationClip" },
                { 189, "AvatarMask" },
                { 190, "AvatarMask" },
                { 191, "AnimatorStateMotionPair" },
                { 192, "AnimatorState" },
                { 193, "AnimatorStateMachine" },
                { 196, "NavMeshSettings" },
                { 197, "NavMeshData" },
                { 198, "NavMesh" },
                { 199, "NavMeshObstacle" },
                { 200, "NavMeshAgent" },
                { 205, "MeshCollider" },
                { 206, "ScriptableObject" },
                { 207, "AnimatorControllerParameter" },
                { 208, "AnimatorController" },
                { 210, "BlendTree" },
                { 213, "Sprite" },
                { 214, "SpriteRenderer" },
                { 218, "SpriteAtlas" },
                { 220, "VFXRenderer" },
                { 222, "VisualEffect" },
                { 223, "VisualEffectAsset" },
                { 224, "VisualEffectObject" },
                { 225, "VisualEffectSubgraph" },
                { 227, "VisualEffectSubgraphOperator" },
                { 228, "VisualEffectSubgraphBlock" },
                { 229, "VisualEffectObject" },
                { 230, "VideoPlayer" },
                { 231, "VideoClip" },
                { 233, "GameObject" },
                { 240, "BuildSettings" },
                { 241, "BuildSettings" },
                { 243, "AssetImporter" },
                { 244, "AssetImporter" },
                { 245, "AssetDatabase" },
                { 246, "AssetBundleRequest" },
                { 247, "AssetBundle" },
                { 248, "Prefab" },
                { 250, "NamedObject" },
                { 251, "EditorExtension" },
                { 252, "EditorExtensionImpl" },
                { 253, "Object" },
                { 1000, "PrefabInstance" },
                { 1001, "Prefab" }
            };

            return typeNames.TryGetValue(classID, out var name) ? name : $"Type_{classID}";
        }

        /// <summary>
        /// 解析 Externals 详情数据（ScriptTypes、Externals、RefTypes、UserInformation）
        /// </summary>
        private void ParseExternalsDetails(AssetsManager assetsManager, AnalysisReport report)
        {
            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                var fileName = assetsFile.fileName;

                // ScriptTypes（版本 >= 11，私有字段需反射）
                if (ScriptTypesField != null)
                {
                    var scriptTypes = (List<LocalSerializedObjectIdentifier>?)ScriptTypesField.GetValue(assetsFile);
                    if (scriptTypes != null)
                    {
                        foreach (var script in scriptTypes)
                        {
                            report.ScriptTypes.Add(new ScriptTypeInfo
                            {
                                InternalFileName = fileName,
                                LocalSerializedFileIndex = script.localSerializedFileIndex,
                                LocalIdentifierInFile = script.localIdentifierInFile
                            });
                        }
                    }
                }

                // Externals
                if (assetsFile.m_Externals != null)
                {
                    foreach (var ext in assetsFile.m_Externals)
                    {
                        report.ExternalDetails.Add(new ExternalDetailInfo
                        {
                            InternalFileName = fileName,
                            Guid = ext.guid.ToString("N"),
                            Type = ext.type,
                            TypeName = GetExternalTypeName(ext.type),
                            PathName = ext.pathName,
                            FileName = ext.fileName
                        });
                    }
                }

                // RefTypes（版本 >= 20）
                if (assetsFile.m_RefTypes != null)
                {
                    foreach (var refType in assetsFile.m_RefTypes)
                    {
                        report.RefTypes.Add(new RefTypeInfo
                        {
                            InternalFileName = fileName,
                            ClassID = refType.classID,
                            IsStrippedType = refType.m_IsStrippedType,
                            ScriptTypeIndex = refType.m_ScriptTypeIndex,
                            KlassName = refType.m_KlassName ?? string.Empty,
                            NameSpace = refType.m_NameSpace ?? string.Empty,
                            AsmName = refType.m_AsmName ?? string.Empty
                        });
                    }
                }

                // UserInformation（版本 >= 5）
                if (!string.IsNullOrEmpty(assetsFile.userInformation))
                {
                    report.UserInformations.Add(new UserInformationInfo
                    {
                        InternalFileName = fileName,
                        UserInformation = assetsFile.userInformation
                    });
                }
            }

            if (_verbose)
            {
                Console.WriteLine($"[Externals] ScriptTypes: {report.ScriptTypes.Count}");
                Console.WriteLine($"[Externals] Externals: {report.ExternalDetails.Count}");
                Console.WriteLine($"[Externals] RefTypes: {report.RefTypes.Count}");
                Console.WriteLine($"[Externals] UserInformation: {report.UserInformations.Count}");
            }
        }

        /// <summary>
        /// 获取 Externals 类型名称
        /// </summary>
        private string GetExternalTypeName(int type)
        {
            return type switch
            {
                0 => "NonAsset",
                2 => "SerializedAsset",
                3 => "MetaAsset",
                _ => $"Unknown({type})"
            };
        }
    }
}