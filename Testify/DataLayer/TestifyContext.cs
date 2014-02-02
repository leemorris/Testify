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
using Leem.Testify.Domain;


namespace Leem.Testify.DataLayer
{
    public class TestifyContext : DbContext, IDisposable
    {
        public TestifyContext()
        {
                
        }
                     
       public  TestifyContext (string solutionName)
       {
          
           var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
           //var directory = @"c:\WIP\Lactose\DataLayer\";
           
           var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(solutionName), "TestifyCE.sdf;password=lactose");
           
           // Set connection string
           var connectionString = string.Format("Data Source={0}", path);
           Database.DefaultConnectionFactory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0", "", connectionString);


       }
        public DbSet<UnitTest> UnitTests { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TestProject> TestProjects { get; set; }

        public DbSet<TrackedMethod> TrackedMethods { get; set; }
        public DbSet<CoveredLine> CoveredLines { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnitTest>()
                .HasKey(x => x.UnitTestId);
            modelBuilder.Entity<TrackedMethod>()
                .HasKey(x => x.MetadataToken);
            modelBuilder.Entity<TrackedMethod>().Property(x => x.MetadataToken).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None); 
            modelBuilder.Entity<Project>()
                .HasKey(x => x.UniqueName);
            modelBuilder.Entity<TestProject>()
                .HasKey(x => x.UniqueName);
            modelBuilder.Entity<CoveredLine>()
                .HasKey(x => x.CoveredLineId);
            modelBuilder.Entity<CoveredLine>()
                .Property(x => x.CoveredLineId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            base.OnModelCreating(modelBuilder);

        }


    }

}
