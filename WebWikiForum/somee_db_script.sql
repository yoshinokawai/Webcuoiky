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
CREATE TABLE [Agencies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LogoUrl] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Agencies] PRIMARY KEY ([Id])
);

CREATE TABLE [Vtubers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Age] int NULL,
    [DebutDate] datetime2 NULL,
    [Birthday] nvarchar(50) NOT NULL,
    [Lore] nvarchar(max) NOT NULL,
    [AvatarUrl] nvarchar(max) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [AgencyId] int NULL,
    CONSTRAINT [PK_Vtubers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Vtubers_Agencies_AgencyId] FOREIGN KEY ([AgencyId]) REFERENCES [Agencies] ([Id])
);

CREATE INDEX [IX_Vtubers_AgencyId] ON [Vtubers] ([AgencyId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260331012102_InitialCreate', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260331103017_AddUserModel', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Vtubers] ADD [IsIndependent] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Vtubers] ADD [Language] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Vtubers] ADD [Region] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Vtubers] ADD [Tags] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Agencies] ADD [Description] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Agencies] ADD [Focus] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Agencies] ADD [Region] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Agencies] ADD [TalentCount] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260402153407_AddFilteringColumns', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Vtubers] ADD [ViewCount] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260402160306_AddVtuberViewCount', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Agencies] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260405181312_SyncModels', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Vtubers] ADD [YoutubeUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260406150059_AddYoutubeUrl', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [News] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Content] nvarchar(max) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [Author] nvarchar(max) NOT NULL,
    [PublishDate] datetime2 NOT NULL,
    [IsFeatured] bit NOT NULL,
    CONSTRAINT [PK_News] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260406151856_AddNewsTable', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Discussions] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Author] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [ViewCount] int NOT NULL,
    [ReplyCount] int NOT NULL,
    [IsPinned] bit NOT NULL,
    [LastReplier] nvarchar(max) NULL,
    [LastReplyDate] datetime2 NULL,
    CONSTRAINT [PK_Discussions] PRIMARY KEY ([Id])
);

CREATE TABLE [DiscussionReplies] (
    [Id] int NOT NULL IDENTITY,
    [Content] nvarchar(max) NOT NULL,
    [Author] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [DiscussionId] int NOT NULL,
    CONSTRAINT [PK_DiscussionReplies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiscussionReplies_Discussions_DiscussionId] FOREIGN KEY ([DiscussionId]) REFERENCES [Discussions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_DiscussionReplies_DiscussionId] ON [DiscussionReplies] ([DiscussionId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260406154942_AddDiscussionsTable', N'10.0.5');

COMMIT;
GO

