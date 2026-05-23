using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Z.EntityFramework.Plus;

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

            if (context.Database.Connection is SqlConnection connection)
            {
                BulkUpdateWithTempTable(context, connection, rows);
                return;
            }

            UpdateWithChangeTracker(context, rows);
        }

        public static int DeleteByColumnValues<TEntity, TValue>(
            this DbContext context,
            IEnumerable<TValue> values,
            Expression<Func<TEntity, TValue>> columnExpression,
            int batchSize = 1000)
            where TEntity : class
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (columnExpression == null)
            {
                throw new ArgumentNullException(nameof(columnExpression));
            }

            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0.");
            }

            var rows = (values ?? Enumerable.Empty<TValue>())
                .Distinct()
                .ToList();
            if (rows.Count == 0)
            {
                return 0;
            }

            if (!(context.Database.Connection is SqlConnection connection))
            {
                throw new NotSupportedException("DeleteByColumnValues only supports SQL Server connections.");
            }

            var tableName = GetTableName(typeof(TEntity));
            var columnName = QuoteIdentifier(GetColumnName(columnExpression));
            var valueColumnName = "Value";
            var tempTableName = "#DeleteByColumnValues_" + Guid.NewGuid().ToString("N");
            var shouldClose = connection.State == ConnectionState.Closed;
            var transaction = context.Database.CurrentTransaction?.UnderlyingTransaction as SqlTransaction;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using (var command = new SqlCommand(CreateDeleteTempTableSql<TValue>(tempTableName, valueColumnName), connection, transaction))
                {
                    command.CommandTimeout = context.Database.CommandTimeout ?? 600;
                    command.ExecuteNonQuery();
                }

                var table = CreateSingleColumnDataTable(rows, valueColumnName);
                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.BatchSize = batchSize;
                    bulkCopy.BulkCopyTimeout = context.Database.CommandTimeout ?? 600;
                    bulkCopy.ColumnMappings.Add(valueColumnName, valueColumnName);
                    bulkCopy.WriteToServer(table);
                }

                using (var command = new SqlCommand(CreateDeleteByColumnValuesSql(tableName, columnName, tempTableName, valueColumnName), connection, transaction))
                {
                    command.CommandTimeout = context.Database.CommandTimeout ?? 600;
                    return command.ExecuteNonQuery();
                }
            }
            finally
            {
                TryDropTempTable(connection, transaction, tempTableName);

                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        private static void BulkUpdateWithTempTable<TEntity>(DbContext context, SqlConnection connection, List<TEntity> rows)
            where TEntity : class
        {
            var entityType = typeof(TEntity);
            var keyMaps = GetKeyPropertyMaps(entityType);
            var updateMaps = GetUpdatePropertyMaps(entityType, keyMaps);

            if (updateMaps.Count == 0)
            {
                return;
            }

            var maps = keyMaps.Concat(updateMaps).ToList();
            var table = CreateDataTable(rows, maps);
            var tempTableName = "#BulkUpdate_" + Guid.NewGuid().ToString("N");
            var shouldClose = connection.State == ConnectionState.Closed;
            var transaction = context.Database.CurrentTransaction?.UnderlyingTransaction as SqlTransaction;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using (var command = new SqlCommand(CreateBulkUpdateTempTableSql(tempTableName, maps), connection, transaction))
                {
                    command.CommandTimeout = context.Database.CommandTimeout ?? 600;
                    command.ExecuteNonQuery();
                }

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.BatchSize = 5000;
                    bulkCopy.BulkCopyTimeout = context.Database.CommandTimeout ?? 600;

                    foreach (var map in maps)
                    {
                        bulkCopy.ColumnMappings.Add(map.Property.Name, map.Property.Name);
                    }

                    bulkCopy.WriteToServer(table);
                }

                using (var command = new SqlCommand(CreateBulkUpdateSql(entityType, tempTableName, keyMaps, updateMaps), connection, transaction))
                {
                    command.CommandTimeout = context.Database.CommandTimeout ?? 600;
                    command.ExecuteNonQuery();
                }

                foreach (var row in rows)
                {
                    var entry = context.Entry(row);
                    if (entry.State != EntityState.Detached)
                    {
                        entry.State = EntityState.Unchanged;
                    }
                }
            }
            finally
            {
                TryDropTempTable(connection, transaction, tempTableName);

                if (shouldClose)
                {
                    connection.Close();
                }
            }
        }

        private static void UpdateWithChangeTracker<TEntity>(DbContext context, List<TEntity> rows)
            where TEntity : class
        {
            var set = context.Set<TEntity>();
            var autoDetectChangesEnabled = context.Configuration.AutoDetectChangesEnabled;
            context.Configuration.AutoDetectChangesEnabled = false;

            try
            {
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
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = autoDetectChangesEnabled;
            }
        }

        public static List<TEntity> WhereBulkContains<TEntity, TContains>(
            this IQueryable<TEntity> source,
            DbContext context,
            IEnumerable<TContains> containsItems,
            Expression<Func<TEntity, object>> entityKeyExpression,
            Expression<Func<TContains, object>> containsKeyExpression,
            Action<BulkContainsOptions> optionsAction = null)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (entityKeyExpression == null)
            {
                throw new ArgumentNullException(nameof(entityKeyExpression));
            }

            if (containsKeyExpression == null)
            {
                throw new ArgumentNullException(nameof(containsKeyExpression));
            }

            var rows = (containsItems ?? Enumerable.Empty<TContains>()).ToList();
            if (rows.Count == 0)
            {
                return new List<TEntity>();
            }

            var options = new BulkContainsOptions();
            optionsAction?.Invoke(options);

            var maps = CreateBulkContainsKeyMaps(entityKeyExpression, containsKeyExpression);
            var table = CreateBulkContainsTable(rows, maps);
            if (table.Rows.Count == 0)
            {
                return new List<TEntity>();
            }

            var objectQuery = source.GetObjectQuery();
            if (objectQuery == null)
            {
                throw new InvalidOperationException("WhereBulkContains only supports Entity Framework queries.");
            }

            var tempTableName = "#BulkContains_" + Guid.NewGuid().ToString("N");
            var connection = (SqlConnection)context.Database.Connection;
            var shouldClose = connection.State == ConnectionState.Closed;
            var transaction = context.Database.CurrentTransaction?.UnderlyingTransaction as SqlTransaction;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using (var command = new SqlCommand(CreateBulkContainsTempTableSql(tempTableName, maps), connection, transaction))
                {
                    command.ExecuteNonQuery();
                }

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.BatchSize = options.BatchSize;
                    bulkCopy.BulkCopyTimeout = context.Database.CommandTimeout ?? options.TimeoutSeconds;

                    foreach (var map in maps)
                    {
                        bulkCopy.ColumnMappings.Add(map.TempColumnName, map.TempColumnName);
                    }

                    bulkCopy.WriteToServer(table);
                }

                var sql = CreateBulkContainsQuerySql(objectQuery, tempTableName, maps, GetSelectablePropertyMaps(typeof(TEntity)));
                var parameters = CreateSqlParameters(objectQuery).ToArray();
                return context.Database.SqlQuery<TEntity>(sql, parameters).ToList();
            }
            finally
            {
                TryDropTempTable(connection, transaction, tempTableName);

                if (shouldClose)
                {
                    connection.Close();
                }
            }
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

        private static DataTable CreateSingleColumnDataTable<TValue>(IEnumerable<TValue> rows, string columnName)
        {
            var table = new DataTable();
            table.Columns.Add(columnName, GetDataColumnType(typeof(TValue)));

            foreach (var row in rows)
            {
                table.Rows.Add(row == null ? DBNull.Value : (object)row);
            }

            return table;
        }

        private static DataTable CreateBulkContainsTable<TContains>(
            IEnumerable<TContains> rows,
            List<BulkContainsKeyMap<TContains>> maps)
        {
            var table = new DataTable();
            foreach (var map in maps)
            {
                table.Columns.Add(map.TempColumnName, map.DataColumnType);
            }

            var signatures = new HashSet<string>();
            foreach (var row in rows)
            {
                var values = maps.Select(map => map.ValueAccessor(row)).ToArray();
                var signature = string.Join(
                    "\u001f",
                    values.Select(value => value == null ? "\u0000" : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));

                if (!signatures.Add(signature))
                {
                    continue;
                }

                table.Rows.Add(values.Select(value => value ?? DBNull.Value).ToArray());
            }

            return table;
        }

        private static List<BulkContainsKeyMap<TContains>> CreateBulkContainsKeyMaps<TEntity, TContains>(
            Expression<Func<TEntity, object>> entityKeyExpression,
            Expression<Func<TContains, object>> containsKeyExpression)
        {
            var entityExpressions = GetKeyExpressions(entityKeyExpression.Body);
            var containsExpressions = GetKeyExpressions(containsKeyExpression.Body);

            if (entityExpressions.Count != containsExpressions.Count)
            {
                throw new ArgumentException("Entity key and contains key must have the same number of fields.");
            }

            var maps = new List<BulkContainsKeyMap<TContains>>();
            for (var index = 0; index < entityExpressions.Count; index++)
            {
                var entityMember = GetMemberExpression(entityExpressions[index]);
                var entityProperty = entityMember.Member as PropertyInfo;
                if (entityProperty == null)
                {
                    throw new ArgumentException("Entity key fields must be public properties.");
                }

                var containsExpression = containsExpressions[index];
                var accessor = Expression.Lambda<Func<TContains, object>>(
                    Expression.Convert(containsExpression, typeof(object)),
                    containsKeyExpression.Parameters[0]).Compile();
                var keyType = Nullable.GetUnderlyingType(containsExpression.Type) ?? containsExpression.Type;

                maps.Add(new BulkContainsKeyMap<TContains>
                {
                    EntityColumnName = entityProperty.GetCustomAttribute<ColumnAttribute>()?.Name ?? entityProperty.Name,
                    TempColumnName = "Key" + index,
                    SqlColumnType = GetSqlColumnType(keyType),
                    DataColumnType = GetDataColumnType(keyType),
                    ValueAccessor = accessor
                });
            }

            return maps;
        }

        private static List<Expression> GetKeyExpressions(Expression expression)
        {
            expression = RemoveConvert(expression);

            var newExpression = expression as NewExpression;
            if (newExpression != null)
            {
                return newExpression.Arguments.Select(RemoveConvert).ToList();
            }

            var memberInit = expression as MemberInitExpression;
            if (memberInit != null)
            {
                return memberInit.Bindings
                    .OfType<MemberAssignment>()
                    .Select(binding => RemoveConvert(binding.Expression))
                    .ToList();
            }

            return new List<Expression> { expression };
        }

        private static MemberExpression GetMemberExpression(Expression expression)
        {
            var memberExpression = RemoveConvert(expression) as MemberExpression;
            if (memberExpression == null)
            {
                throw new ArgumentException("Key expressions must be simple member access expressions.");
            }

            return memberExpression;
        }

        private static string GetColumnName<TEntity, TValue>(Expression<Func<TEntity, TValue>> columnExpression)
        {
            var memberExpression = GetMemberExpression(columnExpression.Body);
            var property = memberExpression.Member as PropertyInfo;
            if (property == null)
            {
                throw new ArgumentException("Column expression must point to a public property.", nameof(columnExpression));
            }

            if (property.GetCustomAttribute<NotMappedAttribute>() != null)
            {
                throw new ArgumentException("Column expression cannot point to a NotMapped property.", nameof(columnExpression));
            }

            return property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;
        }

        private static Expression RemoveConvert(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression;
        }

        private static string CreateBulkUpdateTempTableSql(string tempTableName, IEnumerable<PropertyMap> maps)
        {
            var columns = maps
                .Select(map => $"{QuoteIdentifier(map.Property.Name)} {GetSqlColumnType(map.Property.PropertyType)} null");

            return $"create table {QuoteIdentifier(tempTableName)} ({string.Join(", ", columns)})";
        }

        private static string CreateBulkUpdateSql(
            Type entityType,
            string tempTableName,
            IEnumerable<PropertyMap> keyMaps,
            IEnumerable<PropertyMap> updateMaps)
        {
            var assignments = updateMaps.Select(map =>
                $"[Target].{QuoteIdentifier(map.ColumnName)} = [Source].{QuoteIdentifier(map.Property.Name)}");
            var joinConditions = keyMaps.Select(map =>
                $"[Target].{QuoteIdentifier(map.ColumnName)} = [Source].{QuoteIdentifier(map.Property.Name)}");

            return $@"
update [Target]
set {string.Join(", ", assignments)}
from {GetTableName(entityType)} as [Target]
inner join {QuoteIdentifier(tempTableName)} as [Source]
    on {string.Join(" and ", joinConditions)}";
        }

        private static string CreateBulkContainsTempTableSql<TContains>(
            string tempTableName,
            IEnumerable<BulkContainsKeyMap<TContains>> maps)
        {
            var columns = maps
                .Select(map => $"{QuoteIdentifier(map.TempColumnName)} {map.SqlColumnType} null");

            return $"create table {QuoteIdentifier(tempTableName)} ({string.Join(", ", columns)})";
        }

        private static string CreateBulkContainsQuerySql<TEntity, TContains>(
            ObjectQuery<TEntity> objectQuery,
            string tempTableName,
            IEnumerable<BulkContainsKeyMap<TContains>> maps,
            IEnumerable<PropertyMap> selectMaps)
        {
            var sourceSql = objectQuery.ToTraceString();
            var selectColumns = selectMaps.Select(map =>
                $"[BulkSource].{QuoteIdentifier(map.ColumnName)} as {QuoteIdentifier(map.Property.Name)}");
            var joinConditions = maps.Select(map =>
                $"(([BulkSource].{QuoteIdentifier(map.EntityColumnName)} = [BulkKeys].{QuoteIdentifier(map.TempColumnName)}) or ([BulkSource].{QuoteIdentifier(map.EntityColumnName)} is null and [BulkKeys].{QuoteIdentifier(map.TempColumnName)} is null))");

            return $@"
select {string.Join(", ", selectColumns)}
from (
{sourceSql}
) as [BulkSource]
inner join {QuoteIdentifier(tempTableName)} as [BulkKeys]
    on {string.Join(" and ", joinConditions)}";
        }

        private static IEnumerable<SqlParameter> CreateSqlParameters<TEntity>(ObjectQuery<TEntity> objectQuery)
        {
            foreach (var parameter in objectQuery.Parameters)
            {
                yield return new SqlParameter("@" + parameter.Name, parameter.Value ?? DBNull.Value);
            }
        }

        private static string CreateDropTempTableSql(string tempTableName)
        {
            return $"if object_id('tempdb..{tempTableName}') is not null drop table {QuoteIdentifier(tempTableName)}";
        }

        private static void TryDropTempTable(SqlConnection connection, SqlTransaction transaction, string tempTableName)
        {
            try
            {
                if (connection == null || connection.State != ConnectionState.Open)
                {
                    return;
                }

                using (var command = new SqlCommand(CreateDropTempTableSql(tempTableName), connection, transaction))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static string CreateDeleteTempTableSql<TValue>(string tempTableName, string valueColumnName)
        {
            return $"create table {QuoteIdentifier(tempTableName)} ({QuoteIdentifier(valueColumnName)} {GetSqlColumnType(typeof(TValue))} null)";
        }

        private static string CreateDeleteByColumnValuesSql(
            string tableName,
            string columnName,
            string tempTableName,
            string valueColumnName)
        {
            var sourceColumnName = QuoteIdentifier(valueColumnName);

            return $@"
delete [Target]
from {tableName} as [Target]
where exists (
    select 1
    from {QuoteIdentifier(tempTableName)} as [Source]
    where ([Target].{columnName} = [Source].{sourceColumnName})
        or ([Target].{columnName} is null and [Source].{sourceColumnName} is null)
)";
        }

        private static string QuoteIdentifier(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private static Type GetDataColumnType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;
            return actualType.IsEnum ? Enum.GetUnderlyingType(actualType) : actualType;
        }

        private static string GetSqlColumnType(Type type)
        {
            var actualType = GetDataColumnType(type);

            if (actualType == typeof(string))
            {
                return "nvarchar(4000)";
            }

            if (actualType == typeof(int))
            {
                return "int";
            }

            if (actualType == typeof(long))
            {
                return "bigint";
            }

            if (actualType == typeof(short))
            {
                return "smallint";
            }

            if (actualType == typeof(byte))
            {
                return "tinyint";
            }

            if (actualType == typeof(bool))
            {
                return "bit";
            }

            if (actualType == typeof(DateTime))
            {
                return "datetime2";
            }

            if (actualType == typeof(DateTimeOffset))
            {
                return "datetimeoffset";
            }

            if (actualType == typeof(Guid))
            {
                return "uniqueidentifier";
            }

            if (actualType == typeof(decimal))
            {
                return "decimal(38, 10)";
            }

            if (actualType == typeof(double))
            {
                return "float";
            }

            if (actualType == typeof(float))
            {
                return "real";
            }

            if (actualType == typeof(byte[]))
            {
                return "varbinary(max)";
            }

            return "nvarchar(4000)";
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

        private static List<PropertyMap> GetKeyPropertyMaps(Type entityType)
        {
            var maps = entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead)
                .Where(property => property.GetCustomAttribute<NotMappedAttribute>() == null)
                .Where(property => property.GetCustomAttribute<KeyAttribute>() != null)
                .Select(property => new PropertyMap
                {
                    Property = property,
                    ColumnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name
                })
                .ToList();

            if (maps.Count > 0)
            {
                return maps;
            }

            var fallbackKey = entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(property => string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase))
                ?? entityType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(property => string.Equals(property.Name, entityType.Name + "Id", StringComparison.OrdinalIgnoreCase));

            if (fallbackKey == null)
            {
                throw new InvalidOperationException($"BulkUpdate could not find a key property for {entityType.Name}.");
            }

            return new List<PropertyMap>
            {
                new PropertyMap
                {
                    Property = fallbackKey,
                    ColumnName = fallbackKey.GetCustomAttribute<ColumnAttribute>()?.Name ?? fallbackKey.Name
                }
            };
        }

        private static List<PropertyMap> GetUpdatePropertyMaps(Type entityType, IEnumerable<PropertyMap> keyMaps)
        {
            var keyProperties = new HashSet<string>(keyMaps.Select(map => map.Property.Name), StringComparer.OrdinalIgnoreCase);

            return entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead)
                .Where(property => property.GetCustomAttribute<NotMappedAttribute>() == null)
                .Where(property => !keyProperties.Contains(property.Name))
                .Where(property => !IsDatabaseGeneratedColumn(property))
                .Where(property => IsSimpleType(property.PropertyType))
                .Select(property => new PropertyMap
                {
                    Property = property,
                    ColumnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name
                })
                .ToList();
        }

        private static List<PropertyMap> GetSelectablePropertyMaps(Type entityType)
        {
            return entityType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead)
                .Where(property => property.GetCustomAttribute<NotMappedAttribute>() == null)
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

        private static bool IsDatabaseGeneratedColumn(PropertyInfo property)
        {
            var databaseGenerated = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
            return databaseGenerated?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity
                || databaseGenerated?.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed;
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

        private sealed class BulkContainsKeyMap<TContains>
        {
            public string EntityColumnName { get; set; }

            public string TempColumnName { get; set; }

            public string SqlColumnType { get; set; }

            public Type DataColumnType { get; set; }

            public Func<TContains, object> ValueAccessor { get; set; }
        }
    }

    public sealed class BulkOperationOptions
    {
        public bool AutoMapOutputDirection { get; set; }

        public int BatchSize { get; set; } = 5000;

        public int TimeoutSeconds { get; set; } = 600;
    }

    public sealed class BulkContainsOptions
    {
        public int BatchSize { get; set; } = 5000;

        public int TimeoutSeconds { get; set; } = 600;
    }
}
