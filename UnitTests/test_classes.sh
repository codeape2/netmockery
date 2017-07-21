#! /bin/bash

sed '/\sUnitTests\./!d;s/UnitTests\.//;s/\..*$//' testnames.txt | uniq | xargs -I TESTNAME echo dotnet test --filter \"DisplayName~TESTNAME\"
