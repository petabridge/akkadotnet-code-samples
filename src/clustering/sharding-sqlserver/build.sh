#!/usr/bin/env bash
# Builds docker images
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

version="0.1.0"
hostImageName="sqlsharding.sql.host"
webImageName="sqlsharding.web"

if [ -z $1 ]; then
	echo "No tag for [${hostImageName}] specified. Defaulting to [${version}]"
	echo "No tag for [${webImageName}] specified. Defaulting to [${version}]"
else
	version="$1"
	echo "Building [${hostImageName}] with tag [${version}]"
	echo "Building [${webImageName}] with tag [${version}]"
fi

dotnet publish ./SqlSharding.Sql.Host/SqlSharding.Sql.Host.csproj -c Release -p:Version=${version}

docker build ./SqlSharding.Sql.Host/. -t "${hostImageName}:${version}"

dotnet publish ./SqlSharding.WebApp/SqlSharding.WebApp.csproj -c Release -p:Version=${version}

docker build ./SqlSharding.WebApp/. -t "${webImageName}:${version}"