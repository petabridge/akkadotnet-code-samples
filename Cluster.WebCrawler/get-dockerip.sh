#!/bin/sh
if [ -z "$CLUSTER_IP"]; then
	host=$(hostname -i)
	echo "Docker container bound on $host"
	export CLUSTER_IP="$host"
else
	echo "Docker container bound on $CLUSTER_IP"
fi

exec "$@"