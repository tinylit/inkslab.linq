namespace Inkslab.Linq
{
    /// <summary>
    /// JSONB 有效负载。
    /// </summary>
    public sealed class JsonbPayload
    {
        private readonly string _json;

        /// <summary>
        /// 初始化 <see cref="JsonPayload"/> 类的新实例。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        public JsonbPayload(string json)
        {
            _json = json;
        }

        /// <summary>
        /// 隐式转换。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        public static implicit operator JsonbPayload(string json)
        {
            return new JsonbPayload(json);
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