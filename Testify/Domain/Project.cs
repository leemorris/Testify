//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Leem.Testify
{
    using System;
    using System.Collections.Generic;
    
    public partial class Project
    {
        public Project()
        {
            this.TestProjects = new HashSet<TestProject>();
        }
    
        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string AssemblyName { get; set; }
    
        public virtual ICollection<TestProject> TestProjects { get; set; }
    }
}
