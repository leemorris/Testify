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
        //[ProvideToolWindowVisibility(typeof(MyToolWindow), /*UICONTEXT_SolutionExists*/"f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [Guid(GuidList.guidTestifyPkgString)]
    public sealed class TestifyPackage : Package, IVsSolutionEvents3
    {
        public EventArgs e = null;
        private static Timer _timer;
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
                _timer = new Timer();
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
                // Todo, Determine where the app will actually be executing from and where the initial database file will be.
                //string initialDatabasePath = System.IO.Path.GetFullPath(@"..\..\..\Testify\TestifyCE.sdf");
                //#if (DEBUG == true)
                //initialDatabasePath = System.IO.Path.GetFullPath(@"..\..\..\Testify\TestifyCE.sdf");
                //#endif

                //#if (DEBUG == false)
                //initialDatabasePath = Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location) + @"\TestifyCE.sdf";
                //#endif

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

        // finds the instance of IWpfTextViewHost associated with this margin
        public IWpfTextViewHost GetIWpfTextViewHost()
        {
            // get an instance of IVsTextManager
            IVsTextManager txtMgr = (IVsTextManager)GetService(typeof(SVsTextManager));

            IVsTextView vTextView = null;

            int mustHaveFocus = 1;

            // get the active view from the TextManager
            txtMgr.GetActiveView(mustHaveFocus, null, out vTextView);

            // cast as IVsUSerData
            IVsUserData userData = vTextView as IVsUserData;

            if (userData == null)
            {
                Trace.WriteLine("No text view is currently open");
                return null;
            }

            IWpfTextViewHost viewHost;
            object holder;

            // get the IWpfTextviewHost using the predefined guid for it
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;

            userData.GetData(ref guidViewHost, out holder);

            // convert to IWpfTextviewHost
            viewHost = (IWpfTextViewHost)holder;

            return viewHost;
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
                    assemblyName = GetAssemblyName(proj);
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

        public async void VerifyProjects(IVsSolution solution)
        {
            List<EnvDTE.Project> vsProjects = new List<EnvDTE.Project>();
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
                var assemblyName = GetAssemblyName(project);

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

        public void VerifyProjects(EnvDTE.Project project)
        {
            var projects = new List<Poco.Project>();
            var outputPath = GetProjectOutputBuildFolder(project);
            var assemblyName = GetAssemblyName(project);

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

        private string GetAssemblyName(EnvDTE.Project proj)
        {
            try
            {
                if (proj.Properties != null)
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
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(TestifyCoverageWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
           // window.Content = new SummaryViewControl((TestifyCoverageWindow)window);

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
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
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);

                //////// Bookmark
                // add commands for adding all bookmarks
                // in each one of them I have used an anonymous method to call AddBookmark method
                //CommandID menuCommandBookmark0 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark0);
                //MenuCommand subItemBookmark0 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(0); }), menuCommandBookmark0);
                //mcs.AddCommand(subItemBookmark0);

                //CommandID menuCommandBookmark1 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark1);
                //MenuCommand subItemBookmark1 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(1); }), menuCommandBookmark1);
                //mcs.AddCommand(subItemBookmark1);

                //CommandID menuCommandBookmark2 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark2);
                //MenuCommand subItemBookmark2 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(2); }), menuCommandBookmark2);
                //mcs.AddCommand(subItemBookmark2);

                //CommandID menuCommandBookmark3 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark3);
                //MenuCommand subItemBookmark3 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(3); }), menuCommandBookmark3);
                //mcs.AddCommand(subItemBookmark3);

                //CommandID menuCommandBookmark4 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark4);
                //MenuCommand subItemBookmark4 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(4); }), menuCommandBookmark4);
                //mcs.AddCommand(subItemBookmark4);

                //CommandID menuCommandBookmark5 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark5);
                //MenuCommand subItemBookmark5 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(5); }), menuCommandBookmark5);
                //mcs.AddCommand(subItemBookmark5);

                //CommandID menuCommandBookmark6 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark6);
                //MenuCommand subItemBookmark6 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(6); }), menuCommandBookmark6);
                //mcs.AddCommand(subItemBookmark6);

                //CommandID menuCommandBookmark7 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark7);
                //MenuCommand subItemBookmark7 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(7); }), menuCommandBookmark7);
                //mcs.AddCommand(subItemBookmark7);

                //CommandID menuCommandBookmark8 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark8);
                //MenuCommand subItemBookmark8 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(8); }), menuCommandBookmark8);
                //mcs.AddCommand(subItemBookmark8);

                //CommandID menuCommandBookmark9 = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdBookmark9);
                //MenuCommand subItemBookmark9 = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { AddOrMoveToBookmark(9); }), menuCommandBookmark9);
                //mcs.AddCommand(subItemBookmark9);

                //// add command for Clearing Bookmarks
                //// again I have opted for an anonymous method
                //CommandID menuCommandClearBookmark = new CommandID(GuidList.guidNumberedBookmarksCmdSet, (int)PkgCmdIDList.cmdClearBookmarks);
                //MenuCommand subItemClearBookmark = new MenuCommand(new EventHandler(delegate(object sender, EventArgs args) { ClearAllBookmarks(); }), menuCommandClearBookmark);
                //mcs.AddCommand(subItemClearBookmark);
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
            ShowToolWindow(sender, e);
        }

        private void SolutionTestsCallback(object sender, EventArgs e)
        {
            ShowToolWindow(sender, e);
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

            VerifyProjects(solution);
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
            if (success)
            {
                if (isFirstBuild)
                {
                    IVsSolution pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
                    VerifyProjects(pSolution);
                }
                isFirstBuild = false;
                Log.DebugFormat("Project Build Successful for project name: {0}", project);

                _queries.AddToTestQueue(project);
            }
            sw.Stop();
            Log.DebugFormat("ProjectBuildEventHandler Elapsed Time {0} milliseconds", sw.ElapsedMilliseconds);
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