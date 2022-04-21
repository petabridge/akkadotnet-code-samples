#!/usr/bin/env bash
# launches local SQL Server dependency so we can run from Visual Studio
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

version="3.9-management"
imageName="rabbitmq"

if [ -z $1 ]; then
	echo "No tag for [${imageName}] specified. Defaulting to [${version}]"
else
	version="$1"
	echo "Building [${imageName}] with tag [${version}]"
fi

docker run --name "akkastreams-rabbit" -p 5672:5672 -p 5671:5671 -p 5552:5552 -p 5551:5551 -p 15672:15672 -p 15671:15671 -d "%imageName%:%version%"