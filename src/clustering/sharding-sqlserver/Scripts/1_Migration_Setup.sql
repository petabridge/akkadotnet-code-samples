IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'tags')
BEGIN
    CREATE TABLE [dbo].[tags](
        ordering_id BIGINT NOT NULL,
        tag NVARCHAR(64) NOT NULL,
        sequence_nr BIGINT NOT NULL,
        persistence_id VARCHAR(255) NOT NULL,
        PRIMARY KEY (ordering_id, tag, persistence_id)
    );
END
GO

CREATE OR ALTER FUNCTION [dbo].[Split](@String VARCHAR(8000), @Delimiter CHAR(1))
    RETURNS @temptable TABLE (items VARCHAR(8000)) AS
BEGIN
    DECLARE @idx INT
    DECLARE @slice VARCHAR(8000)

    SELECT @idx = 1
    IF LEN(@String) < 1 OR @String is NULL
        RETURN

    WHILE @idx != 0
        BEGIN
            SET @idx = CHARINDEX(@Delimiter, @String)
            IF @idx != 0
                SET @slice = LEFT(@String,@idx - 1)
            ELSE
                SET @slice = @String

            IF(LEN(@slice) > 0)
                INSERT INTO @temptable(Items) VALUES(@slice)

            SET @String = RIGHT(@String, LEN(@String) - @idx)
            IF len(@String) = 0
                BREAK
        END
    RETURN
END;
GO