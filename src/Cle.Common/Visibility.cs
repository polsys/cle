namespace Cle.Common
{
    /// <summary>
    /// Represents the visibility class of a function, type or member.
    /// </summary>
    public enum Visibility
    {
        /// <summary>
        /// The visibility is unknown, for example, the visibility modifier is missing.
        /// </summary>
        Unknown,
        /// <summary>
        /// The referenced object is only visible within the defining source file.
        /// </summary>
        Private,
        /// <summary>
        /// The referenced object is visible within the defining module.
        /// </summary>
        Internal,
        /// <summary>
        /// The referenced object is visible in all modules that import the defining module.
        /// </summary>
        Public
    }
}
