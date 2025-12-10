namespace Inkslab.Linq
{
    /// <summary>
    /// JSON 有效负载。
    /// </summary>
    public sealed class JsonPayload
    {
        private readonly string _json;

        /// <summary>
        /// 初始化 <see cref="JsonPayload"/> 类的新实例。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        public JsonPayload(string json)
        {
            _json = json;
        }

        /// <summary>
        /// 隐式转换。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        public static implicit operator JsonPayload(string json)
        {
            return new JsonPayload(json);
        }

        /// <summary>
        /// 获取 JSON 字符串。
        /// </summary>
        /// <returns>JSON 字符串。</returns>
        public override string ToString()
        {
            return _json;
        }
    }
}