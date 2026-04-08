-- ======================================================
-- VTuber Wiki & Forum Master Initial Database Script
-- Synchronized: 2026-04-08
-- ======================================================

-- 1. CLEANUP (Drop tables in reverse dependency order)
IF OBJECT_ID('[DiscussionLikes]', 'U') IS NOT NULL DROP TABLE [DiscussionLikes];
IF OBJECT_ID('[DiscussionReplies]', 'U') IS NOT NULL DROP TABLE [DiscussionReplies];
IF OBJECT_ID('[Discussions]', 'U') IS NOT NULL DROP TABLE [Discussions];
IF OBJECT_ID('[Activities]', 'U') IS NOT NULL DROP TABLE [Activities];
IF OBJECT_ID('[News]', 'U') IS NOT NULL DROP TABLE [News];
IF OBJECT_ID('[Vtubers]', 'U') IS NOT NULL DROP TABLE [Vtubers];
IF OBJECT_ID('[Agencies]', 'U') IS NOT NULL DROP TABLE [Agencies];
IF OBJECT_ID('[Users]', 'U') IS NOT NULL DROP TABLE [Users];
GO

-- 2. CORE SCHEMA

-- Agencies Table
CREATE TABLE [Agencies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LogoUrl] nvarchar(max) NULL,
    [Region] nvarchar(50) NULL,
    [Focus] nvarchar(50) NULL,
    [Description] nvarchar(max) NULL,
    [TalentCount] int NOT NULL DEFAULT 0,
    [Status] nvarchar(20) NOT NULL DEFAULT 'Active',
    CONSTRAINT [PK_Agencies] PRIMARY KEY ([Id])
);
GO

-- Users Table (RBAC included)
CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(20) NOT NULL DEFAULT 'User', -- Admin, User
    [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

-- Vtubers Table
CREATE TABLE [Vtubers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Age] int NULL,
    [DebutDate] datetime2 NULL,
    [Birthday] nvarchar(50) NULL,
    [Lore] nvarchar(max) NULL,
    [AvatarUrl] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL DEFAULT 'Approved',
    [Language] nvarchar(50) NULL,
    [Region] nvarchar(50) NULL,
    [Tags] nvarchar(max) NULL,
    [IsIndependent] bit NOT NULL DEFAULT 1,
    [ViewCount] int NOT NULL DEFAULT 0,
    [YoutubeUrl] nvarchar(max) NULL,
    [AgencyId] int NULL,
    CONSTRAINT [PK_Vtubers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Vtubers_Agencies_AgencyId] FOREIGN KEY ([AgencyId]) REFERENCES [Agencies] ([Id]) ON DELETE SET NULL
);
GO

-- News & Events Table
CREATE TABLE [News] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Type] nvarchar(50) NOT NULL, -- Event, Debut, Music, ASMR, Gaming
    [Content] nvarchar(max) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [Author] nvarchar(100) NOT NULL DEFAULT 'Admin',
    [PublishDate] datetime2 NOT NULL DEFAULT GETDATE(),
    [IsFeatured] bit NOT NULL DEFAULT 0,
    CONSTRAINT [PK_News] PRIMARY KEY ([Id])
);
GO

-- 3. FORUM SCHEMA

-- Discussions Table
CREATE TABLE [Discussions] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Author] nvarchar(100) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
    [Category] nvarchar(50) NOT NULL DEFAULT 'General',
    [ViewCount] int NOT NULL DEFAULT 0,
    [ReplyCount] int NOT NULL DEFAULT 0,
    [LikeCount] int NOT NULL DEFAULT 0,
    [IsPinned] bit NOT NULL DEFAULT 0,
    [LastReplier] nvarchar(100) NULL,
    [LastReplyDate] datetime2 NULL,
    CONSTRAINT [PK_Discussions] PRIMARY KEY ([Id])
);
GO

-- Discussion Replies Table
CREATE TABLE [DiscussionReplies] (
    [Id] int NOT NULL IDENTITY,
    [Content] nvarchar(max) NOT NULL,
    [Author] nvarchar(100) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
    [DiscussionId] int NOT NULL,
    [LikeCount] int NOT NULL DEFAULT 0,
    CONSTRAINT [PK_DiscussionReplies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiscussionReplies_Discussions_DiscussionId] FOREIGN KEY ([DiscussionId]) REFERENCES [Discussions] ([Id]) ON DELETE CASCADE
);
GO

