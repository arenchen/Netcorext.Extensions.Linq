using System.Linq.Expressions;
using System.Reflection;

namespace Netcorext.Extensions.Linq.Expressions;

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