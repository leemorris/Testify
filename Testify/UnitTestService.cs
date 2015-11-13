using EnvDTE;
using Leem.Testify.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using OpenCover.Framework.Manager;

namespace Leem.Testify
{
    public delegate void CoverageChangedHandler(string className);

    public class UnitTestService
    {
        private readonly string _solutionDirectory;
        private readonly string _solutionName;
        private readonly ITestifyQueries _queries;
        private readonly string _openCoverCommandLine;
        private readonly string _outputFolder;
        private readonly ILog Log = LogManager.GetLogger(typeof(UnitTestService));
        private readonly string _nunitPath;
        private readonly DTE _dte;
        
        public UnitTestService(DTE dte, string solutionDirectory, string solutionName)
        {

            _dte = dte;
            _queries = TestifyQueries.Instance;

            TestifyQueries.SolutionName = Path.Combine(solutionDirectory, solutionName);

            _solutionName = solutionName;

            _solutionDirectory = solutionDirectory;

            _openCoverCommandLine = Path.GetDirectoryName(typeof(UnitTestService).Assembly.Location) + @"\OpenCover\OpenCover.console.exe";
    
            _nunitPath = Path.GetDirectoryName(typeof(UnitTestService).Assembly.Location) + @"\NUnit.Runners.2.6.2\nunit-console.exe";

            Log.DebugFormat("Load file paths for Release Mode"); 
            Log.DebugFormat("Release Path for OpenCover: {0}", _openCoverCommandLine);
            Log.DebugFormat("Release Path for NUnit: {0}", _nunitPath);
            Log.DebugFormat("Nunit Path: {0}", _nunitPath);

            _outputFolder = GetOutputFolder();

        }


