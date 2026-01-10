namespace RomajiConverter.Core.Options
{
    public class ToRomajiAIOptions : ToRomajiOptions
    {
        public string BaseUrl { get; set; }

        public string Model { get; set; }

        public string ApiKey { get; set; }

        /// <summary>
        /// 提示词，可以不传，使用默认提示词
        /// </summary>
        public string Prompt { get; set; }
    }
}