using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Service.Data
{
    /// <summary>
    /// 專案內使用的 EF6 bulk helper，避免依賴需授權的第三方 bulk methods。
    /// </summary>
    public static class EntityFrameworkBulkExtensions
    {
        public static void BulkInsert<TEntity>(
            this DbContext context,
            IEnumerable<TEntity> entities,
            Action<BulkOperationOptions> optionsAction = null)
            where TEntity : class
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var rows = (entities ?? Enumerable.Empty<TEntity>()).ToList();
            if (rows.Count == 0)
            {
                return;
            }

            var options = new BulkOperationOptions();
            optionsAction?.Invoke(options);

            if (options.AutoMapOutputDirection)
            {
                context.Set<TEntity>().AddRange(rows);
                context.SaveChanges();
                return;
            }

            if (!(context.Database.Connection is SqlConnection connection))
            {
                context.Set<TEntity>().AddRange(rows);
                context.SaveChanges();
                return;
            }

            var shouldClose = connection.State == ConnectionState.Closed;
            var transaction = context.Database.CurrentTransaction?.UnderlyingTransaction as SqlTransaction;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                var maps = GetInsertPropertyMaps(typeof(TEntity));
                var table = CreateDataTable(rows, maps);

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = GetTableName(typeof(TEntity));
                    bulkCopy.BatchSize = options.BatchSize;
                    bulkCopy.BulkCopyTimeout = context.Database.CommandTimeout ?? options.TimeoutSeconds;

                    foreach (var map in maps)
                    {
                        bulkCopy.ColumnMappings.Add(map.Property.Name, map.ColumnName);
                    }

                    bulkCopy.WriteToServer(table);
                }
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        public static void BulkUpdate<TEntity>(this DbContext context, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var rows = (entities ?? Enumerable.Empty<TEntity>()).ToList();
            if (rows.Count == 0)
            {
                return;
            }

            var set = context.Set<TEntity>();
            foreach (var row in rows)
            {
                var entry = context.Entry(row);
                if (entry.State == EntityState.Detached)
                {
                    set.Attach(row);
                }

                context.Entry(row).State = EntityState.Modified;
            }

            context.SaveChanges();
        }

        public static void BulkDelete<TEntity>(this DbContext context, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var rows = (entities ?? Enumerable.Empty<TEntity>()).ToList();
            if (rows.Count == 0)
            {
                return;
            }

            var set = context.Set<TEntity>();
            foreach (var row in rows)
            {
                var entry = context.Entry(row);
                if (entry.State == EntityState.Detached)
                {
                    set.Attach(row);
                }

                set.Remove(row);
            }

            context.SaveChanges();
        }

        private static DataTable CreateDataTable<TEntity>(IEnumerable<TEntity> rows, List<PropertyMap> maps)
            where TEntity : class
        {
            var table = new DataTable();

            foreach (var map in maps)
            {
                var propertyType = Nullable.GetUnderlyingType(map.Property.PropertyType) ?? map.Property.PropertyType;
                table.Columns.Add(map.Property.Name, propertyType);
            }

            foreach (var row in rows)
            {
                var values = maps
                    .Select(map => map.Property.GetValue(row) ?? DBNull.Value)
                    .ToArray();
                table.Rows.Add(values);
            }

            return table;
        }

        private static List<PropertyMap> GetInsertPropertyMaps(Type entityType)
        {
            return entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead)
                .Where(property => property.GetCustomAttribute<NotMappedAttribute>() == null)
                .Where(property => !IsIdentityColumn(property))
                .Where(property => IsSimpleType(property.PropertyType))
                .Select(property => new PropertyMap
                {
                    Property = property,
                    ColumnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name
                })
                .ToList();
        }

        private static bool IsIdentityColumn(PropertyInfo property)
        {
            var databaseGenerated = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
            return databaseGenerated?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity;
        }

        private static bool IsSimpleType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;
            return actualType.IsPrimitive
                || actualType.IsEnum
                || actualType == typeof(string)
                || actualType == typeof(decimal)
                || actualType == typeof(DateTime)
                || actualType == typeof(Guid)
                || actualType == typeof(byte[]);
        }

        private static string GetTableName(Type entityType)
        {
            var table = entityType.GetCustomAttribute<TableAttribute>();
            var schema = string.IsNullOrWhiteSpace(table?.Schema) ? "dbo" : table.Schema;
            var name = string.IsNullOrWhiteSpace(table?.Name) ? entityType.Name : table.Name;
            return $"[{schema}].[{name}]";
        }

        private sealed class PropertyMap
        {
            public PropertyInfo Property { get; set; }

            public string ColumnName { get; set; }
        }
    }

    public sealed class BulkOperationOptions
    {
        public bool AutoMapOutputDirection { get; set; }

        public int BatchSize { get; set; } = 5000;

        public int TimeoutSeconds { get; set; } = 600;
    }
}
