using System;
using System.Collections.Generic;
using System.IO;
using Cle.CodeGeneration.Lir;
using JetBrains.Annotations;

namespace Cle.CodeGeneration.RegisterAllocation
{
    /// <summary>
    /// Contains the allocation decisions for local variables as a map from allocator-provided
    /// indices to local indices and locations.
    /// </summary>
    internal class AllocationInfo<TRegister>
        where TRegister : struct, Enum
    {
        public int IntervalCount => _intervals.Count;

        [NotNull, ItemNotNull]
        private readonly List<Interval<TRegister>> _intervals;

        /// <param name="intervals">The list must match the value numbers in low instructions.</param>
        internal AllocationInfo([NotNull, ItemNotNull] List<Interval<TRegister>> intervals)
        {
            _intervals = intervals;
        }

        /// <summary>
        /// Gets the storage location and local variable index for the given value number generated
        /// by the register allocator. The local index may be zero if the value does not map to a local.
        /// </summary>
        /// <param name="index">The value index set by the register allocator.</param>
        public (StorageLocation<TRegister> location, int localIndex) Get(int index)
        {
            var interval = _intervals[index];
            return (new StorageLocation<TRegister>(interval.Register), interval.LocalIndex);
        }

        internal void Dump([NotNull] TextWriter dumpWriter)
        {
            foreach (var interval in _intervals)
            {
                dumpWriter.WriteLine("; " + interval);
            }
        }
    }
}
