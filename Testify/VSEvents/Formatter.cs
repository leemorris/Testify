using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Diagnostics;

using EnvDTE80;
using log4net;

namespace Leem.Testify
{
    ///todo Change name of this class
    class Formatter
    {
        IWpfTextView _view;
        ITextBuffer _textBuffer;
        //bool _isChangingText;
        DTE _dte;
        private CoverageService _coverageService;
        private ITextDocument _document;
        private UnitTestService _unitTestService;
        private ILog Log = LogManager.GetLogger(typeof(Formatter));

        public Formatter(IWpfTextView view, SVsServiceProvider serviceProvider)
        {
            _view = view;
            _textBuffer = _view.TextBuffer;
            _dte = (DTE)serviceProvider.GetService(typeof(DTE));
            _textBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.Text.ITextDocument), out _document);
            _unitTestService = new UnitTestService(_dte, _dte.Solution.FullName,System.IO.Path.GetFileNameWithoutExtension(_dte.Solution.FullName));

            _view.TextBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(TextBuffer_Changed);
    
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            List<int> linesEdited;
            if ( e.Changes.IncludesLineChanges)
            {

               // _isChangingText = true;///todo Is this needed?

                Debug.WriteLine("Line Changed");
                
                vsCMElement kind = vsCMElement.vsCMElementFunction;
                var textPoint = GetCursorTextPoint();

                var codeElement = GetMethodFromTextPoint(textPoint);
                var projectItem = _dte.ActiveDocument.ProjectItem;
               // RunTestsThatCoverMethod(textPoint, codeElement, projectItem);

            }
        }

        private async System.Threading.Tasks.Task RunTestsThatCoverMethod(TextPoint textPoint, CodeElement codeElement, ProjectItem projectItem)
        {
            await _unitTestService.RunTestsThatCoverLine(projectItem.ContainingProject.UniqueName, projectItem.Name, codeElement.Name, textPoint.Line);
        }



        private CodeElement GetMethodFromTextPoint(TextPoint textPoint)
        {
            // Discover every code element containing the insertion point.
            string elems = "";
            vsCMElement scopes = 0;
            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = textPoint.get_CodeElement(scope);
                if (elem != null)
                    elems += elem.Name +
                        " (" + scope.ToString() + ")\n";
            }
            Log.DebugFormat("The following elements contain the insertion point:\n\n {0}", elems);

            foreach (vsCMElement scope in Enum.GetValues(scopes.GetType()))
            {
                CodeElement elem = textPoint.get_CodeElement(vsCMElement.vsCMElementFunction);
                if (elem != null)
                {
                    return elem;
                }

            }
            return null;
        }


        private EnvDTE.TextPoint GetCursorTextPoint()
        {
            TextPoint textPoint = null;
            try
            {
                var document = _dte.ActiveDocument;
                textPoint = document.Selection.ActivePoint();
            }
            catch (System.Exception ex)
            {
            }

            return textPoint;

        }

        private CodeElement GetCodeElementAtTextPoint(vsCMElement codeElementKind, CodeElements codeElements, TextPoint textPoint)
{

	//EnvDTE.CodeElement objCodeElement = default(EnvDTE.CodeElement);
	EnvDTE.CodeElement resultCodeElement = default(EnvDTE.CodeElement);
	EnvDTE.CodeElements colCodeElementMembers = default(EnvDTE.CodeElements);
	EnvDTE.CodeElement memberCodeElement = default(EnvDTE.CodeElement);


    if (codeElements != null)
    {

        foreach (EnvDTE.CodeElement element in codeElements)
        {

            if (element.StartPoint.GreaterThan(textPoint))
            {
				// The code element starts beyond the point
            }
            else if (element.EndPoint.LessThan(textPoint))
            {
				// The code element ends before the point
            } 
            else
            {
                // The code element contains the point
                if (element.Kind == codeElementKind)
                {
					// Found
                    resultCodeElement = element;
				}

				// We enter in recursion, just in case there is an inner code element that also 
				// satisfies the conditions, for example, if we are searching a namespace or a class
                colCodeElementMembers = GetCodeElementMembers(element);

                memberCodeElement = GetCodeElementAtTextPoint(codeElementKind, colCodeElementMembers, textPoint);

                if ((memberCodeElement != null))
                {
					// A nested code element also satisfies the conditions
                    resultCodeElement = memberCodeElement;
				}

				break; // TODO: might not be correct. Was : Exit For

			}

		}

	}

    return resultCodeElement;

}

        private EnvDTE.CodeElements GetCodeElementMembers(CodeElement objCodeElement)
        {

            EnvDTE.CodeElements colCodeElements = default(EnvDTE.CodeElements);


            if (objCodeElement is EnvDTE.CodeNamespace)
            {
                colCodeElements = ((EnvDTE.CodeNamespace)objCodeElement).Members;


            }
            else if (objCodeElement is EnvDTE.CodeType)
            {
                colCodeElements = ((EnvDTE.CodeType)objCodeElement).Members;


            }
            else if (objCodeElement is EnvDTE.CodeFunction)
            {
                colCodeElements = ((EnvDTE.CodeFunction)objCodeElement).Parameters;

            }

            return colCodeElements;

        }


        //private void FormatCode(TextContentChangedEventArgs e)
        //{
        //    if (e.Changes != null)
        //    {
        //        for (int i = 0; i < e.Changes.Count; i++)
        //        {
        //            HandleChange(e.Changes[0].NewText);
        //        }
        //    }
        //}
        //private void HandleChange(string newText)
        //{
        //    ITextEdit edit = _view.TextBuffer.CreateEdit();
        //    edit.Insert(0, "Hello");
        //    edit.Apply();
        //}
      
    }
}
