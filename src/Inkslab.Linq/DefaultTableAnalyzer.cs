﻿using System;
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
                var key = propertyInfo.Name;

                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                bool isPrimaryKey = propertyInfo.IsDefined(typeof(KeyAttribute), true);

                bool isReadOnly = propertyInfo.IsDefined(typeof(DatabaseGeneratedAttribute), true);

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
                        throw new NotSupportedException(
                            $"不支持“{propertyInfo.PropertyType}”类型属性的版本控制！"
                        );
                    }
                }

                var nameAttr = propertyInfo.GetCustomAttribute<FieldAttribute>(true);

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