        private string GetTarget()
        {
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
            // BuildProject(_solutionDirectory + testQueueItem.ProjectName);
            //Log.DebugFormat("Verify project executing on Thread: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);

            var coverFilename = fileNameGuid.ToString() + "-cover.xml";

            var resultFilename = fileNameGuid.ToString() + "-result.xml";

            var startInfo = new System.Diagnostics.ProcessStartInfo { FileName = openCoverCommandLine,
                                                                      Arguments = arguments + coverFilename,
                                                                      RedirectStandardOutput = true,
                                                                      WindowStyle = ProcessWindowStyle.Hidden,
                                                                      UseShellExecute = false,
                                                                      CreateNoWindow = true
            };
            Log.DebugFormat("*Start Process for Process For Project: {0}", testQueueItem.ProjectName);
            Log.DebugFormat("ProcessStartInfo.Arguments: {0}", startInfo.Arguments.ToString());
            Log.DebugFormat("ProcessStartInfo.FileName: {0}", startInfo.FileName.ToString());
            //var args = new string[] {@"-target:C:\USERS\LEE\APPDATA\LOCAL\MICROSOFT\VISUALSTUDIO\11.0EXP\EXTENSIONS\LEEM\TESTIFY\1.0\NUnit.Runners.2.6.2\nunit-console.exe ",
            //                         @"-targetargs:C:\WIP\UnitTestExperiment\Domain.Test\bin\Debug\Domain.Test.dll /result:C:\Users\Lee\AppData\Local\Testify\UnitTestExperiment\5ce700cd-e242-46fd-b817-ff276495e958-result.xml /noshadow", 
            //                         @"-coverbytest:*.Test.dll",
            //                         @"-hideskipped: Domain",
            //                         @"-filter:+[MyProduct.Domain]* +[Domain.Test]*",
            //                         @"-register:Path64"  };
            //var launcher = new OpenCoverLauncher(args);



            string stdout ;
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                 using (System.Diagnostics.Process exeProcess = System.Diagnostics.Process.Start(startInfo))
                {

                    exeProcess.PriorityClass = ProcessPriorityClass.BelowNormal;


                    stdout = exeProcess.StandardOutput.ReadToEnd(); 

                    await Task.Run(() => exeProcess.WaitForExit());

                   Log.DebugFormat("Results of Unit Test run: {0}", stdout);
                   Log.DebugFormat("Run Tests Completed:");
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



        private async Task<bool> BuildProject(ProjectInfo projectInfo)
        {
            bool isSuccessful;
            isSuccessful = await BuildProject(projectInfo.UniqueName, projectInfo.TestProject.Path);
            isSuccessful = isSuccessful && await BuildProject(projectInfo.TestProject.UniqueName, projectInfo.TestProject.Path);
            return isSuccessful;
        }

        public async Task<bool> BuildProject(string projectPath, string outputPath) 
        {
            var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var msBuildPath = windowsDirectory + @"\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe";
            var outputDirectory = System.IO.Path.GetDirectoryName(outputPath);
            var workingDirectory = System.IO.Path.GetDirectoryName(_solutionDirectory + projectPath);

            var startInfo = new System.Diagnostics.ProcessStartInfo { FileName = msBuildPath,
                                                                      Arguments = " /p:OutputPath=" + outputDirectory,
                                                                      RedirectStandardOutput = true,
                                                                      WindowStyle = ProcessWindowStyle.Hidden,
                                                                      UseShellExecute = false,
                                                                      WorkingDirectory = workingDirectory,
                                                                      CreateNoWindow = true };
            ///Arguments = "/t:Clean;Build /p:OutputPath=" + outputDirectory,
            Log.DebugFormat("MS Build Path: {0}", msBuildPath);



            string stdout ;
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                 using (System.Diagnostics.Process exeProcess = System.Diagnostics.Process.Start(startInfo))
                {
                    stdout = exeProcess.StandardOutput.ReadToEnd(); 

                    await Task.Run(() => exeProcess.WaitForExit());

                    Log.DebugFormat("Results of MSBuild for Project: {0}: {1}", projectPath, stdout);
                   return true;
                }
            }
            catch(Exception ex)
            {
                // Log error.
                Log.ErrorFormat("Error while building Project: {0}: Error:{1}", projectPath, ex.Message);
                return false;
            }
        }


        private async Task ProcessCoverageSessionResults(ProjectInfo projectInfo, QueuedTest testQueueItem, string resultFilename, string fileToRead)
        {
            var sw = Stopwatch.StartNew();

            var coverageSession = new CoverageSession();
            var testOutput = new resultType();

                await Task.Run(() =>
                {
                    coverageSession = GetCoverageSessionFile(fileToRead);

                    TestOutputFileReader testOutputFileReader = new TestOutputFileReader();

                    testOutput = testOutputFileReader.ReadTestResultFile(GetOutputFolder() + resultFilename);

                });
            Log.DebugFormat("Coverage and Test Result Files Read Elapsed Time = {0}", sw.ElapsedMilliseconds);
            sw.Reset();


            if (testOutput != null && coverageSession.Modules.Count == 2)
            {
                Log.DebugFormat("SaveUnitTestResults Elapsed Time = {0}", sw.ElapsedMilliseconds);
                sw.Restart();

              
                await _queries.SaveCoverageSessionResults(coverageSession, testOutput, projectInfo, testQueueItem.IndividualTests);
                Log.DebugFormat("SaveCoverageSessionResults  {0} Elapsed Time = {1} min {2} sec",projectInfo.ProjectName, sw.Elapsed.Minutes,sw.Elapsed.Seconds);

                Log.DebugFormat("ProcessCoverageSessionResults Completed, Name: {0}, Individual Test Count: {1}, Time from Build-to-Complete {2} minutes, {3} seconds",

                    testQueueItem.ProjectName, testQueueItem.IndividualTests == null ? 0 : testQueueItem.IndividualTests.Count(), (DateTime.Now - testQueueItem.TestStartTime).Minutes, (DateTime.Now - testQueueItem.TestStartTime).Seconds); 
            }
            _queries.RemoveFromQueue(testQueueItem);
            System.IO.File.Delete(fileToRead);
        }

   

        private IProjectContent AddFileToProject(IProjectContent project, string fileName)
        {
            var code = string.Empty;
            try
            {
                code = System.IO.File.ReadAllText(fileName);
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


        private async Task RunAllNunitTestsForProject(QueuedTest item)
        {

            Log.DebugFormat("Test Started TestRunId {0} on Project {1}", item.TestRunId, item.ProjectName);

            ProjectInfo projectInfo;
            projectInfo = _queries.GetProjectInfoFromTestProject(item.ProjectName);

            if (projectInfo.TestProject.Path == null)
            {
                BuildProject( projectInfo);
            }

            if(projectInfo.TestProject != null)
            {
                Log.DebugFormat("Called GetProjectInfo for Project: {0}: .TestProject.AssemblyName:{1}", item.ProjectName, projectInfo.TestProject.AssemblyName);
                
                var fileNameGuid = Guid.NewGuid();
                
                StringBuilder testParameters = GetTestParameters(item.ProjectName, item.IndividualTests, projectInfo, fileNameGuid);
               
                Log.DebugFormat("openCoverCommandLine: {0}", _openCoverCommandLine);
                Log.DebugFormat("Test Parameters: {0}", testParameters);
                
                await Task.Run(() =>
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
            var testParameters = new StringBuilder();
            testParameters.Append(GetTarget());
            if ( individualTests.Any())
            {
                testParameters.Append("/run:"); 

                testParameters.Append(GetCommaSeparatedListOfTests(individualTests));
                testParameters.Append(" ");
            }
            const int timeout = 15000;
            testParameters.Append(projectInfo.TestProject.Path);
            testParameters.Append(".dll");
            testParameters.Append(" /result:");
            testParameters.Append(_outputFolder);
            testParameters.Append(fileNameGuid);
            testParameters.Append("-result.xml ");

            testParameters.Append(" /timeout=" + timeout);
            testParameters.Append("\"");
            testParameters.Append(" -coverbytest:*.Test.dll -hideskipped: ");
            testParameters.Append(Path.GetFileNameWithoutExtension(projectName));
            testParameters.Append(" -skipautoprops: ");
            
            testParameters.Append(" -filter:\"+[" + projectInfo.ProjectAssemblyName + "]* +[" + projectInfo.TestProject.AssemblyName + "]* \"");
            testParameters.Append(" -targetdir:" + Path.GetDirectoryName(projectInfo.TestProject.Path));
            testParameters.Append(" -register:Path64 -output:");
            testParameters.Append(_outputFolder);
            
            return testParameters;
        }

        private StringBuilder GetCommaSeparatedListOfTests(IEnumerable<string> individualTests)
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
            var hashCode = Path.Combine(_solutionDirectory, _solutionName).GetHashCode().ToString();
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Testify", _solutionName, hashCode);


      

            return string.Concat(path, "\\");
        }

        private CoverageSession GetCoverageSessionFile(string filename)
        {
            var reader = new CoverageFileReader();

            var codeCoverage = reader.ReadCoverageFile(filename);

            return codeCoverage;
        }

        public void ProcessTestQueue(int testRunId)
        {

            var queuedTest = _queries.GetIndividualTestQueue(testRunId);

            if (queuedTest != null)
            {
                queuedTest.TestStartTime = DateTime.Now;

                if (queuedTest.IndividualTests.Count == 0)
                {
                    Log.DebugFormat("Ready to run another test from Project Test queue");
                    ProcessProjectTestQueueItem(queuedTest);
                }
                else 
                {
                    Log.DebugFormat("Ready to run another test from Individual Test queue");
                    RunAllNunitTestsForProject(queuedTest);
                }

            }
 
        }

        public void ProcessProjectTestQueueItem(QueuedTest queuedTest)
        {

            Log.DebugFormat("Ready to run another test from Project Test queue");
            var projectInfo = _queries.GetProjectInfoFromTestProject(queuedTest.ProjectName);
            if (projectInfo != null && projectInfo.TestProject.Path == string.Empty)
            {
               // GetProjectOutputBuildFolder();
                var projects = (Array)_dte.ActiveSolutionProjects;
                for (var i = 0; i < projects.Length; i++ )
                {
                    var project = (EnvDTE.Project)projects.GetValue(i);
                    if(project.FullName == _solutionDirectory + projectInfo.TestProject.UniqueName)
                    {
                        projectInfo.TestProject.Path = GetProjectOutputBuildFolder(project);
                    }
                 
                }


            }
            //Build the Test project, because we don't know that it was built at the same time as the Code Project
            

           
            RunAllNunitTestsForProject(queuedTest);

        }
        private string GetProjectOutputBuildFolder(EnvDTE.Project proj)
        {
            try
            {
                // Get the configuration manager of the project
                var configManager = proj.ConfigurationManager;

                if (configManager == null)
                {
                    return string.Empty;
                }
                else
                {
                    // Get the active project configuration
                    var activeConfiguration = configManager.ActiveConfiguration;
                    string assemblyName = GetProjectPropertyByName(proj.Properties, "AssemblyName");
                    // Get the output folder
                    string outputPath = activeConfiguration.Properties.Item("OutputPath").Value.ToString();

                    // The output folder can have these patterns:
                    // 1) "\\server\folder"
                    // 2) "drive:\folder"
                    // 3) "..\..\folder"
                    // 4) "folder"

                    string absoluteOutputPath = null;
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
                    else
                    {
                        string projectFolder = null;
                        if (outputPath.IndexOf("..\\") != -1)
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
                    }
                    return System.IO.Path.Combine(absoluteOutputPath, assemblyName);
                }
            }
            catch (Exception ex)
            {
               return string.Empty;
            }
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
              //  _log.ErrorFormat("Error in GetAssemblyName: {0}", ex.Message);
            }

            return string.Empty;
        }
    }
}
      
