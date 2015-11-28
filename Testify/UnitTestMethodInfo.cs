namespace Leem.Testify
{
    public class MethodInfo
    {
        public string MethodName { get; set; }
        public string ReflectionName { get; set; }
        public string NameInUnitTestFormat { get; set; }
        public int BeginLine { get; set; }
        public int BeginColumn { get; set; }
        public string FileName { get; set; }
    }
}