using System.Linq.Expressions;

namespace Netcorext.Extensions.Linq;

public static class EnumerableExtension
{
    #region Join

    public static IEnumerable<TSource> In<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        if (member == null) throw new ArgumentNullException(nameof(member));

        var enumerable = source as TSource[] ?? source.ToArray();

        if (!enumerable.Any()) return null;

        var p = member.Parameters.Single();

        var equals = values.Select(value => (Expression)Expression.Equal(member.Body, Expression.Constant(value, typeof(TValue))));
        var body = equals.Aggregate(Expression.Or);

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return enumerable.Where(predicate);
    }

    public static IEnumerable<TSource> NotIn<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        if (member == null) throw new ArgumentNullException(nameof(member));

        var enumerable = source as TSource[] ?? source.ToArray();

        if (!enumerable.Any()) return null;

        var p = member.Parameters.Single();

        var equals = values.Select(value => (Expression)Expression.NotEqual(member.Body, Expression.Constant(value, typeof(TValue))));
        var body = equals.Aggregate(Expression.AndAlso);

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return enumerable.Where(predicate);
    }

    public static IEnumerable<TSource> Like<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        if (member == null) throw new ArgumentNullException(nameof(member));

        if (typeof(TValue) != typeof(string)) throw new ArgumentException("Must be a string type");

        if (!source.Any()) return null;

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

        if (!source.Any()) return null;

        var p = member.Parameters.Single();

        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

        var equals = values.Select(value =>
                                       (Expression)Expression.Not(
                                                                  (Expression)Expression.Call(member.Body, containsMethod, Expression.Constant(value, typeof(TValue)))
                                                                 ));

        var body = equals.Aggregate((accumulate, equal) => Expression.And(accumulate, equal));

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return source.Where(predicate);
    }

    public static IEnumerable<TSource> NotWhere<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        using (var e = source.GetEnumerator())
        {
            while (e.MoveNext())
            {
                var element = e.Current;

                if (!predicate(element)) yield return element;
            }
        }
    }

    public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TOuter, TInner> defaultValue = null)
    {
        return from o in outer
               join i in inner on outerKeySelector(o) equals innerKeySelector(i) into g
               from i in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(o))
               select resultSelector(o, i);

        // var result = outer.GroupJoin(inner ?? new TInner[0],
        //                              outerKeySelector, innerKeySelector,
        //                              (o, i) => new { Outer = o, Inners = i })
        //                   .SelectMany(t => t.Inners.DefaultIfEmpty(defaultValue == null ? default(TInner) : defaultValue.Invoke(t.Outer)), (g, i) => new { Outer = g.Outer, Inner = i })
        //                   .Select(t => resultSelector(t.Outer, t.Inner));
        //
        // return result;
    }

    public static IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TInner, TOuter> defaultValue = null)
    {
        return from i in inner
               join o in outer on innerKeySelector(i) equals outerKeySelector(o) into g
               from o in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(i))
               select resultSelector(o, i);

        // var result = inner.GroupJoin(outer ?? new TOuter[0],
        //                              innerKeySelector, outerKeySelector,
        //                              (o, i) => new { Outer = o, Inners = i })
        //                   .SelectMany(t => t.Inners.DefaultIfEmpty(defaultValue == null ? default(TOuter) : defaultValue.Invoke(t.Outer)), (g, i) => new { Outer = g.Outer, Inner = i })
        //                   .Select(t => resultSelector(t.Inner, t.Outer));
        //
        // return result;
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
}