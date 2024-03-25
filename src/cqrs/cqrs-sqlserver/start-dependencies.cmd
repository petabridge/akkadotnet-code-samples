@echo off
REM launches local SQL Server dependency so we can run from Visual Studio


REM Step 0: Build the project
echo Building the project in release configuration...
dotnet build -c Release
if errorlevel 1 (
    echo "Failed to build the solution - aborting"
    exit /b -1
)


set version="0.1.0"
set imageName="akkadotnet.sqlserver"

if "%~1"=="" (
	REM No version tag specified
	echo No tag for [%imageName%] specified. Defaulting to [%version%]
) else (
	set version="%~1"
	echo Running [%imageName%] with tag [%~1]
)

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1533:1433 --name "sqlsharding-sql" -d "%imageName%:%version%"

if errorlevel 1 (
	echo "failed to start akkadotnet.sqlserver - building image first then retrying"
	call %~dp0../../../infrastructure/build.all.cmd %version%
	if errorlevel 1 (
		echo "failed to build akkadotnet.sqlserver - aborting"
		exit -1
	) else (
		docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1533:1433 --name "sqlsharding-sql" -d "%imageName%:%version%"
	)
)

REM Step 1: Execute EF Database Update
:EFDatabaseUpdate
set /A attemptCount+=1
echo Attempting to update the database, attempt %attemptCount%
echo "Calling %~dp0CqrsSqlServer.DataModel/populate-db.cmd"
call %~dp0CqrsSqlServer.DataModel/populate-db.cmd
if errorlevel 1 (
    echo Attempt %attemptCount% failed to update database.
    popd
    if %attemptCount% lss 3 (
        echo Waiting for 30 seconds before retry...
        timeout /t 30
        goto EFDatabaseUpdate
    ) else (
        echo "Failed to update the database after 3 attempts - aborting"
        exit /b -1
    )
) else (
    echo Database updated successfully.
    popd
)