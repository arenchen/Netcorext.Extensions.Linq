using System.Linq.Expressions;
using System.Reflection;

namespace Netcorext.Extensions.Linq;

public static class ExpressionExtension
{
    public static Expression<Func<TSource, bool>> And<TSource>(this Expression<Func<TSource, bool>> expr1, Expression<Func<TSource, bool>> expr2)
    {
        var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);

        return Expression.Lambda<Func<TSource, bool>>(Expression.AndAlso(expr1.Body, secondBody), expr1.Parameters);
    }

    public static Expression<Func<TSource, bool>> Or<TSource>(this Expression<Func<TSource, bool>> expr1, Expression<Func<TSource, bool>> expr2)
    {
        var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);

        return Expression.Lambda<Func<TSource, bool>>(Expression.OrElse(expr1.Body, secondBody), expr1.Parameters);
    }

    public static Expression<Func<TDestination, bool>> Convert<TDestination>(this Expression source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var param = Expression.Parameter(typeof(TDestination), "z");

        var result = source switch
                     {
                         LambdaExpression exp => new ConvertExpressionVisitor<TDestination>(param).Visit(exp.Body),
                         _ => new ConvertExpressionVisitor<TDestination>(param).Visit(source)
                     };

        if (result == null)
            throw new ArgumentNullException(nameof(result));

        var lambda = Expression.Lambda<Func<TDestination, bool>>(result, new[] { param });

        return lambda;
    }

    private static Expression Replace(this Expression expression, Expression searchEx, Expression replaceEx)
    {
        return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
    }
}

internal class ReplaceVisitor : ExpressionVisitor
{
    private readonly Expression _from, _to;

    public ReplaceVisitor(Expression from, Expression to)
    {
        _from = from;
        _to = to;
    }

    public override Expression Visit(Expression node)
    {
        return node == _from ? _to : base.Visit(node);
    }
}

internal class ConvertExpressionVisitor<T> : ExpressionVisitor
{
    private readonly ParameterExpression _param;

    public ConvertExpressionVisitor(ParameterExpression param)
    {
        _param = param;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return _param;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member.MemberType != MemberTypes.Property) return base.VisitMember(node);

        MemberExpression memberExpression = null;

        var memberName = node.Member.Name;

        var otherMember = typeof(T).GetProperty(memberName);

        if (otherMember == null) return Expression.Constant(true);

        var exp = Visit(node.Expression);

        memberExpression = Expression.Property(exp, otherMember);

        return memberExpression;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType != ExpressionType.Equal)
        {
            var expression = base.VisitBinary(node);

            if (!(expression is BinaryExpression exp))
            {
                return expression;
            }

            if (exp.Left.NodeType == ExpressionType.Constant && exp.Right.NodeType == ExpressionType.Constant)
                return expression;

            return exp.Left.NodeType == ExpressionType.Constant ? exp.Right : exp.Left;
        }

        var type = typeof(T);

        foreach (var child in new[] { node.Left, node.Right })
        {
            switch (child)
            {
                case MemberExpression exp:
                    if (!type.GetMember(exp.Member.Name).Any())
                        return Expression.Constant(true);

                    break;
                case UnaryExpression exp:
                    var memExp = exp.Operand as MemberExpression;

                    if (memExp == null || !type.GetMember(memExp.Member.Name).Any())
                        return Expression.Constant(true);

                    break;
            }
        }

        return base.VisitBinary(node);
    }
}