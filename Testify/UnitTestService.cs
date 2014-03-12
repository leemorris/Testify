using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Leem.Testify.Model;
using System.Timers;
using EnvDTE80;
using EnvDTE;
using StructureMap;
using OpenCover;
using log4net;
using System.Threading.Tasks;

namespace Leem.Testify
{
    public delegate void CoverageChangedHandler(string className);

    public class UnitTestService
    {
        private static List<LineCoverageInfo> _testQueue;
        private string _testOutputDirectory;
        private string _solutionDirectory;
        private string _solutionName;
        private DTE _dte;
        private List<string> _projectNames;
        private ITestifyQueries _queries;
        private string _openCoverCommandLine;
        private string _outputFolder;
        private ILog Log = LogManager.GetLogger(typeof(UnitTestService));
        private string _nunitPath;
        public event CoverageChangedHandler CoverageChanged;

        public UnitTestService(DTE dte, string solutionDirectory, string solutionName)
        {
            Log.DebugFormat("Inside 3 argument Constructor");
            _queries = new TestifyQueries(solutionName);//ObjectFactory.With("solutionName").EqualTo(solutionName).GetInstance<ITestifyQueries>();
            _dte = dte;
            _solutionName = solutionName;
            _solutionDirectory = solutionDirectory;
       
            //todo find a way to avoid calling EXE, can I call a DLL instead?

            Log.DebugFormat("Load file paths for Release Mode");
            _openCoverCommandLine = Path.GetDirectoryName(typeof(UnitTestService).Assembly.Location) + @"\OpenCover\OpenCover.console.exe";
            Log.DebugFormat("Release Path for OpenCover: {0}", _openCoverCommandLine);
            _nunitPath = Path.GetDirectoryName(typeof(UnitTestService).Assembly.Location) + @"\NUnit.Runners.2.6.2\nunit-console.exe";
            Log.DebugFormat("Release Path for NUnit: {0}", _nunitPath);

            Log.DebugFormat("Nunit Path: {0}", _nunitPath);

            _outputFolder = GetOutputFolder();
        }

        public string ProjectFileName { get; set; }

       

        private string GetTarget()
        {
            Log.Debug("Inside GetTarget");
            string target = string.Empty;
            Log.DebugFormat("Solution Directory: {0}", _solutionDirectory.ToString());
            try
            {
                target = " -target:" + "\"" + _nunitPath + "\" -targetargs:\"";
                Log.DebugFormat("Target: {0}", target);
            }
            catch(Exception ex) 
            {
                Log.Error("Could not locate nunit-console.exe, Please install Nuget package NUnit.Runners" );
            }

            return target;
        }

        private async Task RunNunitTests(string openCoverCommandLine, string arguments, ProjectInfo projectInfo, Guid fileNameGuid, List<string> individualTests)
        {
            await RunTests(openCoverCommandLine, arguments, projectInfo, fileNameGuid, individualTests);

        }
        private async Task RunTests(string openCoverCommandLine, string arguments, ProjectInfo projectInfo, Guid fileNameGuid, List<string> individualTests)
        {
            Log.DebugFormat("Verify project executing on Thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);

            var coverFilename = fileNameGuid.ToString() + "-cover.xml";
            var resultFilename = fileNameGuid.ToString() + "-result.xml";
            var startInfo = new System.Diagnostics.ProcessStartInfo { FileName = openCoverCommandLine,
                                                                  Arguments = arguments + coverFilename,
                                                                  RedirectStandardOutput = true,
                                                                  WindowStyle = ProcessWindowStyle.Hidden,
                                                                  UseShellExecute = false,
                                                                  CreateNoWindow = true
            };

            Log.DebugFormat("ProcessStartInfo.Arguments: {0}", startInfo.Arguments.ToString());
            Log.DebugFormat("ProcessStartInfo.FileName: {0}", startInfo.FileName.ToString());
            string stdout ;
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                 using (System.Diagnostics.Process exeProcess = System.Diagnostics.Process.Start(startInfo))
                {
                    exeProcess.StartInfo.RedirectStandardOutput = true;
                    stdout = exeProcess.StandardOutput.ReadToEnd(); 
                    await Task.Run(() => exeProcess.WaitForExit());
                    Log.DebugFormat("Results of Unit Test run: {0}", stdout);
                }
            }
            catch(Exception ex)
            {
                // Log error.
                Log.ErrorFormat("Error ocurred while RunTest for Project: {0}: Error:{1}", projectInfo.ProjectAssemblyName, ex.Message);
            }
            string fileToRead = GetOutputFolder() + coverFilename;
            await ProcessCoverageSessionResults(projectInfo, individualTests, resultFilename, fileToRead);
            
        }

        private async Task ProcessCoverageSessionResults(ProjectInfo projectInfo, List<string> individualTests, string resultFilename, string fileToRead)
        {
            CoverageSession coverageSession = new CoverageSession();
            resultType testOutput= new resultType();
            await System.Threading.Tasks.Task.Run(() =>
            {
                coverageSession = GetCoverageSessionFile(fileToRead);
                TestOutputFileReader testOutputFileReader = new TestOutputFileReader();
                 testOutput = testOutputFileReader.ReadTestResultFile(GetOutputFolder() + resultFilename);

            });
            _queries.SaveUnitTestResults(testOutput);
            //_queries.SaveResults(coverageSession, testOutput, projectInfo, individualTests);
            var changedClasses = await _queries.SaveCoverageSessionResults(coverageSession, projectInfo, individualTests);
            if (changedClasses.Any())
            {
                var del = CoverageChanged as CoverageChangedHandler;
                foreach( var changedClass in changedClasses)
                {
                    if (del != null)
                    {
                        del(changedClass);
                    }
                }

            }

            //_queries.SaveResults(coverageSession, testOutput, projectInfo, individualTests);
            System.IO.File.Delete(fileToRead);
        }
        public void RunAllNunitTestsForSolution()
        {
            string testOutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _solutionName, "OpenCover.xml");

