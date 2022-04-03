REM @echo off
REM executes all `build.cmd` scripts recursively inside from this directory on down

pushd "%~dp0"
for /R %%f in ("build.cmd") do (
	IF EXIST "%%f" (
		call "%%f" %~1
	)
)
popd