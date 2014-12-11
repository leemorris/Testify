namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Added_summaries : DbMigration
    {
        public override void Up()
        {
            ////DropForeignKey("dbo.CoveredLinePocoTrackedMethod", "CoveredLinePoco_CoveredLineId", "dbo.CoveredLinePoco");
            ////DropForeignKey("dbo.CoveredLinePocoTrackedMethod", "TrackedMethod_UnitTestId", "dbo.TrackedMethod");
            ////DropForeignKey("dbo.TrackedMethodUnitTest", "TrackedMethod_UnitTestId", "dbo.TrackedMethod");
            ////DropForeignKey("dbo.TrackedMethodUnitTest", "UnitTest_UnitTestId", "dbo.UnitTest");
            ////DropIndex("dbo.CoveredLinePocoTrackedMethod", new[] { "CoveredLinePoco_CoveredLineId" });
            ////DropIndex("dbo.CoveredLinePocoTrackedMethod", new[] { "TrackedMethod_UnitTestId" });
            ////DropIndex("dbo.TrackedMethodUnitTest", new[] { "TrackedMethod_UnitTestId" });
            ////DropIndex("dbo.TrackedMethodUnitTest", new[] { "UnitTest_UnitTestId" });
            //CreateTable(
            //    "dbo.CodeModule",
            //    c => new
            //        {
            //            CodeModuleId = c.Int(nullable: false, identity: true),
            //            Name = c.String(maxLength: 4000),
            //            Summary_SummaryId = c.Int(),
            //        })
            //    .PrimaryKey(t => t.CodeModuleId)
            //    .ForeignKey("dbo.Summary", t => t.Summary_SummaryId)
            //    .Index(t => t.Summary_SummaryId);
            
            //CreateTable(
            //    "dbo.Summary",
            //    c => new
            //        {
            //            SummaryId = c.Int(nullable: false, identity: true),
            //            NumSequencePoints = c.Int(nullable: false),
            //            VisitedSequencePoints = c.Int(nullable: false),
            //            NumBranchPoints = c.Int(nullable: false),
            //            VisitedBranchPoints = c.Int(nullable: false),
            //            SequenceCoverage = c.Decimal(nullable: false, precision: 18, scale: 2),
            //            BranchCoverage = c.Decimal(nullable: false, precision: 18, scale: 2),
            //            MaxCyclomaticComplexity = c.Int(nullable: false),
            //            MinCyclomaticComplexity = c.Int(nullable: false),
            //        })
            //    .PrimaryKey(t => t.SummaryId);
            
            //CreateTable(
            //    "dbo.CodeClass",
            //    c => new
            //        {
            //            CodeClassId = c.Int(nullable: false, identity: true),
            //            Name = c.String(maxLength: 4000),
            //            Summary_SummaryId = c.Int(),
            //        })
            //    .PrimaryKey(t => t.CodeClassId)
            //    .ForeignKey("dbo.Summary", t => t.Summary_SummaryId)
            //    .Index(t => t.Summary_SummaryId);
            
            //CreateTable(
            //    "dbo.CodeMethod",
            //    c => new
            //        {
            //            CodeMethodId = c.Int(nullable: false, identity: true),
            //            Name = c.String(maxLength: 4000),
            //            Summary_SummaryId = c.Int(),
            //        })
            //    .PrimaryKey(t => t.CodeMethodId)
            //    .ForeignKey("dbo.Summary", t => t.Summary_SummaryId)
            //    .Index(t => t.Summary_SummaryId);
            
            //CreateTable(
            //    "dbo.TestQueue",
            //    c => new
            //        {
            //            TestQueueId = c.Int(nullable: false, identity: true),
            //            ProjectName = c.String(maxLength: 4000),
            //            IndividualTest = c.String(maxLength: 4000),
            //            TestRunId = c.Int(nullable: false),
            //            QueuedDateTime = c.DateTime(nullable: false),
            //        })
            //    .PrimaryKey(t => t.TestQueueId);
            
            //CreateTable(
            //    "dbo.TrackedMethodCoveredLinePoco",
            //    c => new
            //        {
            //            TrackedMethod_UnitTestId = c.Int(nullable: false),
            //            CoveredLinePoco_CoveredLineId = c.Int(nullable: false),
            //        })
            //    .PrimaryKey(t => new { t.TrackedMethod_UnitTestId, t.CoveredLinePoco_CoveredLineId })
            //    .ForeignKey("dbo.TrackedMethod", t => t.TrackedMethod_UnitTestId, cascadeDelete: true)
            //    .ForeignKey("dbo.CoveredLinePoco", t => t.CoveredLinePoco_CoveredLineId, cascadeDelete: true)
            //    .Index(t => t.TrackedMethod_UnitTestId)
            //    .Index(t => t.CoveredLinePoco_CoveredLineId);
            
            //CreateTable(
            //    "dbo.UnitTestTrackedMethod",
            //    c => new
            //        {
            //            UnitTest_UnitTestId = c.Int(nullable: false),
            //            TrackedMethod_UnitTestId = c.Int(nullable: false),
            //        })
            //    .PrimaryKey(t => new { t.UnitTest_UnitTestId, t.TrackedMethod_UnitTestId })
            //    .ForeignKey("dbo.UnitTest", t => t.UnitTest_UnitTestId, cascadeDelete: true)
            //    .ForeignKey("dbo.TrackedMethod", t => t.TrackedMethod_UnitTestId, cascadeDelete: true)
            //    .Index(t => t.UnitTest_UnitTestId)
            //    .Index(t => t.TrackedMethod_UnitTestId);
            
            //AddColumn("dbo.CoveredLinePoco", "Module_CodeModuleId", c => c.Int());
            //AddColumn("dbo.CoveredLinePoco", "Class_CodeClassId", c => c.Int());
            //AddColumn("dbo.CoveredLinePoco", "Method_CodeMethodId", c => c.Int());
            //AddForeignKey("dbo.CoveredLinePoco", "Module_CodeModuleId", "dbo.CodeModule", "CodeModuleId");
            //AddForeignKey("dbo.CoveredLinePoco", "Class_CodeClassId", "dbo.CodeClass", "CodeClassId");
            //AddForeignKey("dbo.CoveredLinePoco", "Method_CodeMethodId", "dbo.CodeMethod", "CodeMethodId");
            //CreateIndex("dbo.CoveredLinePoco", "Module_CodeModuleId");
            //CreateIndex("dbo.CoveredLinePoco", "Class_CodeClassId");
            //CreateIndex("dbo.CoveredLinePoco", "Method_CodeMethodId");
            //DropColumn("dbo.CoveredLinePoco", "Module");
            //DropColumn("dbo.CoveredLinePoco", "Class");
            //DropColumn("dbo.CoveredLinePoco", "Method");
            ////DropTable("dbo.CoveredLinePocoTrackedMethod");
            //DropTable("dbo.TrackedMethodUnitTest");
        }
        
        public override void Down()
        {
            //CreateTable(
            //    "dbo.TrackedMethodUnitTest",
            //    c => new
            //        {
            //            TrackedMethod_UnitTestId = c.Int(nullable: false),
            //            UnitTest_UnitTestId = c.Int(nullable: false),
            //        })
            //    .PrimaryKey(t => new { t.TrackedMethod_UnitTestId, t.UnitTest_UnitTestId });

            CreateTable(
                "dbo.CoveredLinePocoTrackedMethod",
                c => new
                    {
                        CoveredLinePoco_CoveredLineId = c.Int(nullable: false),
                        TrackedMethod_UnitTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CoveredLinePoco_CoveredLineId, t.TrackedMethod_UnitTestId });
            
            //AddColumn("dbo.CoveredLinePoco", "Method", c => c.String(maxLength: 4000));
            //AddColumn("dbo.CoveredLinePoco", "Class", c => c.String(maxLength: 4000));
            //AddColumn("dbo.CoveredLinePoco", "Module", c => c.String(maxLength: 4000));
            //DropIndex("dbo.UnitTestTrackedMethod", new[] { "TrackedMethod_UnitTestId" });
            //DropIndex("dbo.UnitTestTrackedMethod", new[] { "UnitTest_UnitTestId" });
            //DropIndex("dbo.TrackedMethodCoveredLinePoco", new[] { "CoveredLinePoco_CoveredLineId" });
            //DropIndex("dbo.TrackedMethodCoveredLinePoco", new[] { "TrackedMethod_UnitTestId" });
            //DropIndex("dbo.CodeMethod", new[] { "Summary_SummaryId" });
            //DropIndex("dbo.CodeClass", new[] { "Summary_SummaryId" });
            //DropIndex("dbo.CodeModule", new[] { "Summary_SummaryId" });
            //DropIndex("dbo.CoveredLinePoco", new[] { "Method_CodeMethodId" });
            //DropIndex("dbo.CoveredLinePoco", new[] { "Class_CodeClassId" });
            //DropIndex("dbo.CoveredLinePoco", new[] { "Module_CodeModuleId" });
            //DropForeignKey("dbo.UnitTestTrackedMethod", "TrackedMethod_UnitTestId", "dbo.TrackedMethod");
            //DropForeignKey("dbo.UnitTestTrackedMethod", "UnitTest_UnitTestId", "dbo.UnitTest");
            //DropForeignKey("dbo.TrackedMethodCoveredLinePoco", "CoveredLinePoco_CoveredLineId", "dbo.CoveredLinePoco");
            //DropForeignKey("dbo.TrackedMethodCoveredLinePoco", "TrackedMethod_UnitTestId", "dbo.TrackedMethod");
            //DropForeignKey("dbo.CodeMethod", "Summary_SummaryId", "dbo.Summary");
            //DropForeignKey("dbo.CodeClass", "Summary_SummaryId", "dbo.Summary");
            //DropForeignKey("dbo.CodeModule", "Summary_SummaryId", "dbo.Summary");
            //DropForeignKey("dbo.CoveredLinePoco", "Method_CodeMethodId", "dbo.CodeMethod");
            //DropForeignKey("dbo.CoveredLinePoco", "Class_CodeClassId", "dbo.CodeClass");
            //DropForeignKey("dbo.CoveredLinePoco", "Module_CodeModuleId", "dbo.CodeModule");
            //DropColumn("dbo.CoveredLinePoco", "Method_CodeMethodId");
            //DropColumn("dbo.CoveredLinePoco", "Class_CodeClassId");
            //DropColumn("dbo.CoveredLinePoco", "Module_CodeModuleId");
            //DropTable("dbo.UnitTestTrackedMethod");
            //DropTable("dbo.TrackedMethodCoveredLinePoco");
            //DropTable("dbo.TestQueue");
            //DropTable("dbo.CodeMethod");
            //DropTable("dbo.CodeClass");
            //DropTable("dbo.Summary");
            //DropTable("dbo.CodeModule");
            //CreateIndex("dbo.TrackedMethodUnitTest", "UnitTest_UnitTestId");
            //CreateIndex("dbo.TrackedMethodUnitTest", "TrackedMethod_UnitTestId");
            //CreateIndex("dbo.CoveredLinePocoTrackedMethod", "TrackedMethod_UnitTestId");
            //CreateIndex("dbo.CoveredLinePocoTrackedMethod", "CoveredLinePoco_CoveredLineId");
            //AddForeignKey("dbo.TrackedMethodUnitTest", "UnitTest_UnitTestId", "dbo.UnitTest", "UnitTestId", cascadeDelete: true);
            //AddForeignKey("dbo.TrackedMethodUnitTest", "TrackedMethod_UnitTestId", "dbo.TrackedMethod", "UnitTestId", cascadeDelete: true);
            //AddForeignKey("dbo.CoveredLinePocoTrackedMethod", "TrackedMethod_UnitTestId", "dbo.TrackedMethod", "UnitTestId", cascadeDelete: true);
            //AddForeignKey("dbo.CoveredLinePocoTrackedMethod", "CoveredLinePoco_CoveredLineId", "dbo.CoveredLinePoco", "CoveredLineId", cascadeDelete: true);
        }
    }
}
