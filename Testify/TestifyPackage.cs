using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using EnvDTE;
using EnvDTE80;
using log4net;
using log4net.Appender;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Leem.Testify
{
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(TestifyCoverageWindow))]
    [ProvideToolWindow(typeof(UnitTestSelectorWindow), Style = VsDockStyle.AlwaysFloat, Window = "9197e117-9175-482a-9a0a-44f9af4f11f1")]

    [ProvideToolWindowVisibility(typeof(TestifyCoverageWindow), /*UICONTEXT_SolutionExists*/"f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [Guid(GuidList.guidTestifyPkgString)]
    public sealed class TestifyPackage : Package, IVsSolutionEvents3
    {
        public EventArgs e = null;
        private static System.Timers.Timer _timer;
        private DocumentEvents _documentEvents;
        private EnvDTE.DTE _dte;
        private ITestifyQueries _queries;
        private UnitTestService _service;
        private IVsSolution _solution = null;
        private uint _solutionCookie;
        private string _solutionDirectory;
        private string _solutionName;
        private volatile int _testRunId;
        private bool isFirstBuild = true;
        private ILog Log = LogManager.GetLogger(typeof(TestifyPackage));
        public TestifyPackage()
        {
            Log.DebugFormat(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            try
            {
                FileInfo file;
#if (DEBUG == true)
                file = new FileInfo(Environment.CurrentDirectory.ToString() + @"\log4net.config");
#endif

#if (DEBUG == false)
                file = new FileInfo(Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location) + @"\log4net.config");
#endif
                Log.DebugFormat("Log4net.config path: " + file.ToString());
                ConfigureLogging(file);

                //todo look into why this directory is needed
                //var directory = @"c:\WIP\Testify\DataLayer\";
                //var path = Path.Combine(directory, @"TestifyCE.sdf;password=lactose");

                AppDomain.CurrentDomain.SetData("DataDirectory", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                _timer = new System.Timers.Timer();
                _timer.Interval = 3000;
                _timer.Enabled = true;
                _timer.AutoReset = true;
                _timer.Elapsed += new ElapsedEventHandler(ProcessIndividualTestQueue);
                _timer.Elapsed += new ElapsedEventHandler(ProcessProjectLevelQueue);

 
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.CurrentCulture, ex.Message, this.ToString()));
            }
        }

        public delegate void CoverageChangedEventHandler(string className, string methodName);
        public void CheckForDatabase(string databasePath)
        {
            Log.DebugFormat("CheckForDatabase: {0}", databasePath);
            if (!System.IO.File.Exists(databasePath))
            {
                Log.ErrorFormat("Database was not found");

                // Get copy of blank database from the VSIX folder
                string initialDatabasePath = Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location) + @"\TestifyCE.sdf";
                try
                {
                    Log.ErrorFormat("Copying database from {0} to {1}", initialDatabasePath, databasePath);
                    System.IO.File.Copy(initialDatabasePath.ToString(), databasePath);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error Copying database " + ex.Message);
                }
            }
            else
            {
                Log.DebugFormat("Database was found");
            }
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
                    assemblyName = GetProjectPropertyByName(proj.Properties, "AssemblyName");
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
                    return System.IO.Path.Combine(absoluteOutputPath, assemblyName);
                }
            }
            catch (Exception ex)
            {
                Log.DebugFormat("GetProjectOutputBuildFolder could not determine folder name: {0}", ex.Message);
                return string.Empty;
            }
        }

        public void ProcessIndividualTestQueue(object source, ElapsedEventArgs e)
        {
            if (_service != null)
            {
                _service.ProcessIndividualTestQueue(++_testRunId);
            }

        }

        public void ProcessProjectLevelQueue(object source, ElapsedEventArgs e)
        {
            if (_service != null)
            {
                _service.ProcessProjectTestQueue(++_testRunId);
            }
        }

        public async void VerifyProjects(IVsSolution solution, string projectName)
        {
            //List<EnvDTE.Project> vsProjects = new List<EnvDTE.Project>();
            if (_queries == null)
            {
                object solutionName;
                solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out solutionName);
                _solutionName = solutionName.ToString();
                _queries = TestifyQueries.Instance;
                TestifyQueries.SolutionName = _solutionName;
            }

            var projects = new List<Poco.Project>();

            foreach (EnvDTE.Project project in _dte.Solution.Projects)
            {
                this._documentEvents = _dte.Events.DocumentEvents;
                this._documentEvents.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(this.OnDocumentSaved);
                var outputPath = GetProjectOutputBuildFolder(project);
                var assemblyName = GetProjectPropertyByName(project.Properties,"AssemblyName");

                Log.DebugFormat("Verify project name: {0}", project.Name);
                Log.DebugFormat("  outputPath: {0}", outputPath);
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

        public void UpdateMethodsAndClassesFromCodeFile(string filename)
        {
            IProjectContent project = new CSharpProjectContent();

            project.SetAssemblyName(filename);
            project = AddFileToProject(project, filename);

            var classes = new List<string>();

            var typeDefinitions = project.TopLevelTypeDefinitions;

            foreach (var typeDef in typeDefinitions)
            {
                classes.Add(typeDef.ReflectionName);
                if (typeDef.Kind == TypeKind.Class)
                {
                    var methods = typeDef.Methods;
                    _queries.UpdateMethods(typeDef, methods, filename);
                }

            }

        }


        private IProjectContent AddFileToProject(IProjectContent project, string fileName)
        {
            var code = string.Empty;
            try
            {
                code = File.ReadAllText(fileName);
            }
            catch (Exception)
            {
                Log.ErrorFormat("Could not find file to AddFileToProject, Name: {0}", fileName);
            }

            var syntaxTree = new CSharpParser().Parse(code, fileName);
            var unresolvedFile = syntaxTree.ToTypeSystem();

            if (syntaxTree.Errors.Count == 0)
            {
                project = project.AddOrUpdateFiles(unresolvedFile);
            }
            return project;
        }

        Lazy<IList<IUnresolvedAssembly>> builtInLibs = new Lazy<IList<IUnresolvedAssembly>>(
            delegate
            {
                Assembly[] assemblies = {
		//			        typeof(object).Assembly, // mscorlib
		//			        typeof(Uri).Assembly, // System.dll
		//			        typeof(System.Linq.Enumerable).Assembly, // System.Core.dll
        //					typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
        //					typeof(System.Drawing.Bitmap).Assembly, // System.Drawing.dll
        //					typeof(Form).Assembly, // System.Windows.Forms.dll
		//			        typeof(ICSharpCode.NRefactory.TypeSystem.IProjectContent).Assembly,
				        };
                IUnresolvedAssembly[] projectContents = new IUnresolvedAssembly[assemblies.Length];
                Stopwatch total = Stopwatch.StartNew();
                Parallel.For(
                    0, assemblies.Length,
                    delegate(int i)
                    {
                        Stopwatch w = Stopwatch.StartNew();
                        CecilLoader loader = new CecilLoader();
                        projectContents[i] = loader.LoadAssemblyFile(assemblies[i].Location);
                        Debug.WriteLine(Path.GetFileName(assemblies[i].Location) + ": " + w.Elapsed);
                    });
                Debug.WriteLine("Total: " + total.Elapsed);
                return projectContents;
            });



        public void VerifyProjects(EnvDTE.Project project)
        {
            var projects = new List<Poco.Project>();
            var outputPath = GetProjectOutputBuildFolder(project);
            var assemblyName = GetProjectPropertyByName(project.Properties, "AssemblyName");

            projects.Add(new Poco.Project
            {
                Name = project.Name,
                AssemblyName = assemblyName,
                UniqueName = project.UniqueName,
                Path = outputPath
            });
            _queries = TestifyQueries.Instance;
            TestifyQueries.SolutionName = _dte.Solution.FullName;

            _queries.MaintainProjects(projects);
        }

        private void ConfigureLogging(FileInfo file)
        {
            log4net.Config.XmlConfigurator.Configure(file);
            var appenders = Log.Logger.Repository.GetAppenders();

            log4net.Repository.Hierarchy.Hierarchy h =
            (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
            foreach (var a in h.Root.Appenders)
            {
                if (a is FileAppender)
                {
                    FileAppender fa = (FileAppender)a;

                    FileInfo fileInfo = new FileInfo(fa.File);
                    var logFileLocation = string.Format(@"{0}\Testify\{1}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileInfo.Name);
                    fa.File = logFileLocation;
                    fa.ActivateOptions();
                    Log.DebugFormat("FileAppender is writing to: " + fa.File);
                    Log.Debug("Log4net is configured");
                    break;
                }
            }
        }

        private void DisableMenuCommandIfNoSolutionLoaded(OleMenuCommand menuCommand)
        {
            uint cookie;
            IVsMonitorSelection monitorSelectionService = (IVsMonitorSelection)GetService(typeof(SVsShellMonitorSelection));
            monitorSelectionService.GetCmdUIContextCookie(new Guid(ContextGuids.vsContextGuidSolutionExists), out cookie);
            int isActive;
            monitorSelectionService.IsCmdUIContextActive(cookie, out isActive);

            menuCommand.Enabled = isActive == 1 ? true : false;
        }

        private string GetProjectPropertyByName(EnvDTE.Properties properties, string name)
        {
            try
            {
                if (properties != null)
                {
                    var item = properties.GetEnumerator();
                    while (item.MoveNext())
                    {
                        var property = item.Current as EnvDTE.Property;

                        if (property.Name == name)
                        {
                            return property.Value.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in GetAssemblyName: {0}", ex.Message);
            }

            return string.Empty;
        }

        private int GetColumnNumber()
        {
            // get the DTE2 object
            DTE2 dte2 = GetDTE2();

            if (dte2 == null)
            {
                return 0;
            }

            // get currently active cursor position
            var selection = (TextSelection)dte2.ActiveDocument.Selection;

            VirtualPoint point = selection.ActivePoint;

            return point.DisplayColumn; // get the column number from the location
        }

        private string GetDocumentName()
        {
            // get the DTE2 object
            DTE2 dte2 = GetDTE2();

            if (dte2 == null)
            {
                return string.Empty;
            }

            // get the ActiveDocument name from DTE2 object
            return dte2.ActiveDocument.Name;
        }

        private DTE2 GetDTE2()
        {
            // get the instance of DTE
            DTE dte = (DTE)GetService(typeof(DTE));

            // cast it as DTE2, historical reasons
            DTE2 dte2 = dte as DTE2;

            if (dte2 == null)
            {
                return null;
            }

            return dte2;
        }

        private int GetLineNumber()
        {
            // get the DTE2 object
            DTE2 dte2 = GetDTE2();

            if (dte2 == null)
            {
                return 0;
            }

            // get currently active cursor location
            var selection = (TextSelection)dte2.ActiveDocument.Selection;

            VirtualPoint point = selection.ActivePoint;

            return point.Line; // get the line number from the location
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Testify",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        private void OnDocumentSaved(Document document)
        {
            var project = document.ProjectItem;
            _queries.AddToTestQueue(project.ContainingProject.UniqueName);

        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the
        /// tool window. See the Initialize method to see how the menu item is associated to
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowCoverageToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(TestifyCoverageWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
             window.Content = new SummaryViewControl((TestifyCoverageWindow)window);

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        public void ShowUnitTestToolWindow(object sender, EventArgs e)
        {
           
            TEST(sender,e);
            //// Get the instance number 0 of this tool window. This window is single instance so this instance
            //// is actually the only one.
            //// The last flag is set to true so that if the tool window does not exists it will be created.
            //ToolWindowPane window = this.FindToolWindow(typeof(UnitTestSelectorWindow), 0, true);
            //if ((null == window) || (null == window.Frame))
            //{
            //    throw new NotSupportedException(Resources.CanNotCreateWindow);
            //}
            //window.Content = new UnitTestSelector((UnitTestSelectorWindow)window);

            //IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
         public void TEST(object sender, EventArgs e)
         {

                  IVsUIShell vsUIShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));

            IVsWindowFrame frame;

            Guid guidToolWindow2 = typeof(UnitTestSelectorWindow).GUID;

            vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guidToolWindow2, out frame);

            frame.SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_Float);
          

            frame.Show();
         }
        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        protected override void Dispose(bool disposing)
        {
            UnadviseSolutionEvents();

            base.Dispose(disposing);
        }

        protected override void Initialize()
        {
            base.Initialize();
            AdviseSolutionEvents();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the Run All Solution Tests menu item.
                CommandID menuSolutionTestsCommandID = new CommandID(GuidList.guidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests);
                OleMenuCommand menuSolutionTests = new OleMenuCommand(SolutionTestsCallback, menuSolutionTestsCommandID);
                menuSolutionTests.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                DisableMenuCommandIfNoSolutionLoaded(menuSolutionTests);
                mcs.AddCommand(menuSolutionTests);

                // Create the command for the Run All Project Tests menu item.
                CommandID menuProjectTestsCommandID = new CommandID(GuidList.guidTestifyCmdSet, (int)PkgCmdIDList.cmdidProjectTests);
                OleMenuCommand menuProjectTests = new OleMenuCommand(ProjectTestsCallback, menuProjectTestsCommandID);
                menuProjectTests.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                DisableMenuCommandIfNoSolutionLoaded(menuProjectTests);
                mcs.AddCommand(menuProjectTests);

                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidTestifyCmdSet, (int)PkgCmdIDList.cmdidTestTool);
                MenuCommand menuToolWin = new MenuCommand(ShowCoverageToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);

  
            }
        }

        private void AdviseSolutionEvents()
        {
            UnadviseSolutionEvents();

            _solution = this.GetService(typeof(SVsSolution)) as IVsSolution;

            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionCookie);
            }
        }

        private void ProjectTestsCallback(object sender, EventArgs e)
        {
            var projectName = _dte.ActiveDocument.ProjectItem.ContainingProject.Name.ToString();
            _queries.AddToTestQueue(projectName);
            ShowCoverageToolWindow(sender, e);
        }

        private void SolutionTestsCallback(object sender, EventArgs e)
        {
            ShowCoverageToolWindow(sender, e);
        }

        private void UnadviseSolutionEvents()
        {
            if (_solution != null)
            {
                if (_solutionCookie != uint.MaxValue)
                {
                    _solution.UnadviseSolutionEvents(_solutionCookie);
                    _solutionCookie = uint.MaxValue;
                }

                _solution = null;
            }
        }

        #endregion Package Members
        #region Interface Methods

        public EnvDTE.Project GetProject(string projectUniqueName)
        {
            IVsHierarchy hierarchy;
            _solution.GetProjectOfUniqueName(projectUniqueName, out hierarchy);
            object project;

            ErrorHandler.ThrowOnFailure
                (hierarchy.GetProperty(
                    VSConstants.VSITEMID_ROOT,
                    (int)__VSHPROPID.VSHPROPID_ExtObject,
                    out project));

            return (project as EnvDTE.Project);
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }

        public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            // Do something
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterMergeSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }

        public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnAfterOpenProject(IVsHierarchy hierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            // if the user closes a solution and opens another, we need to rebuild the ConnectionString
            if (_queries != null)
            {
                _queries = null;
            }

            IVsSolution solution = SetSolutionValues();

            Log.DebugFormat("Solution Opened: {0}", _solutionName);
            _service = new UnitTestService(_dte, _solutionDirectory, _solutionName);
            var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var databasePath = GetDatabasePath(appDataDirectory);
            CheckForDatabase(databasePath);

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var solutionTestsMenuCommand = mcs.FindCommand(new CommandID(Testify.GuidList.guidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests));
            solutionTestsMenuCommand.Enabled = true;

            var projectTestsMenuCommand = mcs.FindCommand(new CommandID(Testify.GuidList.guidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests));
            projectTestsMenuCommand.Enabled = true;

            // Setup Project Build Event Handler
            _dte = (DTE)GetService(typeof(DTE));
            var projectEvents = ((Events2)_dte.Events).BuildEvents;
            projectEvents.OnBuildProjConfigDone += ProjectBuildEventHandler;

            VerifyProjects(solution,string.Empty);
            _queries.SetAllQueuedTestsToNotRunning();
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeCloseSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            //Do something
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }

        private string GetDatabasePath(string directory)
        {
            var path = Path.Combine(directory, "Testify", _solutionName);

            var appDataExists = Directory.Exists(Path.Combine(path));
            if (!appDataExists)
            {
                Directory.CreateDirectory(path);
            }
            var databasePath = Path.Combine(path, "TestifyCE.sdf");
            return databasePath;
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            DisableMenuCommandIfNoSolutionLoaded(sender as OleMenuCommand);
        }

        private void ProjectBuildEventHandler(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            var sw = new Stopwatch();
            sw.Restart();
            Log.DebugFormat("Project Build occurred project name: {0}", project);
            IVsSolution pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (success)
            {
                //if (isFirstBuild)
                //{
                    pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
                    VerifyProjects(pSolution, project);
                //}

      
                isFirstBuild = false;
                Log.DebugFormat("Project Build Successful for project name: {0}", project);

                _queries.AddToTestQueue(project);

            }
            sw.Stop();
            //Log.DebugFormat("ProjectBuildEventHandler Elapsed Time {0} milliseconds", sw.ElapsedMilliseconds);
        }

        private IVsSolution SetSolutionValues()
        {
            IVsSolution pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
            string solutionDirectory;
            string solutionOptions;
            string solutionFile;
            pSolution.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionOptions);
            _solutionDirectory = solutionDirectory;
            _solutionName = Path.GetFileNameWithoutExtension(solutionFile);
            return pSolution;
        }
        #endregion Interface Methods
    }
}