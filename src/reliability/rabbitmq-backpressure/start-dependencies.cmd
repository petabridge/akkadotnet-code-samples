@echo off
REM launches local RabbitMQ with management plugin enabled so we can run from Visual Studio

set version="3.9-management"
set imageName="rabbitmq"

if "%~1"=="" (
	REM No version tag specified
	echo No tag for [%imageName%] specified. Defaulting to [%version%]
) else (
	set version="%~1"
	echo Running [%imageName%] with tag [%~1]
)

docker run --name "akkastreams-rabbit" -p 5672:5672 -p 5671:5671 -p 5552:5552 -p 5551:5551 -p 15672:15672 -p 15671:15671 -d "%imageName%:%version%"