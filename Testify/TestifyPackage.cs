using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using Leem.Testify.Domain;
using System.Collections.Generic;
using log4net;
using System.IO;
using log4net.Appender;
using Leem.Testify.Wiring;
using Leem.Testify.DataLayer;
using System.Data.Entity;
using EnvDTE;

namespace Leem.Testify
{

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [Guid(GuidList.guidTestifyPkgString)]
    public sealed class TestifyPackage : Package, IVsSolutionEvents3
    {
        private ILog Log = LogManager.GetLogger(typeof(TestifyPackage));
        private IVsSolution _solution = null;
        private uint _solutionCookie;
        private UnitTestService _service;
        private string _solutionDirectory;
        private string _solutionName;
        private EnvDTE.DTE _dte;
        private bool isFirstBuild = true;


        public TestifyPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            try
            {

                FileInfo file;
#if (DEBUG == true)
                file = new FileInfo(Environment.CurrentDirectory.ToString() + @"\log4net.config");
#endif

#if (DEBUG == false)
                file = new FileInfo(Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location) + @"\log4net.config");
#endif
                Debug.WriteLine("Log4net.config path: " + file.ToString());
                ConfigureLogging(file);
                AppDomain.CurrentDomain.Load("Wiring");
                var boot = new ContainerBootstrapper();
                boot.BootstrapStructureMap();
                //todo look into why this directory is needed
                var directory = @"c:\WIP\Testify\DataLayer\";
                var path = Path.Combine(directory, @"TestifyCE.sdf;password=lactose");
                Database.SetInitializer(new DropCreateDatabaseIfModelChanges<TestifyContext>());

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.CurrentCulture, ex.Message, this.ToString()));
            }
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
                    Debug.WriteLine("FileAppender is writing to: " + fa.File);
                    Log.Debug("Log4net is configured");
                    break;
                }
            }
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
            ToolWindowPane window = this.FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members


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
            }
        }

        private void SolutionTestsCallback(object sender, EventArgs e)
        {
            //_service.RunAllNunitTestsForSolution();
            ShowToolWindow(sender, e);
        }


        private void ProjectTestsCallback(object sender, EventArgs e)
        {
            _service.RunAllNunitTestsForProject(_dte.ActiveDocument.ProjectItem.ContainingProject.Name.ToString(),null);
            ShowToolWindow(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            UnadviseSolutionEvents();

            base.Dispose(disposing);
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
        #endregion

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
        public void CheckForDatabase(string databasePath)
        {
            Log.DebugFormat("CheckForDatabase: ", databasePath);
            if (!System.IO.File.Exists(databasePath))
            {
                // Todo, Determine where the app will actually be executing from and where the initial database file will be.
                string initialDatabasePath = System.IO.Path.GetFullPath(@"..\..\..\DataLayer\TestifyCE.sdf");
#if (DEBUG == true)
                initialDatabasePath = System.IO.Path.GetFullPath(@"..\..\..\Testify\DataLayer\TestifyCE.sdf");
#endif

#if (DEBUG == false)
                        initialDatabasePath = Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location) + @"\TestifyCE.sdf";
#endif
                try
                {
                    System.IO.File.Copy(initialDatabasePath.ToString(), databasePath);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error Copying database " + ex.Message);
                    Debug.WriteLine("Error Copying database " + ex.Message);
                }
            }

        }
     
        public async void VerifyProjects(IVsSolution solution)
        {

            List<EnvDTE.Project> vsProjects = new List<EnvDTE.Project>();
            var projects = new List<Domain.Project>();
            foreach (EnvDTE.Project project in _dte.Solution.Projects)
            {
                Log.DebugFormat("Verify project name: {0}", project.Name);

                var outputPath = GetProjectOutputBuildFolder(project);
                Log.DebugFormat("  outputPath: {0}", outputPath);
                var assemblyName = GetAssemblyName(project);
                Log.DebugFormat("  Assembly name: {0}", assemblyName);
                projects.Add(new Domain.Project
                {
                    Name = project.Name,
                    AssemblyName = assemblyName,
                    UniqueName = project.UniqueName,
                    Path = outputPath
                });
            }

            var queries = new TestifyQueries(_dte.Solution.FullName);
            queries.MaintainProjects(projects);

        }
  
        public void VerifyProjects(EnvDTE.Project project)
        {
            var projects = new List<Domain.Project>();
            Log.DebugFormat("Verify project name: {0}", project.Name);

            var outputPath = GetProjectOutputBuildFolder(project);
            Log.DebugFormat("  outputPath: {0}", outputPath);
            var assemblyName = GetAssemblyName(project);
            Log.DebugFormat("  Assembly name: {0}", assemblyName);
            projects.Add(new Domain.Project
            {
                Name = project.Name,
                AssemblyName = assemblyName,
                UniqueName = project.UniqueName,
                Path = outputPath
            });

            var queries = new TestifyQueries(_dte.Solution.FullName);
            queries.MaintainProjects(projects);

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

        private void DisableMenuCommandIfNoSolutionLoaded(OleMenuCommand menuCommand)
        {
            uint cookie;
            IVsMonitorSelection monitorSelectionService = (IVsMonitorSelection)GetService(typeof(SVsShellMonitorSelection));
            monitorSelectionService.GetCmdUIContextCookie(new Guid(ContextGuids.vsContextGuidSolutionExists), out cookie);
            int isActive;
            monitorSelectionService.IsCmdUIContextActive(cookie, out isActive);

            menuCommand.Enabled = isActive == 1 ? true : false;
        }


        #region Interface Methods


        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            Log.DebugFormat("Hit OnAfterOpenSolution in TestifyPackage" );
            IVsSolution pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
            string solutionDirectory;
            string solutionOptions;
            string solutionFile;
            pSolution.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionOptions);
            _solutionDirectory = solutionDirectory;
            _solutionName = Path.GetFileNameWithoutExtension(solutionFile);
            Log.DebugFormat("Solution Opened: {0}", _solutionName);

            var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(directory, "Testify", _solutionName);

            var appDataExists = Directory.Exists(Path.Combine(path));
            if (!appDataExists)
            {
                Directory.CreateDirectory(path);
            }
            var databasePath = Path.Combine(path, "TestifyCE.sdf");
            CheckForDatabase(databasePath);

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var solutionTestsMenuCommand = mcs.FindCommand(new CommandID(Testify.GuidList.guidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests));
            solutionTestsMenuCommand.Enabled = true;

            var projectTestsMenuCommand = mcs.FindCommand(new CommandID(Testify.GuidList.guidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests));
            projectTestsMenuCommand.Enabled = true;

            _dte = (DTE)GetService(typeof(DTE));

            var projectEvents = ((Events2)_dte.Events).BuildEvents;
            projectEvents.OnBuildProjConfigDone += ProjectBuildEventHandler;
            _service = new UnitTestService(_dte,_solutionDirectory, _solutionName);


            VerifyProjects(pSolution);
            //_service.RunAllNunitTestsForSolution();

            return VSConstants.S_OK;

        }
        private void ProjectBuildEventHandler(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            var sw = new Stopwatch();
            sw.Restart();
            Log.DebugFormat("Project Build occurred project name: {0}", project);
            if (success)
            {
                if(isFirstBuild)
                {
                    IVsSolution pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
                    VerifyProjects(pSolution);
                }
                isFirstBuild = false;
                Log.DebugFormat("Project Build Successful for project name: {0}", project);

                _service.RunAllNunitTestsForProject(project,null);

            }
           sw.Stop();
           Log.DebugFormat("ProjectBuildEventHandler Elapsed Time {0} milliseconds", sw.ElapsedMilliseconds);

        }

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

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            // Do something
            return VSConstants.E_NOTIMPL;
        }
        public int OnAfterOpenProject(IVsHierarchy hierarchy, int fAdded)
        {

            // VerifyProjects(_solution);
            return VSConstants.S_OK;
        }
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            //Do something
            return VSConstants.E_NOTIMPL;
        }
        public int OnAfterCloseSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }
        public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }
        public int OnAfterMergeSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }
        public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        { return VSConstants.E_NOTIMPL; }
        public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }
        public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }
        public int OnBeforeCloseSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }
        void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            DisableMenuCommandIfNoSolutionLoaded(sender as OleMenuCommand);

        }
        #endregion
    }
}