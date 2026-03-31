CREATE TABLE [Agencies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LogoUrl] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Agencies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


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
GO


CREATE INDEX [IX_Vtubers_AgencyId] ON [Vtubers] ([AgencyId]);
GO


