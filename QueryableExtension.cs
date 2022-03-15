namespace Netcorext.Extensions.Linq;

public static class QueryableExtension
{
    public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TOuter, TInner> defaultValue = null)
    {
        return from o in outer
               join i in inner on outerKeySelector(o) equals innerKeySelector(i) into g
               from i in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(o))
               select resultSelector(o, i);
    }

    public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IQueryable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TOuter, TInner> defaultValue = null)
    {
        return from o in outer
               join i in inner on outerKeySelector(o) equals innerKeySelector(i) into g
               from i in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(o))
               select resultSelector(o, i);
    }

    public static IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TInner, TOuter> defaultValue = null)
    {
        return from i in inner
               join o in outer on innerKeySelector(i) equals outerKeySelector(o) into g
               from o in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(i))
               select resultSelector(o, i);
    }

    public static IQueryable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IQueryable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TInner, TOuter> defaultValue = null)
    {
        return from i in inner
               join o in outer on innerKeySelector(i) equals outerKeySelector(o) into g
               from o in g.DefaultIfEmpty(defaultValue == null ? default : defaultValue.Invoke(i))
               select resultSelector(o, i);
    }
}