using System.Xml.Serialization;

namespace Leem.Testify.Model
{
    /// <summary>
    /// a branch point
    /// </summary>
    public class BranchPoint : InstrumentationPoint
    {
        /// <summary>
        /// A path that can be taken
        /// </summary>
        [XmlAttribute("path")]
        public int Path { get; set; }
    }
}