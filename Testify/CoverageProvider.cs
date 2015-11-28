using EnvDTE;
using Leem.Testify.Poco;
using log4net;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Project = EnvDTE.Project;
using Task = System.Threading.Tasks.Task;

namespace Leem.Testify
{
    public class CoverageProvider
    {

        private readonly ILog _log = LogManager.GetLogger(typeof(CoverageProvider));
        private Dictionary<int, CoveredLine> _coveredLines;
        private readonly DTE _dte;

        private readonly Solution _dteSolution;
        private readonly IWpfTextView _textView;

        private volatile bool _IsRebuilding;
        private volatile bool _HasCoveredLinesBeenInitialized;
        private int _currentVersion;
        private string _documentName;
        private SVsServiceProvider _serviceProvider;
        private TestifyContext _context;
        private bool _isContextDirty;

        public CoverageProvider(IWpfTextView textView, DTE dte, SVsServiceProvider serviceProvider,
            TestifyQueries testifyQueries, TestifyContext context)
        {
            _serviceProvider = serviceProvider;
            _context = context;
            this._textView = textView;
            var coverageService = CoverageService.Instance;

            _dte = dte;
            _coveredLines = new Dictionary<int, CoveredLine>();
            coverageService.DTE = _dte;
            _dteSolution = dte.Solution;
            coverageService.DTE = dte;

            coverageService.SolutionName = _dte.Solution.FullName;
            SolutionName = _dte.Solution.FullName;

            Queries = testifyQueries;
            coverageService.Queries = Queries;
            Queries.ClassChanged += ClassChanged;

            _log.DebugFormat("Creating CoverageProvider - For First Time");
            _documentName = GetFileName(textView.TextBuffer);

            //RebuildCoverage(textView.TextBuffer.CurrentSnapshot, _documentName, context);
        }
        public TestifyContext Context {  set { _context = value; } }
        public TestifyQueries Queries { get; private set; }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        public string SolutionName { get; set; }
        protected virtual void ClassChanged(object sender, ClassChangedEventArgs e)
        {

            if (_coveredLines.Any(x => e.ChangedClasses.Contains(x.Value.Class.Name)))
            {
                _context = new TestifyContext(SolutionName);
                // _isContextDirty = true;
                _coveredLines = Queries.GetCoveredLinesForDocument(_context, _documentName).ToDictionary(x => x.LineNumber);

            }
        }

        public static string GetFileName(ITextBuffer buffer)
        {
            ITextDocument textDoc;
            var rc = buffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out textDoc);

            if (textDoc != null)
            {
                return textDoc.FilePath;
            }

            return null;
        }


        private void RebuildCoverage(ITextSnapshot snapshotObject, string documentName, TestifyContext context)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                _IsRebuilding = true;
                _documentName = documentName;
                //_log.DebugFormat("RebuildCoverage - Document: {0}", documentName);
                ITextSnapshot snapshot = snapshotObject;

