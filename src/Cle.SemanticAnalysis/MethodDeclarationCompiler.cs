using System.Collections.Immutable;
using System.Diagnostics;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Provides a static method for compiling method declarations.
    /// </summary>
    public static class MethodDeclarationCompiler
    {
        /// <summary>
        /// Verifies and creates type information for the method.
        /// Returns null if this fails, in which case diagnostics are also emitted.
        /// The name is not checked for duplication in this method.
        /// </summary>
        /// <param name="syntax">The syntax tree for the method.</param>
        /// <param name="definingNamespace">The name of the namespace this method is in.</param>
        /// <param name="definingFilename">The name of the file that contains the method.</param>
        /// <param name="methodBodyIndex">The index associated with the compiled method body.</param>
        /// <param name="declarationProvider">The type provider to use for resolving custom types.</param>
        /// <param name="diagnosticSink">The receiver for any semantic errors or warnings.</param>
        public static MethodDeclaration? Compile(
            FunctionSyntax syntax,
            string definingNamespace,
            string definingFilename,
            int methodBodyIndex,
            IDeclarationProvider declarationProvider,
            IDiagnosticSink diagnosticSink)
        {
            // Resolve the return type
            if (!TypeResolver.TryResolve(syntax.ReturnTypeName, diagnosticSink, syntax.Position, out var returnType))
            {
                return null;
            }
            Debug.Assert(returnType != null);

            // Resolve parameter types
            // The parameter names are checked in InternalCompile()
            var parameterTypes = ImmutableList<TypeDefinition>.Empty;
            foreach (var param in syntax.Parameters)
            {
                if (!TypeResolver.TryResolve(param.TypeName, diagnosticSink, param.Position, out var paramType))
                {
                    return null;
                }
                Debug.Assert(paramType != null);
                if (paramType.Equals(SimpleType.Void))
                {
                    diagnosticSink.Add(DiagnosticCode.VoidIsNotValidType, param.Position, param.Name);
                    return null;
                }
                parameterTypes = parameterTypes.Add(paramType);
            }

            // Apply the attributes
            var isEntryPoint = false;
            foreach (var attribute in syntax.Attributes)
            {
                if (attribute.Name == "EntryPoint")
                {
                    // Check that the method returns int32 and has no parameters
                    if (!returnType.Equals(SimpleType.Int32) || parameterTypes.Count > 0)
                    {
                        diagnosticSink.Add(DiagnosticCode.EntryPointMustBeDeclaredCorrectly, syntax.Position);
                        return null;
                    }

                    isEntryPoint = true;
                }
                else
                {
                    diagnosticSink.Add(DiagnosticCode.UnknownAttribute, attribute.Position, attribute.Name);
                    return null;
                }
            }

            return new MethodDeclaration(methodBodyIndex, returnType, parameterTypes, syntax.Visibility,
                definingNamespace + "::" + syntax.Name, definingFilename, syntax.Position, isEntryPoint);
        }
    }
}
