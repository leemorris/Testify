using System.Data.Entity.Migrations;

namespace Leem.Testify.Migrations
{
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            this.Sql("CREATE INDEX [IX_CodeMethod_Name] ON [CodeMethod] ([Name] ASC)");
            this.Sql("CREATE INDEX [IX_CodeClass_Name] ON [CodeClass] ([Name] ASC)");
            this.Sql("CREATE INDEX [IX_TestMethodName] ON [UnitTest] ([TestMethodName] ASC)");
            this.Sql("CREATE INDEX [IX_CodeClassId] ON [CoveredLinePoco] ([Class_CodeClassId] ASC)");
        }

        public override void Down()
        {
        }
    }
}