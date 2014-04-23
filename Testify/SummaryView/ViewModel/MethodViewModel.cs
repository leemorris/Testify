using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Leem.Testify
{
    public class MethodViewModel //: TreeViewItemViewModel
    {
        readonly Poco.CodeMethod _method;
        private ClassViewModel parent;

        //public MethodViewModel(Poco.CodeMethod method, ClassViewModel parentClass)
        //    : base(parentClass, false)
        //{
        //    parent = parentClass;
        //    _method = method;
        //}

        public string Name
        {
            get {
                string name = _method.Name.Substring(_method.Name.LastIndexOf("::") + 2); 
                name = name.Replace(".ctor",parent.ClassName);
                name = name.Replace("System.Int32", "int");
                return name;
            }
        }
        //#region Level

        //// Returns the number of nodes in the longest path to a leaf

        //public int Depth
        //{
        //    get
        //    {
        //        int max;
        //        if (Items.Count == 0)
        //            max = 0;
        //        else
        //            max = (int)Items.Max(r => r.Depth);
        //        return max + 1;
        //    }
        //}

        //private DirectoryRecord parent;

        //// Returns the maximum depth of all siblings

        //public int Level
        //{
        //    get
        //    {
        //        if (parent == null)
        //            return Depth;
        //        return parent.Level - 1;
        //    }
        //}

        //#endregion
    }
}