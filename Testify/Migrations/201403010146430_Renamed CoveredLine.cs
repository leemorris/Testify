namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenamedCoveredLine : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.UnitTests", newName: "UnitTest");
            RenameTable(name: "dbo.TestProjects", newName: "TestProject");
            RenameTable(name: "dbo.Projects", newName: "Project");
            RenameTable(name: "dbo.TrackedMethods", newName: "TrackedMethod");
            RenameTable(name: "dbo.TrackedMethodUnitTests", newName: "TrackedMethodUnitTest");
            DropForeignKey("dbo.CoveredLineTrackedMethods", "CoveredLine_CoveredLineId", "dbo.CoveredLines");
            DropForeignKey("dbo.CoveredLineTrackedMethods", "TrackedMethod_UnitTestId", "dbo.TrackedMethods");
            DropForeignKey("dbo.CoveredLineUnitTest", "CoveredLineId", "dbo.CoveredLines");
            DropForeignKey("dbo.CoveredLineUnitTest", "UnitTestId", "dbo.UnitTests");
            DropIndex("dbo.CoveredLineTrackedMethods", new[] { "CoveredLine_CoveredLineId" });
            DropIndex("dbo.CoveredLineTrackedMethods", new[] { "TrackedMethod_UnitTestId" });
            DropIndex("dbo.CoveredLineUnitTest", new[] { "CoveredLineId" });
            DropIndex("dbo.CoveredLineUnitTest", new[] { "UnitTestId" });
            CreateTable(
                "dbo.CoveredLinePoco",
                c => new
                    {
                        CoveredLineId = c.Int(nullable: false, identity: true),
                        Module = c.String(maxLength: 4000)
                        ,
                        Class = c.String(maxLength: 4000),
                        Method = c.String(maxLength: 4000),
                        LineNumber = c.Int(nullable: false),
                        IsCode = c.Boolean(nullable: false),
                        IsCovered = c.Boolean(nullable: false),
                        IsSuccessful = c.Boolean(nullable: false),
                        UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CoveredLineId);
            
            CreateTable(
                "dbo.CoveredLinePocoTrackedMethod",
                c => new
                    {
                        CoveredLinePoco_CoveredLineId = c.Int(nullable: false),
                        TrackedMethod_UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLinePoco_CoveredLineId, t.TrackedMethod_UnitTestId })
                .ForeignKey("dbo.CoveredLinePoco", t => t.CoveredLinePoco_CoveredLineId, cascadeDelete: true)
                .ForeignKey("dbo.TrackedMethod", t => t.TrackedMethod_UnitTestId, cascadeDelete: true)
                .Index(t => t.CoveredLinePoco_CoveredLineId)
                .Index(t => t.TrackedMethod_UnitTestId);
            
            CreateTable(
                "dbo.CoveredLineUnitTest",
                c => new
                    {
                        CoveredLineId = c.Int(nullable: false),
                        UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLineId, t.UnitTestId })
                .ForeignKey("dbo.CoveredLinePoco", t => t.CoveredLineId, cascadeDelete: true)
                .ForeignKey("dbo.UnitTest", t => t.UnitTestId, cascadeDelete: true)
                .Index(t => t.CoveredLineId)
                .Index(t => t.UnitTestId);
            
            DropTable("dbo.CoveredLines");
            DropTable("dbo.CoveredLineTrackedMethods");
            DropTable("dbo.CoveredLineUnitTest");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.CoveredLineUnitTest",
                c => new
                    {
                        CoveredLineId = c.Int(nullable: false),
                        UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLineId, t.UnitTestId });
            
            CreateTable(
                "dbo.CoveredLineTrackedMethods",
                c => new
                    {
                        CoveredLine_CoveredLineId = c.Int(nullable: false),
                        TrackedMethod_UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLine_CoveredLineId, t.TrackedMethod_UnitTestId });
            
            CreateTable(
                "dbo.CoveredLines",
                c => new
                    {
                        CoveredLineId = c.Int(nullable: false, identity: true),
                        Module = c.String(maxLength: 4000),
                        Class = c.String(maxLength: 4000),
                        Method = c.String(maxLength: 4000),
                        LineNumber = c.Int(nullable: false),
                        IsCode = c.Boolean(nullable: false),
                        IsCovered = c.Boolean(nullable: false),
                        IsSuccessful = c.Boolean(nullable: false),
                        UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CoveredLineId);
            
            DropIndex("dbo.CoveredLineUnitTest", new[] { "UnitTestId" });
            DropIndex("dbo.CoveredLineUnitTest", new[] { "CoveredLineId" });
            DropIndex("dbo.CoveredLinePocoTrackedMethod", new[] { "TrackedMethod_UnitTestId" });
            DropIndex("dbo.CoveredLinePocoTrackedMethod", new[] { "CoveredLinePoco_CoveredLineId" });
            DropForeignKey("dbo.CoveredLineUnitTest", "UnitTestId", "dbo.UnitTest");
            DropForeignKey("dbo.CoveredLineUnitTest", "CoveredLineId", "dbo.CoveredLinePoco");
            DropForeignKey("dbo.CoveredLinePocoTrackedMethod", "TrackedMethod_UnitTestId", "dbo.TrackedMethod");
            DropForeignKey("dbo.CoveredLinePocoTrackedMethod", "CoveredLinePoco_CoveredLineId", "dbo.CoveredLinePoco");
            DropTable("dbo.CoveredLineUnitTest");
            DropTable("dbo.CoveredLinePocoTrackedMethod");
            DropTable("dbo.CoveredLinePoco");
            CreateIndex("dbo.CoveredLineUnitTest", "UnitTestId");
            CreateIndex("dbo.CoveredLineUnitTest", "CoveredLineId");
            CreateIndex("dbo.CoveredLineTrackedMethods", "TrackedMethod_UnitTestId");
            CreateIndex("dbo.CoveredLineTrackedMethods", "CoveredLine_CoveredLineId");
            AddForeignKey("dbo.CoveredLineUnitTest", "UnitTestId", "dbo.UnitTests", "UnitTestId", cascadeDelete: true);
            AddForeignKey("dbo.CoveredLineUnitTest", "CoveredLineId", "dbo.CoveredLines", "CoveredLineId", cascadeDelete: true);
            AddForeignKey("dbo.CoveredLineTrackedMethods", "TrackedMethod_UnitTestId", "dbo.TrackedMethods", "UnitTestId", cascadeDelete: true);
            AddForeignKey("dbo.CoveredLineTrackedMethods", "CoveredLine_CoveredLineId", "dbo.CoveredLines", "CoveredLineId", cascadeDelete: true);
            RenameTable(name: "dbo.TrackedMethodUnitTest", newName: "TrackedMethodUnitTests");
            RenameTable(name: "dbo.TrackedMethod", newName: "TrackedMethods");
            RenameTable(name: "dbo.Project", newName: "Projects");
            RenameTable(name: "dbo.TestProject", newName: "TestProjects");
            RenameTable(name: "dbo.UnitTest", newName: "UnitTests");
        }
    }
}
