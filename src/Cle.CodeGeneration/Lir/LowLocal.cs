using System;
using Cle.Common.TypeSystem;

namespace Cle.CodeGeneration.Lir
{
    internal class LowLocal<TRegister> 
        where TRegister : struct, Enum
    {
        public readonly TypeDefinition Type;

        public readonly StorageLocation<TRegister> RequiredLocation;

        public LowLocal(TypeDefinition type, StorageLocation<TRegister> requiredLocation = default)
        {
            Type = type;
            RequiredLocation = requiredLocation;
        }
    }
}
