using AssetStudio.Analyzer.Contracts;
using Newtonsoft.Json;
using System.IO;

namespace AssetStudio.CLI.Analyzer
{
    /// <summary>
    /// JSON 报告导出器
    /// </summary>
    public class JsonReportExporter
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// 构造函数
        /// </summary>
        public JsonReportExporter()
        {
            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                DateFormatString = "yyyy-MM-dd HH:mm:ss"
            };
        }

        /// <summary>
        /// 导出分析报告到 JSON 文件
        /// </summary>
        /// <param name="report">分析报告</param>
        /// <param name="outputPath">输出文件路径</param>
        public void Export(AnalysisReport report, string outputPath)
        {
            var json = JsonConvert.SerializeObject(report, _settings);
            File.WriteAllText(outputPath, json);
        }

        /// <summary>
        /// 从 JSON 文件导入分析报告
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        /// <returns>分析报告</returns>
        public static AnalysisReport Import(string inputPath)
        {
            var json = File.ReadAllText(inputPath);
            return JsonConvert.DeserializeObject<AnalysisReport>(json)
                ?? new AnalysisReport();
        }
    }
}