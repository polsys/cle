﻿using System;

namespace Cle.Common.TypeSystem
{
    /// <summary>
    /// Represents built-in value types.
    /// Use the static properties to access type instances.
    /// </summary>
    public class SimpleType : TypeDefinition, IEquatable<SimpleType?>
    {
        public static SimpleType Void { get; } = new SimpleType(SimpleTypeId.Void, false, 0);
        public static SimpleType Bool { get; } = new SimpleType(SimpleTypeId.Bool, false, 1);
        public static SimpleType Int32 { get; } = new SimpleType(SimpleTypeId.Int32, true, 4);
        public static SimpleType UInt32 { get; } = new SimpleType(SimpleTypeId.UInt32, true, 4);

        public bool IsInteger { get; }

        private readonly SimpleTypeId _typeId;

        private SimpleType(SimpleTypeId id, bool isInteger, int size)
        {
            _typeId = id;
            IsInteger = isInteger;
            SizeInBytes = size;
        }

        public bool Equals(SimpleType? other)
        {
            return _typeId == other?._typeId;
        }

        public override string TypeName
        {
            get
            {
                switch (_typeId)
                {
                    case SimpleTypeId.Bool: return "bool";
                    case SimpleTypeId.Int32: return "int32";
                    case SimpleTypeId.UInt32: return "uint32";
                    case SimpleTypeId.Void: return "void";
                    default: throw new NotImplementedException("Unimplemented simple type");
                }
            }
        }

        public override int SizeInBytes { get; }

        public override bool Equals(TypeDefinition? other)
        {
            return other is SimpleType simpleType && Equals(simpleType);
        }

        public override bool Equals(object? obj)
        {
            return obj is SimpleType simpleType && Equals(simpleType);
        }

        public override int GetHashCode()
        {
            return (int)_typeId;
        }

        private enum SimpleTypeId
        {
            // TODO: Add the remaining cases
            Void,
            Bool,
            Int32,
            UInt32
        }
    }
}
