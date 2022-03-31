#!/usr/bin/env bash
# Builds docker images
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

version="0.1.0"
imageName="akkadotnet.sqlserver"

if [ -z $1 ]; then
	echo "No tag for [${imageName}] specified. Defaulting to [${version}]"
else
	version="$1"
	echo "Building [${imageName}] with tag [${version}]"
fi

docker build ./src/. -t "${imageName}:${version}"