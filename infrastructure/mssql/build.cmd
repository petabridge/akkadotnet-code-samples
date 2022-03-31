@echo off
REM builds all docker images

set version="0.1.0"
set imageName="akkadotnet.sqlserver"

if "%~1"=="" (
	REM No version tag specified
	echo No tag for [%imageName%] specified. Defaulting to [0.1.0]
) else (
	set version="%~1"
	echo Building [%imageName%] with tag [%version%]
)

docker build %~dp0/src/. -t "%imageName%:%version%"