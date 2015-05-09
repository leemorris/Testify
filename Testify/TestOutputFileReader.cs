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
            StreamReader file;
            resultType testOutput;
            //testOutput = TestOutput.LoadFromFile(path);
            
            try
            {

                file = new StreamReader(path);
                //Log.DebugFormat("Created StreamReader:");

                var reader = new XmlSerializer(typeof(resultType));
                //Log.DebugFormat("Created XmlSerializer:");
                testOutput = (resultType)reader.Deserialize(file);

            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error ReadCoverageFile: {0} Message{1}", path, ex.Message);
                throw;
            }
            
            file.Close();
            File.Delete(path);
            Log.DebugFormat("ReadCoverageFile for file name: {0} is Complete", path);
            return testOutput;

        } 
        
    }
}
