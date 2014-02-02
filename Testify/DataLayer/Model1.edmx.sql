
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, and Azure
-- --------------------------------------------------
-- Date Created: 05/22/2013 20:23:41
-- Generated from EDMX file: c:\WIP\Lactose\DataLayer\Model1.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [TestifyCEDB];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_Panel_0_0]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Panels] DROP CONSTRAINT [FK_Panel_0_0];
GO
IF OBJECT_ID(N'[dbo].[FK_Queue_0_0]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Queues] DROP CONSTRAINT [FK_Queue_0_0];
GO
IF OBJECT_ID(N'[dbo].[FK_TestProject_0_0]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[TestProjects] DROP CONSTRAINT [FK_TestProject_0_0];
GO
IF OBJECT_ID(N'[dbo].[FK_UnitTest_0_0]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[UnitTests] DROP CONSTRAINT [FK_UnitTest_0_0];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Panels]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Panels];
GO
IF OBJECT_ID(N'[dbo].[Projects]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Projects];
GO
IF OBJECT_ID(N'[dbo].[Queues]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Queues];
GO
IF OBJECT_ID(N'[dbo].[TestProjects]', 'U') IS NOT NULL
    DROP TABLE [dbo].[TestProjects];
GO
IF OBJECT_ID(N'[dbo].[UnitTests]', 'U') IS NOT NULL
    DROP TABLE [dbo].[UnitTests];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Panels'
CREATE TABLE [dbo].[Panels] (
    [PanelId] int  NOT NULL,
    [UnitTestId] int  NOT NULL
);
GO

-- Creating table 'Projects'
CREATE TABLE [dbo].[Projects] (
    [ProjectId] int  NOT NULL,
    [Name] nvarchar(100)  NOT NULL,
    [Path] nvarchar(100)  NOT NULL
);
GO

-- Creating table 'Queues'
CREATE TABLE [dbo].[Queues] (
    [QueueId] int  NOT NULL,
    [Name] nvarchar(100)  NOT NULL,
    [PanelId] int  NOT NULL,
    [IsRunning] bit  NOT NULL,
    [IsDiscarded] bit  NOT NULL
);
GO

-- Creating table 'TestProjects'
CREATE TABLE [dbo].[TestProjects] (
    [TestProjectId] int  NOT NULL,
    [ProjectId] int  NOT NULL,
    [Name] nvarchar(100)  NOT NULL,
    [Path] nvarchar(100)  NOT NULL,
    [PanelId] int  NOT NULL,
    [UnitTestId] nvarchar(100)  NOT NULL
);
GO

-- Creating table 'UnitTests'
CREATE TABLE [dbo].[UnitTests] (
    [UnitTestId] int  NOT NULL,
    [Name] nvarchar(100)  NOT NULL,
    [MetadataToken] int  NOT NULL,
    [UniqueId] int  NOT NULL,
    [TestProjectId] int  NOT NULL,
    [IsBroken] bit  NOT NULL,
    [DurationInMSec] int  NOT NULL,
    [Strategy] nvarchar(100)  NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [PanelId] in table 'Panels'
ALTER TABLE [dbo].[Panels]
ADD CONSTRAINT [PK_Panels]
    PRIMARY KEY CLUSTERED ([PanelId] ASC);
GO

-- Creating primary key on [ProjectId] in table 'Projects'
ALTER TABLE [dbo].[Projects]
ADD CONSTRAINT [PK_Projects]
    PRIMARY KEY CLUSTERED ([ProjectId] ASC);
GO

-- Creating primary key on [QueueId] in table 'Queues'
ALTER TABLE [dbo].[Queues]
ADD CONSTRAINT [PK_Queues]
    PRIMARY KEY CLUSTERED ([QueueId] ASC);
GO

-- Creating primary key on [TestProjectId] in table 'TestProjects'
ALTER TABLE [dbo].[TestProjects]
ADD CONSTRAINT [PK_TestProjects]
    PRIMARY KEY CLUSTERED ([TestProjectId] ASC);
GO

-- Creating primary key on [UnitTestId] in table 'UnitTests'
ALTER TABLE [dbo].[UnitTests]
ADD CONSTRAINT [PK_UnitTests]
    PRIMARY KEY CLUSTERED ([UnitTestId] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [UnitTestId] in table 'Panels'
ALTER TABLE [dbo].[Panels]
ADD CONSTRAINT [FK_Panel_0_0]
    FOREIGN KEY ([UnitTestId])
    REFERENCES [dbo].[UnitTests]
        ([UnitTestId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_Panel_0_0'
CREATE INDEX [IX_FK_Panel_0_0]
ON [dbo].[Panels]
    ([UnitTestId]);
GO

-- Creating foreign key on [PanelId] in table 'Queues'
ALTER TABLE [dbo].[Queues]
ADD CONSTRAINT [FK_Queue_0_0]
    FOREIGN KEY ([PanelId])
    REFERENCES [dbo].[Panels]
        ([PanelId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_Queue_0_0'
CREATE INDEX [IX_FK_Queue_0_0]
ON [dbo].[Queues]
    ([PanelId]);
GO

-- Creating foreign key on [ProjectId] in table 'TestProjects'
ALTER TABLE [dbo].[TestProjects]
ADD CONSTRAINT [FK_TestProject_0_0]
    FOREIGN KEY ([ProjectId])
    REFERENCES [dbo].[Projects]
        ([ProjectId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_TestProject_0_0'
CREATE INDEX [IX_FK_TestProject_0_0]
ON [dbo].[TestProjects]
    ([ProjectId]);
GO

-- Creating foreign key on [TestProjectId] in table 'UnitTests'
ALTER TABLE [dbo].[UnitTests]
ADD CONSTRAINT [FK_UnitTest_0_0]
    FOREIGN KEY ([TestProjectId])
    REFERENCES [dbo].[TestProjects]
        ([TestProjectId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_UnitTest_0_0'
CREATE INDEX [IX_FK_UnitTest_0_0]
ON [dbo].[UnitTests]
    ([TestProjectId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------