INSERT INTO [dbo].[tags]([ordering_id], [tag], [sequence_nr], [persistence_id])
    SELECT * FROM (
        SELECT a.[Ordering], b.[items], a.SequenceNr, a.PersistenceId FROM
            [dbo].[EventJournal] AS a
            CROSS APPLY [dbo].[Split](a.Tags, ';') b
    ) AS s([ordering_id], [tag], [sequence_nr], [persistence_id])
    WHERE NOT EXISTS (
        SELECT * FROM [dbo].[tags] t WITH (updlock)
        WHERE s.[ordering_id] = t.[ordering_id] AND s.[tag] = t.[tag]
    );