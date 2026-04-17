using AssetStudio;
using AssetStudio.Analyzer.Contracts;
using System;
using System.Collections.Generic;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// 非资源数据压缩大小计算器
    /// </summary>
    public static class NonResourceDataCalculator
    {
        /// <summary>
        /// 计算 Bundle Header 的压缩大小
        /// Bundle Header 不压缩，压缩前后大小相等
        /// </summary>
        public static long CalculateBundleHeaderCompressedSize(long uncompressedSize)
        {
            return uncompressedSize; // 不压缩
        }

        /// <summary>
        /// 计算 BlocksInfo Metadata 的压缩大小
        /// 直接从 Bundle header 获取
        /// </summary>
        public static long CalculateBlocksInfoCompressedSize(BundleFile.Header header)
        {
            return header.compressedBlocksInfoSize;
        }

        /// <summary>
        /// 计算 SerializedFile Metadata 各部分的压缩大小
        /// 使用 NodeBlockMapper 比例法计算
        /// </summary>
        public static long CalculateMetadataPartCompressedSize(
            MetadataPartInfo part,
            BundleFile.Node node,
            NodeBlockMapper mapper)
        {
            // 计算 Metadata 部分在 blocksStream 中的绝对位置
            long absoluteStart = node.offset + part.StartPosition;
            long size = part.Size;

            // 使用 NodeBlockMapper 比例法计算压缩贡献
            var contributions = mapper.CalculateCompressedSize(absoluteStart, size);
            return NodeBlockMapper.SumCompressedContributions(contributions);
        }

        /// <summary>
        /// 为非资源数据生成 BlockContributions 列表
        /// </summary>
        public static List<BlockContribution> GenerateBlockContributions(
            long absoluteStart,
            long size,
            NodeBlockMapper mapper)
        {
            return mapper.CalculateCompressedSize(absoluteStart, size);
        }

        /// <summary>
        /// 生成 Bundle 级非资源数据行（Bundle Header 和 BlocksInfo）
        /// </summary>
        public static List<ResourceInfo> GenerateBundleNonResourceData(
            BundleFile.Header header,
            BundleFile.StorageBlock[] blocksInfo,
            string bundleFileName)
        {
            var result = new List<ResourceInfo>();

            // Bundle Header
            long headerSize = MetadataPositionTracker.CalculateBundleHeaderSize(header);
            result.Add(new ResourceInfo
            {
                Name = "Bundle Header",
                DataCategory = DataCategory.NonResource,
                NonResourceType = NonResourceType.BundleMeta,
                TypeName = "[BundleMeta]",
                TypeId = -1,
                PathId = 0,
                DataSource = "BundleHeader",
                SizeUncompressed = headerSize,
                SizeCompressed = headerSize, // 不压缩
                DataOffset = 0,
                InternalFileName = bundleFileName,
                BlockContributions = new List<BlockContribution>()
            });

            // BlocksInfo Metadata
            result.Add(new ResourceInfo
            {
                Name = "BlocksInfo",
                DataCategory = DataCategory.NonResource,
                NonResourceType = NonResourceType.BundleMeta,
                TypeName = "[BundleMeta]",
                TypeId = -1,
                PathId = 0,
                DataSource = "BlocksInfo",
                SizeUncompressed = header.uncompressedBlocksInfoSize,
                SizeCompressed = header.compressedBlocksInfoSize,
                DataOffset = headerSize,
                InternalFileName = bundleFileName,
                BlockContributions = new List<BlockContribution>()
            });

            return result;
        }

        /// <summary>
        /// 生成 SerializedFile Metadata 非资源数据行
        /// </summary>
        public static List<ResourceInfo> GenerateSerializedFileMetadataData(
            List<MetadataPartInfo> parts,
            BundleFile.Node node,
            NodeBlockMapper mapper,
            string internalFileName)
        {
            var result = new List<ResourceInfo>();

            foreach (var part in parts)
            {
                long absoluteStart = node.offset + part.StartPosition;
                var contributions = mapper.CalculateCompressedSize(absoluteStart, part.Size);
                long compressedSize = NodeBlockMapper.SumCompressedContributions(contributions);

                result.Add(new ResourceInfo
                {
                    Name = $"{internalFileName} {part.Name}",
                    DataCategory = DataCategory.NonResource,
                    NonResourceType = part.NonResourceType,
                    TypeName = $"[{part.NonResourceType}]",
                    TypeId = -1,
                    PathId = 0,
                    DataSource = part.NonResourceType.ToString(),
                    SizeUncompressed = part.Size,
                    SizeCompressed = compressedSize,
                    DataOffset = absoluteStart,
                    InternalFileName = internalFileName,
                    BlockContributions = contributions
                });
            }

            return result;
        }
    }
}