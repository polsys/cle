using System;
using System.Collections.Generic;

namespace Cle.Compiler
{
    /// <summary>
    /// An automatically resizable array like <see cref="List{T}"/>, but an index may be reserved
    /// arbitrarily long before adding the associated value. Unassigned indices are not accessible.
    /// This class is not thread safe.
    /// </summary>
    /// <typeparam name="T">The type of the stored item. Must be a reference type.</typeparam>
    internal class IndexedRandomAccessStore<T> where T : class
    {
        private int _nextIndex;
        private T[] _array;

        /// <summary>
        /// Gets the number of reserved indices.
        /// </summary>
        public int Count => _nextIndex;

        public T this[int index]
        {
            get
            {
                // Fail if the element is not set
                if (_array?[index] is null)
                {
                    throw new IndexOutOfRangeException("The element is uninitialized");
                }
                return _array[index];
            }
            set
            {
                if (_array is null || _array.Length <= index)
                {
                    ResizeArray();
                }
                _array[index] = value;
            }
        }

        /// <summary>
        /// Gets an index that can be assigned to later.
        /// </summary>
        public int ReserveIndex()
        {
            _nextIndex++;
            return _nextIndex - 1;
        }

        private void ResizeArray()
        {
            // Resize the array to the current expected size
            var newArray = new T[_nextIndex];

            if (!(_array is null))
            {
                Array.Copy(_array, newArray, _array.Length);
            }

            _array = newArray;
        }
    }
}
