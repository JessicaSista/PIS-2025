-- Script para eliminar el registro de la migración y permitir que se vuelva a aplicar
-- Ejecutar este script en la base de datos SQL Server

-- Eliminar el registro de la migración del historial
DELETE FROM [__EFMigrationsHistory] 
WHERE [MigrationId] = '20251115035931_AddJsonFiltersToDatasetIM';

PRINT 'Registro de migración eliminado. Ahora puedes ejecutar: dotnet ef database update';

