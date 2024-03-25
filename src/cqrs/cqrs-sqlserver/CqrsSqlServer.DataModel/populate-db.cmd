@echo off
REM launches local SQL Server dependency so we can run from Visual Studio

pushd "%~dp0"
dotnet ef database update --no-build
popd