using System.Data;

namespace Inkslab.Linq
{
    /// <summary>
    /// 动态参数。
    /// </summary>
    public sealed class DynamicParameter
    {
        /// <summary> 
        /// 值。
        /// </summary>
        public object Value { get; set; }
        /// <summary> 
        /// 参数方向。
        /// </summary>
        public ParameterDirection Direction { get; set; }
        /// <summary>
        /// 数据库类型。
        /// </summary>
        public DbType? DbType { get; set; }
        /// <summary> 
        /// 大小。
        /// </summary>
        public int? Size { get; set; }
        /// <summary> 
        /// 精度。
        /// </summary>
        public byte? Precision { get; set; }
        /// <summary> 
        /// 小数位数。
        /// </summary>
        public byte? Scale { get; set; }
    }
}