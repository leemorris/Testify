using System.Collections.Generic;

namespace Leem.Testify
{
    public class TrackedMethodMap
    {
        public string MethodName { get; set; }

        public List<MethodInfo> MethodInfos { get; set; }
    }
}