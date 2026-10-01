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
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    CREATE TABLE [InternProfiles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [StudentId] nvarchar(40) NOT NULL,
        [Email] nvarchar(160) NOT NULL,
        [School] nvarchar(160) NOT NULL,
        [Major] nvarchar(120) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [StartDate] date NULL,
        [EndDate] date NULL,
        [Status] nvarchar(40) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_InternProfiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    CREATE TABLE [InternApplications] (
        [Id] int NOT NULL IDENTITY,
        [ProfileId] int NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [AppliedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_InternApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InternApplications_InternProfiles_ProfileId] FOREIGN KEY ([ProfileId]) REFERENCES [InternProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    CREATE TABLE [InternDocuments] (
        [Id] int NOT NULL IDENTITY,
        [ProfileId] int NOT NULL,
        [Type] nvarchar(60) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(120) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [Note] nvarchar(1000) NULL,
        [UploadedAt] datetimeoffset NOT NULL,
        [ConfirmedAt] datetimeoffset NULL,
        CONSTRAINT [PK_InternDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InternDocuments_InternProfiles_ProfileId] FOREIGN KEY ([ProfileId]) REFERENCES [InternProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InternApplications_ProfileId] ON [InternApplications] ([ProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    CREATE INDEX [IX_InternDocuments_ProfileId] ON [InternDocuments] ([ProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InternProfiles_Email] ON [InternProfiles] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InternProfiles_StudentId] ON [InternProfiles] ([StudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928111528_CreateInternHrSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928111528_CreateInternHrSchema', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928162207_AddInternReviewHistory'
)
BEGIN
    CREATE TABLE [InternReviewHistories] (
        [Id] int NOT NULL IDENTITY,
        [ProfileId] int NOT NULL,
        [TargetType] nvarchar(40) NOT NULL,
        [TargetId] int NOT NULL,
        [TargetLabel] nvarchar(120) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [Note] nvarchar(1000) NULL,
        [ReviewedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_InternReviewHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InternReviewHistories_InternProfiles_ProfileId] FOREIGN KEY ([ProfileId]) REFERENCES [InternProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928162207_AddInternReviewHistory'
)
BEGIN
    CREATE INDEX [IX_InternReviewHistories_ProfileId_ReviewedAt] ON [InternReviewHistories] ([ProfileId], [ReviewedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928162207_AddInternReviewHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928162207_AddInternReviewHistory', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    CREATE TABLE [MailJobs] (
        [Id] int NOT NULL IDENTITY,
        [Recipient] nvarchar(max) NOT NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Attempts] int NOT NULL,
        [DueAt] datetimeoffset NOT NULL,
        [Error] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_MailJobs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    CREATE TABLE [PortalAccounts] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(160) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [Active] bit NOT NULL,
        [Permissions] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PortalAccounts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    CREATE TABLE [PortalAudits] (
        [Id] bigint NOT NULL IDENTITY,
        [Actor] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [Resource] nvarchar(max) NOT NULL,
        [StatusCode] int NOT NULL,
        [At] datetimeoffset NOT NULL,
        CONSTRAINT [PK_PortalAudits] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    CREATE TABLE [WorkItems] (
        [Id] int NOT NULL IDENTITY,
        [Kind] nvarchar(40) NOT NULL,
        [Title] nvarchar(240) NOT NULL,
        [Detail] nvarchar(max) NOT NULL,
        [ProfileId] int NULL,
        [ProgramId] int NULL,
        [MentorId] int NULL,
        [Department] nvarchar(max) NOT NULL,
        [Start] datetimeoffset NULL,
        [End] datetimeoffset NULL,
        [Status] nvarchar(max) NOT NULL,
        [Progress] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Capacity] int NOT NULL,
        [Feedback] nvarchar(max) NOT NULL,
        [History] nvarchar(max) NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [Attachment] varbinary(max) NULL,
        [UniqueKey] nvarchar(240) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [Version] rowversion NOT NULL,
        CONSTRAINT [PK_WorkItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PortalAccounts_Email] ON [PortalAccounts] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    CREATE INDEX [IX_WorkItems_Kind_ProfileId] ON [WorkItems] ([Kind], [ProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WorkItems_UniqueKey] ON [WorkItems] ([UniqueKey]) WHERE [UniqueKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930172042_AddPortalWorkflows'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930172042_AddPortalWorkflows', N'10.0.12');
END;

COMMIT;
GO

