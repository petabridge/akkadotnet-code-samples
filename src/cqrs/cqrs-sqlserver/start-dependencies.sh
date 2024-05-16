#!/usr/bin/env bash
# launches local SQL Server dependency so we can run from Visual Studio
parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

# Step 0: Build the project
echo Building the project in release configuration...
dotnet build -c Release

version="0.1.0"
imageName="akkadotnet.sqlserver"

if [ -z $1 ]; then
	echo "No tag for [${imageName}] specified. Defaulting to [${version}]"
else
	version="$1"
	echo "Building [${imageName}] with tag [${version}]"
fi

if docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1533:1433 --name "sqlsharding-sql" -d "${imageName}:${version}" ; then
	echo "failed to start akkadotnet.sqlserver - building image first then retrying"
	
	if ../../../infrastructure/build.all.sh $1 ; then
		echo "failed to build akkadotnet.sqlserver - aborting"
		return -1
		docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1533:1433 --name "sqlsharding-sql" -d "${imageName}:${version}"
	fi
fi

# Step 1: Execute EF Database Update
attemptCount=0
EFDatabaseUpdate() {
    ((attemptCount++))
    echo "Attempting to update the database, attempt $attemptCount"
    echo "Calling $(dirname "$0")/CqrsSqlServer.DataModel/populate-db.sh"
    $(dirname "$0")/CqrsSqlServer.DataModel/populate-db.sh
    if [ $? -ne 0 ]; then
        echo "Attempt $attemptCount failed to update database."
        if [ $attemptCount -lt 3 ]; then
            echo "Waiting for 30 seconds before retry..."
            sleep 30
            EFDatabaseUpdate
        else
            echo "Failed to update the database after 3 attempts - aborting"
            exit 1
        fi
    else
        echo "Database updated successfully."
    fi
}

EFDatabaseUpdate