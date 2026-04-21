using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Inkslab.Linq.Annotations;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Options;

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

            var tableAttr =
                tableType.GetCustomAttribute<TableAttribute>(false)
                ?? new TableAttribute(tableType.Name);

            var propertyInfos = Array.FindAll(
                tableType.GetProperties(),
                x => x.CanRead && x.CanWrite
            );

            Dictionary<string, Column> cloumns = new Dictionary<string, Column>(
                propertyInfos.Length
            );

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                // 一次性取出该属性上的所有特性，避免反复触发反射扫描。
                var attributes = propertyInfo.GetCustomAttributes(true);

                bool isPrimaryKey = false;
                bool isReadOnly = false;
                VersionAttribute versionAttr = null;
                FieldAttribute nameAttr = null;

                for (int i = 0; i < attributes.Length; i++)
                {
                    switch (attributes[i])
                    {
                        case KeyAttribute:
                            isPrimaryKey = true;
                            break;
                        case DatabaseGeneratedAttribute:
                            isReadOnly = true;
                            break;
                        case VersionAttribute v:
                            versionAttr = v;
                            break;
                        case FieldAttribute f:
                            nameAttr = f;
                            break;
                    }
                }

                VersionKind version = VersionKind.None;

                if (versionAttr != null)
                {
                    var propertyType = propertyInfo.PropertyType;

                    if (propertyType == typeof(int))
                    {
                        version = VersionKind.Increment;
                    }
                    else if (propertyType == typeof(long))
                    {
                        version = VersionKind.Ticks;
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        version = VersionKind.Now;
                    }
                    else if (propertyType == typeof(double))
                    {
                        version = VersionKind.Timestamp;
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"不支持“{propertyType}”类型属性的版本控制！"
                        );
                    }
                }

                var key = propertyInfo.Name;

                cloumns.Add(
                    key,
                    new Column(nameAttr is null ? key : nameAttr.Name)
                    {
                        Key = isPrimaryKey,
                        ReadOnly = isReadOnly,
                        Version = version
                    }
                );
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
