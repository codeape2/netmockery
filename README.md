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
docker run --rm -v ${PWD}\:/mnt/repo/ -w /mnt/repo mcr.microsoft.com/dotnet/sdk:6.0 dotnet test .\netmockery.sln
```

RUNNING
====================
```
dotnet run --project .\netmockery\netmockery.csproj --command web --endpoints C:\Projects\NHN\Helsenorge\Test\netmockery_config\helsenorge_endpoints --urls http://*:9876
```


DEPLOYMENT PACKAGES
===================
```
dotnet publish .\netmockery.sln
```