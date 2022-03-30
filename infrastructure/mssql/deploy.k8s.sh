#!/usr/bin/env bash
# Deploys container instance into Kubernetes namespace

namespace="akkasample"

if [ -z $1 ]; then
	echo "No namespace specified. Defaulting to [${namespace}]"
else
	namespace="$1"
	echo "Deploying into K8s namespace [${namespace}]"
fi

kubectl apply -f "akkasql.yaml" -n "%namespace%"