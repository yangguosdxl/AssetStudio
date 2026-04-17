using AssetStudio;
using System.Collections.Generic;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// Block 累积偏移信息
    /// </summary>
    public class BlockOffsetInfo
    {
        /// <summary>
        /// Block 序号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 在 blocksStream 中的起始位置（解压后）
        /// </summary>
        public long StartOffset { get; set; }

        /// <summary>
        /// 在 blocksStream 中的结束位置（解压后）
        /// </summary>
        public long EndOffset { get; set; }

        /// <summary>
        /// 解压后大小
        /// </summary>
        public long UncompressedSize { get; set; }

        /// <summary>
        /// 压缩后大小
        /// </summary>
        public long CompressedSize { get; set; }

        /// <summary>
        /// 压缩类型
        /// </summary>
        public CompressionType CompressionType { get; set; }
    }

    /// <summary>
    /// Block 累积偏移计算器
    /// 用于建立 blocksStream 位置与 StorageBlock 的映射
    /// </summary>
    public static class BlockOffsetCalculator
    {
        /// <summary>
        /// 计算 Block 累积偏移表
        /// </summary>
        /// <param name="blocksInfo">StorageBlock 数组</param>
        /// <returns>Block 累积偏移信息列表</returns>
        public static List<BlockOffsetInfo> CalculateCumulativeOffsets(BundleFile.StorageBlock[] blocksInfo)
        {
            var result = new List<BlockOffsetInfo>();
            long cumulativeOffset = 0;

            for (int i = 0; i < blocksInfo.Length; i++)
            {
                var block = blocksInfo[i];
                var info = new BlockOffsetInfo
                {
                    Index = i,
                    StartOffset = cumulativeOffset,
                    EndOffset = cumulativeOffset + block.uncompressedSize,
                    UncompressedSize = block.uncompressedSize,
                    CompressedSize = block.compressedSize,
                    CompressionType = (CompressionType)(block.flags & StorageBlockFlags.CompressionTypeMask)
                };
                result.Add(info);
                cumulativeOffset += block.uncompressedSize;
            }

            return result;
        }

        /// <summary>
        /// 查找包含指定位置的 Block
        /// </summary>
        /// <param name="offsetTable">累积偏移表</param>
        /// <param name="position">blocksStream 中的位置</param>
        /// <returns>包含该位置的 Block 信息，若无则返回 null</returns>
        public static BlockOffsetInfo? FindBlockAtPosition(List<BlockOffsetInfo> offsetTable, long position)
        {
            foreach (var block in offsetTable)
            {
                if (position >= block.StartOffset && position < block.EndOffset)
                {
                    return block;
                }
            }
            return null;
        }

        /// <summary>
        /// 查找与指定范围有交集的所有 Block
        /// </summary>
        /// <param name="offsetTable">累积偏移表</param>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <returns>有交集的 Block 信息列表</returns>
        public static List<BlockOffsetInfo> FindIntersectingBlocks(List<BlockOffsetInfo> offsetTable, long start, long end)
        {
            var result = new List<BlockOffsetInfo>();
            foreach (var block in offsetTable)
            {
                // 检查是否有交集：[start, end) 与 [block.StartOffset, block.EndOffset)
                if (end > block.StartOffset && start < block.EndOffset)
                {
                    result.Add(block);
                }
            }
            return result;
        }
    }
}