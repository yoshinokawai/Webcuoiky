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
VALUES (N'InitialCreate', N'10.0.5');

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
VALUES (N'AddUserModel', N'10.0.5');

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
VALUES (N'AddFilteringColumns', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Vtubers] ADD [ViewCount] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'AddVtuberViewCount', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Agencies] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'SyncModels', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Vtubers] ADD [YoutubeUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'AddYoutubeUrl', N'10.0.5');

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
VALUES (N'AddNewsTable', N'10.0.5');

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
VALUES (N'AddDiscussionsTable', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Activities] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ActivityType] nvarchar(50) NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [Author] nvarchar(100) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [LinkUrl] nvarchar(max) NULL,
    [Detail] nvarchar(max) NULL,
    CONSTRAINT [PK_Activities] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'AddActivityTracking', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [Role] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'AddUserRole', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Discussions] ADD [LikeCount] int NOT NULL DEFAULT 0;

ALTER TABLE [DiscussionReplies] ADD [LikeCount] int NOT NULL DEFAULT 0;

CREATE TABLE [DiscussionLikes] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [DiscussionId] int NULL,
    [ReplyId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DiscussionLikes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiscussionLikes_DiscussionReplies_ReplyId] FOREIGN KEY ([ReplyId]) REFERENCES [DiscussionReplies] ([Id]),
    CONSTRAINT [FK_DiscussionLikes_Discussions_DiscussionId] FOREIGN KEY ([DiscussionId]) REFERENCES [Discussions] ([Id])
);

CREATE INDEX [IX_DiscussionLikes_DiscussionId] ON [DiscussionLikes] ([DiscussionId]);

CREATE INDEX [IX_DiscussionLikes_ReplyId] ON [DiscussionLikes] ([ReplyId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'AddForumLikesAndManagement', N'10.0.5');

COMMIT;
GO

