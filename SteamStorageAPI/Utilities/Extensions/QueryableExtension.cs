using System.Linq.Expressions;

namespace SteamStorageAPI.Utilities.Extensions;

public static class QueryableExtension
{
    #region Constants

    private const string TO_LOWER = "ToLower";
    private const string CONTAINS = "Contains";

    #endregion Constants

    #region Fields

    private static readonly char[] _separator = [' '];

    #endregion Fields

    #region Methods

    public static IQueryable<TSource> WhereMatchFilter<TSource, TProperty>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, TProperty>> propertySelector,
        string? filter)
    {
        if (filter is null)
            return source;
        string[] filterWords = filter.ToLower().Split(_separator, StringSplitOptions.RemoveEmptyEntries);
        Expression<Func<TSource, bool>> containsFilter = GenerateContainsFilter(propertySelector, filterWords);
        return source.Where(containsFilter);
    }

    private static Expression<Func<TSource, bool>> GenerateContainsFilter<TSource, TProperty>(
        Expression<Func<TSource, TProperty>> propertySelector,
        IEnumerable<string> filterWords)
    {
        ParameterExpression parameter = propertySelector.Parameters.Single();
        Expression body = Expression.Constant(true);

        body = filterWords
            .Select(word =>
                Expression.Call(
                    Expression.Call(
                        propertySelector.Body,
                        TO_LOWER,
                        null),
                    CONTAINS,
                    null,
                    Expression.Constant(word.ToLower())))
            .Aggregate(body, Expression.AndAlso);

        return Expression.Lambda<Func<TSource, bool>>(body, parameter);
    }

    #endregion Methods
}
