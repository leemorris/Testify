using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Serialization;
using Lactose.Domain;
using System.IO;

namespace Lactose
{
    [Serializable]
    public class CoverageFileReader
    {
        public CoverageSession ReadCoverageFile(string path)
        {
            XmlSerializer reader = new XmlSerializer(typeof(CoverageSession));
     
            StreamReader file = new StreamReader(path);
            CoverageSession summary = new CoverageSession();
            try
            {
                summary = (CoverageSession)reader.Deserialize(file);
            }
            catch (Exception ex)
            {
                
                throw;
            }
            
            file.Close();
            return summary;

        } 
        
    }
}
