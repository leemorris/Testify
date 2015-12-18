using log4net;
using System;
using System.IO;
using System.Xml.Serialization;

namespace Leem.Testify
{
    [Serializable]
    public class TestOutputFileReader
    {
        private readonly ILog Log = LogManager.GetLogger(typeof(TestOutputFileReader));

        public resultType ReadTestResultFile(string path)
        {
            Log.DebugFormat("ReadTestResultFile for file name: {0}", path);
            StreamReader file = null;
            resultType testOutput = null;

            try
            {
                file = new StreamReader(path);
                var reader = new XmlSerializer(typeof(resultType));
                testOutput = (resultType)reader.Deserialize(file);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error Reading Nunit Result File: {0} Message{1}", path, ex.Message);
                var queries = TestifyQueries.Instance;
                queries.SetAllQueuedTestsToNotRunning();
                queries.RemoveAllTestsFromQueue();

            }
            if (file != null)
            {
                file.Close();
                File.Delete(path);
            }

            Log.DebugFormat("ReadCoverageFile for file name: {0} is Complete", path);
            return testOutput;
        }
    }
}