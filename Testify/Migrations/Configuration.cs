namespace Leem.Testify.Migrations
{
    using System.Data.Entity;
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<Leem.Testify.TestifyContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            Database.SetInitializer<TestifyContext>(new DropCreateDatabaseAlways<TestifyContext>());
        }

        protected override void Seed(Leem.Testify.TestifyContext context)
        {

            context.Database.ExecuteSqlCommand("CREATE INDEX IX_CodeModuleId ON CodeMethod(CodeClassId)");
            context.Database.ExecuteSqlCommand("CREATE INDEX IX_CodeModuleName ON CodeModule(Name)");
            context.Database.ExecuteSqlCommand("CREATE INDEX IX_CodeClassName ON CodeClass(Name)");
            context.Database.ExecuteSqlCommand("CREATE INDEX IX_CodeMethodName ON CodeMethod(Name)");
            context.Database.ExecuteSqlCommand("CREATE INDEX IX_TestMethodName ON UnitTest(TestMethodName)");
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}