using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Collections;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Linq
{
    public partial class DatabaseExecutor
    {
        private class MapAdapter
        {
            private static readonly MethodInfo _equals;
            private static readonly MethodInfo _typeCode;
            private static readonly MethodInfo _objectToString;
            private static readonly MethodInfo _enumParse;
            private static readonly MethodInfo _stringToChar;
            private static readonly ConstructorInfo _errorCtor;
            private static readonly ConstructorInfo _errorOutOfRangeCtor;
            private static readonly ConstructorInfo _invalidCastCtor;
            private static readonly MethodInfo _concatArray;
            private static readonly PropertyInfo _typeName;
            private static readonly PropertyInfo _exceptionMessage;
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

            static MapAdapter()
            {
                _errorCtor = typeof(NotSupportedException).GetConstructor(new[] { Types.String });

                _errorOutOfRangeCtor = typeof(IndexOutOfRangeException).GetConstructor(new[] { Types.String });

                _equals = typeof(MapAdapter).GetMethod(
                    nameof(Equals),
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                );

                _objectToString = Types.Object.GetMethod(nameof(object.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

                _enumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Type), Types.String, typeof(bool) }, null);

                _stringToChar = Types.String.GetMethod("get_Chars", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new[] { Types.Int32 }, null);

                var type = typeof(Type);

                _typeCode = type.GetMethod(nameof(Type.GetTypeCode), BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new[] { type }, null);

                _invalidCastCtor = typeof(InvalidCastException).GetConstructor(new[] { Types.String, typeof(Exception) });

                _concatArray = Types.String.GetMethod(nameof(string.Concat), new[] { typeof(string[]) });

                _typeName = typeof(Type).GetProperty(nameof(Type.Name));

                _exceptionMessage = typeof(Exception).GetProperty(nameof(Exception.Message));
            }

            /// <summary>
            /// 编译好的 Mapper 工厂委托缓存（消除反射开销）。
            /// Key: 实体类型，Value: (MapAdapter) => IDbMapper 工厂委托。
            /// </summary>
            private static readonly ConcurrentDictionary<Type, Func<MapAdapter, IDbMapper>> _mapperFactories =
                new ConcurrentDictionary<Type, Func<MapAdapter, IDbMapper>>();

            /// <summary>
            /// 为指定类型编译 Mapper 工厂方法（使用表达式树，避免运行时反射）。
            /// </summary>
            private static Func<MapAdapter, IDbMapper> CompileMapperFactory(Type entityType)
            {
                // 构建表达式树: adapter => new DbMapperGen<T>(adapter).CreateMap()
                var adapterParam = Parameter(typeof(MapAdapter), "adapter");

                // DbMapperGen<T>
                var genType = typeof(DbMapperGen<>).MakeGenericType(entityType);

                // new DbMapperGen<T>(adapter)
                var genCtor = genType.GetConstructor(new[] { typeof(MapAdapter) });
                var newGen = New(genCtor, adapterParam);

                // .CreateMap()
                var createMapMethod = genType.GetMethod("CreateMap");
                var callCreateMap = Call(newGen, createMapMethod);

                // 转换为 IDbMapper
                var converted = Expression.Convert(callCreateMap, typeof(IDbMapper));

                // 编译成委托
                var lambda = Lambda<Func<MapAdapter, IDbMapper>>(converted, adapterParam);
                return lambda.Compile();
            }

            private readonly Lfu<Type, IDbMapper> _mappers;

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

            public MapAdapter(Type type, int capacity)
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

                // 初始化 Lfu 缓存（使用编译的委托工厂，避免运行时反射）
                _mappers = new Lfu<Type, IDbMapper>(capacity, entityType =>
                {
                    // 获取或编译工厂委托（首次编译后缓存）
                    var factory = _mapperFactories.GetOrAdd(entityType, CompileMapperFactory);

                    // 调用委托创建 Mapper（无反射开销）
                    return factory(this);
                });
            }

            public static MethodInfo IgnoreCaseEquals => _equals;
            public static ConstructorInfo InvalidCastWithInnerCtor => _invalidCastCtor;
            public static MethodInfo ConcatArray => _concatArray;
            public static PropertyInfo TypeName => _typeName;
            public static PropertyInfo ExceptionMessage => _exceptionMessage;

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

                //? 布尔：字段为 bool 时直接读取；字段为整型时仅 0/1 合法（0→false、1→true），
                //? 其它值按“数值越界”抛出，避免被读取器的 InvalidCastException 伪装成“类型不匹配”。
                if (propertyType == Types.Boolean)
                {
                    var typeArg = Variable(typeof(Type));

                    return Block(new[] { typeArg },
                        Assign(typeArg, Call(dbVar, _getFieldType, iVar)),
                        Condition(Equal(typeArg, Constant(Types.Boolean)),
                            Call(dbVar, originalFn, iVar),
                            ToSolveBoolByTransform(dbVar, iVar, typeArg)
                        )
                    );
                }

                //? 字符/字符串，或登记了类型转换表的数值类型：先比对数据库字段类型，
                //? 与属性类型一致时直接读取；不一致时走类型转换（不丢失精度的小转大、任意类型 ToString 等）。
                if (propertyType == Types.Char || propertyType == Types.String || _typeTransforms.ContainsKey(propertyType))
                {
                    var typeArg = Variable(typeof(Type));

                    return Block(new[] { typeArg },
                        Assign(typeArg, Call(dbVar, _getFieldType, iVar)),
                        Condition(Equal(typeArg, Constant(propertyType)),
                            Call(dbVar, originalFn, iVar),
                            ToSolveByTransform(propertyType, dbVar, iVar, typeArg, originalFn)
                        )
                    );
                }

                return Call(dbVar, originalFn, iVar);
            }

            /// <summary>
            /// 枚举映射：同时支持“数字来源”与“字符串来源”。
            /// 数据库字段为字符串时，使用 <see cref="Enum.Parse(Type, string, bool)"/>（忽略大小写，同时兼容“名称”与“数字文本”）；
            /// 否则按枚举底层数值类型读取（复用 <see cref="ToSolve"/> 的无损小转大能力）后转换为枚举。
            /// </summary>
            /// <param name="enumType">枚举类型。</param>
            /// <param name="underlyingType">枚举的底层数值类型。</param>
            /// <param name="dbVar">数据读取器变量。</param>
            /// <param name="iVar">列序号表达式。</param>
            public Expression ToSolveEnum(Type enumType, Type underlyingType, ParameterExpression dbVar, Expression iVar)
            {
                //? 数字（或数字底层字段）→ 枚举：读取底层数值后转换为枚举。
                Expression numeric = Expression.Convert(ToSolve(underlyingType, dbVar, iVar), enumType);

                if (!_typeMap.TryGetValue(Types.String, out var getStringFn))
                {
                    return numeric;
                }

                //? 字符串 → 枚举：Enum.Parse 同时支持“名称”和“数字字符串”，忽略大小写。
                var typeArg = Variable(typeof(Type));

                Expression parse = Expression.Convert(
                    Call(_enumParse, Constant(enumType), Call(dbVar, getStringFn, iVar), Constant(true)),
                    enumType
                );

                return Block(enumType, new[] { typeArg },
                    Assign(typeArg, Call(dbVar, _getFieldType, iVar)),
                    Condition(Equal(typeArg, Constant(Types.String)), parse, numeric)
                );
            }

            /// <summary>
            /// 构造与 <see cref="DbMapperGen{T}"/> 列映射一致格式的异常消息：
            /// “映射失败，列 {列名}({字段类型}) → 属性({属性类型}){后缀}”。
            /// </summary>
            private Expression MappingErrorMessage(Type propertyType, ParameterExpression dbVar, Expression iVar, ParameterExpression typeArg, string suffix)
            {
                return Call(_concatArray,
                    NewArrayInit(Types.String,
                        Constant("映射失败，列 "),
                        GetName(dbVar, iVar),
                        Constant("("),
                        Property(typeArg, _typeName),
                        Constant(") → 属性("),
                        Constant(propertyType.Name),
                        Constant(")" + suffix)
                    )
                );
            }

            private Expression ToSolveByTransform(Type propertyType, ParameterExpression dbVar, Expression iVar, ParameterExpression typeArg, MethodInfo originalFn = null)
            {
                var valueVar = Variable(propertyType);

                var variables = new List<ParameterExpression>(1)
                {
                    valueVar
                };

                var throwUnary = Throw(New(_errorCtor, MappingErrorMessage(propertyType, dbVar, iVar, typeArg, " 类型不被支持！")));

                if (propertyType == Types.Object)
                {
                    return Block(propertyType, variables, throwUnary, valueVar);
                }

                var expressions = new List<Expression>(2);

                if (!propertyType.IsValueType || !_typeTransforms.TryGetValue(propertyType, out var transforms))
                {
                    switch (Type.GetTypeCode(propertyType))
                    {
                        case TypeCode.String:

                            //? 任意数据库字段类型 → 字符串：读取原始值并调用 ToString()（此处值已确保非 DBNull）。
                            expressions.Add(Assign(valueVar, Call(Call(dbVar, _getValue, iVar), _objectToString)));

                            break;
                        case TypeCode.Char when _typeMap.TryGetValue(Types.String, out var transformFn):

                            var stringVar = Variable(Types.String);

                            expressions.Add(IfThenElse(Equal(typeArg, Constant(Types.String)),
                                Block(new ParameterExpression[1] { stringVar },
                                    Assign(stringVar, Call(dbVar, transformFn, iVar)),
                                    IfThenElse(Equal(Property(stringVar, "Length"), Constant(1)),
                                        Assign(valueVar, Call(stringVar, _stringToChar, Constant(0))),
                                        Throw(New(_errorOutOfRangeCtor, MappingErrorMessage(propertyType, dbVar, iVar, typeArg, " 值超出范围！")))
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

                var throwOutUnary = Throw(New(_errorOutOfRangeCtor, MappingErrorMessage(propertyType, dbVar, iVar, typeArg, " 值超出范围！")));

                foreach (var (key, typeCode) in transforms)
                {
                    if (!_typeMap.TryGetValue(key, out var transformFn))
                    {
                        continue;
                    }

                    bool isGreaterThan = code >= typeCode;

                    bool unsignedTypeCode = typeCode is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

                    //? TypeCode 序值（Single 13 < Double 14 < Decimal 15）与浮点/定点类型的取值范围序不一致：
                    //? decimal 的取值范围被 float/double 完整覆盖（仅丢失精度，不会数值溢出），属于安全的小转大，应直接转换。
                    //? 否则会进入下方区间校验，计算 (decimal)double.MaxValue 时抛出 OverflowException。
                    bool floatingWidening = (code is TypeCode.Single or TypeCode.Double) && typeCode == TypeCode.Decimal;

                    if (floatingWidening || (isGreaterThan && (!unsignedCode || unsignedTypeCode)))
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

                //? 字段类型未登记在转换表中：若属性类型自身可被读取器直接读取（originalFn 非空），
                //? 则回退为直接读取（与历史行为一致，由读取器自行决定成功或抛 InvalidCastException）；
                //? 否则抛出友好的不支持异常。
                Expression defaultBody = originalFn is null
                    ? (Expression)throwUnary
                    : Block(typeof(void), Assign(valueVar, Call(dbVar, originalFn, iVar)));

                expressions.Add(Switch(
                    typeof(void),
                    Call(_typeCode, typeArg),
                    defaultBody,
                    null,
                    switchCases.ToArray()
                ));

                expressions.Add(valueVar);

                return Block(propertyType, variables, expressions);
            }

            //? 可作为布尔来源的整型字段类型（按底层数值读取后再判定 0/1）。
            private static readonly Type[] _boolSourceTypes =
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(int), typeof(uint), typeof(long), typeof(ulong)
            };

            /// <summary>
            /// 非布尔字段 → 布尔属性：整型按底层数值读取，文本/字符按字面量读取；
            /// 值为 0 或 "0"/'0' 映射为 <see langword="false"/>、1 或 "1"/'1' 映射为 <see langword="true"/>；
            /// 其它值抛“数值越界”，无法识别的字段类型抛“类型不被支持”。
            /// </summary>
            private Expression ToSolveBoolByTransform(ParameterExpression dbVar, Expression iVar, ParameterExpression typeArg)
            {
                var valueVar = Variable(Types.Boolean);

                var throwUnsupported = Throw(New(_errorCtor, MappingErrorMessage(Types.Boolean, dbVar, iVar, typeArg, " 类型不被支持！")));

                var switchCases = new List<SwitchCase>(_boolSourceTypes.Length + 2);

                Expression ThrowOutOfRange() =>
                    Throw(New(_errorOutOfRangeCtor, MappingErrorMessage(Types.Boolean, dbVar, iVar, typeArg, " 值超出范围！")));

                //? 真假取值映射：等于 falseValue → false、等于 trueValue → true，否则数值越界。
                Expression MapTrueFalse(ParameterExpression sourceVar, MethodInfo readFn, Expression falseValue, Expression trueValue) =>
                    Block(new[] { sourceVar },
                        Assign(sourceVar, Call(dbVar, readFn, iVar)),
                        IfThenElse(Equal(sourceVar, falseValue),
                            Assign(valueVar, Constant(false)),
                            IfThenElse(Equal(sourceVar, trueValue),
                                Assign(valueVar, Constant(true)),
                                ThrowOutOfRange()))
                    );

                foreach (var key in _boolSourceTypes)
                {
                    if (!_typeMap.TryGetValue(key, out var transformFn))
                    {
                        continue;
                    }

                    switchCases.Add(SwitchCase(
                        MapTrueFalse(Variable(key), transformFn, Constant(System.Convert.ChangeType(0, key), key), Constant(System.Convert.ChangeType(1, key), key)),
                        Constant(Type.GetTypeCode(key))
                    ));
                }

                //? 文本来源："0" → false、"1" → true。
                if (_typeMap.TryGetValue(Types.String, out var getStringFn))
                {
                    switchCases.Add(SwitchCase(
                        MapTrueFalse(Variable(Types.String), getStringFn, Constant("0"), Constant("1")),
                        Constant(TypeCode.String)
                    ));
                }

                //? 字符来源：'0' → false、'1' → true。
                if (_typeMap.TryGetValue(Types.Char, out var getCharFn))
                {
                    switchCases.Add(SwitchCase(
                        MapTrueFalse(Variable(Types.Char), getCharFn, Constant('0'), Constant('1')),
                        Constant(TypeCode.Char)
                    ));
                }

                return Block(Types.Boolean, new[] { valueVar },
                    Switch(typeof(void), Call(_typeCode, typeArg), throwUnsupported, null, switchCases.ToArray()),
                    valueVar
                );
            }

            public Expression IsDbNull(ParameterExpression dbVar, Expression iVar) =>
                Call(dbVar, _isDbNull, iVar);

            public Expression GetName(ParameterExpression dbVar, Expression iVar) =>
                Call(dbVar, _getName, iVar);

            public Expression GetFieldType(ParameterExpression dbVar, Expression iVar) =>
                Call(dbVar, _getFieldType, iVar);

            public DbMapper<T> CreateMap<T>() => (DbMapper<T>)_mappers.Get(typeof(T));
        }

        #region ORM适配器

        private class DbMapperGen<T>
        {
            private readonly MapAdapter _adapter;

            public DbMapperGen(MapAdapter adapter)
            {
                _adapter = adapter;
            }

            private Func<DbDataReader, T> MakeSimple(Type type)
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var iVar = Constant(0);

                var dbVar = _adapter.DbVariable();

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new[] { dbVar },
                        Assign(dbVar, _adapter.Convert(paramterExp)),
                        Condition(_adapter.IsDbNull(dbVar, iVar), Default(type), bodyExp)
                    ),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeSimpleNull(Type type, Type nullableType)
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var iVar = Constant(0);

                var dbVar = _adapter.DbVariable();

                var nullableCtor = nullableType.GetConstructor(new[] { type });

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new[] { dbVar },
                        Assign(dbVar, _adapter.Convert(paramterExp)),
                        Condition(
                            _adapter.IsDbNull(dbVar, iVar),
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

                var dbVar = _adapter.DbVariable();

                var iVar = Parameter(typeof(int));

                var lenVar = Property(dbVar, "FieldCount");

                var list = new List<Expression>
                {
                    Assign(iVar, Constant(0)),
                    Assign(dbVar, _adapter.Convert(paramterExp)),
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
                    _adapter.GetName(dbVar, iVar),
                    null,
                    MapAdapter.IgnoreCaseEquals,
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

                var dbVar = _adapter.DbVariable();

                var parameterInfos = constructorInfo.GetParameters();

                var arguments = new List<Expression>(parameterInfos.Length);

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterInfo = parameterInfos[i];

                    var iVar = Constant(i);

                    var uncheckedValue = UncheckedValue(parameterInfo.ParameterType, dbVar, iVar);

                    arguments.Add(
                        Condition(
                            _adapter.IsDbNull(dbVar, iVar),
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
                        Assign(dbVar, _adapter.Convert(paramterExp)),
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

                Expression body = isEnum
                    ? _adapter.ToSolveEnum(nonullableType, propertyType, dbVar, iVar)
                    : _adapter.ToSolve(propertyType, dbVar, iVar);

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
                var propType = propertyItem.PropertyType;

                Expression body = UncheckedValue(propType, dbVar, iVar);

                var exVar = Parameter(typeof(InvalidCastException), "ex");
                var messageExpr = Call(MapAdapter.ConcatArray,
                    NewArrayInit(Types.String,
                        Constant("映射失败，列 "),
                        _adapter.GetName(dbVar, iVar),
                        Constant("("),
                        Property(_adapter.GetFieldType(dbVar, iVar), MapAdapter.TypeName),
                        Constant(") → 属性 "),
                        Constant(propertyItem.Name),
                        Constant("("),
                        Constant(propType.Name),
                        Constant(") 类型不匹配！")
                    )
                );
                var wrappedBody = TryCatch(
                    body,
                    Catch(exVar,
                        Throw(New(MapAdapter.InvalidCastWithInnerCtor, messageExpr, exVar), propType)
                    )
                );

                var testValues = new List<Expression>(1) { Constant(propertyItem.Name) };

                return SwitchCase(
                    IfThen(
                        Not(_adapter.IsDbNull(dbVar, iVar)),
                        Assign(Property(instanceExp, propertyItem), wrappedBody)
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