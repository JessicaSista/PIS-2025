IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115035931_AddJsonFiltersToDatasetIM'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetDevices]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[DatasetDevices] (
                            [Id] int NOT NULL IDENTITY,
                            [Id_device] int NOT NULL,
                            [DatasetId] int NOT NULL,
                            CONSTRAINT [PK_DatasetDevices] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_DatasetDevices_DatasetsIM_DatasetId] FOREIGN KEY ([DatasetId]) REFERENCES [DatasetsIM] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_DatasetDevices_DatasetId] ON [dbo].[DatasetDevices] ([DatasetId]);
                    END
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115035931_AddJsonFiltersToDatasetIM'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetSensors]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[DatasetSensors] (
                            [Id] int NOT NULL IDENTITY,
                            [DatasetId] int NOT NULL,
                            [SensorName] nvarchar(255) NOT NULL,
                            CONSTRAINT [PK_DatasetSensors] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_DatasetSensors_DatasetsIM_DatasetId] FOREIGN KEY ([DatasetId]) REFERENCES [DatasetsIM] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_DatasetSensors_DatasetId] ON [dbo].[DatasetSensors] ([DatasetId]);
                    END
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115035931_AddJsonFiltersToDatasetIM'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetSources]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[DatasetSources] (
                            [Id] int NOT NULL IDENTITY,
                            [DatasetId] int NOT NULL,
                            [Id_source] int NOT NULL,
                            CONSTRAINT [PK_DatasetSources] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_DatasetSources_DatasetsIM_DatasetId] FOREIGN KEY ([DatasetId]) REFERENCES [DatasetsIM] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_DatasetSources_DatasetId] ON [dbo].[DatasetSources] ([DatasetId]);
                    END
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115035931_AddJsonFiltersToDatasetIM'
)
BEGIN

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DatasetsIM]') AND name = 'JsonFilters')
                    BEGIN
                        ALTER TABLE [dbo].[DatasetsIM] ADD [JsonFilters] nvarchar(max) NULL;
                    END
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115035931_AddJsonFiltersToDatasetIM'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115035931_AddJsonFiltersToDatasetIM', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115041656_inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115041656_inicial', N'9.0.10');
END;

COMMIT;
GO

