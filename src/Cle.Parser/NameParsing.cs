using System;
using System.Runtime.CompilerServices;

namespace Cle.Parser
{
    /// <summary>
    /// Provides static methods for parsing and validating various kinds of names according to Cle rules.
    /// </summary>
    public static class NameParsing
    {
        /// <summary>
        /// Returns whether the given namespace name, possibly composed of multiple parts, is valid.
        /// </summary>
        /// <param name="name">The namespace name to check as an UTF-16 string.</param>
        public static bool IsValidNamespaceName(string name)
        {
            // Namespace names must conform to rules for other identifiers, so
            // this check is equivalent to validating a fully qualified name.
            return IsValidFullName(name.AsSpan());
        }

        private static int FindNextNamespaceSplitPoint(ReadOnlySpan<char> nameSlice)
        {
            for (var i = 0; i < nameSlice.Length - 1; i++)
            {
                if (nameSlice[i] == ':' && nameSlice[i + 1] == ':')
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns whether the given fully qualified (or unqualified) name is valid.
        /// </summary>
        /// <param name="name">The name to check as an UTF-16 string.</param>
        public static bool IsValidFullName(string name)
        {
            return IsValidFullName(name.AsSpan());
        }

        /// <summary>
        /// Returns whether the given fully qualified (or unqualified) name is valid.
        /// </summary>
        /// <param name="name">The name to check as a span of UTF-16 characters.</param>
        public static bool IsValidFullName(ReadOnlySpan<char> name)
        {
            if (name.Length == 0)
                return false;

            // Split name into parts separated by ::, then check each part separately
            var currentIndex = 0;
            while (currentIndex < name.Length)
            {
                // Get the index of next :: relative to current position
                var nextSplitPoint = FindNextNamespaceSplitPoint(name.Slice(currentIndex));

                // If there are no more ::'s, validate the last part
                if (nextSplitPoint == -1)
                {
                    return IsValidSimpleName(name.Slice(currentIndex));
                }

                // If :: follows the previous :: immediately, the name is invalid
                if (nextSplitPoint == 0)
                {
                    return false;
                }

                // Else, we have a well-defined part between two ::'s and can validate it
                if (!IsValidSimpleName(name.Slice(currentIndex, nextSplitPoint)))
                    return false;
                
                // Move past the just-validated part and the :: following it
                currentIndex += nextSplitPoint + 2;

                // If we're now at name end, the :: following the part turned out to be
                // a trailing ::, which is not allowed
                if (currentIndex == name.Length)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns whether the given simple (i.e. not fully qualified or compound) name is valid.
        /// The name is not checked for collision with language keywords, as this check is assumed
        /// to be done by the lexer.
        /// </summary>
        /// <param name="name">The simple name to check as an UTF-16 string.</param>
        public static bool IsValidSimpleName(string name)
        {
            return IsValidSimpleName(name.AsSpan());
        }

        /// <summary>
        /// Returns whether the given simple (i.e. not fully qualified or compound) name is valid.
        /// The name is not checked for collision with language keywords, as this check is assumed
        /// to be done by the lexer.
        /// </summary>
        /// <param name="name">The simple name to check as a span of UTF-16 characters.</param>
        public static bool IsValidSimpleName(ReadOnlySpan<char> name)
        {
            if (name.Length == 0 || name.SequenceEqual("_".AsSpan()))
                return false;

            // First char must be _ or alphabetic
            if (!IsAlphabeticOrUnderscore(name[0]))
                return false;

            // The remaining chars may also be decimal digits
            foreach (var ch in name.Slice(1))
            {
                if (!IsAlphabeticOrUnderscore(ch) && !IsDigit(ch))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether the given name is equal to a reserved type name.
        /// </summary>
        /// <param name="name">The name to check. This may be a simple or a fully qualified name.</param>
        public static bool IsReservedTypeName(string name)
        {
            // TODO: More types once they exist (consider matching on the initial part of the name for [u]int[NN])
            return name == "bool" || name == "int32" || name == "void";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAlphabeticOrUnderscore(char ch)
        {
            // Performance trick to reduce the number of comparisons:
            // ASCII letters can be lowercased by setting a single bit,
            // and the remaining chars will fail the test regardless
            var chInLower = ch | 0x20;
            return chInLower >= 'a' && chInLower <= 'z' || ch == '_';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }
    }
}
