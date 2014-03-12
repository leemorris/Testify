using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Serialization;
using Leem.Testify.Model;
using System.IO;
using log4net;

namespace Leem.Testify
{
    [Serializable]
    public class CoverageFileReader
    {
        private ILog Log = LogManager.GetLogger(typeof(CoverageFileReader));

        public CoverageSession ReadCoverageFile(string path)
        {
            StreamReader file;
            CoverageSession summary = new CoverageSession();
            try
            {
                Log.DebugFormat("ReadCoverageFile for file name: {0}", path);
                file = new StreamReader(path);
                Log.DebugFormat("Created StreamReader:");
                
                XmlSerializer reader = new XmlSerializer(typeof(CoverageSession));
                Log.DebugFormat("Created XmlSerializer:");
                
                summary = (CoverageSession)reader.Deserialize(file);
                Log.DebugFormat("BranchCoverage: {0}", summary.Summary.BranchCoverage);
                Log.DebugFormat("SequenceCoverage: {0}", summary.Summary.SequenceCoverage);
                Log.DebugFormat("VisitedBranchPoints: {0}", summary.Summary.VisitedBranchPoints);
                Log.DebugFormat("VisitedSequencePoints: {0}", summary.Summary.VisitedSequencePoints);
                Log.DebugFormat("Branch Coverage: {0}", summary.Summary.BranchCoverage);
                Log.DebugFormat("Branch Coverage: {0}", summary.Summary.BranchCoverage);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error ReadCoverageFile: {0} Message{1}", path, ex.Message);
                throw;
            }
            
            file.Close();
            return summary;

        } 
        
    }
}
