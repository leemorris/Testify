using Leem.Testify.Poco;
using log4net;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SqlServerCe;
using System.IO;
using EntityFramework.Triggers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Leem.Testify
{
    public class TestifyContext : DbContextWithTriggers 
    {
        public TestifyContext()
            : base("name=TestifyDb")
        {
        }

        private static readonly ILog Log = LogManager.GetLogger(typeof(TestifyContext));

        public TestifyContext(string solutionName)
            : base(new SqlCeConnection(GetConnectionString(solutionName)),
             contextOwnsConnection: true)
        {
            Database.SetInitializer<TestifyContext>(new CreateDatabaseIfNotExists<TestifyContext>());
        }

        public DbSet<Poco.CoveredLine> CoveredLines { get; set; }

        public DbSet<Summary> Summary { get; set; }

        public DbSet<CodeModule> CodeModule { get; set; }

        public DbSet<CodeClass> CodeClass { get; set; }

        public DbSet<CodeMethod> CodeMethod { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<TestProject> TestProjects { get; set; }

        public DbSet<TestQueue> TestQueue { get; set; }
        
        public DbSet<Folder> Folders { get; set; }
        
        public DbSet<TestMethod> TestMethods { get; set; }

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

            modelBuilder.Entity<Project>()
                .HasKey(x => x.UniqueName);

            modelBuilder.Entity<TestProject>()
                .HasKey(x => x.UniqueName);

            modelBuilder.Entity<Folder>()
                .HasMany(p => p.Descendants)
                .WithMany(p => p.Ancestors);
                //.Map(m =>
                //    {
                //        m.MapLeftKey( "DescendantId" ); 
                     
                //        m.MapRightKey("AncestorId");
                //        m.ToTable("FolderClosure");
                //    });

            modelBuilder.Entity<FolderClosure>()
                .HasKey(fc => new { fc.AncestorId, fc.DescendantId, fc.Depth })
                .Property(t => t.FolderClosureId) 
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);;        

            modelBuilder.Entity<CoveredLine>()
                .HasKey(y => y.CoveredLineId)
                .HasRequired(m => m.Method)
                .WithMany()
                .WillCascadeOnDelete(true);
        }

        private static string GetConnectionString(string solutionName)
        {
            var hashCode = solutionName.GetHashCode();
            hashCode = hashCode > 0 ? hashCode : -hashCode;
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(solutionName), hashCode.ToString(), "TestifyCE.sdf;password=lactose");

            // Set connection string
            string connectionString = string.Format("Data Source={0}", path);
            Log.DebugFormat("ConnectionString = {0}", connectionString);
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