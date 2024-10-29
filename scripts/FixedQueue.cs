using System;
using System.Collections;
using System.Collections.Generic;

public class FixedQueue<T> : IEnumerable<T>
{
    private T[] _queueArray;
    private int _front;
    private int _rear;
    private int _count;
    private int _maxSize;

    public FixedQueue(int maxSize)
    {
        _maxSize = maxSize;
        _queueArray = new T[_maxSize];
        Clear();
    }

    public void Clear()
    {
        _front = 0;
        _rear = -1;
        _count = 0;
    }

    public void Enqueue(T item)
    {
        if (_count == _maxSize)
        {
            // Pop (remove) the front item to make room for the new one
            _front = (_front + 1) % _maxSize;
            _count--;  // Decrement the count since we're removing the front item
        }

        _rear = (_rear + 1) % _maxSize; // Circular queue logic
        _queueArray[_rear] = item;
        _count++;
    }

    public T Peek()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Queue is empty");
        }

        return _queueArray[_front];
    }
    public T Get(int position)
    {
        if (position < 0 || position >= _count)
        {
            // If type T is numeric, return NaN, otherwise return default value (null for reference types).
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.NaN;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.NaN;
            }
            return default(T); // null for reference types, default(T) for value types
        }

        int actualIndex = (_front + position) % _maxSize;
        return _queueArray[actualIndex];
    }

    public int Count()
    {
        return _count;
    }

    // Implementing IEnumerable<T> to make the queue iterable
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            int index = (_front + i) % _maxSize;
            yield return _queueArray[index];
        }
    }

    // Explicit non-generic IEnumerable implementation (required for IEnumerable)
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
