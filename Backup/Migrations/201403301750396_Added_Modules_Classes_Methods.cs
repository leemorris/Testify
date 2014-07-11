namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Added_Modules_Classes_Methods : DbMigration
    {
        public override void Up()
        {
            //AddColumn("dbo.CodeClass", "CodeModule_CodeModuleId", c => c.Int());
            //AddColumn("dbo.CodeMethod", "CodeClass_CodeClassId", c => c.Int(nullable: false));
            //AddForeignKey("dbo.CodeClass", "CodeModule_CodeModuleId", "dbo.CodeModule", "CodeModuleId");
            //AddForeignKey("dbo.CodeMethod", "CodeClass_CodeClassId", "dbo.CodeClass", "CodeClassId", cascadeDelete: true);
            //CreateIndex("dbo.CodeClass", "CodeModule_CodeModuleId");
            //CreateIndex("dbo.CodeMethod", "CodeClass_CodeClassId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.CodeMethod", new[] { "CodeClass_CodeClassId" });
            DropIndex("dbo.CodeClass", new[] { "CodeModule_CodeModuleId" });
            DropForeignKey("dbo.CodeMethod", "CodeClass_CodeClassId", "dbo.CodeClass");
            DropForeignKey("dbo.CodeClass", "CodeModule_CodeModuleId", "dbo.CodeModule");
            DropColumn("dbo.CodeMethod", "CodeClass_CodeClassId");
            DropColumn("dbo.CodeClass", "CodeModule_CodeModuleId");
        }
    }
}
