# cle - a simple C-like language

[![Build Status](https://dev.azure.com/polsys/cle/_apis/build/status/polsys.cle?branchName=master)](https://dev.azure.com/polsys/cle/_build/latest?definitionId=3?branchName=master)

Cle (stylized as Clé, for the French word) is a simple programming language inspired by C#, Rust and C/C++, compiled to optimized native code.
It is a hobby project of [@polsys](https://github.com/polsys), and as such not safe for production use but hopefully an interesting learning project.


## Building
This is a .NET Core 2.2 project. Install the SDK, then execute
```
dotnet build
```
to restore NuGet packages and compile the solution.
Alternatively, open the solution in up-to-date Visual Studio 2017 or later.

Unit test projects use NUnit and can be executed with `dotnet test` or in Visual Studio.


## Usage
The final compiler executable is produced in the output folder for `Cle.Frontend` project.
Execute
```
dotnet cle.dll [directory]
```
to compile files in the specified directory.
The default is to compile files in the current directory.
Specify the `--help` option for more information.

**NOTE:** The compiler does not yet produce executable programs. See [Milestone 0.1](https://github.com/polsys/cle/milestone/1) for the status of this work.


## Contributing
As this is a personal hobby project, I'm not really expecting contributions, but feel free to tinker with the code and share your work!
Once past the initial bringup, I'll work in the open using GitHub issues and PRs, both for fun and to make the design history visible.

