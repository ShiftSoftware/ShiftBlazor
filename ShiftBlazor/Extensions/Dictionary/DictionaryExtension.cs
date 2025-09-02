

namespace System.Collections.Generic;

public static class DictionaryExtension
{
    public static TValue? TryGet<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        dictionary.TryGetValue(key, out TValue? value);
        return value;
    }
}
