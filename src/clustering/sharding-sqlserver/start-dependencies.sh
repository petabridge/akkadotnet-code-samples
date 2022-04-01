#!/usr/bin/env bash
# launches local SQL Server dependency so we can run from Visual Studio
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

version="0.1.0"
imageName="akkadotnet.sqlserver"

if [ -z $1 ]; then
	echo "No tag for [${imageName}] specified. Defaulting to [${version}]"
else
	version="$1"
	echo "Building [${imageName}] with tag [${version}]"
fi

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1533:1433 --name "sqlsharding-sql" -d "${imageName}:${version}"

if errorlevel 1 (
	echo "failed to start akkadotnet.sqlserver - building image first then retrying"
	call %~dp0../../../infrastructure/build.all.cmd
	if errorlevel 1 (
		echo "failed to build akkadotnet.sqlserver - aborting"
		exit -1
	) else (
		docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1533:1433 --name "sqlsharding-sql" -d "${imageName}:${version}"
	)
)