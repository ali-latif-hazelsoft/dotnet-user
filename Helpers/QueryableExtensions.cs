using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace dotnet_user.Helpers
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplySorting<T>(
            this IQueryable<T> source,
            string sortBy,
            string sortDirection
        )
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            string propertyName = string.IsNullOrWhiteSpace(sortBy) ? "Id" : sortBy.Trim();

            PropertyInfo propertyInfo = typeof(T).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
            );

            if (propertyInfo == null)
            {
                throw new ArgumentException($"Invalid sort field: {propertyName}");
            }

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            MemberExpression property = Expression.Property(parameter, propertyInfo);
            LambdaExpression lambda = Expression.Lambda(property, parameter);

            string methodName = string.Equals(
                sortDirection?.Trim(),
                "desc",
                StringComparison.OrdinalIgnoreCase
            )
                ? nameof(Queryable.OrderByDescending)
                : nameof(Queryable.OrderBy);

            MethodCallExpression resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), property.Type },
                source.Expression,
                Expression.Quote(lambda)
            );

            return source.Provider.CreateQuery<T>(resultExpression);
        }
    }
}
