#!/usr/bin/env bash
# Builds docker images

version="0.1.0"
imageName="akka.sqlserver"

if [ -z $1 ]; then
	echo "No tag for [${imageName}] specified. Defaulting to [${version}]"
else
	version="$1"
	echo "Building [${imageName}] with tag [${version}]"
fi

docker build . -t "${imageName}:${version}"