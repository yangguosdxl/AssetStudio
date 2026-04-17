using Newtonsoft.Json;
using System.Collections.Generic;

namespace AssetStudio.Analyzer.Contracts
{
    /// <summary>
    /// 分析报告顶层结构
    /// </summary>
    public class AnalysisReport
    {
        /// <summary>
        /// Bundle 文件概览信息
        /// </summary>
        [JsonProperty("bundle")]
        public BundleSummary Bundle { get; set; } = new BundleSummary();

        /// <summary>
        /// 内部文件列表
        /// </summary>
        [JsonProperty("internal_files")]
        public List<InternalFileInfo> InternalFiles { get; set; } = new List<InternalFileInfo>();

        /// <summary>
        /// 资源对象明细
        /// </summary>
        [JsonProperty("resources")]
        public List<ResourceInfo> Resources { get; set; } = new List<ResourceInfo>();

        /// <summary>
        /// 非资源数据明细
        /// Bundle Header, BlocksInfo, SerializedFile Metadata 等
        /// </summary>
        [JsonProperty("non_resource_data")]
        public List<ResourceInfo> NonResourceData { get; set; } = new List<ResourceInfo>();

        /// <summary>
        /// 类型汇总
        /// </summary>
        [JsonProperty("type_summaries")]
        public List<TypeSummary> TypeSummaries { get; set; } = new List<TypeSummary>();

        /// <summary>
        /// 数据块分布
        /// </summary>
        [JsonProperty("blocks")]
        public List<BlockInfo> Blocks { get; set; } = new List<BlockInfo>();

        /// <summary>
        /// TypeTree 类型结构信息
        /// </summary>
        [JsonProperty("type_trees")]
        public List<TypeTreeInfo> TypeTrees { get; set; } = new List<TypeTreeInfo>();

        /// <summary>
        /// 验证汇总信息
        /// 对比计算总和与文件实际大小
        /// </summary>
        [JsonProperty("verification_summary")]
        public VerificationSummary VerificationSummary { get; set; } = new VerificationSummary();

        /// <summary>
        /// ScriptTypes 详情列表（-e/--externals 启用时填充）
        /// </summary>
        [JsonProperty("script_types")]
        public List<ScriptTypeInfo> ScriptTypes { get; set; } = new List<ScriptTypeInfo>();

        /// <summary>
        /// Externals 详情列表（-e/--externals 启用时填充）
        /// </summary>
        [JsonProperty("external_details")]
        public List<ExternalDetailInfo> ExternalDetails { get; set; } = new List<ExternalDetailInfo>();

        /// <summary>
        /// RefTypes 详情列表（-e/--externals 启用时填充）
        /// </summary>
        [JsonProperty("ref_types")]
        public List<RefTypeInfo> RefTypes { get; set; } = new List<RefTypeInfo>();

        /// <summary>
        /// UserInformation 详情列表（-e/--externals 启用时填充）
        /// </summary>
        [JsonProperty("user_informations")]
        public List<UserInformationInfo> UserInformations { get; set; } = new List<UserInformationInfo>();
    }
}