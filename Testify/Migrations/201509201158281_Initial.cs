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
                "dbo.TrackedMethod",
                c => new
                    {
                        TrackedMethodId = c.Int(nullable: false, identity: true),
                        UniqueId = c.Int(nullable: false),
                        Name = c.String(maxLength: 4000),
                        Strategy = c.String(maxLength: 4000),
                        FileName = c.String(maxLength: 4000),
                        CodeModule_CodeModuleId = c.Int(),
                        Project_UniqueName = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.TrackedMethodId)
                .ForeignKey("dbo.CodeModule", t => t.CodeModule_CodeModuleId)
                .ForeignKey("dbo.Project", t => t.Project_UniqueName)
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
                        IsBranch = c.Boolean(nullable: false),
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
                "dbo.UnitTest",
                c => new
                    {
                        UnitTestId = c.Int(nullable: false, identity: true),
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
                        TrackedMethod_TrackedMethodId = c.Int(),
                    })
                .PrimaryKey(t => t.UnitTestId)
                .ForeignKey("dbo.TestProject", t => t.TestProjectUniqueName)
                .ForeignKey("dbo.TrackedMethod", t => t.TrackedMethod_TrackedMethodId)
                .Index(t => t.TestProjectUniqueName)
                .Index(t => t.TrackedMethod_TrackedMethodId);
            
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
                        UnitTest_UnitTestId = c.Int(),
                    })
                .PrimaryKey(t => t.TestQueueId)
                .ForeignKey("dbo.UnitTest", t => t.UnitTest_UnitTestId)
                .Index(t => t.UnitTest_UnitTestId);
            
            CreateTable(
                "dbo.CoveredLineTrackedMethod",
                c => new
                    {
                        CoveredLine_CoveredLineId = c.Int(nullable: false),
                        TrackedMethod_TrackedMethodId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLine_CoveredLineId, t.TrackedMethod_TrackedMethodId })
                .ForeignKey("dbo.CoveredLine", t => t.CoveredLine_CoveredLineId, cascadeDelete: true)
                .ForeignKey("dbo.TrackedMethod", t => t.TrackedMethod_TrackedMethodId, cascadeDelete: true)
                .Index(t => t.CoveredLine_CoveredLineId)
                .Index(t => t.TrackedMethod_TrackedMethodId);
            
            CreateTable(
                "dbo.UnitTestCoveredLine",
                c => new
                    {
                        UnitTest_UnitTestId = c.Int(nullable: false),
                        CoveredLine_CoveredLineId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.UnitTest_UnitTestId, t.CoveredLine_CoveredLineId })
                .ForeignKey("dbo.UnitTest", t => t.UnitTest_UnitTestId, cascadeDelete: true)
                .ForeignKey("dbo.CoveredLine", t => t.CoveredLine_CoveredLineId, cascadeDelete: true)
                .Index(t => t.UnitTest_UnitTestId)
                .Index(t => t.CoveredLine_CoveredLineId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestQueue", "UnitTest_UnitTestId", "dbo.UnitTest");
            DropForeignKey("dbo.CodeClass", "Summary_SummaryId", "dbo.Summary");
            DropForeignKey("dbo.UnitTest", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod");
            DropForeignKey("dbo.UnitTest", "TestProjectUniqueName", "dbo.TestProject");
            DropForeignKey("dbo.TrackedMethod", "Project_UniqueName", "dbo.Project");
            DropForeignKey("dbo.TestProject", "ProjectUniqueName", "dbo.Project");
            DropForeignKey("dbo.UnitTestCoveredLine", "CoveredLine_CoveredLineId", "dbo.CoveredLine");
            DropForeignKey("dbo.UnitTestCoveredLine", "UnitTest_UnitTestId", "dbo.UnitTest");
            DropForeignKey("dbo.CoveredLineTrackedMethod", "TrackedMethod_TrackedMethodId", "dbo.TrackedMethod");
            DropForeignKey("dbo.CoveredLineTrackedMethod", "CoveredLine_CoveredLineId", "dbo.CoveredLine");
            DropForeignKey("dbo.CoveredLine", "Module_CodeModuleId", "dbo.CodeModule");
            DropForeignKey("dbo.CoveredLine", "Method_CodeMethodId", "dbo.CodeMethod");
            DropForeignKey("dbo.CodeMethod", "Summary_SummaryId", "dbo.Summary");
            DropForeignKey("dbo.CodeMethod", "CodeClassId", "dbo.CodeClass");
            DropForeignKey("dbo.CoveredLine", "Class_CodeClassId", "dbo.CodeClass");
            DropForeignKey("dbo.TrackedMethod", "CodeModule_CodeModuleId", "dbo.CodeModule");
            DropForeignKey("dbo.CodeModule", "Summary_SummaryId", "dbo.Summary");
            DropForeignKey("dbo.CodeClass", "CodeModule_CodeModuleId", "dbo.CodeModule");
            DropIndex("dbo.UnitTestCoveredLine", new[] { "CoveredLine_CoveredLineId" });
            DropIndex("dbo.UnitTestCoveredLine", new[] { "UnitTest_UnitTestId" });
            DropIndex("dbo.CoveredLineTrackedMethod", new[] { "TrackedMethod_TrackedMethodId" });
            DropIndex("dbo.CoveredLineTrackedMethod", new[] { "CoveredLine_CoveredLineId" });
            DropIndex("dbo.TestQueue", new[] { "UnitTest_UnitTestId" });
            DropIndex("dbo.TestProject", new[] { "ProjectUniqueName" });
            DropIndex("dbo.UnitTest", new[] { "TrackedMethod_TrackedMethodId" });
            DropIndex("dbo.UnitTest", new[] { "TestProjectUniqueName" });
            DropIndex("dbo.CodeMethod", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeMethod", new[] { "CodeClassId" });
            DropIndex("dbo.CoveredLine", new[] { "Module_CodeModuleId" });
            DropIndex("dbo.CoveredLine", new[] { "Method_CodeMethodId" });
            DropIndex("dbo.CoveredLine", new[] { "Class_CodeClassId" });
            DropIndex("dbo.TrackedMethod", new[] { "Project_UniqueName" });
            DropIndex("dbo.TrackedMethod", new[] { "CodeModule_CodeModuleId" });
            DropIndex("dbo.CodeModule", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeClass", new[] { "Summary_SummaryId" });
            DropIndex("dbo.CodeClass", new[] { "CodeModule_CodeModuleId" });
            DropTable("dbo.UnitTestCoveredLine");
            DropTable("dbo.CoveredLineTrackedMethod");
            DropTable("dbo.TestQueue");
            DropTable("dbo.Project");
            DropTable("dbo.TestProject");
            DropTable("dbo.UnitTest");
            DropTable("dbo.CodeMethod");
            DropTable("dbo.CoveredLine");
            DropTable("dbo.TrackedMethod");
            DropTable("dbo.Summary");
            DropTable("dbo.CodeModule");
            DropTable("dbo.CodeClass");
        }
    }
}
