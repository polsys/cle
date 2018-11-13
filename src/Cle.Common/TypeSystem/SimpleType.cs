using System;

namespace Cle.Common.TypeSystem
{
    /// <summary>
    /// Represents built-in value types.
    /// Use the static properties to access type instances.
    /// </summary>
    public class SimpleType : TypeDefinition, IEquatable<SimpleType>
    {
        public static SimpleType Void { get; } = new SimpleType(SimpleTypeId.Void);
        public static SimpleType Bool { get; } = new SimpleType(SimpleTypeId.Bool);
        public static SimpleType Int32 { get; } = new SimpleType(SimpleTypeId.Int32);

        private readonly SimpleTypeId _typeId;

        private SimpleType(SimpleTypeId id)
        {
            _typeId = id;
        }

        public bool Equals(SimpleType other)
        {
            return _typeId == other?._typeId;
        }

        public override bool Equals(TypeDefinition other)
        {
            return other is SimpleType simpleType && Equals(simpleType);
        }

        public override bool Equals(object obj)
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
            Int32
        }
    }
}
