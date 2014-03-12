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
        private UnitTestService _service;
        private int _currentVersion;
        private TestifyQueries _queries ;
        private volatile bool _IsRebuilding;
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public CoverageProvider(IWpfTextView textView, EnvDTE.DTE dte, SVsServiceProvider serviceProvider, TestifyQueries testifyQueries)
        {
            _serviceProvider = serviceProvider;

            this.textView = textView;
            _coverageService = CoverageService.Instance;
            textView.TextBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(TextView_Changed);
            _dte = dte;
            _coveredLines = new ConcurrentDictionary<int, Poco.CoveredLinePoco>();
            _coverageService.DTE = _dte;
            _dteSolution = dte.Solution;
            _coverageService.DTE = (DTE)dte;

            _coverageService.SolutionName = _dte.Solution.FullName;

            _queries = testifyQueries;
            _coverageService.Queries = _queries;
            Log.DebugFormat("Creating CoverageProvider - For First Time");
            var documentName = GetFileName(textView.TextBuffer);

            RebuildCoverage(textView.TextBuffer.CurrentSnapshot, documentName);
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
        private void TextView_Changed(object sender, TextContentChangedEventArgs e)
        {
            List<int> linesEdited;
            if ( e.Changes.IncludesLineChanges)
            {
                Debug.WriteLine("Line Changed");
                //var editRange = new List<int>();
                //editRange.Add(_dte.ActiveDocument.Selection.ActivePoint.Line);
      
                //editRange.Add(editRange.First() - e.Changes.First().LineCountDelta);
                //editRange.Sort();

                //int delta = Math.Abs(e.Changes.First().LineCountDelta) + 1;

                //linesEdited = Enumerable.Range(editRange.First(), delta).ToList();

                var modifiedMethod = GetModifiedMethod();
                _queries.GetUnitTestsCoveringMethod(modifiedMethod);
                //CodeModelService.GetMethodFromLine(fcm);
                //_service.AddTestsToQueue(_coveredLines.Where(x=>x.Class == )
                //_coverageService.RunTestsThatCoverLine(linesEdited, _dte.ActiveDocument.ProjectItem, ref _testQueue,_dte.Solution.FullName);

            }
        }


        private void RebuildCoverage( ITextSnapshot snapshotObject, string documentName)
        {
            try
            {
              
                Log.DebugFormat("Rebuilding Covered Lines - inside RebuildCoverageAsync Thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
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
                        System.Threading.Tasks.Task.Run(() => GetCoveredLinesFromCodeModel( fcm));
                    }

                    _currentVersion = snapshot.Version.VersionNumber;
                    
                }

 
                Log.DebugFormat("Rebuilding Covered Lines - Complete Thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
                
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error RebuildingCoveredLines Error: {0} StackTrace: {1} InnerException: {2}", ex.Message,ex.StackTrace,ex.InnerException);
   
            }
        }

        public FileCodeModel GetFileCodeModel(string documentName)
        {
            Log.DebugFormat("Rebuilding Covered Lines -  we have an Active Document that is not a Test  IsRebuilding = {0}, Project: {1}", _IsRebuilding, _dte.ActiveDocument.Name);
            ProjectItem projectItem = FindProjectItemInProject(_dte.ActiveDocument.ProjectItem.ContainingProject, documentName, true);

            if (projectItem == null)
            {
                Log.DebugFormat("ERROR projectItem is null for Active Document:{0}", _dte.ActiveDocument.FullName);
            }

            var fcm = projectItem.FileCodeModel;
            return fcm;
        }

        private void GetCoveredLinesFromCodeModel( FileCodeModel fcm)
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
                    var lines = _queries.GetCoveredLines(context, codeClass.FullName);
                    // var lines = _coverageService.GetCoveredLinesForClass(codeClass.FullName);
                    Log.DebugFormat("Got Lines Elapsed Time  {0}", sw.ElapsedMilliseconds);
                    coveredLines.AddRange(lines);
                    Log.DebugFormat("Added Lines Elapsed Time  {0}", sw.ElapsedMilliseconds);
                }
            }
            sw.Stop();
            Log.DebugFormat("Get Covered lines from Classes Elapsed Time {0} Number of Classes {1}", sw.ElapsedMilliseconds, classes.Count());
            lock (_coveredLines)
            {
                foreach (var line in coveredLines)
                {
                    _coveredLines.TryAdd(line.LineNumber, line);
                }
            }
        }
        //private ProjectItem GetProjectItem(ProjectItems projectItems, string fileName)
        //{
        //    Log.DebugFormat("The FileName we are looking for: {0}", fileName);
        //    ProjectItem matchingItem = null ;
 
        //    foreach (ProjectItem item in projectItems)
        //    {
        //        if (item.ProjectItems.Count > 1)
        //        {
        //            var result = GetProjectItem(item.ProjectItems, fileName);
        //            if (result != null)
        //            {
        //                matchingItem = result;
        //                break;
        //            }
        //        }
        //        var path = item.Properties.Item("FullPath").Value;
        //        Log.DebugFormat("The Path of the current ProjectItem: {0}", path);
        //        if (fileName.Equals(path, StringComparison.OrdinalIgnoreCase))
        //        {
        //            Log.DebugFormat("Found a match");
        //            matchingItem = item;
        //            return matchingItem;

        //        }
        //    }
  
        //    return matchingItem;
        //}

        public ProjectItem FindProjectItemInProject(EnvDTE.Project project, string name, bool recursive)
        {
            Log.DebugFormat("FindProjectItemInProject -  Name: {0}", project.Name);
            Log.DebugFormat("FindProjectItemInProject -  Kind: {0}", project.Kind);
            
            ProjectItem projectItem = null;

             try 
	            {	        
		            if (project.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                    {
                        Log.DebugFormat("Is not a Solution Folder");
                        if (project.ProjectItems != null && project.ProjectItems.Count > 0)
                        {
                            var sw = new Stopwatch();
                            sw.Restart();


                            var returnedItem = _dteSolution.FindProjectItem(name);
                            Log.DebugFormat("Calling GetProjectItem for Project {0}", project.Name);
                            //var returnedItem = GetProjectItem(project.ProjectItems, name);
                            sw.Stop();
                            Log.DebugFormat("GetProjectItem took {0} ms", sw.ElapsedMilliseconds);
                            if (returnedItem != null)
                            {
                                projectItem = returnedItem;
                                Log.DebugFormat("Found ProjectItem - Returning");
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
        public void VerifyProjects()
        {

            List<EnvDTE.Project> vsProjects = new List<EnvDTE.Project>();
            var projects = new List<Poco.Project>();
            foreach (EnvDTE.Project project in _dte.Solution.Projects)
            {
                Log.DebugFormat("Verify project name: {0}", project.Name);
                var outputPath = GetProjectOutputBuildFolder(project);
                Log.DebugFormat("  outputPath: {0}", outputPath);
                var assemblyName = GetAssemblyName(project);
                Log.DebugFormat("  Assembly name: {0}", assemblyName);
                projects.Add(new Poco.Project
                {
                    Name = project.Name,
                    AssemblyName = assemblyName,
                    UniqueName = project.UniqueName,
                    Path = outputPath
                });
            }
            _queries.MaintainProjects(projects);

        }

        public string GetProjectOutputBuildFolder(EnvDTE.Project proj)
        {
            EnvDTE.Configuration activeConfiguration = default(EnvDTE.Configuration);
            EnvDTE.ConfigurationManager configManager = default(EnvDTE.ConfigurationManager);
            string outputPath = null;
            string absoluteOutputPath = null;
            string projectFolder = null;

            try
            {
                // Get the configuration manager of the project
                configManager = proj.ConfigurationManager;
                string assemblyName = string.Empty;

                if (configManager == null)
                {
                    return string.Empty;
                }
                else
                {
                    // Get the active project configuration
                    activeConfiguration = configManager.ActiveConfiguration;
                    assemblyName = proj.Properties.Item("AssemblyName").Value.ToString();
                    // Get the output folder
                    outputPath = activeConfiguration.Properties.Item("OutputPath").Value.ToString();

                    // The output folder can have these patterns:
                    // 1) "\\server\folder"
                    // 2) "drive:\folder"
                    // 3) "..\..\folder"
                    // 4) "folder"

                    if (outputPath.StartsWith((System.IO.Path.DirectorySeparatorChar + System.IO.Path.DirectorySeparatorChar).ToString()))
                    {
                        // This is the case 1: "\\server\folder"
                        absoluteOutputPath = outputPath;
                    }
                    else if (outputPath.Length >= 2 && outputPath[1] == System.IO.Path.VolumeSeparatorChar)
                    {
                        // This is the case 2: "drive:\folder"
                        absoluteOutputPath = outputPath;
                    }
                    else if (outputPath.IndexOf("..\\") != -1)
                    {
                        // This is the case 3: "..\..\folder"
                        projectFolder = System.IO.Path.GetDirectoryName(proj.FullName);

                        while (outputPath.StartsWith("..\\"))
                        {
                            outputPath = outputPath.Substring(3);
                            projectFolder = System.IO.Path.GetDirectoryName(projectFolder);
                        }
                        absoluteOutputPath = System.IO.Path.Combine(projectFolder, outputPath);
                    }
                    else
                    {
                        // This is the case 4: "folder"
                        projectFolder = System.IO.Path.GetDirectoryName(proj.FullName);
                        absoluteOutputPath = System.IO.Path.Combine(projectFolder, outputPath);
                    }

                    //MessageBox.Show("Output folder of " + proj.Name + ": " + absoluteOutputPath);
                    return System.IO.Path.Combine(absoluteOutputPath, assemblyName);
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return string.Empty;
            }

        }


        private string GetModifiedMethod()
        {
            Document activeDoc = _dte.ActiveDocument;
            TextSelection textSelection = activeDoc.Selection as TextSelection;
            CodeElement2 codeElement = textSelection.ActivePoint.get_CodeElement(vsCMElement.vsCMElementFunction) as CodeElement2;
            var methodName = codeElement.FullName;
            var positionOfLastPeriod = methodName.LastIndexOf('.');
            var newName = methodName.ReplaceAt(positionOfLastPeriod, "::");
            return newName;
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
//            Log.DebugFormat("GetCoveredLines for version: {0}, Current Version: {1}, Number of Covered Lines {2}", snapshotSpan.Snapshot.Version.VersionNumber, _currentVersion, _coveredLines.Count());
            if(textView.TextBuffer.CurrentSnapshot.Version.VersionNumber != _currentVersion )
            {
                Log.DebugFormat("Launching RebuildCoverage");
                var documentName = GetFileName(textView.TextBuffer);
                RebuildCoverage(textView.TextBuffer.CurrentSnapshot, documentName);
            }
            return  _coveredLines;
        }
         
        //#region Interface Methods


        //public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        //{
        //    Log.DebugFormat("Hit OnAfterOpenSolution in CoverageProvider");
        //    IVsSolution pSolution = _serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        //    string solutionDirectory;
        //    string solutionOptions;
        //    string solutionFile;
        //    pSolution.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionOptions);
        //    _solutionDirectory = solutionDirectory;
        //    _solutionName = Path.GetFileNameWithoutExtension(solutionFile);
        //    Log.DebugFormat("Solution Opened: {0}", _solutionName);

        //    _dte = (DTE)_serviceProvider.GetService(typeof(DTE));

        //    var projectEvents = ((Events2)_dte.Events).BuildEvents;
        //    projectEvents.OnBuildProjConfigDone += ProjectBuildEventHandler;
        //    _service = new UnitTestService(_dte, _solutionDirectory, _solutionName);

        //    //_service.SetS)olution( _solutionDirectory, _solutionName);
        //    VerifyProjects();
        //    //_service.RunAllNunitTestsForSolution();

        //    return VSConstants.S_OK;

        //}
        //private void ProjectBuildEventHandler(string project, string projectConfig, string platform, string solutionConfig, bool success)
        //{
        //    Log.DebugFormat("Project Build occurred project name: {0}", project);
        //    if (success)
        //    {
        //        //VerifyProjects();
        //        Log.DebugFormat("Project Build Successful for project name: {0}", project);

        //        _service.RunAllNunitTestsForProject(project);
        //    }

        //}
        //public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        //{
        //    // Do something
        //    return VSConstants.E_NOTIMPL;
        //}
        //public int OnAfterOpenProject(IVsHierarchy hierarchy, int fAdded)
        //{

        //    // VerifyProjects(_solution);
        //    return VSConstants.S_OK;
        //}
        //public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        //{
        //    //Do something
        //    return VSConstants.E_NOTIMPL;
        //}
        //public int OnAfterCloseSolution(object pUnkReserved)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnAfterMergeSolution(object pUnkReserved)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnBeforeCloseSolution(object pUnkReserved)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        //{ return VSConstants.E_NOTIMPL; }
        //public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        //{ return VSConstants.E_NOTIMPL; }
        //void OnBeforeQueryStatus(object sender, EventArgs e)
        //{ }
        //#endregion


    }
}


