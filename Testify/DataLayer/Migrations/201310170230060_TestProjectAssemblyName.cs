namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TestProjectAssemblyName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TestProjects", "AssemblyName", c => c.String(maxLength: 4000));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TestProjects", "AssemblyName");
        }
    }
}
