using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Linq
{
    /// <summary>
    /// 适配器格式化基类。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public abstract class AdapterFormatter<T> where T : AdapterFormatter<T>, IFormatter
    {
        /// <summary>
        /// MVC。
        /// </summary>
        private class Adapter : IComparable<Adapter>
        {
            private readonly int _argumentsCount;

            public Adapter(int argumentsCount)
            {
                _argumentsCount = argumentsCount;
            }

            /// <summary>
            /// 能否解决。
            /// </summary>
            public Func<Match, bool> CanConvert { get; set; }

            /// <summary>
            /// 解决方案。
            /// </summary>
            public Func<T, Match, string> Convert { get; set; }

            public int CompareTo(Adapter other)
            {
                if (other is null)
                {
                    return -1;
                }

                return _argumentsCount.CompareTo(other._argumentsCount);
            }
        }

        private static readonly SortedSet<Adapter> _adapterCachings = new SortedSet<Adapter>();

        private static MethodInfo GetMethodInfo(Func<Match, string, Group> func) => func.Method;
        private static MethodInfo GetMethodInfo(Func<Group, bool> func) => func.Method;

        /// <summary>
        /// 获取分组信息。
        /// </summary>
        /// <param name="match">匹配项。</param>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        private static Group GetGroup(Match match, string name) => match.Groups[name];

        /// <summary>
        /// 判断匹配的长度是否唯一。
        /// </summary>
        /// <param name="group">分组。</param>
        /// <returns></returns>
        private static bool CheckGroup(Group group) => group.Success && group.Captures.Count == 1;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AdapterFormatter()
        {
            var contextType = typeof(T);

            var matchType = typeof(Match);
            var groupType = typeof(Group);

            var contextArg = Parameter(contextType, "context");
            var parameterArg = Parameter(matchType, "item");

            var getGroupFn = GetMethodInfo(GetGroup);

            var checkGroupFn = GetMethodInfo(CheckGroup);

            foreach (var methodInfo in contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (methodInfo.ReturnType != Types.String)
                {
                    continue;
                }

                var parameterInfos = methodInfo.GetParameters();

                if (parameterInfos.Length == 0)
                {
                    continue;
                }

                if (!Array.TrueForAll(parameterInfos, y => y.ParameterType == matchType || y.ParameterType == groupType || y.ParameterType == Types.String || y.ParameterType == Types.Boolean))
                {
                    throw new NotSupportedException("仅支持类型System.Text.RegularExpressions.Match、System.Text.RegularExpressions.Group、System.String、System.Boolean类型的映射。");
                }

                var conditions = new List<Expression>(parameterInfos.Length);
                var variables = new List<ParameterExpression>(parameterInfos.Length);
                var arguments = new List<Expression>(parameterInfos.Length);
                var expressions = new List<Expression>(parameterInfos.Length);

                Array.ForEach(parameterInfos, y =>
                {
                    if (y.ParameterType == matchType)
                    {
                        arguments.Add(parameterArg);
                    }
                    else
                    {
                        var groupExp = Variable(groupType, y.Name);

                        variables.Add(groupExp);

                        expressions.Add(Assign(groupExp, Call(null, getGroupFn, parameterArg, Constant(y.Name))));

                        if (y.ParameterType == groupType)
                        {
                            conditions.Add(Property(groupExp, "Success"));

                            arguments.Add(groupExp);
                        }
                        else
                        {
                            if (y.ParameterType == Types.Boolean)
                            {
                                arguments.Add(Property(groupExp, "Success"));
                            }
                            else
                            {
                                arguments.Add(Property(groupExp, "Value"));

                                conditions.Add(Call(null, checkGroupFn, groupExp));
                            }
                        }
                    }
                });

                var adapter = new Adapter(parameterInfos.Length);

                var enumerator = conditions.GetEnumerator();

                if (enumerator.MoveNext())
                {
                    var condition = enumerator.Current;

                    while (enumerator.MoveNext())
                    {
                        condition = AndAlso(condition, enumerator.Current);
                    }

                    var invoke = Lambda<Func<Match, bool>>(Block(variables, expressions.Concat(new Expression[1] { condition })), parameterArg);

                    adapter.CanConvert = invoke.Compile();
                }

                expressions.Add(Call(contextArg, methodInfo, arguments));

                var lamdaExp = Lambda<Func<T, Match, string>>(Block(variables, expressions), contextArg, parameterArg);

                adapter.Convert = lamdaExp.Compile();

                _adapterCachings.Add(adapter);
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="pattern">正则。</param>
        public AdapterFormatter(string pattern) : this(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="regex">正则。</param>
        public AdapterFormatter(Regex regex) => RegularExpression = regex;

        /// <summary>
        /// 未能解决时，抛出异常。（默认：true）。
        /// </summary>
        public bool UnsolvedThrowError { get; set; } = true;

        /// <summary>
        /// 表达式。
        /// </summary>
        public Regex RegularExpression { get; }

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="match">匹配到的内容。</param>
        /// <returns></returns>
        public string Evaluator(Match match)
        {
            foreach (Adapter mvc in _adapterCachings)
            {
                if (mvc.CanConvert(match))
                {
                    return mvc.Convert((T)this, match);
                }
            }

            if (UnsolvedThrowError)
            {
                throw new NotSupportedException();
            }

            return match.Value;
        }
    }
}
