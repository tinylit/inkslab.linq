using Inkslab.Linq.Annotations;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Inkslab.Linq
{
    /// <inheritdoc/>
    public class DefaultTableAnalyzer : ITableAnalyzer
    {
        /// <inheritdoc/>
        public TabelOptions Table(Type tableType)
        {
            if (tableType is null)
            {
                throw new ArgumentNullException(nameof(tableType));
            }

            var tableAttr = tableType.GetCustomAttribute<TableAttribute>() ?? new TableAttribute(tableType.Name);

            var propertyInfos = Array.FindAll(tableType.GetProperties(), x => x.CanRead && x.CanWrite);

            Dictionary<string, Column> cloumns = new Dictionary<string, Column>(propertyInfos.Length);

            foreach (var propertyInfo in propertyInfos)
            {
                var key = propertyInfo.Name;

                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                bool isPrimaryKey = propertyInfo.IsDefined(typeof(KeyAttribute), true);

#if NET6_0_OR_GREATER
                if (!isPrimaryKey)
                {
                    isPrimaryKey = propertyInfo.IsDefined(typeof(System.ComponentModel.DataAnnotations.KeyAttribute), true);
                }
#endif
                var readOnlyAttr = propertyInfo.GetCustomAttribute<ReadOnlyAttribute>(true);
                
                bool isReadOnly = readOnlyAttr?.IsReadOnly == true;

                VersionKind version = VersionKind.None;

                if (propertyInfo.IsDefined(typeof(VersionAttribute), true))
                {
                    if (propertyInfo.PropertyType == typeof(int))
                    {
                        version = VersionKind.Increment;
                    }
                    else if (propertyInfo.PropertyType == typeof(long))
                    {
                        version = VersionKind.Ticks;
                    }
                    else if (propertyInfo.PropertyType == typeof(DateTime))
                    {
                        version = VersionKind.Now;
                    }
                    else if (propertyInfo.PropertyType == typeof(double))
                    {
                        version = VersionKind.Timestamp;
                    }
                    else
                    {
                        throw new NotSupportedException($"不支持“{propertyInfo.PropertyType}”类型属性的版本控制！");
                    }
                }

                var nameAttr = propertyInfo.GetCustomAttribute<FieldAttribute>(true);

                cloumns.Add(key, new Column(nameAttr is null ? key : nameAttr.Name)
                {
                    Key = isPrimaryKey,
                    ReadOnly = isReadOnly,
                    Version = version
                });
            }

            return new TabelOptions
            {
                Schema = tableAttr.Schema,
                Name = tableAttr.Name,
                Columns = cloumns
            };
        }
    }
}
