// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Core.Memory;

public sealed class RingBuffer<T>
{
    private readonly T[] _buffer;

    private int _head;
    private int _count;

    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _buffer = new T[capacity];
    }

    public int Capacity => _buffer.Length;

    public int Count => _count;

    public bool IsFull => _count == Capacity;

    public bool IsEmpty => _count == 0;

    public void Clear()
    {
        Array.Clear(_buffer);

        _head = _count = 0;
    }

    public void Add(T item)
    {
        if (_count < Capacity)
        {
            _buffer[(_head + _count) % Capacity] = item;
            _count++;
            return;
        }

        _buffer[_head] = item;
        _head = (_head + 1) % Capacity;
    }

    public bool TryRemoveOldest(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];

        _buffer[_head] = default!;

        _head = (_head + 1) % Capacity;
        _count--;

        return true;
    }

    public T Oldest
    {
        get
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Ring buffer is empty.");
            }

            return _buffer[_head];
        }
    }

    public T Newest
    {
        get
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Ring buffer is empty.");
            }

            return _buffer[(_head + _count - 1) % Capacity];
        }
    }

    public ref readonly T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);

            return ref _buffer[(_head + index) % Capacity];
        }
    }

    public int CopyTo(Span<T> destination)
    {
        int copied = Math.Min(destination.Length, _count);

        for (int i = 0; i < copied; i++)
        {
            destination[i] = _buffer[(_head + i) % Capacity];
        }

        return copied;
    }

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator
    {
        private readonly RingBuffer<T> _buffer;
        private int _index;

        internal Enumerator(RingBuffer<T> buffer)
        {
            _buffer = buffer;
            _index = -1;
        }

        public readonly ref readonly T Current
        {
            get => ref _buffer[_index];
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _buffer.Count;
        }
    }
}
