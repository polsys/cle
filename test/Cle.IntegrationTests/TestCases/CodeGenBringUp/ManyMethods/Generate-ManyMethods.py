# The file header
print("// Generated by Generate-ManyMethods.py")
print("// This test verifies that a long chain of method calls is handled correctly.")
print("// Additionally, the methods span more than one memory page, uncovering file layout issues.")
print("// ")
print("// Expected return code: 999")
print("// No console output expected.")
print()
print("namespace ManyMethods;")
print()

# An imported method
print("// Known to return -1 (we should not depend on this, though...)")
print("[Import(\"GetCurrentProcess\", \"Kernel32.dll\")]")
print("private int32 GetCurrentProcess();")
print()

# The entry point
print("[EntryPoint]")
print("private int32 Main()")
print("{")
print("    return Method1();")
print("}")
print()

# A lot of methods
for i in range(1, 1001):
    print("private int32 Method", i, "() { return Method", i+1, "() + 1; }", sep="")

# The final method
print()
print("private int32 Method1001()")
print("{")
print("    return GetCurrentProcess();")
print("}")
print()
