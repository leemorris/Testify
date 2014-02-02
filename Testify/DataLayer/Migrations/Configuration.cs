namespace Leem.Testify.Migrations
{
    using System;
    using System.Data.Entity.Core;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Leem.Testify.DataLayer.TestifyContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }
//Enable-Migrations -ProjectName "DataLayer" -StartUpProjectName "Testify"
//Add-Migration TestProjectAssemblyName -ProjectName "DataLayer" -StartUpProjectName "Testify"

        protected override void Seed(Leem.Testify.DataLayer.TestifyContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            context.Database.ExecuteSqlCommand("CREATE INDEX idxClass ON CoveredLines (Class);");
            context.Database.ExecuteSqlCommand("CREATE INDEX idxModule ON CoveredLines (Module);");
            context.Database.ExecuteSqlCommand("CREATE INDEX idxTestMethodName ON UnitTests(TestMethodName)");
        }
    }
}
