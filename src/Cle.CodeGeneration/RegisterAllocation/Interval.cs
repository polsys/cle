using System;

namespace Cle.CodeGeneration.RegisterAllocation
{
    /// <summary>
    /// For register allocator internal use only; the public interface is <see cref="AllocationInfo{TRegister}"/>.
    /// A MUTABLE CLASS representing a single live interval and its allocation decision.
    /// </summary>
    internal class Interval<TRegister> : IComparable<Interval<TRegister>>
        where TRegister : struct, Enum
    {
        public int Start = -1;
        public int End = -1;
        public int LocalIndex = -1;
        public TRegister Register;

        /// <summary>
        /// Updates the start and end positions.
        /// </summary>
        public void Use(int index)
        {
            Start = Start == -1 ? index : Math.Min(Start, index);
            End = Math.Max(End, index);
        }

        /// <summary>
        /// Extends the lifetime of this interval to include the given interval.
        /// </summary>
        public void MergeWith(Interval<TRegister> other)
        {
            Use(other.Start);
            Use(other.End);
        }

        public int CompareTo(Interval<TRegister> other)
        {
            return Start.CompareTo(other.Start);
        }

        public override string ToString()
        {
            return $"[{Start}, {End}) #{LocalIndex} in {Register}";
        }
    }
}