-- Discussion Likes Table (Interaction Tracking)
CREATE TABLE [DiscussionLikes] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(100) NOT NULL,
    [DiscussionId] int NULL,
    [ReplyId] int NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_DiscussionLikes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiscussionLikes_Discussions_DiscussionId] FOREIGN KEY ([DiscussionId]) REFERENCES [Discussions] ([Id]),
    CONSTRAINT [FK_DiscussionLikes_DiscussionReplies_ReplyId] FOREIGN KEY ([ReplyId]) REFERENCES [DiscussionReplies] ([Id])
);
GO

-- Activity Tracking Table
CREATE TABLE [Activities] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [ActivityType] nvarchar(50) NOT NULL, -- Article, Media, User, Community
    [Action] nvarchar(50) NOT NULL,       -- Created, Updated, Deleted, Commented
    [Author] nvarchar(100) NOT NULL,
    [Timestamp] datetime2 NOT NULL DEFAULT GETDATE(),
    [LinkUrl] nvarchar(max) NULL,
    [Detail] nvarchar(max) NULL,
    CONSTRAINT [PK_Activities] PRIMARY KEY ([Id])
);
GO

-- 4. SEED DATA

-- Agencies
INSERT INTO [Agencies] ([Name], [LogoUrl], [Region], [Focus], [Description], [TalentCount], [Status])
VALUES 
('Hololive Production', 'https://upload.wikimedia.org/wikipedia/commons/e/e6/Hololive_Production_logo.png', 'Japan', 'Idol, Gaming', 'Pioneers of the idol-centric VTuber model.', 80, 'Active'),
('NIJISANJI', 'https://upload.wikimedia.org/wikipedia/commons/e/e3/ANYCOLOR_Inc_logo.png', 'Japan', 'Variety, Chat', 'Known for massive roster and variety content.', 200, 'Active'),
('VShojo', 'https://upload.wikimedia.org/wikipedia/en/thumb/0/07/VShojo_logo.png/250px-VShojo_logo.png', 'US', 'Streamer-led', 'A talent-first agency focusing on creator freedom.', 14, 'Active');

-- Admin User (Password: 123456 - Hash)
INSERT INTO [Users] ([Username], [Email], [PasswordHash], [Role])
VALUES ('Admin', 'admin@wiki.com', 'AQAAAAIAAYagAAAAEG...', 'Admin');

-- News
INSERT INTO [News] ([Title], [Type], [Content], [IsFeatured])
VALUES 
('Welcome to VTuber Wiki!', 'SiteUpdate', 'The community platform is now officially live!', 1),
('New Debut: Shylily 2.0', 'Debut', 'Shylily reveals her stunning new 2.0 model with advanced tracking.', 0);

-- Vtubers
INSERT INTO [Vtubers] ([Name], [Age], [DebutDate], [Birthday], [AvatarUrl], [Status], [Language], [Region], [Tags], [IsIndependent], [YoutubeUrl])
VALUES 
('Shylily', 24, '2022-01-11', 'January 11', 'https://lh3.googleusercontent.com/aida-public/AB6AXuBcwqJmvI6QRhBoLIvALsqquG49TuN0g7UBVPkcoSNKKy9vCM6dN1_VrD4lpd7U7We5VcrAh0xyQ_pF7ZRJWi9xRe8pGEIncoMe78B52USIo5eq2dLHcBLuwQnGtyHU8D72b_9E38dspsL3CjHGaANusFkBvfszldyi6RRlEtSikuNm-3uo1iLiB8rsBRVu4OLrXFtBNSTOLkpM5nOLYuI9Gy3NJhrtjwM8Y0D1FbZ8Ql6_6Tu4OtbFSTWFM7Wrl_dk-1yA5gTxYzE', 'Approved', 'English / German', 'Europe', 'Gaming, Variety', 1, 'https://youtube.com/@shylily'),
('Gawr Gura', NULL, '2020-09-13', 'June 20', 'https://upload.wikimedia.org/wikipedia/en/2/2f/Gawr_Gura_portrait.png', 'Approved', 'English', 'NA', 'Shark, Music, Gaming', 0, 'https://youtube.com/@gawrgura');

-- Update Agency Talent Counts
UPDATE [Agencies] SET [TalentCount] = (SELECT COUNT(*) FROM [Vtubers] WHERE [AgencyId] = [Agencies].[Id]);

GO
