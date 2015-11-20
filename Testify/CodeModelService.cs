//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using System.Diagnostics;
using log4net;

namespace Leem.Testify
{
    public static class CodeModelService
    {
        private static ILog _log = LogManager.GetLogger(typeof(CodeModelService));
        public static void GetCodeBlocks(FileCodeModel fcm, out IList<CodeElement> classes)
        {
            var sw = Stopwatch.StartNew();

   
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

                        }
                    }
                }
            }
            catch (System.ObjectDisposedException)
            {
                // If the user closes the Solution, we may have a FileCodeModel that has been disposed
            }
            _log.DebugFormat("GetCodeBlocks = {0} ms", sw.ElapsedMilliseconds);


            classes = _classes != null ? _classes.ToList() : new List<CodeElement>();
        }


        public static void GetCodeBlocks(FileCodeModel fcm, out IList<CodeElement> classes,
            out IList<CodeElement> methods)
        {
            //var sw = Stopwatch.StartNew();

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
            //_log.DebugFormat("RecreateCoverage = {0} ms", sw.ElapsedMilliseconds);
            methods = _methods != null ? _methods.ToList() : new List<CodeElement>();

            classes = _classes != null ? _classes.ToList() : new List<CodeElement>();
        }

      
    }
}