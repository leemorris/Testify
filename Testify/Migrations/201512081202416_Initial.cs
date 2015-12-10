namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CodeClass",
                c => new
                    {
                        CodeClassId = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 4000),
                        FileName = c.String(maxLength: 4000),
                        Line = c.Int(nullable: false),
                        Column = c.Int(nullable: false),
                        CodeModule_CodeModuleId = c.Int(),
                        Summary_SummaryId = c.Int(),
                    })
                .PrimaryKey(t => t.CodeClassId)
                .ForeignKey("dbo.CodeModule", t => t.CodeModule_CodeModuleId)
                .ForeignKey("dbo.Summary", t => t.Summary_SummaryId)
                .Index(t => t.CodeModule_CodeModuleId)
                .Index(t => t.Summary_SummaryId);
            
            CreateTable(
                "dbo.CodeModule",
                c => new
                    {
                        CodeModuleId = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 4000),
                        FileName = c.String(maxLength: 4000),
                        AssemblyName = c.String(maxLength: 4000),
                        Summary_SummaryId = c.Int(),
                    })
                .PrimaryKey(t => t.CodeModuleId)
                .ForeignKey("dbo.Summary", t => t.Summary_SummaryId)
                .Index(t => t.Summary_SummaryId);
            
            CreateTable(
                "dbo.Summary",
                c => new
                    {
                        SummaryId = c.Int(nullable: false, identity: true),
                        NumSequencePoints = c.Int(nullable: false),
                        VisitedSequencePoints = c.Int(nullable: false),
                        NumBranchPoints = c.Int(nullable: false),
                        VisitedBranchPoints = c.Int(nullable: false),
                        SequenceCoverage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BranchCoverage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MaxCyclomaticComplexity = c.Int(nullable: false),
                        MinCyclomaticComplexity = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.SummaryId);
            
            CreateTable(
                "dbo.TestMethod",
                c => new
                    {
                        TestMethodId = c.Int(nullable: false, identity: true),
                        UniqueId = c.Int(nullable: false),
                        Name = c.String(maxLength: 4000),
                        Strategy = c.String(maxLength: 4000),
                        FileName = c.String(maxLength: 4000),
                        MetadataToken = c.Int(nullable: false),
                        TestProjectUniqueName = c.String(maxLength: 4000),
                        FilePath = c.String(maxLength: 4000),
                        IsSuccessful = c.Boolean(nullable: false),
                        TestMethodName = c.String(maxLength: 4000),
                        NumberOfAsserts = c.Int(),
                        Executed = c.Boolean(nullable: false),
                        Result = c.String(maxLength: 4000),
                        AssemblyName = c.String(maxLength: 4000),
                        LastRunDatetime = c.String(maxLength: 4000),
                        LastSuccessfulRunDatetime = c.DateTime(),
                        TestDuration = c.String(maxLength: 4000),
                        LineNumber = c.Int(nullable: false),
                        FailureLineNumber = c.Int(nullable: false),
                        FailureMessage = c.String(maxLength: 4000),
                        CodeModule_CodeModuleId = c.Int(),
                        Project_UniqueName = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.TestMethodId)
                .ForeignKey("dbo.CodeModule", t => t.CodeModule_CodeModuleId)
                .ForeignKey("dbo.Project", t => t.Project_UniqueName)
                .ForeignKey("dbo.TestProject", t => t.TestProjectUniqueName)
                .Index(t => t.TestProjectUniqueName)
                .Index(t => t.CodeModule_CodeModuleId)
                .Index(t => t.Project_UniqueName);
            
            CreateTable(
                "dbo.CoveredLine",
                c => new
                    {
                        CoveredLineId = c.Int(nullable: false, identity: true),
                        LineNumber = c.Int(nullable: false),
                        IsCode = c.Boolean(nullable: false),
                        IsCovered = c.Boolean(nullable: false),
                        IsSuccessful = c.Boolean(nullable: false),
                        UnitTestId = c.Int(nullable: false),
                        FileName = c.String(maxLength: 4000),
                        BranchCoverage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsBranch = c.Boolean(nullable: false),
                        FailureLineNumber = c.Int(nullable: false),
                        FailureMessage = c.String(maxLength: 4000),
                        Class_CodeClassId = c.Int(),
                        Method_CodeMethodId = c.Int(nullable: false),
                        Module_CodeModuleId = c.Int(),
                    })
                .PrimaryKey(t => t.CoveredLineId)
                .ForeignKey("dbo.CodeClass", t => t.Class_CodeClassId)
                .ForeignKey("dbo.CodeMethod", t => t.Method_CodeMethodId, cascadeDelete: true)
                .ForeignKey("dbo.CodeModule", t => t.Module_CodeModuleId)
                .Index(t => t.Class_CodeClassId)
                .Index(t => t.Method_CodeMethodId)
                .Index(t => t.Module_CodeModuleId);
            
            CreateTable(
                "dbo.CodeMethod",
                c => new
                    {
                        CodeMethodId = c.Int(nullable: false, identity: true),
                        CodeClassId = c.Int(),
                        Name = c.String(maxLength: 4000),
                        FileName = c.String(maxLength: 4000),
                        Line = c.Int(nullable: false),
                        Column = c.Int(nullable: false),
                        Summary_SummaryId = c.Int(),
                    })
                .PrimaryKey(t => t.CodeMethodId)
                .ForeignKey("dbo.CodeClass", t => t.CodeClassId)
                .ForeignKey("dbo.Summary", t => t.Summary_SummaryId)
                .Index(t => t.CodeClassId)
                .Index(t => t.Summary_SummaryId);
            
            CreateTable(
                "dbo.TestProject",
                c => new
                    {
                        UniqueName = c.String(nullable: false, maxLength: 4000),
                        Name = c.String(maxLength: 4000),
                        Path = c.String(maxLength: 4000),
                        OutputPath = c.String(maxLength: 4000),
                        ProjectUniqueName = c.String(maxLength: 4000),
                        AssemblyName = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.UniqueName)
                .ForeignKey("dbo.Project", t => t.ProjectUniqueName)
                .Index(t => t.ProjectUniqueName);
            
            CreateTable(
                "dbo.Project",
                c => new
                    {
                        UniqueName = c.String(nullable: false, maxLength: 4000),
                        Name = c.String(maxLength: 4000),
                        Path = c.String(maxLength: 4000),
                        AssemblyName = c.String(maxLength: 4000),
                        SourceControlVersion = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.UniqueName);
            
            CreateTable(
                "dbo.TestQueue",
                c => new
                    {
                        TestQueueId = c.Int(nullable: false, identity: true),
                        ProjectName = c.String(maxLength: 4000),
                        IndividualTest = c.String(maxLength: 4000),
                        TestRunId = c.Int(nullable: false),
                        QueuedDateTime = c.DateTime(nullable: false),
                        TestStartedDateTime = c.DateTime(),
                        Priority = c.Int(nullable: false),
                        TestMethod_TestMethodId = c.Int(),
                    })
                .PrimaryKey(t => t.TestQueueId)
                .ForeignKey("dbo.TestMethod", t => t.TestMethod_TestMethodId)
                .Index(t => t.TestMethod_TestMethodId);
            
            CreateTable(
                "dbo.CoveredLineTestMethod",
                c => new
                    {
                        CoveredLine_CoveredLineId = c.Int(nullable: false),
                        TestMethod_TestMethodId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLine_CoveredLineId, t.TestMethod_TestMethodId })
                .ForeignKey("dbo.CoveredLine", t => t.CoveredLine_CoveredLineId, cascadeDelete: true)
                .ForeignKey("dbo.TestMethod", t => t.TestMethod_TestMethodId, cascadeDelete: true)
                .Index(t => t.CoveredLine_CoveredLineId)
                .Index(t => t.TestMethod_TestMethodId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestQueue", "TestMethod_TestMethodId", "dbo.TestMethod");
            DropForeignKey("dbo.CodeClass", "Summary_SummaryId", "dbo.Summary");
            DropForeignKey("dbo.TestMethod", "TestProjectUniqueName", "dbo.TestProject");
            DropForeignKey("dbo.TestProject", "ProjectUniqueName", "dbo.Project");
            DropForeignKey("dbo.TestMethod", "Project_UniqueName", "dbo.Project");
            DropForeignKey("dbo.CoveredLineTestMethod", "TestMethod_TestMethodId", "dbo.TestMethod");
            DropForeignKey("dbo.CoveredLineTestMethod", "CoveredLine_CoveredLineId", "dbo.CoveredLine");
            DropForeignKey("dbo.CoveredLine", "Module_CodeModuleId", "dbo.CodeModule");
            DropForeignKey("dbo.CoveredLine", "Method_CodeMethodId", "dbo.CodeMethod");
            DropForeignKey("dbo.CodeMethod", "Summary_SummaryId", "dbo.Summary");
            DropForeignKey("dbo.CodeMethod", "CodeClassId", "dbo.CodeClass");
            DropForeignKey("dbo.CoveredLine", "Class_CodeClassId", "dbo.CodeClass");
            DropForeignKey("dbo.TestMethod", "CodeModule_CodeModuleId", "dbo.CodeModule");
            DropForeignKey("dbo.CodeModule", "Summary_SummaryId", "dbo.Summary");
            DropForeignKey("dbo.CodeClass", "CodeModule_CodeModuleId", "dbo.CodeModule");
            DropIndex("dbo.CoveredLineTestMethod", new[] { "TestMethod_TestMethodId" });
            DropIndex("dbo.CoveredLineTestMethod", new[] { "CoveredLine_CoveredLineId" });
            DropIndex("dbo.TestQueue", new[] { "TestMethod_TestMethodId" });
            DropIndex("dbo.TestProject", new[] { "ProjectUniqueName" });
            DropIndex("dbo.CodeMethod", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeMethod", new[] { "CodeClassId" });
            DropIndex("dbo.CoveredLine", new[] { "Module_CodeModuleId" });
            DropIndex("dbo.CoveredLine", new[] { "Method_CodeMethodId" });
            DropIndex("dbo.CoveredLine", new[] { "Class_CodeClassId" });
            DropIndex("dbo.TestMethod", new[] { "Project_UniqueName" });
            DropIndex("dbo.TestMethod", new[] { "CodeModule_CodeModuleId" });
            DropIndex("dbo.TestMethod", new[] { "TestProjectUniqueName" });
            DropIndex("dbo.CodeModule", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeClass", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeClass", new[] { "CodeModule_CodeModuleId" });
            DropTable("dbo.CoveredLineTestMethod");
            DropTable("dbo.TestQueue");
            DropTable("dbo.Project");
            DropTable("dbo.TestProject");
            DropTable("dbo.CodeMethod");
            DropTable("dbo.CoveredLine");
            DropTable("dbo.TestMethod");
            DropTable("dbo.Summary");
            DropTable("dbo.CodeModule");
            DropTable("dbo.CodeClass");
        }
    }
}
