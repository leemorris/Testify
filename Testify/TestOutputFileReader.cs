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
    public class TestOutputFileReader
    {
        private ILog Log = LogManager.GetLogger(typeof(TestOutputFileReader));

        public resultType ReadTestResultFile(string path)
        {
            Log.DebugFormat("ReadCoverageFile for file name: {0}", path);
            StreamReader file;
            resultType testOutput = new resultType();
            //testOutput = TestOutput.LoadFromFile(path);
            
            try
            {

                file = new StreamReader(path);
                //Log.DebugFormat("Created StreamReader:");

                XmlSerializer reader = new XmlSerializer(typeof(resultType));
                //Log.DebugFormat("Created XmlSerializer:");
                testOutput = (resultType)reader.Deserialize(file);

            }
            catch (Exception ex)
            {
                //Log.DebugFormat("Error ReadCoverageFile: {0} Message{1}", path, ex.Message);
                throw;
            }
            
            file.Close();
            System.IO.File.Delete(path);
            Log.DebugFormat("ReadCoverageFile for file name: {0} is Complete", path);
            return testOutput;

        } 
        
    }
}
