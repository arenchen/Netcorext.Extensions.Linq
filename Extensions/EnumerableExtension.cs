using System.Linq.Expressions;

namespace Netcorext.Extensions.Linq;

public static class EnumerableExtension
{
    #region Iterate

    public delegate void ForAction<TElement>(long index, TElement element, ref bool isBreak);

    public delegate void ForEachAction<TElement>(TElement element, ref bool isBreak);

    public static void For<TSource>(this IEnumerable<TSource> source, Action<long, TSource> process)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (process == null) throw new ArgumentNullException(nameof(process));

        var enumerable = source as TSource[] ?? source.ToArray();

        for (var i = 0; i < enumerable.Length; i++)
        {
            process?.Invoke(i, enumerable[i]);
        }
    }

    public static void For<TSource>(this IEnumerable<TSource> source, ForAction<TSource> process)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (process == null) throw new ArgumentNullException(nameof(process));

        var enumerable = source as TSource[] ?? source.ToArray();

        for (var i = 0; i < enumerable.Length; i++)
        {
            var isBreak = false;
            process?.Invoke(i, enumerable[i], ref isBreak);

            if (isBreak) break;
        }
    }

    public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> process)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (process == null) throw new ArgumentNullException(nameof(process));

        foreach (var src in source)
        {
            process?.Invoke(src);
        }
    }

    public static void ForEach<TSource>(this IEnumerable<TSource> source, ForEachAction<TSource> process)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (process == null) throw new ArgumentNullException(nameof(process));

        foreach (var src in source)
        {
            var isBreak = false;

            process?.Invoke(src, ref isBreak);

            if (isBreak) break;
        }
    }

    #endregion

    #region Join

    public static bool AllExists<TSource, TValue>(this IEnumerable<TSource> source, Expression<Func<TSource, TValue>> member, params TValue[] values)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (member == null) throw new ArgumentNullException(nameof(member));

        values = values.Distinct()
                       .ToArray();

        if (!values.Any()) return false;

        var p = member.Parameters.Single();
        var equals = values.Select(value => (Expression)Expression.Equal(member.Body, Expression.Constant(value, typeof(TValue))));
        var body = equals.Aggregate(Expression.Or);

        var predicate = Expression.Lambda<Func<TSource, bool>>(body, p)
                                  .Compile();

        return source.Count(predicate) == values.Length;
    }

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

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) ExceptBoth<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();

        var r1 = ExceptIterator(a, b, null);
        var r2 = ExceptIterator(b, a, null);

        return (r1, r2);
    }

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) ExceptBoth<TSource>(this IEnumerable<TSource> first, params TSource[] second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second;

        var r1 = ExceptIterator(a, b, null);
        var r2 = ExceptIterator(b, a, null);

        return (r1, r2);
    }

    public static (IEnumerable<TSource> First, IEnumerable<TSource> Second) ExceptBoth<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();

        var r1 = ExceptByIterator(a, b, keySelector, null);
        var r2 = ExceptByIterator(b, a, keySelector, null);


        return (r1, r2);
    }

    public static (IEnumerable<TSource> Intersect, IEnumerable<TSource> FirstExcept, IEnumerable<TSource> SecondExcept) IntersectExcept<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();
        var intersect = a.Intersect(b);
        var (exceptFirst, exceptSecond) = ExceptBoth(a, b);

        return (intersect, exceptFirst, exceptSecond);
    }

    public static (IEnumerable<TSource> Intersect, IEnumerable<TSource> FirstExcept, IEnumerable<TSource> SecondExcept) IntersectExcept<TSource>(this IEnumerable<TSource> first, params TSource[] second)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second;
        var intersect = a.Intersect(b);
        var (exceptFirst, exceptSecond) = ExceptBoth(a, b);

        return (intersect, exceptFirst, exceptSecond);
    }

    public static (IEnumerable<TSource> Intersect, IEnumerable<TSource> FirstExcept, IEnumerable<TSource> SecondExcept) IntersectExcept<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
    {
        var a = first as TSource[] ?? first.ToArray();
        var b = second as TSource[] ?? second.ToArray();
        var intersect = IntersectByIterator(a, b, keySelector, null);
        var (exceptFirst, exceptSecond) = ExceptBoth(a, b, keySelector);

        return (intersect, exceptFirst, exceptSecond);
    }

    private static IEnumerable<TSource> IntersectByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        var set = new HashSet<TKey>(second.Select(keySelector), comparer);

        foreach (TSource element in first)
        {
            if (set.Remove(keySelector(element)))
            {
                yield return element;
            }
        }
    }

    private static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
    {
        var set = new HashSet<TSource>(second, comparer);

        foreach (TSource element in first)
        {
            if (set.Add(element))
            {
                yield return element;
            }
        }
    }

    private static IEnumerable<TSource> ExceptByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        var set = new HashSet<TKey>(second.Select(keySelector), comparer);

        foreach (TSource element in first)
        {
            if (set.Add(keySelector(element)))
            {
                yield return element;
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

    public static IEnumerable<TSource> Merge<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, Func<TSource, TSource, TSource>? updater) where TKey : notnull
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

            if (updater != null)
                result[key] = updater.Invoke(result[key], item);
        }

        return result.Values;
    }

    #endregion
}
