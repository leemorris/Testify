using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
            get {
                string name = _method.Name.Substring(_method.Name.LastIndexOf("::") + 2); 
                name = name.Replace(".ctor",parent.Name);
                name = name.Replace("System.Int32", "int");
                return name;
            }
        }

        public int Level { get { return 1; } }
 
    }
}