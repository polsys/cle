using System.Diagnostics.CodeAnalysis;
using Cle.Common;
using Cle.Common.TypeSystem;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Logic for resolving a <see cref="TypeDefinition"/> from a type name.
    /// </summary>
    internal class TypeResolver
    {
        /// <summary>
        /// Tries to resolve the given type name.
        /// </summary>
        /// <param name="typeName">The simple or full type name.</param>
        /// <param name="diagnostics">A diagnostics sink for resolution errors.</param>
        /// <param name="position">The source position of the type name, used for diagnostics.</param>
        /// <param name="resolvedType">If this method returns true, the resolved type.</param>
        public static bool TryResolve(string typeName, IDiagnosticSink diagnostics, TextPosition position, 
            [NotNullWhen(true)] out TypeDefinition? resolvedType)
        {
            // TODO: Proper type resolution with a declaration provider
            switch (typeName)
            {
                case "bool":
                    resolvedType = SimpleType.Bool;
                    break;
                case "int32":
                    resolvedType = SimpleType.Int32;
                    break;
                case "void":
                    resolvedType = SimpleType.Void;
                    break;
                default:
                    diagnostics.Add(DiagnosticCode.TypeNotFound, position, typeName);
                    resolvedType = null;
                    return false;
            }

            return true;
        }
    }
}
