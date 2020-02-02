using System.Diagnostics.CodeAnalysis;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Logic for resolving a <see cref="TypeDefinition"/> from a type name.
    /// </summary>
    internal static class TypeResolver
    {
        /// <summary>
        /// Tries to resolve the given type name.
        /// </summary>
        /// <param name="type">The simple or full type name.</param>
        /// <param name="diagnostics">A diagnostics sink for resolution errors.</param>
        /// <param name="resolvedType">If this method returns true, the resolved type.</param>
        public static bool TryResolve(TypeSyntax type, IDiagnosticSink diagnostics, 
            [NotNullWhen(true)] out TypeDefinition? resolvedType)
        {
            // TODO: Proper type resolution with a declaration provider
            switch (((TypeNameSyntax)type).TypeName)
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
                    diagnostics.Add(DiagnosticCode.TypeNotFound, type.Position, type.ToString());
                    resolvedType = null;
                    return false;
            }

            return true;
        }
    }
}
