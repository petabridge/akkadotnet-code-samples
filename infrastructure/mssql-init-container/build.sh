#!/usr/bin/env bash
# Builds docker images
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

version="0.1.0"
imageName="akkadotnet.sqlchecker"



# To resolve relative path call issues
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

cd "$parent_path"

if [ -z $1 ]; then
	echo "No tag for [${imageName}] specified. Defaulting to [${version}]"
else
	version="$1"
	echo "Building [${imageName}] with tag [${version}]"
fi

dotnet publish ./src/Akka.SqlInitContainer/Akka.SqlInitContainer.csproj -c Release -p:Version=${version}

docker build ./src/Akka.SqlInitContainer/. -t "${imageName}:${version}"