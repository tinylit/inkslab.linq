namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库链接。
    /// </summary>
    public interface IDatabaseStrings<TConnectionStrings> : IConnection where TConnectionStrings : IConnectionStrings
    {
    }

    /// <summary>
    /// 默认连接。
    /// </summary>
    public interface IDatabaseStrings : IDatabaseStrings<IConnectionStrings>
    {
    }
}