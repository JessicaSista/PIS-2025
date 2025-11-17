-- Script para crear las tablas faltantes para IM datasets
-- Ejecutar este script directamente en la base de datos SQL Server

-- Crear tabla DatasetDevices si no existe
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetDevices]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DatasetDevices] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Id_device] int NOT NULL,
        [DatasetId] int NOT NULL,
        CONSTRAINT [PK_DatasetDevices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DatasetDevices_DatasetsIM_DatasetId] FOREIGN KEY ([DatasetId]) REFERENCES [DatasetsIM] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_DatasetDevices_DatasetId] ON [dbo].[DatasetDevices] ([DatasetId]);
    PRINT 'Tabla DatasetDevices creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla DatasetDevices ya existe.';
END
GO

-- Crear tabla DatasetSensors si no existe
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetSensors]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DatasetSensors] (
        [Id] int NOT NULL IDENTITY(1,1),
        [DatasetId] int NOT NULL,
        [SensorName] nvarchar(255) NOT NULL,
        CONSTRAINT [PK_DatasetSensors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DatasetSensors_DatasetsIM_DatasetId] FOREIGN KEY ([DatasetId]) REFERENCES [DatasetsIM] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_DatasetSensors_DatasetId] ON [dbo].[DatasetSensors] ([DatasetId]);
    PRINT 'Tabla DatasetSensors creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla DatasetSensors ya existe.';
END
GO

-- Crear tabla DatasetSources si no existe
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetSources]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DatasetSources] (
        [Id] int NOT NULL IDENTITY(1,1),
        [DatasetId] int NOT NULL,
        [Id_source] int NOT NULL,
        CONSTRAINT [PK_DatasetSources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DatasetSources_DatasetsIM_DatasetId] FOREIGN KEY ([DatasetId]) REFERENCES [DatasetsIM] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_DatasetSources_DatasetId] ON [dbo].[DatasetSources] ([DatasetId]);
    PRINT 'Tabla DatasetSources creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla DatasetSources ya existe.';
END
GO

-- Agregar la columna JsonFilters si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DatasetsIM]') AND name = 'JsonFilters')
BEGIN
    ALTER TABLE [dbo].[DatasetsIM] ADD [JsonFilters] nvarchar(max) NULL;
    PRINT 'Columna JsonFilters agregada exitosamente a DatasetsIM.';
END
ELSE
BEGIN
    PRINT 'La columna JsonFilters ya existe en DatasetsIM.';
END
GO

PRINT 'Script completado.';

