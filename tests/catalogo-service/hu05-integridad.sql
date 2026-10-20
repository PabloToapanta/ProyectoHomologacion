SET NOCOUNT ON;
USE Catalogo;
IF (SELECT COUNT(*) FROM __EFMigrationsHistory) <> 2 THROW 51001, 'No se aplicaron las dos migraciones del catalogo.', 1;
PRINT 'PASS: ambas migraciones aplicadas en SQL Server.';
IF NOT EXISTS (SELECT 1 FROM Asignaturas) THROW 51002, 'Ejecutar primero el smoke HTTP para generar datos sinteticos.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT INTO Asignaturas (Id, CarreraId, Codigo, CodigoNormalizado, Nombre, Nivel, Creditos, Horas)
        SELECT TOP (1) NEWID(), CarreraId, Codigo, CodigoNormalizado, Nombre, Nivel, Creditos, Horas FROM Asignaturas;
    ROLLBACK;
    THROW 51003, 'El indice permitio un duplicado.', 1;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
    PRINT 'PASS: SQL Server impide duplicados aun al omitir la API (2601/2627).';
END CATCH;
BEGIN TRY
    BEGIN TRANSACTION;
    DELETE FROM Carreras WHERE Id = (SELECT TOP (1) CarreraId FROM Asignaturas);
    ROLLBACK;
    THROW 51004, 'La FK permitio eliminar una carrera relacionada.', 1;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: FK SQL Server impide eliminar carrera relacionada (547), sin cascada.';
END CATCH;
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE TOP (1) Asignaturas SET Horas = 0;
    ROLLBACK;
    THROW 51005, 'CHECK permitio horas cero.', 1;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: CHECK de SQL Server impide horas cero aun al omitir la API.';
END CATCH;
IF EXISTS (SELECT 1 FROM Asignaturas a LEFT JOIN Carreras c ON c.Id = a.CarreraId WHERE c.Id IS NULL) THROW 51006, 'Se encontraron asignaturas huerfanas.', 1;
PRINT 'PASS: no existen asignaturas huerfanas; operaciones de comprobacion revertidas.';
SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(40)) AS SqlServerVersion;
