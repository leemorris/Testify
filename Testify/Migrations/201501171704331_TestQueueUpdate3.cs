
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TestQueueUpdate3 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CoveredLinePocos", "IsBranch", c => c.Boolean(nullable: false));
            AddColumn("dbo.TrackedMethod", "FileName", c => c.String(maxLength: 4000));
            AddColumn("dbo.TestQueue", "Priority", c => c.Int(nullable: false));
            AddColumn("dbo.TestQueue", "UnitTest_UnitTestId", c => c.Int());
            AlterColumn("dbo.UnitTest", "LineNumber", c => c.Int(nullable: false));
            CreateIndex("dbo.TestQueue", "UnitTest_UnitTestId");
            AddForeignKey("dbo.TestQueue", "UnitTest_UnitTestId", "dbo.UnitTest", "UnitTestId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestQueue", "UnitTest_UnitTestId", "dbo.UnitTest");
            DropIndex("dbo.TestQueue", new[] { "UnitTest_UnitTestId" });
            AlterColumn("dbo.UnitTest", "LineNumber", c => c.String(maxLength: 4000));
            DropColumn("dbo.TestQueue", "UnitTest_UnitTestId");
            DropColumn("dbo.TestQueue", "Priority");
            DropColumn("dbo.TrackedMethod", "FileName");
            DropColumn("dbo.CoveredLinePocos", "IsBranch");
        }
    }

