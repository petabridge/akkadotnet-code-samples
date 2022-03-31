#!/usr/bin/env bash
# executes all `build.sh` scripts recursively inside from this directory on down
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

for file in ./**/build.sh; do
	./${file}
done