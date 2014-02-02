using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Leem.Lactose.Domain.Model;

namespace Leem.Lactose.DataLayer
{
    public class CoverageSessionContext:DbContext
    {
        public CoverageSessionContext()
        {

        }
        public DbSet<CoverageSession> CoverageSession { get; set; }
        public DbSet<BranchPoint> BranchPoint { get; set; }
        public DbSet<Class> Class { get; set; }
        public DbSet<File> File { get; set; }
        public DbSet<InstrumentationPoint> InstrumentationPoint { get; set; }
        public DbSet<Method> Method { get; set; }
        public DbSet<Module> Module  { get; set; }
        public DbSet<SequencePoint> SequencePoint { get; set; }
        public DbSet<SkippedEntity> SkippedEntity { get; set; }
        //public DbSet<SkippedMethod> SkippedMethod { get; set; }
        public DbSet<Summary> Summary { get; set; }
        public DbSet<TrackedMethod> TrackedMethod { get; set; }
        public DbSet<TrackedMethodRef> TrackedMethodRef { get; set; }


    }
}
