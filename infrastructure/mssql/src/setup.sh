#!/usr/bin/env bash

# start SQL Server
/opt/mssql/bin/sqlservr &

#wait for the SQL Server to come up
sleep 15s

# setup the tables
echo "Connecting to SQL and creating Akka database."
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P ${SA_PASSWORD} -d master -i setup.sql

sleep infinity