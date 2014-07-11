//
// OpenCover - S Wilde
//
// This source code is released under the MIT License; see the accompanying license file.
//

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Leem.Testify.Model
{
    /// <summary>
    /// a sequence point
    /// </summary>
    public class SequencePoint : InstrumentationPoint
    {
        [XmlAttribute("sl")]
        public int StartLine { get; set; }

        [XmlAttribute("sc")]
        public int StartColumn { get; set; }

        [XmlAttribute("el")]
        public int EndLine { get; set; }

        [XmlAttribute("ec")]
        public int EndColumn { get; set; }

        public new List<TrackedMethodRef> TrackedMethodRefs
        {
            get { return base.TrackedMethodRefs; }
            set { base.TrackedMethodRefs = value; }
        }
    }
}