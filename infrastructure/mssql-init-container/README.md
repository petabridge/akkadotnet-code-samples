# MSSQL Init Container

This folder contains the source code and `Dockerfile` definition for the `akka.sql-init-container` image. 

This is used to function as a Kubernetes init container to help prevent Akka.NET services that depend upon [`akka.sqlserver`](../mssql/) from starting until we know that SQL Server is ready and has the `Akka` database populated.