using System;
using System.Linq;
using System.Linq.Expressions;

namespace Service.Extensions
{
    /// <summary>
    /// 提供 IQueryable 查詢擴充方法。
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// 當指定條件成立時套用篩選條件；條件不成立時回傳原查詢。
        /// </summary>
        /// <typeparam name="T">查詢資料類型。</typeparam>
        /// <param name="source">原始查詢。</param>
        /// <param name="condition">是否套用篩選條件。</param>
        /// <param name="predicate">篩選條件運算式。</param>
        /// <returns>套用條件後或未變更的查詢。</returns>
        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> source,
            bool condition,
            Expression<Func<T, bool>> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return condition ? source.Where(predicate) : source;
        }
    }
}
