using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace Leem.Testify
{
    public static class CodeModelService
    {
        public static void GetCodeBlocks(FileCodeModel fcm, out IList<CodeElement> classes, out IList<CodeElement> methods)
        {
            var _methods = new List<CodeElement>();
            var _classes = new List<CodeElement>();

            foreach (CodeElement element in fcm.CodeElements)
            {
                CodeElements elements = GetCodeElementMembers(element);
                if (element.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement classElement in element.Children)
                    {
                        if (classElement.Kind == vsCMElement.vsCMElementClass)
                        {
                            _classes.Add(classElement);
                        }
                        foreach (CodeElement methodElement in classElement.Children)
                        {
                            foreach (CodeElement method in classElement.Children)
                            {
                                if (method.Kind == vsCMElement.vsCMElementFunction)
                                {
                                    _methods.Add(method);
                                }
                            }
                        }
                    }
                }
                
            }
            methods = _methods.ToList();
            classes = _classes.ToList();
        }

        private static List<CodeFunction2> GetConstructors(CodeClass2 codeClass)
        {
            List<CodeFunction2> constructors = new List<CodeFunction2>();

            foreach (CodeElement codeElement in codeClass.Members)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementFunction && codeElement.Name == codeClass.Name)
                {
                    constructors.Add((CodeFunction2)codeElement);
                }
            }
            return constructors;
        }
        public static CodeElements GetCodeElementMembers(CodeElement codeElement)
        {
            if (codeElement.Kind == vsCMElement.vsCMElementClass)
            {
                CodeClass2 codeClass = (CodeClass2)codeElement;
                return codeClass.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementInterface)
            {
                CodeInterface2 codeInterface = (CodeInterface2)codeElement;
                return codeInterface.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementStruct)
            {
                CodeStruct2 codeStruct = (CodeStruct2)codeElement;
                return codeStruct.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementEnum)
            {
                CodeEnum codeEnum = (CodeEnum)codeElement;
                return codeEnum.Members;
            }
            else if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
            {
                CodeNamespace codeNamespace = (CodeNamespace)codeElement;
                return codeNamespace.Members;
            }

            return null;
        }

        //public static string GetMethodFromLine(FileCodeModel fcm)
        //{
        //    var _methods = new List<CodeElement>();
        //    var _classes = new List<CodeElement>();

        //    foreach (CodeElement element in fcm.CodeElements)
        //    {
        //        CodeElements elements = GetCodeElementMembers(element);
        //        if (element.Kind == vsCMElement.vsCMElementNamespace)
        //        {
        //            foreach (CodeElement classElement in element.Children)
        //            {
        //                if (classElement.Kind == vsCMElement.vsCMElementClass)
        //                {
        //                    _classes.Add(classElement);
        //                }
        //                foreach (CodeElement methodElement in classElement.Children)
        //                {
        //                    foreach (CodeElement method in classElement.Children)
        //                    {
        //                        if (method.Kind == vsCMElement.vsCMElementFunction)
        //                        {
        //                            _methods.Add(method);
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //    }
        //    //methods = _methods.ToList();
        //    //classes = _classes.ToList();
        //    return "Method";
        //}

     
    }
}
