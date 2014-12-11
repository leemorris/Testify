using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Leem.Testify
{
    public class MethodViewModel : TreeViewItemViewModel
    {
        readonly Poco.CodeMethod _method;
        private ClassViewModel parent;

        public MethodViewModel(Poco.CodeMethod method, ClassViewModel parentClass)
            : base(parentClass, false)
        {
            parent = parentClass;
            _method = method;
        }

        public string Name
        {
            get
            {
                string name = _method.Name.Substring(_method.Name.LastIndexOf("::") + 2);
                name = name.Replace(".ctor", parent.Name)
                           .Replace(".cctor", parent.Name);

                int startOfParameters = name.IndexOf("(");
                var arguments = name.Substring(startOfParameters + 1, name.IndexOf(")") - name.IndexOf("(") - 1);
                var originalArgumentArray = arguments.Split(',');
                var outputArgumentArray = new string[originalArgumentArray.Length];
                string modifiedArgument = string.Empty;
                for (int i = 0; i < originalArgumentArray.Length; i++)
                {
                    var arg = originalArgumentArray[i];
                    int positionOfLastPeriod = arg.LastIndexOf(".");
                    if (positionOfLastPeriod > 0)
                    {
                        modifiedArgument = arg.Substring(positionOfLastPeriod + 1, arg.Length - positionOfLastPeriod - 1);
                    }
                    else
                    {
                        modifiedArgument = arg;
                    }

                    outputArgumentArray[i] = modifiedArgument.Replace(">", string.Empty)
                                                             .Replace("`1<", " ")
                                                             .Replace("Int32", "int");
                }

                var result = new StringBuilder();
                result.Append(name.Substring(0, startOfParameters));
                result.Append("(");
                result.Append(String.Join(", ", outputArgumentArray));
                result.Append(")");



                return result.ToString();
            }
        }
        public string FullName
        {
            get
            {

                return _method.Name;
            }
        }
        public string FileName
        {
            get { return _method.FileName; }
        }

        public int NumSequencePoints
        {
            get { return _method.Summary.NumSequencePoints; }
        }

        public int NumBranchPoints
        {
            get { return _method.Summary.NumBranchPoints; }
        }

        public decimal SequenceCoverage
        {
            get { return _method.Summary.SequenceCoverage; }
        }

        public int VisitedBranchPoints
        {
            get { return _method.Summary.VisitedBranchPoints; }
        }

        public int VisitedSequencePoints
        {
            get { return _method.Summary.VisitedSequencePoints; }
        }

        public decimal BranchCoverage
        {
            get { return _method.Summary.BranchCoverage; }
        }
        

        public int Line
        {
            get { return _method.Line; }
        }

        public int Column
        {
            get { return _method.Column; }
        }
        public int Level { get { return 1; } }
 
    }
}