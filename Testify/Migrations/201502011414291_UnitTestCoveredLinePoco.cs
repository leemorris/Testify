namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UnitTestCoveredLinePoco : DbMigration
    {
        public override void Up()
        {
           // RenameTable(name: "dbo.CoveredLineUnitTest", newName: "UnitTestCoveredLinePoco");
            //RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "CoveredLineId", newName: "CoveredLinePoco_CoveredLineId");
           // RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "UnitTestId", newName: "UnitTest_UnitTestId");
           // RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_UnitTestId", newName: "IX_UnitTest_UnitTestId");
            //RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_CoveredLineId", newName: "IX_CoveredLinePoco_CoveredLineId");
            DropPrimaryKey("dbo.UnitTestCoveredLinePoco");
            //AddColumn("dbo.CoveredLinePoco", "IsBranch", c => c.Boolean(nullable: false)); // was already there?
            //AddColumn("dbo.TrackedMethod", "FileName", c => c.String(maxLength: 4000));// was already there?
            //AddColumn("dbo.TestQueue", "Priority", c => c.Int(nullable: false));// was already there?
            //AddColumn("dbo.TestQueue", "UnitTest_UnitTestId", c => c.Int());// was already there?
            //AlterColumn("dbo.UnitTest", "LineNumber", c => c.Int(nullable: false));
            //AddPrimaryKey("dbo.UnitTestCoveredLinePoco", new[] { "UnitTest_UnitTestId", "CoveredLinePoco_CoveredLineId" });
            CreateIndex("dbo.TestQueue", "UnitTest_UnitTestId");
            AddForeignKey("dbo.TestQueue", "UnitTest_UnitTestId", "dbo.UnitTest", "UnitTestId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestQueue", "UnitTest_UnitTestId", "dbo.UnitTest");
            DropIndex("dbo.TestQueue", new[] { "UnitTest_UnitTestId" });
            DropPrimaryKey("dbo.UnitTestCoveredLinePoco");
            AlterColumn("dbo.UnitTest", "LineNumber", c => c.String(maxLength: 4000));
            DropColumn("dbo.TestQueue", "UnitTest_UnitTestId");
            DropColumn("dbo.TestQueue", "Priority");
            DropColumn("dbo.TrackedMethod", "FileName");
            DropColumn("dbo.CoveredLinePoco", "IsBranch");
            AddPrimaryKey("dbo.UnitTestCoveredLinePoco", new[] { "CoveredLineId", "UnitTestId" });
            RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_CoveredLinePoco_CoveredLineId", newName: "IX_CoveredLineId");
            RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_UnitTest_UnitTestId", newName: "IX_UnitTestId");
            RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "UnitTest_UnitTestId", newName: "UnitTestId");
            RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "CoveredLinePoco_CoveredLineId", newName: "CoveredLineId");
            RenameTable(name: "dbo.UnitTestCoveredLinePoco", newName: "CoveredLineUnitTest");
        }
    }
}
