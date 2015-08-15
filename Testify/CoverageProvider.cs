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
        private readonly ConcurrentDictionary<int, CoveredLinePoco> _coveredLines;
        private readonly DTE _dte;

        private readonly Solution _dteSolution;
        private readonly IWpfTextView _textView;

        private volatile bool _IsRebuilding;
        private volatile bool _HasCoveredLinesBeenInitialized;
        private int _currentVersion;
        private string _documentName;
        private SVsServiceProvider _serviceProvider;

        public CoverageProvider(IWpfTextView textView, DTE dte, SVsServiceProvider serviceProvider,
            TestifyQueries testifyQueries)
        {
            _serviceProvider = serviceProvider;

            this._textView = textView;
            var coverageService = CoverageService.Instance;

            _dte = dte;
            _coveredLines = new ConcurrentDictionary<int, CoveredLinePoco>();
            coverageService.DTE = _dte;
            _dteSolution = dte.Solution;
            coverageService.DTE = dte;

            coverageService.SolutionName = _dte.Solution.FullName;

            Queries = testifyQueries;
            coverageService.Queries = Queries;
            Queries.ClassChanged += ClassChanged;

            _log.DebugFormat("Creating CoverageProvider - For First Time");
            _documentName = GetFileName(textView.TextBuffer);

            RebuildCoverage(textView.TextBuffer.CurrentSnapshot, _documentName);
        }

        public TestifyQueries Queries { get; private set; }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        protected virtual void ClassChanged(object sender, ClassChangedEventArgs e)
        {
            if (_coveredLines.Any(x => e.ChangedClasses.Contains(x.Value.Class.Name)))
            {
                _documentName = GetFileName(_textView.TextBuffer);
                if (_documentName != null)
                {
                    RecreateCoverage(_textView);
                    RebuildCoverage(_textView.TextBuffer.CurrentSnapshot, _documentName);

                }
            }
        }

        public static string GetFileName(ITextBuffer buffer)
        {
            IVsTextBuffer bufferAdapter;
            buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out bufferAdapter);
            if (bufferAdapter != null)
            {
                var persistFileFormat = bufferAdapter as IPersistFileFormat;
                string ppzsFilename = null;
                uint iii;
                if (persistFileFormat != null) persistFileFormat.GetCurFile(out ppzsFilename, out iii);
                return ppzsFilename;
            }
            return null;
        }


        private void RebuildCoverage(ITextSnapshot snapshotObject, string documentName)
        {
            try
            {
                _IsRebuilding = true;
                _documentName = documentName;
                _log.DebugFormat("RebuildCoverage - Document: {0}", documentName);
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
                            Task.Run(() => GetCoveredLinesFromCodeModel(fcm));
                        }
                        else
                        {
                            GetCoveredLinesFromCodeModel(fcm);
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

        private void GetCoveredLinesFromCodeModel(FileCodeModel fcm)
        {
            var sw = Stopwatch.StartNew();
            IList<CodeElement> classes;
            IList<CodeElement> methods;
            var getCodeBlocksSw = Stopwatch.StartNew();

            CodeModelService.GetCodeBlocks(fcm, out classes, out methods);
            _log.DebugFormat("Get Code Blocks Elapsed Time {0}", getCodeBlocksSw.ElapsedMilliseconds);
            var coveredLines = new List<CoveredLinePoco>();

            IEnumerable<CoveredLinePoco> lines;
            var solutionName = fcm.DTE.Solution.FullName;
            using (var context = new TestifyContext(solutionName))
            {
                if (classes.Count > 0)
                {
                    lines = Queries.GetCoveredLines(context, classes.First().FullName).ToList();
                }
                else
                {
                    // the count of "classes' will be zero if the user closed the Solution and the FileCodeModel was disposed
                    // just return an empty list because we are essentially terminated.
                    lines = new List<CoveredLinePoco>();
                }

            }

            _log.DebugFormat("Queries.GetCoveredLines Elapsed Time {0}", getCodeBlocksSw.ElapsedMilliseconds);

            var addRangeSw = Stopwatch.StartNew();
            coveredLines.AddRange(lines);

            sw.Stop();

            _log.DebugFormat("Get Covered lines from Classes Elapsed Time {0} Number of Classes {1}",
                sw.ElapsedMilliseconds, classes.Count());
            var lockAndLoadSw = Stopwatch.StartNew();
            lock (_coveredLines)
            {
                foreach (var line in coveredLines)
                {
                    int lineNumber = line.LineNumber;

                    bool isAdded = _coveredLines.TryAdd(lineNumber, line);

                    if (!isAdded)
                    {
                        CoveredLinePoco currentValue;

                        _coveredLines.TryGetValue(lineNumber, out currentValue);

                        _coveredLines.TryUpdate(line.LineNumber, currentValue, line);

                        _coveredLines.AddOrUpdate(lineNumber, line, (oldKey, oldValue) => line);
                    }
                }
                _HasCoveredLinesBeenInitialized = true;
                _IsRebuilding = false;
            }
            _log.DebugFormat("Update _coveredLInes Elapsed Time {0}", lockAndLoadSw.ElapsedMilliseconds);
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

        internal ConcurrentDictionary<int, CoveredLinePoco> GetCoveredLines(IWpfTextView view)
        {
            if (view.TextBuffer.CurrentSnapshot.Version.VersionNumber != _currentVersion
                || (_HasCoveredLinesBeenInitialized == false && _IsRebuilding == false)
                || WasClosed
                || WasUpdated)
            {
                RecreateCoverage(view);
                WasClosed = false;
            }

            return _coveredLines;
        }

        internal void RecreateCoverage(IWpfTextView view)
        {
            if (!_IsRebuilding)
            {
                string documentName = GetFileName(view.TextBuffer);

                RebuildCoverage(view.TextBuffer.CurrentSnapshot, documentName);
            }
        }

        public bool WasClosed { get; set; }
        public bool WasUpdated { get; set; }
    }
}