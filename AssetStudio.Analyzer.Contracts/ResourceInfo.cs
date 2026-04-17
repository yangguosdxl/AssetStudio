using Newtonsoft.Json;
using System.Collections.Generic;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 资源对象明细（也用于非资源数据）
    /// </summary>
    public class ResourceInfo
    {
        /// <summary>
        /// 资源名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 数据类别（资源/非资源）
        /// </summary>
        [JsonProperty("data_category")]
        public DataCategory DataCategory { get; set; } = DataCategory.Resource;

        /// <summary>
        /// 非资源数据类型（仅非资源数据有值）
        /// </summary>
        [JsonProperty("non_resource_type")]
        public NonResourceType? NonResourceType { get; set; }

        /// <summary>
        /// 资源类型名称
        /// </summary>
        [JsonProperty("type_name")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 资源类型 ID
        /// </summary>
        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        /// <summary>
        /// 资源 PathID
        /// </summary>
        [JsonProperty("path_id")]
        public long PathId { get; set; }

        /// <summary>
        /// 数据来源类型
        /// Embedded: 内嵌在 SerializedFile 中
        /// ExternalTexture: Texture2D 数据在 .resource 文件
        /// ExternalAudio: AudioClip 数据在 .resource 文件
        /// ExternalVideo: VideoClip 数据在 .resource 文件
        /// ExternalMissing: 外部文件缺失
        /// </summary>
        [JsonProperty("data_source")]
        public string DataSource { get; set; } = string.Empty;

        /// <summary>
        /// 资源大小（解压后）
        /// </summary>
        [JsonProperty("size_uncompressed")]
        public long SizeUncompressed { get; set; }

        /// <summary>
        /// 资源大小（压缩后，估算值）
        /// </summary>
        [JsonProperty("size_compressed")]
        public long SizeCompressed { get; set; }

        /// <summary>
        /// 数据在解压流中的偏移
        /// </summary>
        [JsonProperty("data_offset")]
        public long DataOffset { get; set; }

        /// <summary>
        /// 外部文件路径（如有）
        /// </summary>
        [JsonProperty("external_file_path")]
        public string? ExternalFilePath { get; set; }

        /// <summary>
        /// Container 路径（AssetBundle 中的路径）
        /// </summary>
        [JsonProperty("container_path")]
        public string? ContainerPath { get; set; }

        /// <summary>
        /// 所属内部文件名
        /// </summary>
        [JsonProperty("internal_file_name")]
        public string InternalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Block 贡献明细
        /// </summary>
        [JsonProperty("block_contributions")]
        public List<BlockContribution> BlockContributions { get; set; } = new List<BlockContribution>();
    }
}