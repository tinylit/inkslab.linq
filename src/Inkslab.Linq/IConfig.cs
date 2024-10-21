using System;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq
{
    /// <summary>
    /// 配置器。
    /// </summary>
    public interface IConfig
    {
        /// <summary>
        /// 配置表。
        /// </summary>
        /// <param name="name">表名称。</param>
        /// <param name="schema">表所在架构。</param>
        /// <returns>配置表。</returns>
        IConfigTable Table(string name, string schema = null);
    }

    /// <summary>
    /// 配置表。
    /// </summary>
    public interface IConfigTable
    {
        /// <summary>
        /// 配置字段。
        /// </summary>
        /// <param name="propertyInfo">属性。</param>
        /// <param name="name">列名。</param>
        /// <param name="configCol">配置列。</param>
        /// <returns>配置表。</returns>
        IConfigTable Field(PropertyInfo propertyInfo, string name, Action<IConfigCol> configCol = null);
    }

    /// <summary>
    /// 配置器。
    /// </summary>
    /// <typeparam name="TTable">表类型。</typeparam>
    public interface IConfig<TTable>
    {
        /// <summary>
        /// 配置表。
        /// </summary>
        /// <param name="name">表名称。</param>
        /// <param name="schema">表所在架构。</param>
        /// <returns>配置表。</returns>
        IConfigTable<TTable> Table(string name, string schema = null);
    }

    /// <summary>
    /// 配置表。
    /// </summary>
    /// <typeparam name="TTable">表类型。</typeparam>
    public interface IConfigTable<TTable>
    {
        /// <summary>
        /// 配置字段。
        /// </summary>
        /// <typeparam name="TCol">列类型。</typeparam>
        /// <param name="memberCol">列表达式。</param>
        /// <param name="name">列名。</param>
        /// <param name="configCol">配置列。</param>
        /// <returns>配置表。</returns>
        IConfigTable<TTable> Field<TCol>(Expression<Func<TTable, TCol>> memberCol, string name, Action<IConfigCol> configCol = null);
    }

    /// <summary>
    /// 配置列。
    /// </summary>
    public interface IConfigCol
    {
        /// <summary>
        /// 忽略。
        /// </summary>
        void Ignore();

        /// <summary>
        /// 主键。
        /// </summary>
        IConfigCol IsPrimaryKey();

        /// <summary>
        /// 只读。
        /// </summary>
        IConfigCol IsReadOnly();

        /// <summary>
        /// 版本字段。
        /// </summary>
        IConfigCol Version(VersionKind version);
    }
}