            var nunitDirectories = Directory.GetDirectories(_solutionDirectory + @"\packages", "NUnit.Runners*");
            var openCoverDirectories = Directory.GetDirectories(_solutionDirectory + @"\packages", "OpenCover*");
            var nunit = Directory.GetFiles(nunitDirectories.First() + @"\tools\", "nunit-console.exe /out:" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _solutionName, "TestResult.txt"));
            var openCover = Directory.GetFiles(openCoverDirectories.First(), "OpenCover.Console.exe");
            var proc = new System.Diagnostics.Process();

            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;

            var testProjects = _queries.GetTestProjects().ToList();
            foreach (var project in testProjects)
            {
                string arguments = " -target:" + "\"" + nunit.First() + "\" -targetargs:\"" + project.Path + " /noshadow\"" + " -output:\"" + testOutputDirectory + "\\opencover.xml\"";
                proc.StartInfo = new System.Diagnostics.ProcessStartInfo(openCover.First(), arguments);
                proc.Start();
            }

        }

     
        public async Task RunAllNunitTestsForProject(string projectName, List<string> individualTests)
        {
            Debug.WriteLine("Run Tests for " + projectName);
            Log.DebugFormat("RunAllNunitTestsForProject for project name: {0}", projectName);
            var projectInfo = new ProjectInfo();

            if (individualTests == null || !individualTests.Any())
            {
                projectInfo = _queries.GetProjectInfo(projectName);
            }
            else 
            {
                projectInfo = _queries.GetProjectInfoFromTestProject(projectName);
            }
            

            if(projectInfo.TestProject != null)
            {
                Log.DebugFormat("projectInfo for projectName: {0}, Test Project.Name: {1}, Test Project.UniqueName: {2}", projectInfo.ProjectName, projectInfo.TestProject.Name, projectInfo.TestProject.UniqueName);
                Log.DebugFormat("Called GetProjectInfo for Project: {0}: .TestProject.AssemblyName:{1}", projectName, projectInfo.TestProject.AssemblyName);
                var fileNameGuid = Guid.NewGuid();
                StringBuilder testParameters = GetTestParameters(projectName, individualTests, projectInfo, fileNameGuid);
                Log.DebugFormat("openCoverCommandLine: {0}", _openCoverCommandLine.ToString());
                Log.DebugFormat("Test Parameters: {0}", testParameters.ToString());
                await System.Threading.Tasks.Task.Run(() =>
                    {
                         RunNunitTests(_openCoverCommandLine, testParameters.ToString(), projectInfo, fileNameGuid, individualTests);
                    });
            }
            else
            {
                Log.DebugFormat("GetProjectInfo returned a null TestProject for {0}", projectName);
            }

        }

        private StringBuilder GetTestParameters(string projectName, List<string> individualTests, ProjectInfo projectInfo, Guid fileNameGuid)
        {
            StringBuilder testParameters = new StringBuilder();
            testParameters.Append(GetTarget());
            if (individualTests != null && individualTests.Any())
            {
                testParameters.Append("/run:");
                
                testParameters.Append(GetComaSeparatedListOfTests(individualTests));
                testParameters.Append(" ");
            }
            testParameters.Append(projectInfo.TestProject.Path);
            testParameters.Append(".dll");
            testParameters.Append(" /result:");
            testParameters.Append(_outputFolder);
            testParameters.Append(fileNameGuid);
            testParameters.Append("-result.xml");
            testParameters.Append(" /noshadow");
            testParameters.Append("\"");
            testParameters.Append(" -coverbytest:*.Test.dll -hideskipped: ");
            testParameters.Append(Path.GetFileNameWithoutExtension(projectName));
            testParameters.Append(" -filter:\"+[" + projectInfo.ProjectAssemblyName + "]* +[" + projectInfo.TestProject.AssemblyName + "]* \"");
            testParameters.Append(" -register:user -output:");
            testParameters.Append(_outputFolder);
            return testParameters;
        }

        private StringBuilder GetComaSeparatedListOfTests(List<string> individualTests)
        {
            var listOfTests = new StringBuilder();
            foreach(var test in individualTests)
            {
                listOfTests.Append(test);
                listOfTests.Append(",");
            }
            // remove the last coma
            listOfTests.Remove(listOfTests.Length - 1, 1);
            return listOfTests;
            
        }

        private string GetOutputFolder()
        {
            StringBuilder folderPath = new StringBuilder();
            folderPath.Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
            folderPath.Append("\\Testify\\");
            folderPath.Append(_solutionName);
            folderPath.Append("\\");
            return folderPath.ToString();
        }

        private CoverageSession GetCoverageSessionFile(string filename)
        {
            Log.DebugFormat("GetCoverageSessionFile for file name: {0}", filename);
            var reader = new CoverageFileReader();

            CoverageSession codeCoverage = reader.ReadCoverageFile(filename);
            return codeCoverage;
        }


        public async Task  RunTestsThatCoverLine(string projectName, string className, string methodName, int lineNumber)
        {
            var unitTestNames = _queries.GetUnitTestsThatCoverLines(className.Substring(0, className.IndexOf('.')), methodName, lineNumber);
           
             var projectInfo =  _queries.GetProjectInfo(projectName);

            if(projectInfo.TestProject != null)
            {
                await RunAllNunitTestsForProject(projectInfo.TestProject.UniqueName, unitTestNames);
            }
            else
            {
                Log.DebugFormat("GetProjectInfo returned a null TestProject for {0}", projectName);
            }
            
            
        }
    }
}
      
