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

using OpenCover;
using log4net;
using System.Threading.Tasks;
using System.Timers;

namespace Leem.Testify
{
    public delegate void CoverageChangedHandler(string className);

    public class UnitTestService
    {
        private  List<TestQueue> _testQueue;
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
            _queries = TestifyQueries.Instance;
            TestifyQueries.SolutionName = solutionName;

            _dte = dte;
            _solutionName = solutionName;
            _solutionDirectory = solutionDirectory;

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
            Log.DebugFormat("Solution Directory: {0}", _solutionDirectory.ToString());

            string target = string.Empty;

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

        private async Task RunNunitTests(string openCoverCommandLine, string arguments, ProjectInfo projectInfo, Guid fileNameGuid, QueuedTest testQueueItem)
        {
            await RunTests(openCoverCommandLine, arguments, projectInfo, fileNameGuid, testQueueItem);

        }
        private async Task RunTests(string openCoverCommandLine, string arguments, ProjectInfo projectInfo, Guid fileNameGuid, QueuedTest testQueueItem)
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
            var args = new string[] {@"-target:C:\USERS\LEE\APPDATA\LOCAL\MICROSOFT\VISUALSTUDIO\11.0EXP\EXTENSIONS\LEEM\TESTIFY\1.0\NUnit.Runners.2.6.2\nunit-console.exe ",
                                     @"-targetargs:C:\WIP\UnitTestExperiment\Domain.Test\bin\Debug\Domain.Test.dll /result:C:\Users\Lee\AppData\Local\Testify\UnitTestExperiment\5ce700cd-e242-46fd-b817-ff276495e958-result.xml /noshadow", 
                                     @"-coverbytest:*.Test.dll",
                                     @"-hideskipped: Domain",
                                     @"-filter:+[MyProduct.Domain]* +[Domain.Test]*",
                                     @"-register:user"  };
            //var launcher = new OpenCoverLauncher(args);


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

                    //Log.DebugFormat("Results of Unit Test run: {0}", stdout);
                }
            }
            catch(Exception ex)
            {
                // Log error.
                Log.ErrorFormat("Error ocurred while RunTest for Project: {0}: Error:{1}", projectInfo.ProjectAssemblyName, ex.Message);
            }
            string fileToRead = GetOutputFolder() + coverFilename;
            await ProcessCoverageSessionResults(projectInfo, testQueueItem, resultFilename, fileToRead);
            
        }

        private async Task ProcessCoverageSessionResults(ProjectInfo projectInfo, QueuedTest testQueueItem, string resultFilename, string fileToRead)
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
            
            await _queries.SaveCoverageSessionResults(coverageSession, projectInfo, testQueueItem.IndividualTests);

            _queries.RemoveFromQueue(testQueueItem);

            Log.DebugFormat("Test Completed, Name: {0}, Individual Test Count: {1}, ElapsedTime {2}",

            testQueueItem.ProjectName, testQueueItem.IndividualTests.Count(), DateTime.Now - testQueueItem.TestStartTime);

            System.IO.File.Delete(fileToRead);
        }





        public async Task RunAllNunitTestsForProject(QueuedTest item)//(string projectName, List<string> individualTests)
        {

            Log.DebugFormat("Test Started on Project {0}", item.ProjectName);

            var projectInfo = new ProjectInfo();

            if (item.IndividualTests == null || !item.IndividualTests.Any())
            {
                projectInfo = _queries.GetProjectInfo(item.ProjectName);
            }
            else 
            {
                projectInfo = _queries.GetProjectInfoFromTestProject(item.ProjectName);
            }
            

            if(projectInfo.TestProject != null)
            {
               // Log.DebugFormat("projectInfo for projectName: {0}, Test Project.Name: {1}, Test Project.UniqueName: {2}", projectInfo.ProjectName, projectInfo.TestProject.Name, projectInfo.TestProject.UniqueName);
                Log.DebugFormat("Called GetProjectInfo for Project: {0}: .TestProject.AssemblyName:{1}", item.ProjectName, projectInfo.TestProject.AssemblyName);
                
                var fileNameGuid = Guid.NewGuid();
                
                StringBuilder testParameters = GetTestParameters(item.ProjectName, item.IndividualTests, projectInfo, fileNameGuid);
               
                Log.DebugFormat("openCoverCommandLine: {0}", _openCoverCommandLine.ToString());
                Log.DebugFormat("Test Parameters: {0}", testParameters.ToString());
                
                await System.Threading.Tasks.Task.Run(() =>
                    {
                        RunNunitTests(_openCoverCommandLine, testParameters.ToString(), projectInfo, fileNameGuid, item);
                    });
            }
            else
            {
                Log.DebugFormat("GetProjectInfo returned a null TestProject for {0}", item.ProjectName);
            }
            Log.DebugFormat("Test Finished on Project {0} Elapsed Time {1}", item.ProjectName,DateTime.Now-item.TestStartTime);
        }

        private StringBuilder GetTestParameters(string projectName, List<string> individualTests, ProjectInfo projectInfo, Guid fileNameGuid)
        {
            StringBuilder testParameters = new StringBuilder();
            testParameters.Append(GetTarget());
            if (individualTests != null && individualTests.Any())
            {
                testParameters.Append("/run:");
                
                testParameters.Append(GetCommaSeparatedListOfTests(individualTests));
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

        private StringBuilder GetCommaSeparatedListOfTests(List<string> individualTests)
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

        public void ProcessIndividualTestQueue(int testRunId)
        {

            var queuedTest = _queries.GetIndividualTestQueue(testRunId);
             if (queuedTest != null)
             {
                 Log.DebugFormat("Ready to run another test from Individual Test queue");

                 queuedTest.TestStartTime = DateTime.Now;

                 RunAllNunitTestsForProject(queuedTest);

             }
        }
        public void ProcessProjectTestQueue(int testRunId)
        {
            var queuedTest = _queries.GetProjectTestQueue(testRunId);
            if (queuedTest != null)
            {
                Log.DebugFormat("Ready to run another test from Project Test queue");

                queuedTest.TestStartTime = DateTime.Now;

                RunAllNunitTestsForProject(queuedTest);

            }
        }

    }
}
      
