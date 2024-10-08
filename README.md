Netmockery is a simple system for simulating network services.

The [end user documentation](netmockery/documentation.md) is a work in progress.

[![Build Status](https://travis-ci.org/codeape2/netmockery.svg?branch=master)](https://travis-ci.org/codeape2/netmockery)


TESTING
====================

On local machine
```
dotnet test .\netmockery.sln
```

In linux container
```
docker run --rm -v ${PWD}\:/mnt/repo/ -w /mnt/repo mcr.microsoft.com/dotnet/sdk:8.0 dotnet test .\netmockery.sln
```

RUNNING
====================

Run from code with watch
```
dotnet watch run --project .\netmockery\netmockery.csproj web --endpoints [YourEndpointFilesPath] --urls http://localhost:9876
```

Run published .dll
```
dotnet publish .\netmockery.sln

dotnet .\netmockery\bin\Debug\net6.0\publish\netmockery.dll web --endpoints [YourEndpointFilesPath] --urls http://localhost:9876
```

Run published .exe
```
dotnet publish .\netmockery.sln

.\netmockery\bin\Debug\net6.0\publish\netmockery.exe web --endpoints [YourEndpointFilesPath] --urls http://localhost:9876
```


DEPLOYMENT PACKAGES
===================
```
dotnet publish .\netmockery.sln
```