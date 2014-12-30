namespace Leem.Testify
{
    public class LineCoverageResult
    {
        public int CoveredLineId { get; set; }
        public int UnitTestId { get; set; }
        public string TestMethodName { get; set; }
        public string Module { get; set; }
        public string Class { get; set; }
        public string Method { get; set; }
        public int LineNumber { get; set; }
        public bool IsCovered { get; set; }
        public bool IsCode { get; set; }
        public bool IsSuccessful { get; set; }
    }
}