using System.Linq.Expressions;
using Netcorext.Extensions.Linq.Expressions;

namespace Netcorext.Extensions.Linq;

public static class ExpressionExtension
{
    public static Expression<Func<TSource, bool>> Equal<TSource>(this Expression<Func<TSource, bool>> expr1, Expression<Func<TSource, bool>> expr2)
    {
        if (expr1 == null) throw new ArgumentNullException(nameof(expr1));
        if (expr2 == null) throw new ArgumentNullException(nameof(expr2));

        var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);

        return Expression.Lambda<Func<TSource, bool>>(Expression.Equal(expr1.Body, secondBody), expr1.Parameters);
    }
    
    public static Expression<Func<TSource, bool>> NotEqual<TSource>(this Expression<Func<TSource, bool>> expr1, Expression<Func<TSource, bool>> expr2)
    {
        if (expr1 == null) throw new ArgumentNullException(nameof(expr1));
        if (expr2 == null) throw new ArgumentNullException(nameof(expr2));

        var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);

        return Expression.Lambda<Func<TSource, bool>>(Expression.NotEqual(expr1.Body, secondBody), expr1.Parameters);
    }
    
    public static Expression<Func<TSource, bool>> And<TSource>(this Expression<Func<TSource, bool>> expr1, Expression<Func<TSource, bool>> expr2)
    {
        if (expr1 == null) throw new ArgumentNullException(nameof(expr1));
        if (expr2 == null) throw new ArgumentNullException(nameof(expr2));

        var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);

        return Expression.Lambda<Func<TSource, bool>>(Expression.AndAlso(expr1.Body, secondBody), expr1.Parameters);
    }

    public static Expression<Func<TSource, bool>> Or<TSource>(this Expression<Func<TSource, bool>> expr1, Expression<Func<TSource, bool>> expr2)
    {
        if (expr1 == null) throw new ArgumentNullException(nameof(expr1));
        if (expr2 == null) throw new ArgumentNullException(nameof(expr2));

        var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);

        return Expression.Lambda<Func<TSource, bool>>(Expression.OrElse(expr1.Body, secondBody), expr1.Parameters);
    }

    public static Expression<Func<TDestination, bool>> Convert<TDestination>(this Expression source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var param = Expression.Parameter(typeof(TDestination), "z");

        var result = source switch
                     {
                         LambdaExpression exp => new ConvertExpressionVisitor<TDestination>(param).Visit(exp.Body),
                         _ => new ConvertExpressionVisitor<TDestination>(param).Visit(source)
                     };

        if (result == null) throw new ArgumentNullException(nameof(result));

        var lambda = Expression.Lambda<Func<TDestination, bool>>(result, new[] { param });

        return lambda;
    }

    public static Expression Replace(this Expression expression, Expression searchEx, Expression replaceEx)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        if (searchEx == null) throw new ArgumentNullException(nameof(searchEx));
        if (replaceEx == null) throw new ArgumentNullException(nameof(replaceEx));

        return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
    }
}