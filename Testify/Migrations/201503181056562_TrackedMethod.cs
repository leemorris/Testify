namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TrackedMethod : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.CoveredLineUnitTest", newName: "UnitTestCoveredLinePoco");
            DropForeignKey("dbo.UnitTestTrackedMethod", "UnitTest_UnitTestId", "dbo.UnitTest");
            DropForeignKey("dbo.UnitTestTrackedMethod", "TrackedMethod_UnitTestId", "dbo.TrackedMethod");
            DropForeignKey("dbo.CoveredLinePoco", "Method_CodeMethodId", "dbo.CodeMethod");
            DropForeignKey("dbo.TrackedMethodCoveredLinePoco", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod");
            DropForeignKey("dbo.UnitTest", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod");
            DropIndex("dbo.CoveredLinePoco", new[] { "Method_CodeMethodId" });
            DropIndex("dbo.UnitTestTrackedMethod", new[] { "UnitTest_UnitTestId" });
            DropIndex("dbo.UnitTestTrackedMethod", new[] { "TrackedMethod_UnitTestId" });
            RenameColumn(table: "dbo.TrackedMethodCoveredLinePoco", name: "TrackedMethod_UnitTestId", newName: "TrackedMethod_TrackedMethodId");
            RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "CoveredLineId", newName: "CoveredLinePoco_CoveredLineId");
            RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "UnitTestId", newName: "UnitTest_UnitTestId");
            RenameIndex(table: "dbo.TrackedMethodCoveredLinePoco", name: "IX_TrackedMethod_UnitTestId", newName: "IX_TrackedMethod_TrackedMethodId");
            RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_UnitTestId", newName: "IX_UnitTest_UnitTestId");
            RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_CoveredLineId", newName: "IX_CoveredLinePoco_CoveredLineId");
            DropPrimaryKey("dbo.TrackedMethod");
            DropPrimaryKey("dbo.UnitTestCoveredLinePoco");
            AddColumn("dbo.CoveredLinePoco", "IsBranch", c => c.Boolean(nullable: false));
            AddColumn("dbo.TrackedMethod", "TrackedMethodId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.TrackedMethod", "FileName", c => c.String(maxLength: 4000));
            AddColumn("dbo.UnitTest", "TrackedMethod_TrackedMethodId", c => c.Int());
            AddColumn("dbo.TestQueue", "Priority", c => c.Int(nullable: false));
            AddColumn("dbo.TestQueue", "UnitTest_UnitTestId", c => c.Int());
            AlterColumn("dbo.CoveredLinePoco", "Method_CodeMethodId", c => c.Int(nullable: false));
            AlterColumn("dbo.UnitTest", "LineNumber", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.TrackedMethod", "TrackedMethodId");
            AddPrimaryKey("dbo.UnitTestCoveredLinePoco", new[] { "UnitTest_UnitTestId", "CoveredLinePoco_CoveredLineId" });
            CreateIndex("dbo.CoveredLinePoco", "Method_CodeMethodId");
            CreateIndex("dbo.UnitTest", "TrackedMethod_TrackedMethodId");
            CreateIndex("dbo.TestQueue", "UnitTest_UnitTestId");
            AddForeignKey("dbo.UnitTest", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod", "TrackedMethodId");
            AddForeignKey("dbo.TestQueue", "UnitTest_UnitTestId", "dbo.UnitTest", "UnitTestId");
            AddForeignKey("dbo.CoveredLinePoco", "Method_CodeMethodId", "dbo.CodeMethod", "CodeMethodId", cascadeDelete: true);
            AddForeignKey("dbo.TrackedMethodCoveredLinePoco", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod", "TrackedMethodId", cascadeDelete: true);
            DropColumn("dbo.TrackedMethod", "UnitTestId");
            DropTable("dbo.UnitTestTrackedMethod");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.UnitTestTrackedMethod",
                c => new
                    {
                        UnitTest_UnitTestId = c.Int(nullable: false),
                        TrackedMethod_UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.UnitTest_UnitTestId, t.TrackedMethod_UnitTestId });
            
            AddColumn("dbo.TrackedMethod", "UnitTestId", c => c.Int(nullable: false, identity: true));
            DropForeignKey("dbo.TrackedMethodCoveredLinePoco", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod");
            DropForeignKey("dbo.CoveredLinePoco", "Method_CodeMethodId", "dbo.CodeMethod");
            DropForeignKey("dbo.TestQueue", "UnitTest_UnitTestId", "dbo.UnitTest");
            DropForeignKey("dbo.UnitTest", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod");
            DropIndex("dbo.TestQueue", new[] { "UnitTest_UnitTestId" });
            DropIndex("dbo.UnitTest", new[] { "TrackedMethod_TrackedMethodId" });
            DropIndex("dbo.CoveredLinePoco", new[] { "Method_CodeMethodId" });
            DropPrimaryKey("dbo.UnitTestCoveredLinePoco");
            DropPrimaryKey("dbo.TrackedMethod");
            AlterColumn("dbo.UnitTest", "LineNumber", c => c.String(maxLength: 4000));
            AlterColumn("dbo.CoveredLinePoco", "Method_CodeMethodId", c => c.Int());
            DropColumn("dbo.TestQueue", "UnitTest_UnitTestId");
            DropColumn("dbo.TestQueue", "Priority");
            DropColumn("dbo.UnitTest", "TrackedMethod_TrackedMethodId");
            DropColumn("dbo.TrackedMethod", "FileName");
            DropColumn("dbo.TrackedMethod", "TrackedMethodId");
            DropColumn("dbo.CoveredLinePoco", "IsBranch");
            AddPrimaryKey("dbo.UnitTestCoveredLinePoco", new[] { "CoveredLineId", "UnitTestId" });
            AddPrimaryKey("dbo.TrackedMethod", "UnitTestId");
            RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_CoveredLinePoco_CoveredLineId", newName: "IX_CoveredLineId");
            RenameIndex(table: "dbo.UnitTestCoveredLinePoco", name: "IX_UnitTest_UnitTestId", newName: "IX_UnitTestId");
            RenameIndex(table: "dbo.TrackedMethodCoveredLinePoco", name: "IX_TrackedMethod_TrackedMethodId", newName: "IX_TrackedMethod_UnitTestId");
            RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "UnitTest_UnitTestId", newName: "UnitTestId");
            RenameColumn(table: "dbo.UnitTestCoveredLinePoco", name: "CoveredLinePoco_CoveredLineId", newName: "CoveredLineId");
            RenameColumn(table: "dbo.TrackedMethodCoveredLinePoco", name: "TrackedMethod_TrackedMethodId", newName: "TrackedMethod_UnitTestId");
            CreateIndex("dbo.UnitTestTrackedMethod", "TrackedMethod_UnitTestId");
            CreateIndex("dbo.UnitTestTrackedMethod", "UnitTest_UnitTestId");
            CreateIndex("dbo.CoveredLinePoco", "Method_CodeMethodId");
            AddForeignKey("dbo.UnitTest", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod", "TrackedMethodId");
            AddForeignKey("dbo.TrackedMethodCoveredLinePoco", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod", "TrackedMethodId", cascadeDelete: true);
            AddForeignKey("dbo.CoveredLinePoco", "Method_CodeMethodId", "dbo.CodeMethod", "CodeMethodId");
            AddForeignKey("dbo.UnitTestTrackedMethod", "TrackedMethod_UnitTestId", "dbo.TrackedMethod", "UnitTestId", cascadeDelete: true);
            AddForeignKey("dbo.UnitTestTrackedMethod", "UnitTest_UnitTestId", "dbo.UnitTest", "UnitTestId", cascadeDelete: true);
            RenameTable(name: "dbo.UnitTestCoveredLinePoco", newName: "CoveredLineUnitTest");
        }
    }
}
