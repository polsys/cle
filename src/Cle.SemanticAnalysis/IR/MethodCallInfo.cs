namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// Descriptor for a method call.
    /// The Call opcode refers to a <see cref="MethodCallInfo"/> instance for call information.
    /// </summary>
    public class MethodCallInfo
    {
        /// <summary>
        /// Gets the body index of the called method.
        /// </summary>
        public int CalleeIndex { get; }

        /// <summary>
        /// Gets the local indices that are passed as parameters.
        /// </summary>
        public int[] ParameterIndices { get; }

        /// <summary>
        /// Gets the full name of the called method.
        /// This information is only for debugging purposes.
        /// </summary>
        public string CalleeFullName { get; }

        /// <summary>
        /// Gets the code generation implementation of the call.
        /// </summary>
        public MethodCallType CallType { get; }

        public MethodCallInfo(int calleeIndex, int[] parameterIndices, string calleeFullName, MethodCallType callType)
        {
            CalleeIndex = calleeIndex;
            ParameterIndices = parameterIndices;
            CalleeFullName = calleeFullName;
            CallType = callType;
        }
    }
}
