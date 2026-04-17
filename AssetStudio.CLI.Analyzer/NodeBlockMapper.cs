using AssetStudio;
using AssetStudio.Analyzer.Contracts;
using System;
using System.Collections.Generic;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// Node 到 Block 的映射器
    /// 用于计算 Node 在压缩数据块中的大小贡献
    /// </summary>
    public class NodeBlockMapper
    {
        private readonly List<BlockOffsetInfo> _offsetTable;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="blocksInfo">StorageBlock 数组</param>
        public NodeBlockMapper(BundleFile.StorageBlock[] blocksInfo)
        {
            _offsetTable = BlockOffsetCalculator.CalculateCumulativeOffsets(blocksInfo);
        }

        /// <summary>
        /// 获取累积偏移表
        /// </summary>
        public List<BlockOffsetInfo> OffsetTable => _offsetTable;

        /// <summary>
        /// 计算 Node 的压缩大小
        /// 按比例计算每个 Block 的压缩贡献
        /// </summary>
        /// <param name="node">Node 信息</param>
        /// <returns>Node 的 Block 贡献明细</returns>
        public List<BlockContribution> CalculateNodeCompressedSize(BundleFile.Node node)
        {
            return CalculateCompressedSize(node.offset, node.size);
        }

        /// <summary>
        /// 计算指定范围的压缩大小
        /// </summary>
        /// <param name="absoluteStart">在 blocksStream 中的绝对起始位置</param>
        /// <param name="size">数据大小</param>
        /// <returns>Block 贡献明细</returns>
        public List<BlockContribution> CalculateCompressedSize(long absoluteStart, long size)
        {
            var contributions = new List<BlockContribution>();
            long absoluteEnd = absoluteStart + size;

            // 找出涉及的 Block
            var intersectingBlocks = BlockOffsetCalculator.FindIntersectingBlocks(_offsetTable, absoluteStart, absoluteEnd);

            foreach (var block in intersectingBlocks)
            {
                // 计算交集范围
                long intersectStart = Math.Max(absoluteStart, block.StartOffset);
                long intersectEnd = Math.Min(absoluteEnd, block.EndOffset);
                long intersectSize = intersectEnd - intersectStart;

                // 计算比例和压缩贡献
                double ratio = (double)intersectSize / block.UncompressedSize;
                long compressedContribution = (long)(block.CompressedSize * ratio);

                contributions.Add(new BlockContribution
                {
                    BlockIndex = block.Index,
                    IntersectStart = intersectStart,
                    IntersectEnd = intersectEnd,
                    IntersectSize = intersectSize,
                    Ratio = ratio,
                    CompressedContribution = compressedContribution
                });
            }

            return contributions;
        }

        /// <summary>
        /// 汇总压缩贡献得到总压缩大小
        /// </summary>
        /// <param name="contributions">Block 贡献明细</param>
        /// <returns>总压缩大小</returns>
        public static long SumCompressedContributions(List<BlockContribution> contributions)
        {
            long total = 0;
            foreach (var c in contributions)
            {
                total += c.CompressedContribution;
            }
            return total;
        }

        /// <summary>
        /// 获取每个 Block 包含的文件列表
        /// </summary>
        /// <param name="nodes">Node 数组</param>
        /// <returns>Block 到文件列表的映射</returns>
        public Dictionary<int, List<string>> GetBlockContainedFiles(BundleFile.Node[] nodes)
        {
            var result = new Dictionary<int, List<string>>();

            foreach (var node in nodes)
            {
                long nodeStart = node.offset;
                long nodeEnd = node.offset + node.size;

                var intersectingBlocks = BlockOffsetCalculator.FindIntersectingBlocks(_offsetTable, nodeStart, nodeEnd);
                foreach (var block in intersectingBlocks)
                {
                    if (!result.ContainsKey(block.Index))
                    {
                        result[block.Index] = new List<string>();
                    }
                    result[block.Index].Add(node.path);
                }
            }

            return result;
        }
    }
}