namespace Leem.Testify.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ChangeLastSuccessfulToDateTime : DbMigration
    {
        public override void Up()
        {
            //AlterColumn("dbo.UnitTests", "LastSuccessfulRunDatetime", c => c.DateTime());
        }

        public override void Down()
        {
            AlterColumn("dbo.UnitTests", "LastSuccessfulRunDatetime", c => c.String(maxLength: 4000));
        }
    }
}