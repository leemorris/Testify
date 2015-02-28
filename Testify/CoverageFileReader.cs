using System;
using System.IO;
using System.Xml.Serialization;
using Leem.Testify.Model;
using log4net;

namespace Leem.Testify
{
    [Serializable]
    public class CoverageFileReader
    {
        private readonly ILog _log = LogManager.GetLogger(typeof (CoverageFileReader));

        public CoverageSession ReadCoverageFile(string path)
        {
            StreamReader file;

            var summary = new CoverageSession();

            try
            {
                _log.DebugFormat("ReadCoverageFile for file name: {0}", path);
                file = new StreamReader(path);

                var reader = new XmlSerializer(typeof (CoverageSession));

                summary = (CoverageSession) reader.Deserialize(file);
                _log.DebugFormat("BranchCoverage: {0}", summary.Summary.BranchCoverage);
                _log.DebugFormat("SequenceCoverage: {0}", summary.Summary.SequenceCoverage);
                _log.DebugFormat("VisitedBranchPoints: {0}", summary.Summary.VisitedBranchPoints);
                _log.DebugFormat("VisitedSequencePoints: {0}", summary.Summary.VisitedSequencePoints);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error ReadCoverageFile: {0} Message{1}", path, ex.Message);
                return null;
            }

            file.Close();
            return summary;
        }
    }
}