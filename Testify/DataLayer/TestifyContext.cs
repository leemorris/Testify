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


namespace Leem.Testify.DataLayer
{
    public class TestifyContext : DbContext, IDisposable
    {

        public TestifyContext() : base("name=TestifyDb") { }


        public TestifyContext(string solutionName)
            : base(new SqlCeConnection(GetConnectionString(solutionName)),
             contextOwnsConnection: true)
        {
            Database.SetInitializer<TestifyContext>(new DropCreateDatabaseIfModelChanges<TestifyContext>());
        }
        private static string GetConnectionString(string solutionName) 
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(solutionName), "TestifyCE.sdf;password=lactose");

            // Set connection string
           string connectionString = string.Format("Data Source={0}", path);
           return connectionString;
        }
        public DbSet<UnitTest> UnitTests { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TestProject> TestProjects { get; set; }

        public DbSet<TrackedMethod> TrackedMethods { get; set; }
        public DbSet<CoveredLine> CoveredLines { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Poco.UnitTest>()
                .HasKey(x => x.UnitTestId)
                .Ignore(c => c.MetadataToken);
            modelBuilder.Entity<TrackedMethod>()
                .HasKey(x => x.UnitTestId);
            modelBuilder.Entity<TrackedMethod>()
                .Ignore(t => t.MetadataToken);
            modelBuilder.Entity<Project>()
                .HasKey(x => x.UniqueName);
            modelBuilder.Entity<TestProject>()
                .HasKey(x => x.UniqueName);
            modelBuilder.Entity<CoveredLine>()
                .HasMany(c => c.UnitTests)
                .WithMany(u => u.CoveredLines)
                .Map(mc => 
                {
                    mc.MapLeftKey("CoveredLineId");
                    mc.MapRightKey("UnitTestId");
                    mc.ToTable("CoveredLineUnitTest");
                });
                //.HasRequired(y => y.UnitTests)
                //.WithMany()
                //.HasForeignKey(x=>x.CoveredLineId);
                //.WithMany(x => x.CoveredLines);
            //modelBuilder.Entity<CoveredLine>()
            //    .Property(x => x.CoveredLineId)
            //    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            //modelBuilder.Entity<CoveredLine>()
            //    .HasMany(a => a.UnitTests);
            modelBuilder.Entity<UnitTest>().Ignore(c => c.MetadataToken);
            base.OnModelCreating(modelBuilder);

        }


    }

}
