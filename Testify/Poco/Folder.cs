using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFramework.Triggers;
using log4net;

namespace Leem.Testify.Poco
{
    public class Folder : ITriggerable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Folder));
        private ITestifyQueries _queries;

        public Folder()
        {
            Classes = new HashSet<CodeClass>();
            Ancestors = new HashSet<Folder>();
            Descendants = new HashSet<Folder>();

            this.Triggers().Inserted += entry => {
                _queries = TestifyQueries.Instance;
                var parentFolderId = entry.Entity.Ancestors.Any() ? entry.Entity.Ancestors.First().FolderId : entry.Entity.FolderId;

                    _queries.AddRootFolderClosure(entry.Entity.FolderId);
                    // we are inserting a folder that has Ancestors
                    Log.DebugFormat("FolderId = {0}, Name = {1}, Depth = {2}, Parent FolderId = {3}", entry.Entity.FolderId, entry.Entity.FolderName, entry.Entity.Depth, parentFolderId);
                    _queries.AddFolderClosures(entry.Entity.FolderId, parentFolderId, entry.Entity.Depth);

            };
            //this.Triggers().Updating += entry => { entry.Entity.UpdateDateTime = DateTime.Now; };
            this.Triggers().Deleted += entry =>
            {
                entry.Entity.IsDeleted = true;
                //entry.Cancel(); // Cancels the deletion, but will persist changes with the same effects as EntityState.Modified
            };
        }

       
        public int FolderId { get; set; }
        public int Depth { get; set; }
        public virtual ICollection<CodeClass> Classes { get; set; }

        public string FolderName { get; set; }
        public Folder Parent { get; set; }
        public Project ParentProject { get; set; }

        public Boolean IsDeleted { get; set; }
    
        public virtual ICollection<Folder> Descendants { get; set; }


        public virtual ICollection<Folder> Ancestors { get; set; } 

    }
}
