using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCover.Framework;
using OpenCover.Framework.Persistance;
using OpenCover.Framework.Model;
using log4net;

namespace Leem.Testify
{
    class CoverageSessionPersistance: BasePersistance
    {

        private readonly ILog _logger;

        /// <summary>
        /// Construct a file persistence object
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="logger"></param>
        public CoverageSessionPersistance(ICommandLine commandLine, ILog logger)
            : base(commandLine, logger)
        {
            _logger = logger;
        }


        ///// <summary>
        ///// Initialise the file persistence
        ///// </summary>
        ///// <param name="fileName">The filename to save to</param>
        public void Initialise(string fileName)
        {
           // _fileName = fileName;
        }

        public void Commit()
        {
            _logger.Info("Committing...");
            base.Commit();
        }
        public CoverageSession GetSession()
        {
            return CoverageSession;
        }

    }

}
