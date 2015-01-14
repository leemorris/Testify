//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace Leem.Testify
{
    public static class CodeModelService
    {
        public static void GetCodeBlocks(FileCodeModel fcm, out IList<CodeElement> classes,
            out IList<CodeElement> methods)
        {
            var _methods = new List<CodeElement>();
            var _classes = new List<CodeElement>();

            try
            {
                foreach (CodeElement element in fcm.CodeElements)
                {

                    if (element.Kind == vsCMElement.vsCMElementNamespace)
                    {
                        foreach (CodeElement classElement in element.Children)
                        {
                            if (classElement.Kind == vsCMElement.vsCMElementClass)
                            {
                                _classes.Add(classElement);
                            }

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
            catch (System.ObjectDisposedException)
            {
                // If the user closes the Solution, we may have a FileCodeModel that has been disposed
            }

            methods = _methods != null ? _methods.ToList() : new List<CodeElement>();

            classes = _classes != null ? _classes.ToList() : new List<CodeElement>();
        }

        private static List<CodeFunction2> GetConstructors(CodeClass2 codeClass)
        {
            var constructors = new List<CodeFunction2>();

            foreach (CodeElement codeElement in codeClass.Members)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementFunction && codeElement.Name == codeClass.Name)
                {
                    constructors.Add((CodeFunction2) codeElement);
                }
            }

            return constructors;
        }

        private static CodeElements GetCodeElementMembers(CodeElement codeElement)
        {
            if (codeElement.Kind == vsCMElement.vsCMElementClass)
            {
                var codeClass = (CodeClass2) codeElement;
                return codeClass.Members;
            }
            if (codeElement.Kind == vsCMElement.vsCMElementInterface)
            {
                var codeInterface = (CodeInterface2) codeElement;
                return codeInterface.Members;
            }
            if (codeElement.Kind == vsCMElement.vsCMElementStruct)
            {
                var codeStruct = (CodeStruct2) codeElement;
                return codeStruct.Members;
            }
            if (codeElement.Kind == vsCMElement.vsCMElementEnum)
            {
                var codeEnum = (CodeEnum) codeElement;
                return codeEnum.Members;
            }
            if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
            {
                var codeNamespace = (CodeNamespace) codeElement;
                return codeNamespace.Members;
            }

            return null;
        }
    }
}