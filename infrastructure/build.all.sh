#!/usr/bin/env bash
# executes all `build.sh` scripts recursively inside from this directory on down
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

for file in ./**/build.sh; do
	./${file} $1
done