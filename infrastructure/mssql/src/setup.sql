-- from https://stackoverflow.com/questions/679000/how-to-check-if-a-database-exists-in-sql-server

CREATE FUNCTION dbo.DatabaseExists(@dbname nvarchar(128))
RETURNS bit
AS
BEGIN
    declare @result bit = 0 
    SELECT @result = CAST(
        CASE WHEN db_id(@dbname) is not null THEN 1 
        ELSE 0 
        END 
    AS BIT)
    return @result
END
GO

CREATE DATABASE Akka;
GO