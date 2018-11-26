using System;

namespace Cle.Common.TypeSystem
{
    /// <summary>
    /// A constant value of a Cle simple type.
    /// Integer types are always stored as 64 bits wide.
    /// Use associated static methods to create instances.
    /// </summary>
    public readonly struct ConstantValue : IEquatable<ConstantValue>
    {
        /// <summary>
        /// Gets the data type of this constant.
        /// </summary>
        public readonly ConstantType Type;

        /// <summary>
        /// The bit pattern of the constant, stored in 64 bits.
        /// </summary>
        private readonly ulong _data;

        /// <summary>
        /// Gets the value of this constant as a boolean.
        /// Throws if <see cref="Type"/> is not equal to <see cref="ConstantType.Boolean"/>.
        /// </summary>
        public bool AsBool => ReturnAssertingType(ConstantType.Boolean, _data == 1);

        /// <summary>
        /// Gets the value of this constant as a signed integer.
        /// Throws if <see cref="Type"/> is not equal to <see cref="ConstantType.SignedInteger"/>.
        /// </summary>
        public int AsSignedInteger => ReturnAssertingType(ConstantType.SignedInteger, (int)_data);

        private T ReturnAssertingType<T>(ConstantType expectedType, T value)
        {
            if (Type != expectedType)
                throw new InvalidOperationException("Type mismatch");
            return value;
        }

        /// <summary>
        /// Creates a new boolean constant with the specified value.
        /// </summary>
        public static ConstantValue Bool(bool value)
        {
            return new ConstantValue(ConstantType.Boolean, value ? 1ul : 0ul);
        }

        /// <summary>
        /// Creates a new signed integer constant with the specified value.
        /// </summary>
        public static ConstantValue SignedInteger(long value)
        {
            return new ConstantValue(ConstantType.SignedInteger, (ulong)value);
        }

        /// <summary>
        /// Creates a new void constant.
        /// </summary>
        public static ConstantValue Void()
        {
            return new ConstantValue(ConstantType.Void, 0);
        }

        private ConstantValue(ConstantType type, ulong data)
        {
            Type = type;
            _data = data;
        }
        
        public override bool Equals(object obj)
        {
            return obj is ConstantValue value && Equals(value);
        }

        public bool Equals(ConstantValue other)
        {
            return Type == other.Type && _data == other._data;
        }

        public override int GetHashCode()
        {
            var hashCode = 547019363;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + _data.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ConstantType.Void:
                    return "void";
                case ConstantType.Boolean:
                    return _data == 1 ? "true" : "false";
                case ConstantType.SignedInteger:
                    return ((long)_data).ToString();
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool operator ==(ConstantValue value1, ConstantValue value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(ConstantValue value1, ConstantValue value2)
        {
            return !(value1 == value2);
        }
    }

    /// <summary>
    /// Possible types of constant values.
    /// These match Cle simple types but the width of integers is not considered.
    /// </summary>
    public enum ConstantType
    {
        Invalid,
        Void,
        Boolean,
        SignedInteger
    }
}
