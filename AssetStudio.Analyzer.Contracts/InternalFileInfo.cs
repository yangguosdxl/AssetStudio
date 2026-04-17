using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 内部文件信息
    /// </summary>
    public class InternalFileInfo
    {
        /// <summary>
        /// 文件名（完整路径）
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（解压后）
        /// </summary>
        [JsonProperty("size_uncompressed")]
        public long SizeUncompressed { get; set; }

        /// <summary>
        /// 文件类型（SerializedFile/ResourceFile）
        /// </summary>
        [JsonProperty("file_type")]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 在解压流中的偏移
        /// </summary>
        [JsonProperty("offset")]
        public long Offset { get; set; }

        /// <summary>
        /// 元数据大小（仅 SerializedFile）
        /// </summary>
        [JsonProperty("metadata_size")]
        public long? MetadataSize { get; set; }

        /// <summary>
        /// 数据区大小（仅 SerializedFile）
        /// </summary>
        [JsonProperty("data_size")]
        public long? DataSize { get; set; }

        /// <summary>
        /// 资源数量（仅 SerializedFile）
        /// </summary>
        [JsonProperty("resource_count")]
        public int? ResourceCount { get; set; }

        /// <summary>
        /// 类型数量（仅 SerializedFile）
        /// </summary>
        [JsonProperty("type_count")]
        public int? TypeCount { get; set; }
    }
}