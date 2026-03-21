using Dapper;
using System;
using System.Text;

namespace Service.Extensions
{
    public static class SqlBuilderExtensions
    {

        /// <summary>
        /// 兵ンΘミ穝糤ぃ把计 WHERE 
        /// </summary>
        public static StringBuilder WhereIf(
            this StringBuilder sql,
            bool condition,
            string clause)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            if (condition && !string.IsNullOrWhiteSpace(clause))
            {
                sql.AppendLine($"AND {clause}");
            }

            return sql;
        }

        /// <summary>
        /// 兵ンΘミ穝糤 WHERE 把计
        /// </summary>
        public static StringBuilder WhereIf(
            this StringBuilder sql,
            bool condition,
            string clause,
            DynamicParameters parameters,
            Action<DynamicParameters> addParameters)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (addParameters == null)
                throw new ArgumentNullException(nameof(addParameters));

            if (condition && !string.IsNullOrWhiteSpace(clause))
            {
                sql.AppendLine($"AND {clause}");
                addParameters(parameters);
            }

            return sql;
        }

    }
}
