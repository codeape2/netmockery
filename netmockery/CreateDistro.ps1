#
# CreateDistro.ps1
#
# Prereq: Built debug config

$ErrorActionPreference = "Stop"

mkdir .\_distro

copy .\bin\Debug\net461\win7-x64\* .\_distro
copy -Recurse .\Views .\_distro
copy -Recurse .\wwwroot .\_distro
copy .\documentation.md .\_distro