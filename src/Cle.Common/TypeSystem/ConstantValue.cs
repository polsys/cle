﻿using System;

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
        /// <see cref="Type"/> is not checked.
        /// </summary>
        public bool AsBool => _data == 1;

        /// <summary>
        /// Gets the value of this constant as a signed integer.
        /// <see cref="Type"/> is not checked.
        /// </summary>
        public long AsSignedInteger => (long)_data;

        /// <summary>
        /// Gets the value of this constant as an unsigned integer.
        /// <see cref="Type"/> is not checked.
        /// </summary>
        public ulong AsUnsignedInteger => _data;

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
        /// Creates a new void/uninitialized constant that is not a method parameter.
        /// </summary>
        public static ConstantValue Void()
        {
            return new ConstantValue(ConstantType.Void, 0);
        }

        /// <summary>
        /// Creates a new constant that signifies a method parameter.
        /// </summary>
        public static ConstantValue Parameter()
        {
            return new ConstantValue(ConstantType.Parameter, 0);
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
                case ConstantType.Parameter:
                    return "param";
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
        Parameter,
        Boolean,
        SignedInteger
    }
}
