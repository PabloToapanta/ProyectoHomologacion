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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019192933_InitialCatalogo'
)
BEGIN
    CREATE TABLE [Carreras] (
        [Id] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Facultad] nvarchar(200) NOT NULL,
        [Modalidad] nvarchar(100) NULL,
        [Vigencia] bit NOT NULL,
        [NombreNormalizado] nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [FacultadNormalizada] nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL,
        CONSTRAINT [PK_Carreras] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019192933_InitialCatalogo'
)
BEGIN
    CREATE TABLE [Asignaturas] (
        [Id] uniqueidentifier NOT NULL,
        [CarreraId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Asignaturas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Asignaturas_Carreras_CarreraId] FOREIGN KEY ([CarreraId]) REFERENCES [Carreras] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019192933_InitialCatalogo'
)
BEGIN
    CREATE INDEX [IX_Asignaturas_CarreraId] ON [Asignaturas] ([CarreraId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019192933_InitialCatalogo'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Carreras_NombreNormalizado_FacultadNormalizada] ON [Carreras] ([NombreNormalizado], [FacultadNormalizada]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019192933_InitialCatalogo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019192933_InitialCatalogo', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    IF EXISTS (SELECT 1 FROM [Asignaturas])
        THROW 51000, 'HU-05: existen referencias de asignaturas sin datos academicos. Acordar una migracion de datos antes de continuar; no se eliminaron registros.', 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    DROP INDEX [IX_Asignaturas_CarreraId] ON [Asignaturas];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    ALTER TABLE [Asignaturas] ADD [Codigo] nvarchar(50) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    ALTER TABLE [Asignaturas] ADD [CodigoNormalizado] nvarchar(50) COLLATE Latin1_General_100_BIN2 NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    ALTER TABLE [Asignaturas] ADD [Creditos] decimal(9,2) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    ALTER TABLE [Asignaturas] ADD [Horas] int NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    ALTER TABLE [Asignaturas] ADD [Nivel] int NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    ALTER TABLE [Asignaturas] ADD [Nombre] nvarchar(200) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Asignaturas_CarreraId_CodigoNormalizado] ON [Asignaturas] ([CarreraId], [CodigoNormalizado]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    EXEC(N'ALTER TABLE [Asignaturas] ADD CONSTRAINT [CK_Asignaturas_CreditosPositivos] CHECK ([Creditos] > 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    EXEC(N'ALTER TABLE [Asignaturas] ADD CONSTRAINT [CK_Asignaturas_HorasPositivas] CHECK ([Horas] > 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    EXEC(N'ALTER TABLE [Asignaturas] ADD CONSTRAINT [CK_Asignaturas_NivelPositivo] CHECK ([Nivel] > 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020000355_AmpliarAsignaturasHu05'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020000355_AmpliarAsignaturasHu05', N'8.0.30');
END;
GO

COMMIT;
GO
