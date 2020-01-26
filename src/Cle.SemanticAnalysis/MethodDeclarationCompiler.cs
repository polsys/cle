using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            if (!TypeResolver.TryResolve(syntax.ReturnType, diagnosticSink, syntax.ReturnType.Position,
                out var returnType))
            {
                return null;
            }
            Debug.Assert(returnType != null);

            // Resolve parameter types
            // The parameter names are checked in InternalCompile()
            var parameterTypes = ImmutableList<TypeDefinition>.Empty;
            foreach (var param in syntax.Parameters)
            {
                if (!TypeResolver.TryResolve(param.Type, diagnosticSink, param.Position, out var paramType))
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
            // Each attribute can be validated independently, so don't stop on first failure
            var isEntryPoint = false;
            var isValid = true;
            byte[]? importName = null;
            byte[]? importLibrary = null;
            foreach (var attribute in syntax.Attributes)
            {
                switch (attribute.Name)
                {
                    case "EntryPoint":
                        isValid &= ValidateEntryPoint(syntax, diagnosticSink, returnType, parameterTypes);
                        isEntryPoint = true;
                        break;
                    case "Import":
                        isValid &= TryParseImportAttribute(attribute, diagnosticSink, out importName, out importLibrary);
                        break;
                    default:
                        diagnosticSink.Add(DiagnosticCode.UnknownAttribute, attribute.Position, attribute.Name);
                        isValid = false;
                        break;
                }
            }

            if (!isValid)
            {
                return null;
            }

            // Some attributes may not be applied to the same method
            if (isEntryPoint && importName != null)
            {
                diagnosticSink.Add(DiagnosticCode.EntryPointAndImportNotCompatible, syntax.Position);
                return null;
            }

            // Return a suitable subtype of the MethodDeclaration class
            if (importName != null)
            {
                Debug.Assert(importLibrary != null);

                return new ImportedMethodDeclaration(methodBodyIndex, returnType, parameterTypes, syntax.Visibility,
                    definingNamespace + "::" + syntax.Name, definingFilename, syntax.Position, importName, importLibrary);
            }
            else
            {
                return new NativeMethodDeclaration(methodBodyIndex, returnType, parameterTypes, syntax.Visibility,
                    definingNamespace + "::" + syntax.Name, definingFilename, syntax.Position, isEntryPoint);
            }
        }

        private static bool ValidateEntryPoint(FunctionSyntax syntax, IDiagnosticSink diagnosticSink,
            TypeDefinition returnType, ImmutableList<TypeDefinition> parameterTypes)
        {
            // The method must return int32 and take no parameters
            if (returnType.Equals(SimpleType.Int32) && parameterTypes.Count == 0)
            {
                return true;
            }

            diagnosticSink.Add(DiagnosticCode.EntryPointMustBeDeclaredCorrectly, syntax.Position);
            return false;
        }

        private static bool TryParseImportAttribute(AttributeSyntax attribute, IDiagnosticSink diagnosticSink,
            [NotNullWhen(true)] out byte[]? nameBytes, [NotNullWhen(true)] out byte[]? libraryBytes)
        {
            nameBytes = libraryBytes = default;
            var isValid = true;

            // There must be exactly two string parameters: name and library
            if (attribute.Parameters.Count != 2)
            {
                diagnosticSink.Add(DiagnosticCode.ParameterCountMismatch, attribute.Position,
                    attribute.Parameters.Count.ToString(), "2");
                return false;
            }

            if (attribute.Parameters[0] is StringLiteralSyntax nameLiteral &&
                IsValidImportParameter(nameLiteral))
            {
                nameBytes = nameLiteral.Value;
            }
            else
            {
                diagnosticSink.Add(DiagnosticCode.ImportParameterNotValid, attribute.Parameters[0].Position);
                isValid = false;
            }

            if (attribute.Parameters[1] is StringLiteralSyntax libraryLiteral &&
                IsValidImportParameter(libraryLiteral))
            {
                libraryBytes = libraryLiteral.Value;
            }
            else
            {
                diagnosticSink.Add(DiagnosticCode.ImportParameterNotValid, attribute.Parameters[1].Position);
                isValid = false;
            }

            return isValid;
        }

        private static bool IsValidImportParameter(StringLiteralSyntax parameterLiteral)
        {
            var bytes = parameterLiteral.Value;

            // The string literal must be non-empty...
            if (bytes.Length == 0)
                return false;

            // ...and contain only ASCII characters.
            foreach (var b in bytes)
            {
                if ((b & 0b_1000_0000) != 0)
                    return false;
            }

            return true;
        }
    }
}
