
using System.Collections.Concurrent;
using Android.Text.Method;

namespace maui_memory;

internal static class Extensions
{
    public static KeyValuePair<TKey, TValue> Pop<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> collection, int position)
        where TKey : notnull
        where TValue : notnull
    {
        if (position >= collection.Count)
        {
            throw new IndexOutOfRangeException();
        }

        KeyValuePair<TKey, TValue> item = collection.ElementAt(position);
        while (collection.TryRemove(item));
        return item;
    }
}