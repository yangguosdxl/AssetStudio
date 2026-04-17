using Newtonsoft.Json;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// ScriptTypes 详情 - LocalSerializedObjectIdentifier 数据
    /// </summary>
    public class ScriptTypeInfo
    {
        /// <summary>
        /// 所属内部文件名
        /// </summary>
        [JsonProperty("internal_file_name")]
        public string InternalFileName { get; set; } = string.Empty;

        /// <summary>
        /// 本地序列化文件索引
        /// </summary>
        [JsonProperty("local_serialized_file_index")]
        public int LocalSerializedFileIndex { get; set; }

        /// <summary>
        /// 文件内的本地标识符
        /// </summary>
        [JsonProperty("local_identifier_in_file")]
        public long LocalIdentifierInFile { get; set; }
    }

    /// <summary>
    /// Externals 详情 - FileIdentifier 数据
    /// </summary>
    public class ExternalDetailInfo
    {
        /// <summary>
        /// 所属内部文件名
        /// </summary>
        [JsonProperty("internal_file_name")]
        public string InternalFileName { get; set; } = string.Empty;

        /// <summary>
        /// GUID（32字符十六进制格式）
        /// </summary>
        [JsonProperty("guid")]
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// 类型值（0=NonAsset, 2=SerializedAsset, 3=MetaAsset）
        /// </summary>
        [JsonProperty("type")]
        public int Type { get; set; }

        /// <summary>
        /// 类型名称（可读形式）
        /// </summary>
        [JsonProperty("type_name")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        [JsonProperty("path_name")]
        public string PathName { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// RefTypes 详情 - SerializedType（isRefType=true）数据
    /// </summary>
    public class RefTypeInfo
    {
        /// <summary>
        /// 所属内部文件名
        /// </summary>
        [JsonProperty("internal_file_name")]
        public string InternalFileName { get; set; } = string.Empty;

        /// <summary>
        /// 类型ID
        /// </summary>
        [JsonProperty("class_id")]
        public int ClassID { get; set; }

        /// <summary>
        /// 是否剥离类型
        /// </summary>
        [JsonProperty("is_stripped_type")]
        public bool IsStrippedType { get; set; }

        /// <summary>
        /// 脚本类型索引
        /// </summary>
        [JsonProperty("script_type_index")]
        public short ScriptTypeIndex { get; set; }

        /// <summary>
        /// 类名
        /// </summary>
        [JsonProperty("klass_name")]
        public string KlassName { get; set; } = string.Empty;

        /// <summary>
        /// 命名空间
        /// </summary>
        [JsonProperty("name_space")]
        public string NameSpace { get; set; } = string.Empty;

        /// <summary>
        /// 程序集名称
        /// </summary>
        [JsonProperty("asm_name")]
        public string AsmName { get; set; } = string.Empty;
    }

    /// <summary>
    /// UserInformation 详情
    /// </summary>
    public class UserInformationInfo
    {
        /// <summary>
        /// 所属内部文件名
        /// </summary>
        [JsonProperty("internal_file_name")]
        public string InternalFileName { get; set; } = string.Empty;

        /// <summary>
        /// 用户自定义信息
        /// </summary>
        [JsonProperty("user_information")]
        public string UserInformation { get; set; } = string.Empty;
    }
}