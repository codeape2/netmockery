Netmockery is a simple system for simulating network services.

The [end user documentation](netmockery/documentation.md) is a work in progress.


BUILDING AND TESTING
====================

.NET Core:

```
cd UnitTests
dotnet restore -r netcoreapp1.1
dotnet test -f netcoreapp1.1
```

.NET Framework 4.6.2:

```
cd UnitTests
dotnet restore -r net462
dotnet test -f net462
```
