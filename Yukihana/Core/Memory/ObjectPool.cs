// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Memory;

public sealed class ObjectPool<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly T?[] _items;
    private int _count;

    public int Capacity => _items.Length;
    public int Count => _count;

    public ObjectPool(int capacity, Func<T> factory, Action<T>? reset = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(0, capacity, nameof(capacity));
        ArgumentNullException.ThrowIfNull(factory);

        _items = new T[capacity];
        _factory = factory;
        _reset = reset;
        _count = 0;
    }

    public T Rent()
    {
        if(_count == 0)
            return _factory();

        int idx = --_count;
        
        T obj = _items[idx]!;
        _items[idx] = null;

        return obj;
    }

    public void Return(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        _reset?.Invoke(obj);

        if(_count < _items.Length)
            _items[_count++] = obj;
    }

    public void Clear(bool resetItems = false)
    {
        if (resetItems && _reset is not null)
        {
            for (int i = 0; i < _count; i++)
                _reset(_items[i]!);
        }

        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    public void Prewarm(int count)
    {
        int toCreate = Math.Min(count, _items.Length - _count);

        for(int i = 0; i < toCreate; i++)
            _items[_count++] = _factory();
    }

    public bool TryRent(out T? value)
    {
        if (_count == 0)
        {
            value = null;
            return false;
        }

        int index = --_count;
        value = _items[index];
        _items[index] = null;
        return true;
    }
}