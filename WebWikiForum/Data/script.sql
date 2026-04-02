-- VTuber Wiki Foundation Script
-- Update schema for dynamic filtering

IF OBJECT_ID('[Vtubers]', 'U') IS NOT NULL DROP TABLE [Vtubers];
IF OBJECT_ID('[Agencies]', 'U') IS NOT NULL DROP TABLE [Agencies];
IF OBJECT_ID('[Users]', 'U') IS NOT NULL DROP TABLE [Users];
GO

CREATE TABLE [Agencies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LogoUrl] nvarchar(max) NULL,
    [Region] nvarchar(50) NULL,
    [Focus] nvarchar(50) NULL,
    [Description] nvarchar(max) NULL,
    [TalentCount] int NOT NULL DEFAULT 0,
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
    [Birthday] nvarchar(50) NULL,
    [Lore] nvarchar(max) NULL,
    [AvatarUrl] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL DEFAULT 'Pending',
    [Language] nvarchar(50) NULL,
    [Region] nvarchar(50) NULL,
    [Tags] nvarchar(max) NULL,
    [IsIndependent] bit NOT NULL DEFAULT 1,
    [AgencyId] int NULL,
    CONSTRAINT [PK_Vtubers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Vtubers_Agencies_AgencyId] FOREIGN KEY ([AgencyId]) REFERENCES [Agencies] ([Id])
);
GO

CREATE INDEX [IX_Vtubers_AgencyId] ON [Vtubers] ([AgencyId]);
GO

-- Seed Sample Data
INSERT INTO [Agencies] ([Name], [LogoUrl], [Region], [Focus], [Description], [TalentCount])
VALUES 
('Hololive Production', 'https://upload.wikimedia.org/wikipedia/commons/e/e6/Hololive_Production_logo.png', 'Japan', 'Idol, Gaming', 'Pioneers of the idol-centric VTuber model.', 80),
('NIJISANJI', 'https://upload.wikimedia.org/wikipedia/commons/e/e3/ANYCOLOR_Inc_logo.png', 'Japan', 'Variety, Chat', 'Known for massive roster and variety content.', 200),
('VShojo', 'https://upload.wikimedia.org/wikipedia/en/thumb/0/07/VShojo_logo.png/250px-VShojo_logo.png', 'US', 'Streamer-led', 'A talent-first agency focusing on creator freedom.', 14);

INSERT INTO [Vtubers] ([Name], [Age], [DebutDate], [Birthday], [AvatarUrl], [Status], [Language], [Region], [Tags], [IsIndependent])
VALUES 
('Shylily', 24, '2022-01-11', 'January 11', 'https://lh3.googleusercontent.com/aida-public/AB6AXuBcwqJmvI6QRhBoLIvALsqquG49TuN0g7UBVPkcoSNKKy9vCM6dN1_VrD4lpd7U7We5VcrAh0xyQ_pF7ZRJWi9xRe8pGEIncoMe78B52USIo5eq2dLHcBLuwQnGtyHU8D72b_9E38dspsL3CjHGaANusFkBvfszldyi6RRlEtSikuNm-3uo1iLiB8rsBRVu4OLrXFtBNSTOLkpM5nOLYuI9Gy3NJhrtjwM8Y0D1FbZ8Ql6_6Tu4OtbFSTWFM7Wrl_dk-1yA5gTxYzE', 'Approved', 'English / German', 'Europe', 'Gaming, Variety', 1),
('Filian', NULL, '2021-04-24', NULL, 'https://lh3.googleusercontent.com/aida-public/AB6AXuBLcFhjHtjvzfH9HT0uyxEO4mplc9JkEGXfi1MnszXpPgz-H-1CS4K9FZax9I9083Zckab7YXk20adSR7Q_5LfKmPviKvXCVeYL9RtOKNnz8fI3XXho5RWpEodVI02yigAjGlCjh342VeCiWWpylsw-uU5pCvPgUIplbQUCvgPpQj6fuctbDVW5_6exvXAzN-mts65aHDeZbcBazd057xYaMteCOujQcZKiWf2qaw-0Anezonht6bnpCJJIk1SjqXciVc3Am2Srpus', 'Approved', 'English', 'NA', 'Gaming, Comedy', 1),
('Saruei', NULL, '2021-09-17', NULL, 'https://lh3.googleusercontent.com/aida-public/AB6AXuBnDVC6BsqkBhuyYJ2kxvNRzMuz2BoVcrDDRYoZ-XDrUcuUCjUjIwKH_IrTPr-vvyhbapDPqoxn9oNy8WNPEhGV17Ky7l8VWAtiGzmyNQ6bxxiZdjpnCnhASG4JFBD2WNOvlNMW6kXEzSXaimOBfa3fNBfZfLWGS5fqYGvXJMweu829GGAZFVMOSbgs1pHVeUmsL3N7yr1WISZiQKBToiJ_oX9PjaZu7-b2YweBF_BrHnQm8GmRK7_pK8yT1g9tNKSdhCqyhQpGib8', 'Approved', 'English / French', 'Europe', 'Art, ASMR', 1);
-- Example: Deleting an agency while preserving its VTubers
-- UPDATE [Vtubers] SET [AgencyId] = NULL, [IsIndependent] = 1 WHERE [AgencyId] = @AgencyIdToRemove;
-- DELETE FROM [Agencies] WHERE [Id] = @AgencyIdToRemove;
