@echo off
REM Deploys container instance into Kubernetes namespace

set namespace="akkasample"

if "%~1"=="" (
	REM No K8S namespace specified
	echo No namespace specified. Defaulting to [%namespace%]
) else (
	set namespace="%~1"
	echo Deploying into K8s namespace [%namespace%]
)

kubectl apply -f "%~dp0/akkasql.yaml" -n "%namespace%"