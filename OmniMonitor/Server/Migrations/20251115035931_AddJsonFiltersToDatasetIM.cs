using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonFiltersToDatasetIM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crear tabla DatasetDevices si no existe
            migrationBuilder.Sql(@"
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
            ");

            // Crear tabla DatasetSensors si no existe
            migrationBuilder.Sql(@"
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
            ");

            // Crear tabla DatasetSources si no existe
            migrationBuilder.Sql(@"
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
            ");

            // Agregar la columna JsonFilters si no existe
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DatasetsIM]') AND name = 'JsonFilters')
                BEGIN
                    ALTER TABLE [dbo].[DatasetsIM] ADD [JsonFilters] nvarchar(max) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar columna JsonFilters si existe
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DatasetsIM]') AND name = 'JsonFilters')
                BEGIN
                    ALTER TABLE [dbo].[DatasetsIM] DROP COLUMN [JsonFilters];
                END
            ");

            // Eliminar tablas si existen
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetDevices]') AND type in (N'U'))
                BEGIN
                    DROP TABLE [dbo].[DatasetDevices];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetSensors]') AND type in (N'U'))
                BEGIN
                    DROP TABLE [dbo].[DatasetSensors];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatasetSources]') AND type in (N'U'))
                BEGIN
                    DROP TABLE [dbo].[DatasetSources];
                END
            ");
        }
    }
}
