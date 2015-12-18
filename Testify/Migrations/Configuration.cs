namespace Leem.Testify.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<Leem.Testify.TestifyContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

    }
}