using System;
using System.Collections;
using System.Collections.Generic;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Collections
{
    /// <summary>
    /// Список с заменой диапазона (используется <see cref="NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.Export.BufferedWriter{T}"/>).
    /// </summary>
    public sealed class StreamCollection<T> : IList<T>
    {
        private readonly List<T> _items = new List<T>();

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(T item) => _items.Add(item);

        public void Clear() => _items.Clear();

        public bool Contains(T item) => _items.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(T item) => _items.IndexOf(item);

        public void Insert(int index, T item) => _items.Insert(index, item);

        public bool Remove(T item) => _items.Remove(item);

        public void RemoveAt(int index) => _items.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Replace(int startIndex, T[] buffer, int bufferOffset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (startIndex < 0 || count < 0 || bufferOffset < 0
                || startIndex + count > _items.Count
                || bufferOffset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                _items[startIndex + i] = buffer[bufferOffset + i];
            }
        }
    }
}
