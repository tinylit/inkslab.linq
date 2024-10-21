namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库链接。
    /// </summary>
    /// <remarks>
    /// 说明：<br/>
    /// 1、链接类型相当于是一个数据库链接，由“<see cref="Strings"/>”提供链接字符串，框架默认请注入“<see cref="IDatabase"/>”操作数据库。<br/>
    /// 2、若做其它业务需指定不同数据库的，请自定义“<see cref="IConnectionStrings"/>”实现，并注入“<see cref="IDatabase{TConnectionStrings}"/>”操作数据库或注入“<see cref="IDatabaseExecutor"/>”自己指定数据库链接。
    /// </remarks>
    public interface IConnectionStrings
    {
        /// <summary>
        /// 链接字符串。
        /// </summary>
        public string Strings { get; }
    }
}