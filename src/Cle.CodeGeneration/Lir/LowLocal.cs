using System;
using Cle.Common.TypeSystem;

namespace Cle.CodeGeneration.Lir
{
    internal class LowLocal<TRegister> 
        where TRegister : struct, Enum
    {
        public readonly TypeDefinition Type;

        public StorageLocation<TRegister> Location;

        public LowLocal(TypeDefinition type)
        {
            Type = type;
        }
    }
}
