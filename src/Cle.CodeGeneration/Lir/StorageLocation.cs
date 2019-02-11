using System;

namespace Cle.CodeGeneration.Lir
{
    /// <summary>
    /// A storage location for a local: either in a register or on stack.
    /// </summary>
    internal readonly struct StorageLocation<TRegister>
        where TRegister : struct, Enum
    {

        private readonly int _stackOffset; // Stored with offset 1 to distinguish the uninitialized case

        public bool IsRegister => !Register.Equals((TRegister)default);
        public bool IsStack => _stackOffset > 0;

        public TRegister Register { get; }
        public int StackOffset => _stackOffset - 1;

        public StorageLocation(TRegister register)
        {
            _stackOffset = 0;
            Register = register;
        }

        public StorageLocation(int stackOffset)
        {
            _stackOffset = stackOffset + 1;
            Register = default;
        }

        public override string ToString()
        {
            if (IsRegister)
            {
                return Register.ToString().ToLowerInvariant();
            }
            else if (IsStack)
            {
                return StackOffset.ToString();
            }
            else
            {
                return "?";
            }
        }

        public bool Equals(StorageLocation<TRegister> other)
        {
            return _stackOffset == other._stackOffset && Register.Equals(other.Register);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is StorageLocation<TRegister> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_stackOffset * 397) ^ Register.GetHashCode();
            }
        }

        public static bool operator ==(StorageLocation<TRegister> left, StorageLocation<TRegister> right)
        {
            // This does not handle invalid locations with both stack and register set
            return left._stackOffset == right._stackOffset && left.Register.Equals(right.Register);
        }

        public static bool operator !=(StorageLocation<TRegister> left, StorageLocation<TRegister> right)
        {
            return !(left == right);
        }
    }
}
