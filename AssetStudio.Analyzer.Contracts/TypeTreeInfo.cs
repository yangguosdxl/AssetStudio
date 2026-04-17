using Newtonsoft.Json;
using System.Collections.Generic;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// TypeTree 节点信息
    /// </summary>
    public class TypeTreeNodeInfo
    {
        /// <summary>
        /// 类型名（如 int, string, Vector3）
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 字段名
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 层级深度（0=根节点）
        /// </summary>
        [JsonProperty("level")]
        public int Level { get; set; }

        /// <summary>
        /// 字段字节大小
        /// </summary>
        [JsonProperty("byte_size")]
        public int ByteSize { get; set; }

        /// <summary>
        /// 索引
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// 类型标志（是否数组等）
        /// </summary>
        [JsonProperty("type_flags")]
        public int TypeFlags { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// 元数据标志
        /// </summary>
        [JsonProperty("meta_flag")]
        public int MetaFlag { get; set; }

        /// <summary>
        /// 引用类型哈希
        /// </summary>
        [JsonProperty("ref_type_hash")]
        public ulong? RefTypeHash { get; set; }
    }

    /// <summary>
    /// TypeTree 类型信息
    /// </summary>
    public class TypeTreeInfo
    {
        /// <summary>
        /// 类型 ID
        /// </summary>
        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        /// <summary>
        /// 类型名称
        /// </summary>
        [JsonProperty("type_name")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 是否为剥离类型
        /// </summary>
        [JsonProperty("is_stripped")]
        public bool IsStripped { get; set; }

        /// <summary>
        /// 脚本类型索引
        /// </summary>
        [JsonProperty("script_type_index")]
        public short? ScriptTypeIndex { get; set; }

        /// <summary>
        /// 所属内部文件名
        /// </summary>
        [JsonProperty("internal_file_name")]
        public string InternalFileName { get; set; } = string.Empty;

        /// <summary>
        /// TypeTree 节点列表
        /// </summary>
        [JsonProperty("nodes")]
        public List<TypeTreeNodeInfo> Nodes { get; set; } = new List<TypeTreeNodeInfo>();

        /// <summary>
        /// 节点数量
        /// </summary>
        [JsonProperty("node_count")]
        public int NodeCount { get; set; }
    }
}