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
