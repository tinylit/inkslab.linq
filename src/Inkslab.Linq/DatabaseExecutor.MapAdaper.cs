using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Linq
{
    public partial class DatabaseExecutor
    {
        private class MapAdaper
        {
            private const int COLLECT_PER_ITEMS = 1000;

            private static readonly MethodInfo _equals;
            private static readonly MethodInfo _concat;
            private static readonly MethodInfo _typeCode;
            private static readonly MethodInfo _charToString;
            private static readonly MethodInfo _stringToChar;
            private static readonly ConstructorInfo _errorCtor;
            private static readonly ConstructorInfo _errorOutOfRangeCtor;
            private static readonly HashSet<string> _nameHooks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GetBoolean",
                "GetByte",
                "GetByteArray",
                "GetChar",
                "GetDateOnly",
                "GetDateTime",
                "GetDateTimeOffset",
                "GetDecimal",
                "GetDouble",
                "GetDoubleArray",
                "GetFloat",
                "GetFloatArray",
                "GetGuid",
                "GetInt16",
                "GetInt16Array",
                "GetInt32",
                "GetInt64",
                "GetSByte",
                "GetStream",
                "GetString",
                "GetTextReader",
                "GetTimeOnly",
                "GetTimeSpan",
                "GetUInt16",
                "GetUInt32",
                "GetUInt64",
                // ----------- MySQL ------------
                "GetMySqlDateTime",
                "GetMySqlDecimal",
                "GetMySqlGeometry",
                // ----------- SqlServer ------------
                "GetSqlBinary",
                "GetSqlBoolean",
                "GetSqlByte",
                "GetSqlBytes",
                "GetSqlChars",
                "GetSqlDateTime",
                "GetSqlDecimal",
                "GetSqlDouble",
                "GetSqlGuid",
                "GetSqlInt16",
                "GetSqlInt32",
                "GetSqlInt64",
                "GetSqlMoney",
                "GetSqlSingle",
                "GetSqlString",
                // ----------- Oracle ------------
                "GetOracleBFile",
                "GetOracleBinary",
                "GetOracleBlob",
                "GetOracleBoolean",
                "GetOracleClob",
                "GetOracleDate",
                "GetOracleDecimal",
                "GetOracleIntervalDS",
                "GetOracleIntervalYM",
                "GetOracleRef",
                "GetOracleRefCursor",
                "GetOracleString",
                "GetOracleTimeStamp",
                "GetOracleTimeStampLTZ",
                "GetOracleTimeStampTZ"
                // ----------- Npgsql ------------
            };
            private static readonly Dictionary<Type, Dictionary<Type, TypeCode>> _typeTransforms = new Dictionary<Type, Dictionary<Type, TypeCode>>
            {
                [typeof(sbyte)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(byte)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(short)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(ushort)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(int)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(uint)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(long)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(ulong)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64
                },
                [typeof(float)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64,
                    [typeof(double)] = TypeCode.Double,
                    [typeof(decimal)] = TypeCode.Decimal
                },
                [typeof(double)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64,
                    [typeof(float)] = TypeCode.Single,
                    [typeof(decimal)] = TypeCode.Decimal
                },
                [typeof(decimal)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64,
                    [typeof(float)] = TypeCode.Single,
                    [typeof(double)] = TypeCode.Double
                }
            };

            static MapAdaper()
            {
                _errorCtor = typeof(NotSupportedException).GetConstructor(new[] { Types.String });

                _errorOutOfRangeCtor = typeof(IndexOutOfRangeException).GetConstructor(new[] { Types.String });

                _equals = typeof(MapAdaper).GetMethod(
                    nameof(Equals),
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                );

                _concat = Types.String.GetMethod(
                    nameof(string.Concat),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly,
                    null,
                    new[] { Types.String, Types.String, Types.String },
                    null);

                _charToString = Types.Char.GetMethod(nameof(char.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

                _stringToChar = Types.String.GetMethod("get_Chars", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new[] { Types.Int32 }, null);

                var type = typeof(Type);

                _typeCode = type.GetMethod(nameof(Type.GetTypeCode), BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new[] { type }, null);
            }

            private int _refCount;

            private volatile bool _recovering;

            private readonly ConcurrentDictionary<Type, IDbMapper> _mappers =
                new ConcurrentDictionary<Type, IDbMapper>(100, 2 * COLLECT_PER_ITEMS);

            private readonly Type _type;
            private readonly MethodInfo _isDbNull;
            private readonly MethodInfo _getName;
            private readonly MethodInfo _getValue;
            private readonly MethodInfo _getFieldType;
            private readonly Dictionary<Type, MethodInfo> _typeMap = new Dictionary<Type, MethodInfo>(16);

            private static bool Equals(string a, string b)
            {
                return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            }

            public MapAdaper(Type type)
            {
                var types = new[] { Types.Int32 };

                _type = type;

                _getName = type.GetMethod("GetName", types);

                _getValue = type.GetMethod("GetValue", types);

                _isDbNull = type.GetMethod("IsDBNull", types);

                _getFieldType = type.GetMethod("GetFieldType", types);

                foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!_nameHooks.Contains(methodInfo.Name))
                    {
                        continue;
                    }

                    var parameterInfos = methodInfo.GetParameters();

                    if (parameterInfos.Length != 1)
                    {
                        continue;
                    }

                    var parameterInfo = parameterInfos[0];

                    if (parameterInfo.ParameterType == Types.Int32)
                    {
                        _typeMap.TryAdd(methodInfo.ReturnType, methodInfo);
                    }
                }
            }

            public static MethodInfo IgnoreCaseEquals => _equals;

            public ParameterExpression DbVariable() => Parameter(_type);

            public UnaryExpression Convert(ParameterExpression parameterExp) =>
                Expression.Convert(parameterExp, _type);

            public Expression ToSolve(Type propertyType, ParameterExpression dbVar, Expression iVar)
            {
                if (propertyType.FullName == LinqBinary)
                {
                    return New(propertyType.GetConstructor(new[] { Types.Object }), Call(dbVar, _getValue, iVar));
                }

                if (propertyType.FullName is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray")
                {
                    var jsonVar = Variable(Types.String);

                    return Block(propertyType, new[] { jsonVar },
                        Assign(jsonVar, Call(dbVar, _typeMap[Types.String], iVar)),
                        Condition(Equal(jsonVar, Constant(null, Types.String)),
                            Constant(null, propertyType),
                            Call(propertyType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new[] { Types.String }, null), jsonVar)
                        )
                    );
                }

                if (propertyType == Types.JsonPayload || propertyType == Types.JsonbPayload)
                {
                    var jsonVar = Variable(Types.String);

                    return Block(propertyType, new[] { jsonVar },
                        Assign(jsonVar, Call(dbVar, _typeMap[Types.String], iVar)),
                        Condition(Equal(jsonVar, Constant(null, Types.String)),
                            Constant(null, propertyType),
                            New(propertyType.GetConstructor(new[] { Types.String }), jsonVar)
                        )
                    );
                }

                if (!_typeMap.TryGetValue(propertyType, out MethodInfo originalFn))
                {
                    var typeArg = Variable(typeof(Type));

                    return Block(new[] { typeArg },
                        Assign(typeArg, Call(dbVar, _getFieldType, iVar)),
                        ToSolveByTransform(propertyType, dbVar, iVar, typeArg)
                    );
                }

                if (propertyType == Types.Object)
                {
                    return Call(dbVar, originalFn, iVar);
                }

                if (propertyType == Types.Char || propertyType == Types.String)
                {
                    var typeArg = Variable(typeof(Type));

                    return Block(new[] { typeArg },
                        Assign(typeArg, Call(dbVar, _getFieldType, iVar)),
                        Condition(Equal(typeArg, Constant(propertyType)),
                            Call(dbVar, originalFn, iVar),
                            ToSolveByTransform(propertyType, dbVar, iVar, typeArg)
                        )
                    );
                }

                return Call(dbVar, originalFn, iVar);
            }

            private Expression ToSolveByTransform(Type propertyType, ParameterExpression dbVar, Expression iVar, ParameterExpression typeArg)
            {
                var valueVar = Variable(propertyType);

                var variables = new List<ParameterExpression>(1)
                {
                    valueVar
                };

                var throwUnary = Throw(New(_errorCtor, Call(_concat, Constant("数据库字段“"), GetName(dbVar, iVar), Constant("”的类型和实体属性的类型映射不被支持，请检查映射实体的属性类型！"))));

                if (propertyType == Types.Object)
                {
                    return Block(propertyType, variables, throwUnary, valueVar);
                }

                var expressions = new List<Expression>(2);

                if (!propertyType.IsValueType || !_typeTransforms.TryGetValue(propertyType, out var transforms))
                {
                    switch (Type.GetTypeCode(propertyType))
                    {
                        case TypeCode.String when _typeMap.TryGetValue(Types.Char, out var transformFn):

                            expressions.Add(IfThenElse(Equal(typeArg, Constant(Types.Char)), Assign(valueVar, Call(Call(dbVar, transformFn, iVar), _charToString)), throwUnary));

                            break;
                        case TypeCode.Char when _typeMap.TryGetValue(Types.String, out var transformFn):

                            var stringVar = Variable(Types.String);

                            expressions.Add(IfThenElse(Equal(typeArg, Constant(Types.String)),
                                Block(new ParameterExpression[1] { stringVar },
                                    Assign(stringVar, Call(dbVar, transformFn, iVar)),
                                    IfThenElse(Equal(Property(stringVar, "Length"), Constant(1)),
                                        Assign(valueVar, Call(stringVar, _stringToChar, Constant(0))),
                                        Throw(New(_errorOutOfRangeCtor, Call(_concat, Constant("数据库字段“"), GetName(dbVar, iVar), Constant("”的值超出了实体属性的类型容值范围，请检查映射实体的属性类型！"))))
                                    )
                                ),
                                throwUnary)
                            );

                            break;
                        default:

                            expressions.Add(throwUnary);

                            break;
                    }

                    expressions.Add(valueVar);

                    return Block(propertyType, variables, expressions);
                }

                var switchCases = new List<SwitchCase>(transforms.Count);

                var code = Type.GetTypeCode(propertyType);

                bool unsignedCode = code is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

                var throwOutUnary = Throw(New(_errorOutOfRangeCtor, Call(_concat, Constant("数据库字段“"), GetName(dbVar, iVar), Constant("”的值超出了实体属性的类型容值范围，请检查映射实体的属性类型！"))));

                foreach (var (key, typeCode) in transforms)
                {
                    if (!_typeMap.TryGetValue(key, out var transformFn))
                    {
                        continue;
                    }

                    bool isGreaterThan = code >= typeCode;

                    bool unsignedTypeCode = typeCode is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

                    if (isGreaterThan && (!unsignedCode || unsignedTypeCode))
                    {
                        switchCases.Add(SwitchCase(Assign(valueVar, Expression.Convert(Call(dbVar, transformFn, iVar), propertyType)), Constant(typeCode)));

                        continue;
                    }

                    BinaryExpression test;

                    var transformVar = Variable(key);

                    variables.Add(transformVar);

                    if (!unsignedCode || unsignedTypeCode) //? 原类型有符号，数据类型无符号，或者都是无符号的。 且数据类型一定大于源类型。
                    {
                        if (unsignedTypeCode) //? 源类型有符号，数据类型无符号。
                        {
                            test = LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key));
                        }
                        else if (unsignedCode) //? 原类型无符号，但数据类型有符号，要判断数据值是否大于等于“0”且小于等于“最大值”。
                        {
                            test = AndAlso(GreaterThanOrEqual(transformVar, Constant(System.Convert.ChangeType(0, key))), LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key)));
                        }
                        else  //? 原类型有符号，但数据类型有符号，要判断数据值是否大于等于“最小值”且小于等于“最大值”。。
                        {
                            test = AndAlso(GreaterThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MinValue"), key)), LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key)));
                        }
                    }
                    else //? 源类型无符号，数据类型有符号。
                    {
                        test = GreaterThanOrEqual(transformVar, Constant(System.Convert.ChangeType(0, key)));

                        if (!isGreaterThan)
                        {
                            test = AndAlso(test, LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key)));
                        }
                    }

                    switchCases.Add(SwitchCase(Block(transformVar,
                            Assign(transformVar, Call(dbVar, transformFn, iVar)),
                            IfThenElse(test,
                                Assign(valueVar, Expression.Convert(transformVar, propertyType)),
                                throwOutUnary)
                            ),
                            Constant(typeCode)
                        )
                    );
                }

                expressions.Add(Switch(
                    typeof(void),
                    Call(_typeCode, typeArg),
                    throwUnary,
                    null,
                    switchCases.ToArray()
                ));

                expressions.Add(valueVar);

                return Block(propertyType, variables, expressions);
            }

            public Expression IsDbNull(ParameterExpression dbVar, Expression iVar) =>
                Call(dbVar, _isDbNull, iVar);

            public Expression GetName(ParameterExpression dbVar, Expression iVar) =>
                Call(dbVar, _getName, iVar);

            public DbMapper<T> CreateMap<T>() =>
                (DbMapper<T>)
                    _mappers.GetOrAdd(
                        typeof(T),
                        type =>
                        {
                            if (Interlocked.Increment(ref _refCount) >= COLLECT_PER_ITEMS)
                            {
                                if (_recovering) { }
                                else
                                {
                                    _recovering = true;

                                    new Timer(
                                        render =>
                                        {
                                            var r = new Random();

                                            var keys = (ICollection<Type>)render;

                                            int offset = keys.Count / 2;

                                            var count = r.Next(offset, keys.Count); //? 随机移除一半以上的数据。

                                            var skipSize = r.Next(0, offset); //? 随机开始移除的位置。

                                            try
                                            {
                                                foreach (
                                                    var key in keys.Skip(skipSize).Take(offset)
                                                )
                                                {
                                                    _mappers.TryRemove(key, out _);
                                                }
                                            }
                                            catch { }
                                            finally
                                            {
                                                _recovering = false;

                                                Interlocked.Exchange(ref _refCount, _mappers.Count);
                                            }
                                        },
                                        _mappers.Keys,
                                        100,
                                        Timeout.Infinite
                                    );
                                }
                            }

                            return new DbMapperGen<T>(this).CreateMap();
                        }
                    );
        }

        #region ORM适配器

        private class DbMapperGen<T>
        {
            private readonly MapAdaper _adaper;

            public DbMapperGen(MapAdaper adaper)
            {
                _adaper = adaper;
            }

            private Func<DbDataReader, T> MakeSimple(Type type)
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var iVar = Constant(0);

                var dbVar = _adaper.DbVariable();

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new[] { dbVar },
                        Assign(dbVar, _adaper.Convert(paramterExp)),
                        Condition(_adaper.IsDbNull(dbVar, iVar), Default(type), bodyExp)
                    ),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeSimpleNull(Type type, Type nullableType)
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var iVar = Constant(0);

                var dbVar = _adaper.DbVariable();

                var nullableCtor = nullableType.GetConstructor(new[] { type });

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new[] { dbVar },
                        Assign(dbVar, _adaper.Convert(paramterExp)),
                        Condition(
                            _adaper.IsDbNull(dbVar, iVar),
                            Default(nullableType),
                            New(nullableCtor, bodyExp)
                        )
                    ),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeNull(Type type, Type nullableType)
            {
                var nullCtor = nullableType.GetConstructor(new[] { type });

                //? 无参构造函数。
                var nonCtor = type.GetConstructor(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if (nonCtor is null)
                {
                    var constructorInfos = type.GetConstructors(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                    );

                    foreach (var constructorInfo in constructorInfos)
                    {
                        return MakeFor(constructorInfo, newExp => New(nullCtor, newExp));
                    }
                }

                return MakeFor(type, nonCtor, instanceExp => New(nullCtor, instanceExp));
            }

            private Func<DbDataReader, T> MakeFor(
                Type type,
                ConstructorInfo constructorInfo,
                Func<ParameterExpression, Expression> convert
            )
            {
                var instanceExp = Variable(type);

                var paramterExp = Parameter(typeof(DbDataReader));

                var dbVar = _adaper.DbVariable();

                var iVar = Parameter(typeof(int));

                var lenVar = Property(dbVar, "FieldCount");

                var list = new List<Expression>
                {
                    Assign(iVar, Constant(0)),
                    Assign(dbVar, _adaper.Convert(paramterExp)),
                    Assign(instanceExp, New(constructorInfo))
                };

                var listCases = new List<SwitchCase>();

                foreach (
                    var propertyInfo in type.GetProperties(
                        BindingFlags.Instance | BindingFlags.Public
                    )
                )
                {
                    if (!propertyInfo.CanWrite)
                    {
                        continue;
                    }

                    if (propertyInfo.IsIgnore())
                    {
                        continue;
                    }

                    listCases.Add(SwitchCaseAssign(instanceExp, propertyInfo, dbVar, iVar));
                }

                LabelTarget break_label = Label(typeof(void));
                LabelTarget continue_label = Label(typeof(void));

                var body = Switch(
                    _adaper.GetName(dbVar, iVar),
                    null,
                    MapAdaper.IgnoreCaseEquals,
                    listCases
                );

                list.Add(
                    Loop(
                        IfThenElse(
                            LessThan(iVar, lenVar),
                            Block(
                                body,
                                AddAssign(iVar, Constant(1)),
                                Continue(continue_label, typeof(void))
                            ),
                            Break(break_label, typeof(void))
                        ),
                        break_label,
                        continue_label
                    )
                );

                list.Add(convert.Invoke(instanceExp));

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(new[] { iVar, dbVar, instanceExp }, list),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeNoArgumentsCtor(
                Type type,
                ConstructorInfo constructorInfo
            ) => MakeFor(type, constructorInfo, instanceExp => instanceExp);

            private Func<DbDataReader, T> MakeFor(
                ConstructorInfo constructorInfo,
                Func<NewExpression, Expression> convert
            )
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var dbVar = _adaper.DbVariable();

                var parameterInfos = constructorInfo.GetParameters();

                var arguments = new List<Expression>(parameterInfos.Length);

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterInfo = parameterInfos[i];

                    var iVar = Constant(i);

                    var uncheckedValue = UncheckedValue(parameterInfo.ParameterType, dbVar, iVar);

                    arguments.Add(
                        Condition(
                            _adaper.IsDbNull(dbVar, iVar),
                            parameterInfo.ParameterType.IsValueType
                                ? Default(parameterInfo.ParameterType)
                                : Constant(null, parameterInfo.ParameterType),
                            uncheckedValue
                        )
                    );
                }

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new[] { dbVar },
                        Assign(dbVar, _adaper.Convert(paramterExp)),
                        convert.Invoke(New(constructorInfo, arguments))
                    ),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeCtor(ConstructorInfo constructorInfo) =>
                MakeFor(constructorInfo, newExp => newExp);

            private Expression UncheckedValue(Type type, ParameterExpression dbVar, Expression iVar)
            {
                bool isEnum = false;
                bool isNullable = false;

                Type propertyType = type;
                Type nonullableType = type;

                if (propertyType.IsValueType)
                {
                    if (propertyType.IsNullable())
                    {
                        isNullable = true;

                        propertyType = nonullableType = Nullable.GetUnderlyingType(propertyType);
                    }

                    if (propertyType.IsEnum)
                    {
                        isEnum = true;

                        propertyType = Enum.GetUnderlyingType(propertyType);
                    }
                }

                Expression body = _adaper.ToSolve(propertyType, dbVar, iVar);

                if (isEnum)
                {
                    body = Convert(body, nonullableType);
                }

                if (isNullable)
                {
                    body = New(type.GetConstructor(new[] { nonullableType }), body);
                }

                return body;
            }

            private SwitchCase SwitchCaseAssign(
                Expression instanceExp,
                PropertyInfo propertyItem,
                ParameterExpression dbVar,
                ParameterExpression iVar
            )
            {
                Expression body = UncheckedValue(propertyItem.PropertyType, dbVar, iVar);

                var testValues = new List<Expression>(1) { Constant(propertyItem.Name) };

                return SwitchCase(
                    IfThen(
                        Not(_adaper.IsDbNull(dbVar, iVar)),
                        Assign(Property(instanceExp, propertyItem), body)
                    ),
                    testValues
                );
            }

            public DbMapper<T> CreateMap()
            {
                var type = typeof(T);

                if (type.IsSimple() || type.FullName == LinqBinary)
                {
                    return new DbMapper<T>(MakeSimple(type), true);
                }

                if (type.IsNullable())
                {
                    var conversionType = Nullable.GetUnderlyingType(type);

                    if (conversionType.IsSimple())
                    {
                        return new DbMapper<T>(MakeSimpleNull(conversionType, type), false);
                    }

                    return new DbMapper<T>(MakeNull(conversionType, type), false);
                }

                //? 无参构造函数。
                var nonCtor = type.GetConstructor(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if (nonCtor is null)
                {
                    var constructorInfos = type.GetConstructors(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                    );

                    foreach (var constructorInfo in constructorInfos)
                    {
                        return new DbMapper<T>(MakeCtor(constructorInfo), false);
                    }
                }

                return new DbMapper<T>(MakeNoArgumentsCtor(type, nonCtor), false);
            }
        }

        private interface IDbMapper
        {
            object Map(DbDataReader reader);

            bool IsInvalid(DbDataReader reader);

            Task<bool> IsInvalidAsync(DbDataReader reader, CancellationToken cancellationToken);
        }

        private class DbMapper<T> : IDbMapper
        {
            private readonly Func<DbDataReader, T> _read;
            private readonly bool _useDefault;

            public DbMapper(Func<DbDataReader, T> read, bool useDefault)
            {
                _read = read;
                _useDefault = useDefault;
            }

            public T Map(DbDataReader reader) => _read.Invoke(reader);

            public bool IsInvalid(DbDataReader reader) => _useDefault && reader.IsDBNull(0);

            public Task<bool> IsInvalidAsync(DbDataReader reader, CancellationToken cancellationToken)
            {
                return _useDefault ? reader.IsDBNullAsync(0, cancellationToken) : Task.FromResult(false);
            }

            object IDbMapper.Map(DbDataReader reader) => _read.Invoke(reader);
        }
        #endregion
    }
}