                if (_dte != null && _dte.ActiveDocument != null)
                {
                    FileCodeModel fcm = GetFileCodeModel(documentName);
                    if (fcm == null)
                    {
                        _log.DebugFormat("ERROR File Code Model is null for Project Item:{0}",
                            _dte.ActiveDocument.ProjectItem);
                        _IsRebuilding = false;
                    }
                    else
                    {
                        if (_HasCoveredLinesBeenInitialized)
                        {
                            Task.Run(() => GetCoveredLinesFromCodeModel(fcm, documentName));
                        }
                        else
                        {
                            GetCoveredLinesFromCodeModel(fcm, documentName);
                        }

                    }

                    _currentVersion = snapshot.Version.VersionNumber;
                }

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error RebuildingCoveredLines Error: {0} StackTrace: {1} InnerException: {2}",
                    ex.Message, ex.StackTrace, ex.InnerException);
            }
            _log.DebugFormat("RebuildCoverage = {0} ms", sw.ElapsedMilliseconds);
        }

        public FileCodeModel GetFileCodeModel(string documentName)
        {
            
            FileCodeModel fcm = null;
            if (_dte.ActiveDocument != null && _dte.ActiveDocument.ProjectItem != null)
            {

                var projectItem = FindProjectItemInProject(_dte.ActiveDocument.ProjectItem.ContainingProject,
                    documentName, true);

                if (projectItem == null)
                {
                    _log.ErrorFormat("ERROR projectItem is null for Active Document:{0}", _dte.ActiveDocument.FullName);
                }
                else
                {
                    fcm = projectItem.FileCodeModel;
                }
            }
            return fcm;
        }

        private void GetCoveredLinesFromCodeModel(FileCodeModel fcm, string documentName)
        {
            var sw = Stopwatch.StartNew();
            IList<CodeElement> classes;
            IList<CodeElement> methods;
            var getCodeBlocksSw = Stopwatch.StartNew();

           // CodeModelService.GetCodeBlocks(fcm, out classes, out methods);
            //CodeModelService.GetCodeBlocks(fcm, out classes);
            _log.DebugFormat("Get Code Blocks Elapsed Time {0}", getCodeBlocksSw.ElapsedMilliseconds);
            var coveredLines = new List<CoveredLine>();

            IEnumerable<CoveredLine> lines;
            var solutionName = fcm.DTE.Solution.FullName;
            //using (var context = new TestifyContext(solutionName))
            //{
                //if (classes.Count > 0)
                //{
                    _log.Debug("Getting CoveredLines from Database");
                    //lines = Queries.GetCoveredLines(context, classes.First().FullName).ToList();
            //if(_isContextDirty)
            //{
            //    _context = new TestifyContext(SolutionName);
            //    _isContextDirty = false;
            //}

                    lines = Queries.GetCoveredLinesForDocument(_context, documentName).ToList();
                //}
                //else
                //{
                //    // the count of "classes' will be zero if the user closed the Solution and the FileCodeModel was disposed
                //    // just return an empty list because we are essentially terminated.
                //    lines = new List<CoveredLine>();
                //}

            //}

            //_log.DebugFormat("Queries.GetCoveredLines Elapsed Time {0}", getCodeBlocksSw.ElapsedMilliseconds);

            var addRangeSw = Stopwatch.StartNew();
            coveredLines.AddRange(lines);

            sw.Stop();

            //_log.DebugFormat("Get Covered lines from Classes Elapsed Time {0} Number of Classes {1}",
            //    sw.ElapsedMilliseconds, classes.Count());
            var lockAndLoadSw = Stopwatch.StartNew();
            lock (_coveredLines)
            {
                foreach (var line in coveredLines)
                {
                    int lineNumber = line.LineNumber;

                    //bool isAdded = _coveredLines.TryAdd(lineNumber, line);

                   // if (!isAdded)
                   // {
                        CoveredLine currentValue;

                        //_coveredLines.TryGetValue(lineNumber, out currentValue);

                        //_coveredLines.TryUpdate(line.LineNumber, line,currentValue );

                        _coveredLines[lineNumber] = line;
                    //}
                }
                var linesToBeRemoved= new List<int>();
                foreach (var line in _coveredLines)
                { 
                    if(!coveredLines.Any(x=>x.LineNumber== line.Value.LineNumber))
                    {
                        linesToBeRemoved.Add(line.Key);
                    }
                }
                foreach (var line in linesToBeRemoved)
                {
                    _coveredLines.Remove(line);
                }
                _HasCoveredLinesBeenInitialized = true;
                _IsRebuilding = false;
            }
            //_log.DebugFormat("Update _coveredLInes Elapsed Time {0}", lockAndLoadSw.ElapsedMilliseconds);
        }

        private ProjectItem FindProjectItemInProject(Project project, string name, bool recursive)
        {
            ProjectItem projectItem = null;

            try
            {
                if (project.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                {
                    if (project.ProjectItems != null && project.ProjectItems.Count > 0)
                    {
                        ProjectItem returnedItem = _dteSolution.FindProjectItem(name);

                        if (returnedItem != null)
                        {
                            projectItem = returnedItem;
                            return projectItem;
                        }
                    }
                }
                else
                {
                    _log.DebugFormat("Is a Solution Folder");
                    // if solution folder, one of its ProjectItems might be a real project
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        var realProject = item.Object as Project;

                        if (realProject != null)
                        {
                            _log.DebugFormat("Calling FindProjectItemInProject recursively");
                            projectItem = FindProjectItemInProject(realProject, name, recursive);

                            if (projectItem != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error FindProjectItemInProject Error:{0}  StackTrace: {1} Inner Exception: {2}",
                    ex.Message, ex.StackTrace, ex.InnerException);
            }

            return projectItem;
        }

        internal Dictionary<int, CoveredLine> GetCoveredLines(IWpfTextView view, TestifyContext context)
        {
            _log.DebugFormat("view.TextBuffer.CurrentSnapshot.Version.VersionNumber = {0}", view.TextBuffer.CurrentSnapshot.Version.VersionNumber);
            _log.DebugFormat("_currentVersion = {0}", _currentVersion);
            _log.DebugFormat("_HasCoveredLinesBeenInitialized = {0}", _HasCoveredLinesBeenInitialized);
            _log.DebugFormat("_IsRebuilding = {0}", _IsRebuilding);

            _log.DebugFormat("WasClosed = {0}", WasClosed);
            _log.DebugFormat("WasUpdated = {0}", WasUpdated);

            if (view.TextBuffer.CurrentSnapshot.Version.VersionNumber != _currentVersion
                || (_HasCoveredLinesBeenInitialized == false && _IsRebuilding == false)
                || WasClosed
                || WasUpdated)
            {
                _log.DebugFormat("Recreating Coverage " );
                //using (var context1 = new TestifyContext(_dte.Solution.FullName))
                //{
                    RecreateCoverage(view, context);
                //}
               
                WasClosed = false;
            }

            return _coveredLines;
        }

        internal void RecreateCoverage(IWpfTextView view, TestifyContext context)
        {
            var sw = Stopwatch.StartNew();
            if (!_IsRebuilding)
            {
                string documentName = GetFileName(view.TextBuffer);

                RebuildCoverage(view.TextBuffer.CurrentSnapshot, documentName, context);
            }

            _log.DebugFormat("RecreateCoverage = {0} ms", sw.ElapsedMilliseconds);
        }

        public bool WasClosed { get; set; }
        public bool WasUpdated { get; set; }
    }
}