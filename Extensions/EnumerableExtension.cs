using System.Linq.Expressions;

namespace Netcorext.Extensions.Linq;

public static class EnumerableExtension
{
    #region Join

    public static IEnumerable<TSource> In<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (member == null) throw new ArgumentNullException(nameof(member));

        if (!values.Any()) return source;

        var p = member.Parameters.Single();
        var equals = values.Select(value => (Expression)Expression.Equal(member.Body, Expression.Constant(value, typeof(TValue))));
        var body = equals.Aggregate(Expression.Or);

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return source.Where(predicate);
    }

    public static IEnumerable<TSource> NotIn<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (member == null) throw new ArgumentNullException(nameof(member));

        if (!values.Any()) return source;

        var p = member.Parameters.Single();
        var equals = values.Select(value => (Expression)Expression.NotEqual(member.Body, Expression.Constant(value, typeof(TValue))));
        var body = equals.Aggregate(Expression.AndAlso);

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return source.Where(predicate);
    }

    public static IEnumerable<TSource> Like<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (member == null) throw new ArgumentNullException(nameof(member));
        if (typeof(TValue) != typeof(string)) throw new ArgumentException("Must be a string type");

        if (!values.Any()) return source;

        var p = member.Parameters.Single();
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var equals = values.Select(value => (Expression)Expression.Call(member.Body, containsMethod, Expression.Constant(value, typeof(TValue))));
        var body = equals.Aggregate((accumulate, equal) => Expression.Or(accumulate, equal));

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return source.Where(predicate);
    }

    public static IEnumerable<TSource> NotLike<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (member == null) throw new ArgumentNullException(nameof(member));
        if (typeof(TValue) != typeof(string)) throw new ArgumentException("Must be a string type");

        if (!values.Any()) return source;

        var p = member.Parameters.Single();
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var equals = values.Select(value => (Expression)Expression.Not(Expression.Call(member.Body, containsMethod, Expression.Constant(value, typeof(TValue)))));
        var body = equals.Aggregate((accumulate, equal) => Expression.And(accumulate, equal));

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return source.Where(predicate);
    }

    public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TOuter, TInner> defaultValue = null)
    {
        if (outer == null) throw new ArgumentNullException(nameof(outer));
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
        if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        return from o in outer
               join i in inner on outerKeySelector(o) equals innerKeySelector(i) into g
               from i in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(o))
               select resultSelector(o, i);
    }

    public static IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TInner, TOuter> defaultValue = null)
    {
        if (outer == null) throw new ArgumentNullException(nameof(outer));
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
        if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        return from i in inner
               join o in outer on innerKeySelector(i) equals outerKeySelector(o) into g
               from o in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(i))
               select resultSelector(o, i);
    }

    #endregion

    #region Intersect、Except

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) IntersectBoth<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();

        var r = a.Join(b, t => t, t => t, (arg1, arg2) => (arg1, arg2)).ToArray();
        var r1 = r.Select(t => t.arg1);
        var r2 = r.Select(t => t.arg2);

        return (r1, r2);
    }

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) IntersectBoth<TSource>(this IEnumerable<TSource> first, params TSource[] second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second;

        var r = a.Join(b, t => t, t => t, (arg1, arg2) => (arg1, arg2)).ToArray();
        var r1 = r.Select(t => t.arg1);
        var r2 = r.Select(t => t.arg2);

        return (r1, r2);
    }

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) IntersectBoth<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();

        var r = a.Join(b, keySelector, keySelector, (arg1, arg2) => (arg1, arg2)).ToArray();
        var r1 = r.Select(t => t.arg1);
        var r2 = r.Select(t => t.arg2);

        return (r1, r2);
    }

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) ExceptBoth<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();

        var r1 = ExceptIterator(a, b);
        var r2 = ExceptIterator(b, a);

        return (r1, r2);
    }

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) ExceptBoth<TSource>(this IEnumerable<TSource> first, params TSource[] second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second;

        var r1 = ExceptIterator(a, b);
        var r2 = ExceptIterator(b, a);

        return (r1, r2);
    }

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) ExceptBoth<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();

        var r1 = ExceptIterator(a, b, keySelector);
        var r2 = ExceptIterator(b, a, keySelector);


        return (r1, r2);
    }

    public static (IEnumerable<TSource> FirstExcept, IEnumerable<TSource> SecondExcept, IEnumerable<TSource> FirstIntersect, IEnumerable<TSource> SecondIntersect) IntersectExcept<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        var (exceptFirst, exceptSecond) = ExceptBoth(first, second);

        var (intersectFirst, intersectSecond) = IntersectBoth(first, second);

        return (exceptFirst, exceptSecond, intersectFirst, intersectSecond);
    }

    public static (IEnumerable<TSource> FirstExcept, IEnumerable<TSource> SecondExcept, IEnumerable<TSource> FirstIntersect, IEnumerable<TSource> SecondIntersect) IntersectExcept<TSource>(this IEnumerable<TSource> first, params TSource[]? second)
    {
        var (exceptFirst, exceptSecond) = ExceptBoth(first, second);

        var (intersectFirst, intersectSecond) = IntersectBoth(first, second);

        return (exceptFirst, exceptSecond, intersectFirst, intersectSecond);
    }

    public static (IEnumerable<TSource> FirstExcept, IEnumerable<TSource> SecondExcept, IEnumerable<TSource> FirstIntersect, IEnumerable<TSource> SecondIntersect) IntersectExcept<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
    {
        var (exceptFirst, exceptSecond) = ExceptBoth(first, second, keySelector);

        var (intersectFirst, intersectSecond) = IntersectBoth(first, second, keySelector);

        return (exceptFirst, exceptSecond, intersectFirst, intersectSecond);
    }

    private static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        var set = new HashSet<TSource>(second);
        var newSet = new HashSet<TSource>();

        foreach (var item in first)
        {
            if (!set.Add(item) && !newSet.Contains(item)) continue;

            newSet.Add(item);

            yield return item;
        }
    }

    private static IEnumerable<TSource> ExceptIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
    {
        var set = new HashSet<TKey>(second.Select(keySelector));
        var newSet = new HashSet<TKey>();

        foreach (var item in first)
        {
            var key = keySelector(item);

            if (!set.Add(key) && !newSet.Contains(key)) continue;

            newSet.Add(key);

            yield return item;
        }
    }

    private static IEnumerable<TSource> IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        var set = new HashSet<TSource>(second);
        var newSet = new HashSet<TSource>();

        foreach (var item in first)
        {
            if (!set.Add(item) && !newSet.Contains(item))
            {
                yield return item;
            }
            else
            {
                newSet.Add(item);
            }
        }
    }

    private static IEnumerable<TSource> IntersectIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
    {
        var set = new HashSet<TKey>(second.Select(keySelector));
        var newSet = new HashSet<TKey>();

        foreach (var item in first)
        {
            if (!set.Add(keySelector(item)) && !newSet.Contains(keySelector(item)))
            {
                yield return item;
            }
            else
            {
                newSet.Add(keySelector(item));
            }
        }
    }

    #endregion

    #region Merge

    public static IEnumerable<TSource> Merge<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, bool overwrite = false) where TKey : notnull
    {
        var result = new Dictionary<TKey, TSource>(first.ToDictionary(keySelector, t => t));

        foreach (var item in second)
        {
            var key = keySelector(item);

            if (!result.ContainsKey(key))
            {
                result.Add(key, item);

                continue;
            }

            if (!overwrite) continue;

            result[key] = item;
        }

        return result.Values;
    }

    public static IEnumerable<TSource> Merge<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, Action<TSource, TSource>? updater) where TKey : notnull
    {
        var result = new Dictionary<TKey, TSource>(first.ToDictionary(keySelector, t => t));

        foreach (var item in second)
        {
            var key = keySelector(item);

            if (!result.ContainsKey(key))
            {
                result.Add(key, item);

                continue;
            }

            updater?.Invoke(result[key], item);
        }

        return result.Values;
    }

    #endregion
}