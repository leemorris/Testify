namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Added_UnitTese_FileName : DbMigration
    {
        public override void Up()
        {
            //DropForeignKey("dbo.CodeMethod", "CodeClass_CodeClassId", "dbo.CodeClass");
            //RenameColumn(table: "dbo.CodeMethod", name: "CodeClass_CodeClassId", newName: "CodeClassId");
            AddColumn("dbo.CoveredLinePoco", "FileName", c => c.String(maxLength: 4000));
            //AddColumn("dbo.CodeModule", "FileName", c => c.String(maxLength: 4000));
            //AddColumn("dbo.CodeModule", "AssemblyName", c => c.String(maxLength: 4000));
            //AddColumn("dbo.CodeClass", "FileName", c => c.String(maxLength: 4000));
            //AddColumn("dbo.CodeClass", "Line", c => c.Int(nullable: false));
            //AddColumn("dbo.CodeClass", "Column", c => c.Int(nullable: false));
            //AddColumn("dbo.CodeMethod", "FileName", c => c.String(maxLength: 4000));
            //AddColumn("dbo.CodeMethod", "Line", c => c.Int(nullable: false));
            //AddColumn("dbo.CodeMethod", "Column", c => c.Int(nullable: false));
            AddColumn("dbo.TrackedMethod", "FileId", c => c.Int(nullable: false));
            AddColumn("dbo.UnitTest", "FilePath", c => c.String(maxLength: 4000));
            //AddColumn("dbo.TestQueue", "TestStartedDateTime", c => c.DateTime());
            //AlterColumn("dbo.CodeMethod", "CodeClassId", c => c.Int());
            //CreateIndex("dbo.CodeClass", "CodeModule_CodeModuleId");
            //CreateIndex("dbo.CodeClass", "Summary_SummaryId");
            //CreateIndex("dbo.CodeModule", "Summary_SummaryId");
            //CreateIndex("dbo.CodeMethod", "CodeClassId");
            //CreateIndex("dbo.CodeMethod", "Summary_SummaryId");
            //CreateIndex("dbo.CoveredLinePoco", "Class_CodeClassId");
            //CreateIndex("dbo.CoveredLinePoco", "Method_CodeMethodId");
            //CreateIndex("dbo.CoveredLinePoco", "Module_CodeModuleId");
            //            CreateIndex("dbo.UnitTest", "TestProjectUniqueName");
            //CreateIndex("dbo.TestProject", "ProjectUniqueName");
            //CreateIndex("dbo.TrackedMethodCoveredLinePoco", "TrackedMethod_UnitTestId");
            //CreateIndex("dbo.TrackedMethodCoveredLinePoco", "CoveredLinePoco_CoveredLineId");
            //CreateIndex("dbo.UnitTestTrackedMethod", "UnitTest_UnitTestId");
            //CreateIndex("dbo.UnitTestTrackedMethod", "TrackedMethod_UnitTestId");
            //CreateIndex("dbo.CoveredLineUnitTest", "CoveredLineId");
            //CreateIndex("dbo.CoveredLineUnitTest", "UnitTestId");
            //AddForeignKey("dbo.CodeMethod", "CodeClassId", "dbo.CodeClass", "CodeClassId");
        }
        
        public override void Down()
        {
            //DropForeignKey("dbo.CodeMethod", "CodeClassId", "dbo.CodeClass");
            DropIndex("dbo.CoveredLineUnitTest", new[] { "UnitTestId" });
            DropIndex("dbo.CoveredLineUnitTest", new[] { "CoveredLineId" });
            DropIndex("dbo.UnitTestTrackedMethod", new[] { "TrackedMethod_UnitTestId" });
            DropIndex("dbo.UnitTestTrackedMethod", new[] { "UnitTest_UnitTestId" });
            DropIndex("dbo.TrackedMethodCoveredLinePoco", new[] { "CoveredLinePoco_CoveredLineId" });
            DropIndex("dbo.TrackedMethodCoveredLinePoco", new[] { "TrackedMethod_UnitTestId" });
            DropIndex("dbo.TestProject", new[] { "ProjectUniqueName" });
            DropIndex("dbo.UnitTest", new[] { "TestProjectUniqueName" });
            DropIndex("dbo.CoveredLinePoco", new[] { "Module_CodeModuleId" });
            DropIndex("dbo.CoveredLinePoco", new[] { "Method_CodeMethodId" });
            DropIndex("dbo.CoveredLinePoco", new[] { "Class_CodeClassId" });
            //DropIndex("dbo.CodeMethod", new[] { "Summary_SummaryId" });
            //DropIndex("dbo.CodeMethod", new[] { "CodeClassId" });
            //DropIndex("dbo.CodeModule", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeClass", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeClass", new[] { "CodeModule_CodeModuleId" });
           // AlterColumn("dbo.CodeMethod", "CodeClassId", c => c.Int(nullable: false));
            //DropColumn("dbo.TestQueue", "TestStartedDateTime");
            DropColumn("dbo.UnitTest", "FilePath");
            DropColumn("dbo.TrackedMethod", "FileId");
            //DropColumn("dbo.CodeMethod", "Column");
            //DropColumn("dbo.CodeMethod", "Line");
            //DropColumn("dbo.CodeMethod", "FileName");
            //DropColumn("dbo.CodeClass", "Column");
            //DropColumn("dbo.CodeClass", "Line");
            //DropColumn("dbo.CodeClass", "FileName");
            //DropColumn("dbo.CodeModule", "AssemblyName");
            //DropColumn("dbo.CodeModule", "FileName");
            DropColumn("dbo.CoveredLinePoco", "FileName");
           // RenameColumn(table: "dbo.CodeMethod", name: "CodeClassId", newName: "CodeClass_CodeClassId");
            AddForeignKey("dbo.CodeMethod", "CodeClass_CodeClassId", "dbo.CodeClass", "CodeClassId", cascadeDelete: true);
        }
    }
}
