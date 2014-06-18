using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using log4net;
using System.IO;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using EnvDTE80;
using log4net.Appender;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.Text.Editor;


namespace Leem.Testify
{

    public class CoverageProvider//: IVsSolutionEvents3
    {

        internal SVsServiceProvider _serviceProvider;

        private IWpfTextView textView;
        private ICoverageService _coverageService;
        private EnvDTE.DTE _dte;
        private ConcurrentDictionary<int, Poco.CoveredLinePoco> _coveredLines;
        private string _solutionDirectory;
        private string _solutionName;
        private ILog Log = LogManager.GetLogger(typeof(CoverageProvider));
        private IVsSolution _solution = null;
        private EnvDTE.Solution _dteSolution = null;
        string _documentName;
        private int _currentVersion;
       
        private volatile bool _IsRebuilding;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public TestifyQueries Queries {get; set;}

        public CoverageProvider(IWpfTextView textView, EnvDTE.DTE dte, SVsServiceProvider serviceProvider, TestifyQueries testifyQueries)
        {
            _serviceProvider = serviceProvider;

            this.textView = textView;
            _coverageService = CoverageService.Instance;
            //textView.TextBuffer.Changed += TextView_Changed;
            _dte = dte;
            _coveredLines = new ConcurrentDictionary<int, Poco.CoveredLinePoco>();
            _coverageService.DTE = _dte;
            _dteSolution = dte.Solution;
            _coverageService.DTE = (DTE)dte;

            _coverageService.SolutionName = _dte.Solution.FullName;

            Queries = testifyQueries;
            _coverageService.Queries = Queries;
            Queries.ClassChanged += ClassChanged;

            Log.DebugFormat("Creating CoverageProvider - For First Time");
            _documentName = GetFileName(textView.TextBuffer);

            RebuildCoverage(textView.TextBuffer.CurrentSnapshot, _documentName);
        }
        internal virtual void ClassChanged(object sender, ClassChangedEventArgs e) 
        {
            if(_coveredLines.Any(x=> e.ChangedClasses.Contains(x.Value.Class.Name)))
            {
                _documentName = GetFileName(textView.TextBuffer);
                if (_documentName != null)
                {
                    RecreateCoverage(textView);
                    RebuildCoverage(textView.TextBuffer.CurrentSnapshot, _documentName);
                }

            }
        }
        public static string GetFileName(ITextBuffer buffer)
        {
            Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer bufferAdapter;
            buffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer), out bufferAdapter);
            if (bufferAdapter != null)
            {
                var persistFileFormat = bufferAdapter as IPersistFileFormat;
                string ppzsFilename = null;
                uint iii;
                if (persistFileFormat != null) persistFileFormat.GetCurFile(out ppzsFilename, out iii);
                return ppzsFilename;
            }
            else return null;
            return null;
        }



        private void RebuildCoverage( ITextSnapshot snapshotObject, string documentName)
        {
            try
            {
                _documentName = documentName;
                //Log.DebugFormat("Rebuilding Covered Lines - inside RebuildCoverageAsync Thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
                ITextSnapshot snapshot = (ITextSnapshot)snapshotObject;

                if (_dte != null && _dte.ActiveDocument != null && !_dte.ActiveDocument.Path.Contains(".test"))
                {
                    var fcm = GetFileCodeModel(documentName);
                    if (fcm == null)
                    {
                        Log.DebugFormat("ERROR File Code Model is null for Project Item:{0}", _dte.ActiveDocument.ProjectItem);
                    }
                    else 
                    {
                        System.Threading.Tasks.Task.Run(() => GetCoveredLinesFromCodeModel(fcm));
                    }

                    _currentVersion = snapshot.Version.VersionNumber;
                    
                }

 
               // Log.DebugFormat("Rebuilding Covered Lines - Complete Thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
                
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error RebuildingCoveredLines Error: {0} StackTrace: {1} InnerException: {2}", ex.Message,ex.StackTrace,ex.InnerException);
   
            }
        }

        public FileCodeModel GetFileCodeModel(string documentName)
        {

            ProjectItem projectItem = FindProjectItemInProject(_dte.ActiveDocument.ProjectItem.ContainingProject, documentName, true);

            if (projectItem == null)
            {
                Log.DebugFormat("ERROR projectItem is null for Active Document:{0}", _dte.ActiveDocument.FullName);
            }

            var fcm = projectItem.FileCodeModel;
            return fcm;
        }

        public void GetCoveredLinesFromCodeModel( FileCodeModel fcm)
        {
            IList<CodeElement> classes;
            IList<CodeElement> methods;

            CodeModelService.GetCodeBlocks(fcm, out classes, out methods);
            var coveredLines = new List<Poco.CoveredLinePoco>();
            var sw = new Stopwatch();
            sw.Restart();
            foreach (var codeClass in classes.ToList())
            {
                using (var context = new TestifyContext(fcm.DTE.Solution.FullName))
                {
                    var lines = Queries.GetCoveredLines(context, codeClass.FullName);

                    coveredLines.AddRange(lines);
                }
            }

            sw.Stop();

            Log.DebugFormat("Get Covered lines from Classes Elapsed Time {0} Number of Classes {1}", sw.ElapsedMilliseconds, classes.Count());
            lock (_coveredLines)
            {
                foreach (var line in coveredLines)
                {
                    var lineNumber = line.LineNumber;

                    var isAdded = _coveredLines.TryAdd(lineNumber, line);

                    if (!isAdded)
                    {
                        Poco.CoveredLinePoco currentValue;

                        var canGetValue = _coveredLines.TryGetValue(lineNumber,out currentValue);

                        var canUpdateValue = _coveredLines.TryUpdate(line.LineNumber, currentValue, line);

                        _coveredLines.AddOrUpdate(lineNumber, line, (oldKey, oldValue) => line);
                    }
                }
            }
        }

        public ProjectItem FindProjectItemInProject(EnvDTE.Project project, string name, bool recursive)
        {

            ProjectItem projectItem = null;

             try 
	            {	        
		            if (project.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                    {
                        if (project.ProjectItems != null && project.ProjectItems.Count > 0)
                        {
                            var returnedItem = _dteSolution.FindProjectItem(name);

                            if (returnedItem != null)
                            {
                                projectItem = returnedItem;
                                return projectItem;
                            }

                        }

                    }
                    else
                    {
                        Log.DebugFormat("Is a Solution Folder");
                        // if solution folder, one of its ProjectItems might be a real project
                        foreach (ProjectItem item in project.ProjectItems)
                        {
                            EnvDTE.Project realProject = item.Object as EnvDTE.Project;

                            if (realProject != null)
                            {
                                Log.DebugFormat("Calling FindProjectItemInProject recursively");
                                projectItem = (ProjectItem)FindProjectItemInProject(realProject, name, recursive);

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
                    Log.ErrorFormat("Error FindProjectItemInProject Error:{0}  StackTrace: {1} Inner Exception: {2}",ex.Message,ex.StackTrace, ex.InnerException);
	            }

             return projectItem;
        }



        private string GetAssemblyName(EnvDTE.Project proj)
        {
            try
            {
                var item = proj.Properties.GetEnumerator();

                while (item.MoveNext())
                {
                    var property = item.Current as EnvDTE.Property;

                    if (property.Name == "AssemblyName")
                    {
                        return property.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in GetAssemblyName: {0}", ex.Message);
            }

            return string.Empty;
        }


        internal ConcurrentDictionary<int, Poco.CoveredLinePoco> GetCoveredLines(IWpfTextView textView)
        {
            if(textView.TextBuffer.CurrentSnapshot.Version.VersionNumber != _currentVersion )
            {
                RecreateCoverage(textView);
            }

            return  _coveredLines;
        }

        internal void RecreateCoverage(IWpfTextView textView)
        {
            //Log.DebugFormat("Launching RebuildCoverage");

            var documentName = GetFileName(textView.TextBuffer);

            RebuildCoverage(textView.TextBuffer.CurrentSnapshot, documentName);
        }
         
       


    }
}


