using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Leem.Testify.Domain.Model
{

    /// <summary>
    /// A method being tracked
    /// </summary>
       public class TrackedMethod
    {
        /// <summary>
        /// unique id assigned 
        /// </summary>
        [XmlAttribute("uid")]
        public UInt32 UniqueId { get; set; }

        /// <summary>
        /// The MetadataToken used to identify this entity within the assembly
        /// </summary>
        [XmlAttribute("token")]
        public int MetadataToken { get; set; }

        /// <summary>
        /// The name of the method being tracked
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// The reason/plugin why the method is being tracked
        /// </summary>
        [XmlAttribute("strategy")]
        public string Strategy { get; set; }


        public int UnitTestId { get; set; }
    }
}
