@echo off
REM builds all docker images

set version="0.1.0"
set imageName="akkadotnet.sqlchecker"

if "%~1"=="" (
	REM No version tag specified
	echo No tag for [%imageName%] specified. Defaulting to [%version%]
) else (
	set version="%~1"
	echo Building [%imageName%] with tag [%~1]
)

dotnet publish %~dp0/src/Akka.SqlInitContainer/Akka.SqlInitContainer.csproj -c Release -p:Version=%version%

docker build %~dp0/src/Akka.SqlInitContainer/. -t "%imageName%:%version%"