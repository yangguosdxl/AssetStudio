using AssetStudio;
using AssetStudio.Analyzer.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// 数据来源类型
    /// </summary>
    public enum DataSourceType
    {
        Embedded,           // 内嵌在 SerializedFile 中
        ExternalTexture,    // Texture2D 数据在 .resource 文件
        ExternalAudio,      // AudioClip 数据在 .resource 文件
        ExternalVideo,      // VideoClip 数据在 .resource 文件
        ExternalMissing,    // 外部文件缺失
        BundleResource,     // 数据在同 bundle 内的 .resS 文件中
        BundleResourceShared // 多个 BundleResource 共享同一 .resS，非首个声明者
    }

    /// <summary>
    /// 资源到 Block 的映射器
    /// 用于计算资源在压缩数据块中的大小贡献
    /// </summary>
    public class ResourceBlockMapper
    {
        private readonly NodeBlockMapper _nodeMapper;
        private readonly BundleFile.Node[] _nodes;
        private readonly Dictionary<string, BundleFile.Node> _nodeByFileName;
        private readonly BundleFile.Node? _resSNode;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="nodeMapper">Node 到 Block 的映射器</param>
        /// <param name="nodes">Node 数组</param>
        public ResourceBlockMapper(NodeBlockMapper nodeMapper, BundleFile.Node[] nodes)
        {
            _nodeMapper = nodeMapper;
            _nodes = nodes;
            _nodeByFileName = nodes.ToDictionary(n => Path.GetFileName(n.path), n => n);
            // 查找 .resS 资源文件 Node
            _resSNode = nodes.FirstOrDefault(n => n.path.EndsWith(".resS"));
        }

        /// <summary>
        /// 计算嵌入资源的压缩大小
        /// </summary>
        /// <param name="objectInfo">资源信息</param>
        /// <param name="serializedFileNode">SerializedFile 对应的 Node</param>
        /// <returns>Block 贡献明细</returns>
        public List<BlockContribution> CalculateEmbeddedResourceCompressedSize(
            ObjectInfo objectInfo,
            BundleFile.Node serializedFileNode)
        {
            // 资源在 blocksStream 中的绝对位置
            // byteStart 已经包含了 m_DataOffset，直接加上 Node.offset
            long absoluteStart = serializedFileNode.offset + objectInfo.byteStart;
            long size = objectInfo.byteSize;

            return _nodeMapper.CalculateCompressedSize(absoluteStart, size);
        }

        /// <summary>
        /// 计算外部资源（Texture2D）的压缩大小
        /// </summary>
        /// <param name="streamingInfo">StreamingInfo 信息</param>
        /// <returns>Block 贡献明细和是否找到外部文件</returns>
        public (List<BlockContribution> contributions, bool found, BundleFile.Node? node) CalculateExternalTextureCompressedSize(
            StreamingInfo streamingInfo)
        {
            return CalculateExternalResourceCompressedSize(streamingInfo.path, streamingInfo.offset, streamingInfo.size);
        }

        /// <summary>
        /// 计算外部资源（AudioClip）的压缩大小
        /// </summary>
        /// <param name="source">外部文件路径</param>
        /// <param name="offset">数据偏移</param>
        /// <param name="size">数据大小</param>
        /// <returns>Block 贡献明细和是否找到外部文件</returns>
        public (List<BlockContribution> contributions, bool found, BundleFile.Node? node) CalculateExternalAudioCompressedSize(
            string source, long offset, long size)
        {
            return CalculateExternalResourceCompressedSize(source, offset, size);
        }

        /// <summary>
        /// 计算外部资源的压缩大小（通用方法）
        /// </summary>
        private (List<BlockContribution> contributions, bool found, BundleFile.Node? node) CalculateExternalResourceCompressedSize(
            string externalFileName, long dataOffset, long dataSize)
        {
            // 查找外部文件对应的 Node
            var fileName = Path.GetFileName(externalFileName);
            if (!_nodeByFileName.TryGetValue(fileName, out var externalNode))
            {
                // 外部文件未找到
                return (new List<BlockContribution>(), false, null);
            }

            // 外部数据在 blocksStream 中的绝对位置
            long absoluteStart = externalNode.offset + dataOffset;
            long size = dataSize;

            var contributions = _nodeMapper.CalculateCompressedSize(absoluteStart, size);
            return (contributions, true, externalNode);
        }

        /// <summary>
        /// 计算同 bundle .resS 文件中资源的压缩大小
        /// 当 Texture2D 的 m_StreamData 字段为零值/空但数据实际在 .resS 中时使用
        /// </summary>
        /// <returns>Block 贡献明细和是否找到 .resS Node</returns>
        public (List<BlockContribution> contributions, bool found, BundleFile.Node? node) CalculateBundleResourceCompressedSize()
        {
            if (_resSNode == null)
            {
                return (new List<BlockContribution>(), false, null);
            }

            // .resS Node 在 blocksStream 中的绝对位置
            long absoluteStart = _resSNode.offset;
            long size = _resSNode.size;

            var contributions = _nodeMapper.CalculateCompressedSize(absoluteStart, size);
            return (contributions, true, _resSNode);
        }

        /// <summary>
        /// 查找同 bundle 内的 .resS Node
        /// </summary>
        /// <returns>.resS Node 或 null</returns>
        public BundleFile.Node? FindResSNode()
        {
            return _resSNode;
        }

        /// <summary>
        /// 查找 Node 对应的内部文件名
        /// </summary>
        public string GetNodeFileName(BundleFile.Node node)
        {
            return Path.GetFileName(node.path);
        }

        /// <summary>
        /// 查找文件名对应的 Node
        /// </summary>
        public BundleFile.Node? FindNodeByFileName(string fileName)
        {
            return _nodeByFileName.TryGetValue(fileName, out var node) ? node : null;
        }

        /// <summary>
        /// 确定资源的数据来源类型
        /// </summary>
        public static DataSourceType DetermineDataSourceType(Object obj)
        {
            if (obj is Texture2D texture)
            {
                if (!string.IsNullOrEmpty(texture.m_StreamData?.path))
                {
                    return DataSourceType.ExternalTexture;
                }
                // image_data.Size == 0 表示纹理数据不在 SerializedFile 内
                // 可能存储在同 bundle 的 .resS 文件中
                if (texture.image_data?.Size == 0 && texture.m_StreamData != null)
                {
                    return DataSourceType.BundleResource;
                }
            }
            else if (obj is AudioClip audio)
            {
                if (!string.IsNullOrEmpty(audio.m_Source))
                {
                    return DataSourceType.ExternalAudio;
                }
            }
            else if (obj is VideoClip video)
            {
                if (!string.IsNullOrEmpty(video.m_OriginalPath))
                {
                    return DataSourceType.ExternalVideo;
                }
            }

            return DataSourceType.Embedded;
        }
    }
}
