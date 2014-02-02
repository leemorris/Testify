namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UnitTests",
                c => new
                    {
                        UnitTestId = c.Int(nullable: false, identity: true),
                        TestProjectUniqueName = c.String(maxLength: 4000),
                        IsSuccessful = c.Boolean(nullable: false),
                        MetadataToken = c.Int(),
                        TestMethodName = c.String(maxLength: 4000),
                        NumberOfAsserts = c.Int(),
                        Executed = c.Boolean(nullable: false),
                        Result = c.String(maxLength: 4000),
                        AssemblyName = c.String(maxLength: 4000),
                        LastRunDatetime = c.String(maxLength: 4000),
                        LastSuccessfulRunDatetime = c.String(maxLength: 4000),
                        TestDuration = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.UnitTestId)
                .ForeignKey("dbo.TestProjects", t => t.TestProjectUniqueName)
                .Index(t => t.TestProjectUniqueName);
            
            CreateTable(
                "dbo.TestProjects",
                c => new
                    {
                        UniqueName = c.String(nullable: false, maxLength: 4000),
                        Name = c.String(maxLength: 4000),
                        Path = c.String(maxLength: 4000),
                        OutputPath = c.String(maxLength: 4000),
                        ProjectUniqueName = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.UniqueName)
                .ForeignKey("dbo.Projects", t => t.ProjectUniqueName)
                .Index(t => t.ProjectUniqueName);
            
            CreateTable(
                "dbo.Projects",
                c => new
                    {
                        UniqueName = c.String(nullable: false, maxLength: 4000),
                        Name = c.String(maxLength: 4000),
                        Path = c.String(maxLength: 4000),
                        AssemblyName = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.UniqueName);
            
            CreateTable(
                "dbo.TrackedMethods",
                c => new
                    {
                        MetadataToken = c.Int(nullable: false),
                        UniqueId = c.Int(nullable: false),
                        Name = c.String(maxLength: 4000),
                        Strategy = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.MetadataToken);
            
            CreateTable(
                "dbo.CoveredLines",
                c => new
                    {
                        CoveredLineId = c.Int(nullable: false, identity: true),
                        Module = c.String(maxLength: 4000),
                        Class = c.String(maxLength: 4000),
                        Method = c.String(maxLength: 4000),
                        LineNumber = c.Int(nullable: false),
                        MetadataToken = c.Int(nullable: false),
                        IsCode = c.Boolean(nullable: false),
                        IsCovered = c.Boolean(nullable: false),
                        IsSuccessful = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CoveredLineId);
            
            CreateTable(
                "dbo.CoveredLineTrackedMethods",
                c => new
                    {
                        CoveredLine_CoveredLineId = c.Int(nullable: false),
                        TrackedMethod_MetadataToken = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLine_CoveredLineId, t.TrackedMethod_MetadataToken })
                .ForeignKey("dbo.CoveredLines", t => t.CoveredLine_CoveredLineId, cascadeDelete: true)
                .ForeignKey("dbo.TrackedMethods", t => t.TrackedMethod_MetadataToken, cascadeDelete: true)
                .Index(t => t.CoveredLine_CoveredLineId)
                .Index(t => t.TrackedMethod_MetadataToken);
            
            CreateTable(
                "dbo.TrackedMethodUnitTests",
                c => new
                    {
                        TrackedMethod_MetadataToken = c.Int(nullable: false),
                        UnitTest_UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.TrackedMethod_MetadataToken, t.UnitTest_UnitTestId })
                .ForeignKey("dbo.TrackedMethods", t => t.TrackedMethod_MetadataToken, cascadeDelete: true)
                .ForeignKey("dbo.UnitTests", t => t.UnitTest_UnitTestId, cascadeDelete: true)
                .Index(t => t.TrackedMethod_MetadataToken)
                .Index(t => t.UnitTest_UnitTestId);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.TrackedMethodUnitTests", new[] { "UnitTest_UnitTestId" });
            DropIndex("dbo.TrackedMethodUnitTests", new[] { "TrackedMethod_MetadataToken" });
            DropIndex("dbo.CoveredLineTrackedMethods", new[] { "TrackedMethod_MetadataToken" });
            DropIndex("dbo.CoveredLineTrackedMethods", new[] { "CoveredLine_CoveredLineId" });
            DropIndex("dbo.TestProjects", new[] { "ProjectUniqueName" });
            DropIndex("dbo.UnitTests", new[] { "TestProjectUniqueName" });
            DropForeignKey("dbo.TrackedMethodUnitTests", "UnitTest_UnitTestId", "dbo.UnitTests");
            DropForeignKey("dbo.TrackedMethodUnitTests", "TrackedMethod_MetadataToken", "dbo.TrackedMethods");
            DropForeignKey("dbo.CoveredLineTrackedMethods", "TrackedMethod_MetadataToken", "dbo.TrackedMethods");
            DropForeignKey("dbo.CoveredLineTrackedMethods", "CoveredLine_CoveredLineId", "dbo.CoveredLines");
            DropForeignKey("dbo.TestProjects", "ProjectUniqueName", "dbo.Projects");
            DropForeignKey("dbo.UnitTests", "TestProjectUniqueName", "dbo.TestProjects");
            DropTable("dbo.TrackedMethodUnitTests");
            DropTable("dbo.CoveredLineTrackedMethods");
            DropTable("dbo.CoveredLines");
            DropTable("dbo.TrackedMethods");
            DropTable("dbo.Projects");
            DropTable("dbo.TestProjects");
            DropTable("dbo.UnitTests");
        }
    }
}
