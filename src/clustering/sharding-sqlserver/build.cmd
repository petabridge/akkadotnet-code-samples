@echo off
REM builds all docker images

set version="0.1.0"
set hostImageName="sqlsharding.host"
set webImageName="sqlsharding.web"

if "%~1"=="" (
	REM No version tag specified
	echo No tag for [%hostImageName%] specified. Defaulting to [%version%]
	echo No tag for [%webImageName%] specified. Defaulting to [%version%]
) else (
	set version="%~1"
	echo Building [%hostImageName%] with tag [%version%]
	echo Building [%webImageName%] with tag [%version%]
)

dotnet publish %~dp0/SqlSharding.Host/SqlSharding.Host.csproj -c Release -p:Version=%version%

docker build %~dp0/SqlSharding.Host/. -t "%hostImageName%:%version%"

dotnet publish %~dp0/SqlSharding.WebApp/SqlSharding.WebApp.csproj -c Release -p:Version=%version%

docker build %~dp0/SqlSharding.WebApp/. -t "%webImageName%:%version%"