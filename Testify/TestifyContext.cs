using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.IO;
using System.Data.Entity.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Leem.Testify.Poco;
using System.Data.SqlServerCe;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.ModelConfiguration.Configuration;
using log4net;

namespace Leem.Testify
{
    public class TestifyContext : DbContext
    {
        public TestifyContext() : base("name=TestifyDb") { }
        private static readonly ILog Log = LogManager.GetLogger(typeof(TestifyContext));

        public TestifyContext(string solutionName)
            : base(new SqlCeConnection(GetConnectionString(solutionName)),
             contextOwnsConnection: true)
        {
            Database.SetInitializer<TestifyContext>(new CreateDatabaseIfNotExists<TestifyContext>());
        }

        public DbSet<Poco.CoveredLinePoco> CoveredLines { get; set; }

        public DbSet<Summary> Summary { get; set; }

        public DbSet<CodeModule> CodeModule { get; set; }

        public DbSet<CodeClass> CodeClass { get; set; }

        public DbSet<CodeMethod> CodeMethod { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<TestProject> TestProjects { get; set; }

        public DbSet<TestQueue> TestQueue { get; set; }

        public DbSet<TrackedMethod> TrackedMethods { get; set; }

        public DbSet<UnitTest> UnitTests { get; set; }

        //public DbSet<Config> Configuration { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<TestQueue>()
                .HasKey(x => x.TestQueueId);

            modelBuilder.Entity<Summary>()
                .HasKey(x => x.SummaryId);

            modelBuilder.Entity<CodeModule>()
                .HasKey(x => x.CodeModuleId);

            modelBuilder.Entity<CodeClass>()
                .HasKey(x => x.CodeClassId);

            modelBuilder.Entity<CodeMethod>()
                .HasKey(x => x.CodeMethodId);  

            modelBuilder.Entity<UnitTest>()
                .HasKey(x => x.UnitTestId)
                .Ignore(c => c.MetadataToken);

            modelBuilder.Entity<TrackedMethod>()
                .HasKey(x => x.TrackedMethodId);

            modelBuilder.Entity<TrackedMethod>()
               .Ignore(t => t.MetadataToken);

            modelBuilder.Entity<Project>()
                .HasKey(x => x.UniqueName);

            modelBuilder.Entity<TestProject>()
                .HasKey(x => x.UniqueName);

            modelBuilder.Entity<CoveredLinePoco>()
                .HasKey(y => y.CoveredLineId)
                .HasRequired(m=>m.Method)
                .WithMany()
                .WillCascadeOnDelete(true);



        }

        private static string GetConnectionString(string solutionName) 
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(solutionName), "TestifyCE.sdf;password=lactose");

            // Set connection string
           string connectionString = string.Format("Data Source={0}", path);
           return connectionString;
        }

        public class MyConfiguration : DbConfiguration
        {
            public MyConfiguration()
            {

                SetProviderServices(
                    System.Data.Entity.SqlServerCompact.SqlCeProviderServices.ProviderInvariantName,
                    System.Data.Entity.SqlServerCompact.SqlCeProviderServices.Instance);
            }
        }
    }

}
