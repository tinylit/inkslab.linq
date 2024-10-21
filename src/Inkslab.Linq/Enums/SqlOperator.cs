namespace Inkslab.Linq.Enums
{
    /// <summary>
    /// SQL 运算符。
    /// </summary>
    public enum SqlOperator
    {
        /// <summary>
        /// a &amp; b
        /// </summary>
        And,
        /// <summary>
        /// a ^ b
        /// </summary>
        ExclusiveOr,
        /// <summary>
        /// a | b
        /// </summary>
        Or,
        /// <summary>
        /// a &lt;&lt; b
        /// </summary>
        LeftShift,
        /// <summary>
        /// a &gt;&gt; b
        /// </summary>
        RightShift,
        /// <summary>
        /// a > b
        /// </summary>
        GreaterThan,
        /// <summary>
        /// a >= b
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// a = b
        /// </summary>
        Equal,
        /// <summary>
        /// a &lt;= b;
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// a &lt; a
        /// </summary>
        LessThan,
        /// <summary>
        /// a &lt;&gt; b
        /// </summary>
        NotEqual,
        /// <summary>
        /// a + b
        /// </summary>
        Add,
        /// <summary>
        /// a - b
        /// </summary>
        Subtract,
        /// <summary>
        /// a * b
        /// </summary>
        Multiply,
        /// <summary>
        /// a / b
        /// </summary>
        Divide,
        /// <summary>
        /// a % b
        /// </summary>
        Modulo,
        /// <summary>
        /// a - 1
        /// </summary>
        Decrement,
        /// <summary>
        /// a + 1
        /// </summary>
        Increment,
        /// <summary>
        /// -a
        /// </summary>
        Negate,
        /// <summary>
        /// +a
        /// </summary>
        UnaryPlus,
        /// <summary>
        /// ~a
        /// </summary>
        OnesComplement,
        /// <summary>
        /// is true
        /// </summary>
        IsTrue,
        /// <summary>
        /// is false
        /// </summary>
        IsFalse
    }
}
