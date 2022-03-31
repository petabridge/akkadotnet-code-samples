#!/usr/bin/env bash

echo "checking to see if [Akka] database is available..."

/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P ${SA_PASSWORD} -d master -q "select [dbo].[DatabaseExists]('Akka')"