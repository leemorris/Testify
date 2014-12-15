using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Data;
using log4net;
using ErikEJ.SqlCe;
using System.Data.Entity;
using System.IO;
using Leem.Testify.Model;
using Leem.Testify.Poco;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using ICSharpCode.NRefactory.TypeSystem;
using CSharp = System.Threading.Tasks;


namespace Leem.Testify
{

    [Export(typeof(ITestifyQueries))]
    public class TestifyQueries : ITestifyQueries
    {
        // static holder for instance, need to use lambda to construct since constructor private
        private static readonly Lazy<TestifyQueries> _instance = new Lazy<TestifyQueries>(() => new TestifyQueries());

        private static string _connectionString;
        private static string _solutionName;
        private static Stopwatch _sw;
        private static ILog Log = LogManager.GetLogger(typeof(TestifyQueries));
       
        // private to prevent direct instantiation.
        private TestifyQueries()
        {
            _sw = new Stopwatch();
        }

        public event EventHandler<ClassChangedEventArgs> ClassChanged;
        // accessor for instance
        public static TestifyQueries Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public static string SolutionName
        {
            set
          {
                _solutionName = value;

                var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(value), "TestifyCE.sdf;password=lactose");

                // Set connection string
                _connectionString = string.Format("Data Source={0}", path);
            }
            get 
            {
                return _solutionName;
            }
        }


        public static string ConvertTrackedMethodFormatToUnitTestFormat(string trackedMethodName)
        {
            // Convert This:
            // System.Void UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest::TestIt()
            // Into This:
            // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest.TestIt
            if (string.IsNullOrEmpty(trackedMethodName))
            {
                return string.Empty;
            }
            else
            {
                int locationOfSpace = trackedMethodName.IndexOf(' ') + 1;

                int locationOfParen = trackedMethodName.IndexOf('(');

                var testMethodName = trackedMethodName.Substring(locationOfSpace, locationOfParen - locationOfSpace);

                testMethodName = testMethodName.Replace("::", ".");

                return testMethodName;
            }

        }

        public static string ConvertUnitTestFormatToFormatTrackedMethod(string testMethodName)
        {
            // Convert This:
            // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest.TestIt
            // Into This:
            // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest::TestIt()
            if (string.IsNullOrEmpty(testMethodName))
            {
                return string.Empty;
            }
            else
            {
                int locationOfLastDot = testMethodName.LastIndexOf(".");

                testMethodName = testMethodName.Remove(locationOfLastDot, 1);

                testMethodName = testMethodName.Insert(locationOfLastDot, "::");

                testMethodName = testMethodName + "()";

                return testMethodName;
            }

        }

        public void AddToTestQueue(string projectName)
        {
            try
            {
                // make sure this is not a test project
                if (!projectName.Contains(".Test"))
                {
                    var projectInfo = GetProjectInfo(projectName);

                    // make sure there is a matching test project
                    if (projectInfo != null && projectInfo.TestProject != null)
                    {
                        var testQueue = new TestQueue
                        {
                            ProjectName = projectName,
                            QueuedDateTime = DateTime.Now
                        };
                        using (var context = new TestifyContext(_solutionName))
                        {
                            context.TestQueue.Add(testQueue);
                            context.SaveChanges();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in AddToTestQueue {0}", ex);
            }
        }

        public void AddToTestQueue(TestQueue testQueue)
        {
            try
            {
                using (var context = new TestifyContext(_solutionName))
                {
                    context.TestQueue.Add(testQueue);

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in AddToTestQueue {0}", ex);
            }
        }

        public IEnumerable<Poco.CoveredLinePoco> GetCoveredLines(TestifyContext context, string className)
        {
            var sw = new Stopwatch();

            sw.Restart();

            var coveredLines = context.CoveredLines
                                        .Include(u => u.UnitTests)
                                        .Include(mo => mo.Module).Include(c =>c.Class)
                                        .Include(me =>me.Method)
                                        .Where(x => x.Class.Name.Equals(className));

            return coveredLines;
        }

        public QueuedTest GetIndividualTestQueue(int testRunId) // List<TestQueueItem> 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                QueuedTest nextItem = null;

                if (context.TestQueue.Where(i => i.IndividualTest != null).All(x => x.TestRunId == 0))// there aren't any Individual tests currently running
                {

                    var individual = (from queueItem in context.TestQueue
                                      where queueItem.IndividualTest != null
                                      group queueItem by queueItem.ProjectName).AsEnumerable().Select(x => new QueuedTest
                                    {
                                        ProjectName = x.Key,
                                        IndividualTests = (from test in context.TestQueue
                                                           where test.ProjectName == x.Key
                                                           && test.IndividualTest != null
                                                           select test.IndividualTest).ToList()
                                    }).OrderBy(o => o.IndividualTests.Count());

                    nextItem = individual.FirstOrDefault();

                }

                var testsToMarkInProgress = new List<TestQueue>();

                if (nextItem != null)
                {
                    testsToMarkInProgress = MarkTestAsInProgress(testRunId, context, nextItem, testsToMarkInProgress);
                }

                return nextItem;
            }

        }

        public ProjectInfo GetProjectInfo(string uniqueName)
        {
            try
            {
                using (var context = new TestifyContext(_solutionName))
                {

                    var result = from project in context.Projects
                                 join testProject in context.TestProjects on project.UniqueName equals uniqueName
                                 where testProject.ProjectUniqueName == project.UniqueName
                                 select new ProjectInfo
                                 {
                                     ProjectName = project.Name,
                                     ProjectAssemblyName = project.AssemblyName,
                                     TestProject = testProject
                                 };

                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error Getting Project Info, error: {0}", ex);
                return null;
            }

        }

        public ProjectInfo GetProjectInfoFromTestProject(string uniqueName)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var result = from project in context.Projects
                             join testProject in context.TestProjects on project.UniqueName equals testProject.ProjectUniqueName
                             where project.UniqueName == uniqueName
                             select new ProjectInfo
                             {
                                 ProjectName = project.Name,
                                 ProjectAssemblyName = project.AssemblyName,
                                 TestProject = testProject
                             };

                return result.FirstOrDefault();
            }
        }

        public IQueryable<Project> GetProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.Projects;
            }
        }

        public QueuedTest GetProjectTestQueue(int testRunId) // List<TestQueueItem> 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                QueuedTest nextItem = null;

                if (context.TestQueue.Where(i => i.IndividualTest == null).All(x => x.TestRunId == 0))// there aren't any Project tests currently running
                {

                    var query = (from queueItem in context.TestQueue
                                 where queueItem.IndividualTest == null
                                 orderby queueItem.QueuedDateTime
                                 group queueItem by queueItem.ProjectName).AsEnumerable().Select(x => new QueuedTest { ProjectName = x.Key });

                    nextItem = query.FirstOrDefault();

                }

                var testsToMarkInProgress = new List<TestQueue>();

                if (nextItem != null)
                {
                    testsToMarkInProgress = MarkTestAsInProgress(testRunId, context, nextItem, testsToMarkInProgress);
                }

                return nextItem;
            }

        }

        public IList<TestProject> GetTestProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.TestProjects.ToList();
            }
        }

        public List<Poco.UnitTest> GetUnitTestByName(string name)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return SelectUnitTestByName(name, context);
            }
        }

        public void GetUnitTestsCoveringMethod(string modifiedMethod)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var query = from unitTest in context.TrackedMethods
                            where unitTest.Name.Contains(modifiedMethod)
                            select unitTest.UnitTestId;

            }
        }

        public List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber)
        {
            string methodNameFragment = className + "::" + methodName;

            List<UnitTest> tests = new List<UnitTest>();

            using (var context = new TestifyContext(_solutionName))
            {
                var query = (from line in context.CoveredLines.Include(x => x.UnitTests)

                             where line.Method.Name.Contains(methodNameFragment)
                             select line.UnitTests);

                tests = query.SelectMany(x => x).ToList();
            }

            List<string> testNames = new List<string>();

            testNames = tests.Select(x => x.TestMethodName).Distinct().ToList();

            return testNames;
        }

        public async void MaintainProjects(IList<Project> projects)
        {

            using (var context = new TestifyContext(_solutionName))
            {
                try
                {
                    UpdateProjects(projects, context);

                    UpdateTestProjects(projects, context);
                }
                catch (Exception ex)
                {
                    Log.DebugFormat(ex.Message);
                }

                try
                {
                    context.SaveChanges();
                }

                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in MaintainProjects Message: {0}", ex.InnerException);
                }

                _sw.Stop();

                Log.DebugFormat("MaintainProjects Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);
            }
        }

        public void RemoveFromQueue(QueuedTest testQueueItem)
        {
            Log.DebugFormat("Test Completed: {0} Elapsed Time {1}", testQueueItem.ProjectName, DateTime.Now - testQueueItem.TestStartTime);

            var testsToDelete = new List<TestQueue>();

            using (var context = new TestifyContext(_solutionName))
            {
                testsToDelete = context.TestQueue.Where(x => x.TestRunId == testQueueItem.TestRunId).ToList();

                foreach (var test in testsToDelete.ToList())
                {
                    context.TestQueue.Remove(test);
                }

                context.SaveChanges();
            }
        }

        public async System.Threading.Tasks.Task RunTestsThatCoverLine(string projectName, string className, string methodName, int lineNumber)
        {
            try
            {
                var unitTestNames = GetUnitTestsThatCoverLines(className.Substring(0, className.IndexOf('.')), methodName, lineNumber);

                var projectInfo = GetProjectInfo(projectName);

                if (projectInfo != null && projectInfo.TestProject != null)
                {
                    var testQueueItem = new QueuedTest { ProjectName = projectName, IndividualTests = unitTestNames };

                    using (var context = new TestifyContext(_solutionName))
                    {
                        if (testQueueItem.IndividualTests.Any())
                        {
                            foreach (var test in testQueueItem.IndividualTests)
                            {
                                var testQueue = new TestQueue { ProjectName = testQueueItem.ProjectName, IndividualTest = test, QueuedDateTime = DateTime.Now };

                                context.TestQueue.Add(testQueue);
                            }
                        }
                        else
                        {
                            var testQueue = new TestQueue { ProjectName = testQueueItem.ProjectName, QueuedDateTime = DateTime.Now };

                            context.TestQueue.Add(testQueue);
                        }

                        context.SaveChanges();
                    }

                }
                else
                {
                    Log.DebugFormat("GetProjectInfo returned a null TestProject for {0}", projectName);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in RunTestsThatCoverLine {0}", ex);
            }


        }

        public async Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, ProjectInfo projectInfo, List<string> individualTests)
        {
            Log.DebugFormat("SaveCoverageSessionResults for ModuleName {0} ", projectInfo.ProjectName);
            var setUpSW = new Stopwatch();
            setUpSW.Start();
            var coverageService = CoverageService.Instance;
            coverageService.Queries = this;

            ILookup<string, CodeMethod> methodLookup = null;
            ILookup<string, CodeClass> classLookup = null;
            ILookup<string, UnitTest> unitTestLookup = null;
            coverageService.SolutionName = _solutionName;

            var changedClasses = new List<string>();

            IList<LineCoverageInfo> newCoveredLineInfos = new List<LineCoverageInfo>();

            var sessionModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName));
            sessionModule.AssemblyName = projectInfo.ProjectAssemblyName;
            var moduleName = sessionModule != null ? sessionModule.FullName : string.Empty;

            var testModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName));
            var updateSummariesSW = new Stopwatch();
            updateSummariesSW.Start();

            UpdateModulesClassesMethodsSummaries(sessionModule);
            updateSummariesSW.Stop();
            setUpSW.Stop();         
            try
            {
                if (individualTests == null || !individualTests.Any())
                {
                    // Tests have been run on the whole module, so any line not in CoverageSession is not "Covered"

                    newCoveredLineInfos = coverageService.GetCoveredLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName);

                    var newCoveredLineList = new List<CoveredLinePocoDto>();

                    UpdateUnitTests(sessionModule, testModule);

                    using (var context = new TestifyContext(_solutionName))
                    {
                        try
                        {
                            var outerSW = new Stopwatch();
                            outerSW.Start();
                            var contextSW = new Stopwatch();
                            var addNewLineSW = new Stopwatch();
                            var coveredLineSW = new Stopwatch();
                            var unitTestByNameSW = new Stopwatch();
                            var processExistingLinesSW = new Stopwatch();
                            var parseMethodNameSW = new Stopwatch();

                            var existingCoveredLines = GetCoveredLinesForModule(sessionModule.ModuleName, context);
                            var trackedMethodLoopSW = new Stopwatch();
                            
                            var unitTests = context.UnitTests.Where(x => x.AssemblyName.Contains(projectInfo.TestProject.Path));
                            methodLookup = context.CodeMethod.ToLookup(m => m.Name, m => m );
                            classLookup = context.CodeClass.ToLookup(c => c.Name, c => c );
                            unitTestLookup = context.UnitTests.ToLookup(c => c.TestMethodName, c => c);

                            var module = context.CodeModule.FirstOrDefault(x => x.Name == sessionModule.ModuleName);
                            // The module has a list of file names and each method has a file uid. Need to use this to set the file name of the method 
                            // and use NRefactory to get Line number of the Method

                            foreach (var line in newCoveredLineInfos)
                            {
                                contextSW.Start();

                                await GetModuleClassMethodForLine(context, line, methodLookup, classLookup);
                                line.Module = module;
                                contextSW.Stop();
                                parseMethodNameSW.Start();
                                var isNotAnonOrGetterSetter = !line.MethodName.Contains("__")
                                            && !line.MethodName.Contains("::get_")
                                            && !line.MethodName.Contains("::set_");
                                parseMethodNameSW.Stop();
                                Poco.CoveredLinePoco existingLine = null;

                                //CSharp.Task.WhenAll(getModuleClassMethodTask);
                                coveredLineSW.Start();
                                existingLine =  await GetCoveredLinesByClassAndLine(existingCoveredLines, line);
                                coveredLineSW.Stop();                         
                                if (existingLine != null)
                                {
                                    processExistingLinesSW.Start();
                                    ProcessExistingLine(unitTestLookup, line, existingLine);
                                    processExistingLinesSW.Stop();

                                }
                                else if (isNotAnonOrGetterSetter)
                                {
                                    addNewLineSW.Start();
                                    var newCoverage = ConstructCoveredLine(line);

                                    newCoveredLineList.Add(newCoverage);
                                    //context.CoveredLines.Add(newCoverage);
                                    addNewLineSW.Stop();
                                }


                                ///todo remove  deleted Unit Tests
                                ///
      
                            }

                            outerSW.Stop();

                            Log.DebugFormat("SaveCoverageSessionResults Elapsed Times");
                            Log.DebugFormat("Setup Elapsed Time: {0} ms", setUpSW.ElapsedMilliseconds);
                            Log.DebugFormat("  Update Summaries Elapsed Time: {0} ms", updateSummariesSW.ElapsedMilliseconds);
                            
                            
                            Log.DebugFormat("Outer Loop in Elapsed Time: {0} ms", outerSW.ElapsedMilliseconds);
                            Log.DebugFormat("  Access Context Elapsed Time:{0}", contextSW.ElapsedMilliseconds);
                            Log.DebugFormat("  Parse Method Names Elapsed Time: {0}", parseMethodNameSW.ElapsedMilliseconds);
                            Log.DebugFormat("  Add New Line Elapsed Time: {0}", addNewLineSW.ElapsedMilliseconds);
                            Log.DebugFormat("    Process ExistingLines Elapsed Time: {0}", processExistingLinesSW.ElapsedMilliseconds);
                           
                        
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error in SaveCoverageSessionResults Inner Exception: {0} Message: {1} StackTrace {2}", ex.InnerException, ex.Message, ex.StackTrace);
                        }
                       
                        DoBulkCopy("CoveredLinePoco", newCoveredLineList, context);
                        // var isDirty = CheckForChanges(context);

                        try
                        {
                            context.SaveChanges();
                        }
                        catch (DbEntityValidationException dbEx)
                        {
                            foreach (var validationErrors in dbEx.EntityValidationErrors)
                            {
                                foreach (var validationError in validationErrors.ValidationErrors)
                                {
                                    Log.ErrorFormat("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error in UpdateTrackedMethods Message: {0}", ex.InnerException);
                        }


                    }
                }
                else
                {
                    // Only a single unit test was run, so only the lines in the CoverageSession will be updated
                    // Get the MetadataTokens for the unitTests we just ran

                    var individualTestUniqueIds = new List<int>();

                    foreach (var test in individualTests)
                    {
                        var testMethodName = ConvertUnitTestFormatToFormatTrackedMethod(test);
                        individualTestUniqueIds.Add((int)testModule.TrackedMethods
                                                                 .Where(x => x.Name.Contains(testMethodName))
                                                                 .FirstOrDefault().UniqueId);
                    }

                    // GetMetadatTokenForUnitTest(individualTests);                     
                    List<int> unitTestIds;

                    using (var context = new TestifyContext(_solutionName))
                    {
                        var unitTests = context.UnitTests.Where(x => individualTests.Contains(x.TestMethodName));

                        unitTestIds = unitTests.Select(x => x.UnitTestId).ToList();

                        if (unitTestIds != null)
                        {
                            newCoveredLineInfos = coverageService.GetRetestedLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName, individualTestUniqueIds);
                            changedClasses = newCoveredLineInfos.Select(x => x.Class.Name).Distinct().ToList();
                        }
                        context.SaveChanges();
                    }


                }

                RefreshUnitTestIds(newCoveredLineInfos, sessionModule, testModule);
                // Fire and Forget
                //System.Threading.Tasks.Task.Run(() =>
                //{
                //    UpdateClassesAndMethods(project);
                //});
                OnClassChanged(changedClasses);

                return changedClasses;

            }

            catch (Exception ex)
            {

                Log.ErrorFormat("Error in SaveCoverageSessionResults Inner Exception: {0} Message: {1} StackTrace {2}", ex.InnerException, ex.Message, ex.StackTrace);
                return new List<string>();

            }
        }


        //public void UpdateMethodsAndClassesFromCodeFile(string filename)
        //{
        //    IProjectContent project = new CSharpProjectContent();

        //    project.SetAssemblyName(filename);
        //    project = AddFileToProject(project, filename);

        //    var classes = new List<string>();

        //    var typeDefinitions = project.TopLevelTypeDefinitions;

        //    foreach (var typeDef in typeDefinitions)
        //    {
        //        classes.Add(typeDef.ReflectionName);
        //        if (typeDef.Kind == TypeKind.Class)
        //        {
        //            var methods = typeDef.Methods;
        //            //UpdateMethods(typeDef, methods, filename);
        //        }

        //    }

        //}

        //private IProjectContent AddFileToProject(IProjectContent project, string fileName)
        //{
        //    var code = string.Empty;
        //    try
        //    {
        //        code = System.IO.File.ReadAllText(fileName);
        //    }
        //    catch (Exception)
        //    {
        //        Log.ErrorFormat("Could not find file to AddFileToProject, Name: {0}", fileName);
        //    }

        //    var syntaxTree = new CSharpParser().Parse(code, fileName);
        //    var unresolvedFile = syntaxTree.ToTypeSystem();

        //    if (syntaxTree.Errors.Count == 0)
        //    {
        //        project = project.AddOrUpdateFiles(unresolvedFile);
        //    }
        //    return project;
        //}
        private async CSharp.Task GetModuleClassMethodForLine(TestifyContext context, LineCoverageInfo line, ILookup<string,CodeMethod> methodLookup, ILookup<string,CodeClass> classLookup)
        {
            line.Class = classLookup[line.ClassName].FirstOrDefault();
            line.Method = methodLookup[line.MethodName].FirstOrDefault();

        }

        private async CSharp.Task ProcessExistingLine(ILookup<string, UnitTest> unitTestLookup, LineCoverageInfo line, Poco.CoveredLinePoco existingLine)
        {

            if (existingLine.IsCode != line.IsCode)
                existingLine.IsCode = line.IsCode;

            if (existingLine.IsCovered != line.IsCovered)
                existingLine.IsCovered = line.IsCovered;

            if (existingLine.FileName != line.FileName)
                existingLine.FileName = line.FileName;

            // Todo Profile and refactor to improve performance
            foreach (var trackedMethod in line.TrackedMethods)
            {
                //var matchingUnitTest = unitTests
                //                              .FirstOrDefault(x => coveringTest.NameInUnitTestFormat
                //                                  .Equals(x.TestMethodName));
                var matchingUnitTest = unitTestLookup[trackedMethod.NameInUnitTestFormat].FirstOrDefault();
                if (matchingUnitTest != null)
                {
                    if (!existingLine.UnitTests.Any(x => x.UnitTestId == matchingUnitTest.UnitTestId))
                    {
                        existingLine.UnitTests.Add(matchingUnitTest);
                    }
                }


            }

        }

        private async CSharp.Task ProcessTrackedMethods(TestifyContext context, Stopwatch unitTestByNameSW, LineCoverageInfo line, ILookup<string, UnitTest> unitTestLookup)
        {

            foreach (var trackedMethod in line.TrackedMethods)
            {
                unitTestByNameSW.Start();
                var unitTest = unitTestLookup[trackedMethod.NameInUnitTestFormat].FirstOrDefault();
                line.UnitTests.Add(unitTest);
                unitTestByNameSW.Stop();
            }
        }

        public void SaveUnitTest(UnitTest test)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                context.UnitTests.Add(test);

                try
                {
                    context.SaveChanges();
                }

                catch (Exception ex)
                {
                    Log.ErrorFormat("Error SaveUnitTest, InnerException:{0}, UnitTest Name: {1}", ex.InnerException, test.TestMethodName);
                }
            }
        }
        public void SaveUnitTestResults(resultType testOutput)
        {

            string runDate = testOutput.date;

            string runTime = testOutput.time;

            string fileName = testOutput.name;
           
     

            var unitTests = GetUnitTests(testOutput.testsuite);

            foreach (var test in unitTests)
            {
                test.LastRunDatetime = runDate + " " + runTime;

                test.AssemblyName = fileName;

                if (test.IsSuccessful)
                {
                    test.LastSuccessfulRunDatetime = DateTime.Parse(test.LastRunDatetime);

                }

                if (test.TestMethodName.Contains("("))
                {
                    Debug.WriteLine("test.TestMethodName= {0}", test.TestMethodName);
                }

            }

            using (var context = new TestifyContext(_solutionName))
            {

                try
                {
                    foreach (var test in unitTests)
                    {
                        var existingTest = context.UnitTests.FirstOrDefault(y => y.TestMethodName == test.TestMethodName);

                        if (existingTest == null)
                        {
                            // todo get the actual line number from the FileCodeModel for this unit test, to be used in the bookmark
                            test.LineNumber = "1";

                            context.UnitTests.Add(test);

                           // Log.DebugFormat("Added UnitTest to Context: Name: {0}, IsSucessful : {1}", test.TestMethodName, test.IsSuccessful);

                        }
                        else
                        {
                            test.UnitTestId = existingTest.UnitTestId;

                            existingTest.LastSuccessfulRunDatetime = test.LastSuccessfulRunDatetime;

                            context.Entry(existingTest).CurrentValues.SetValues(test);
                        }

                    }

                    context.SaveChanges();

                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in SaveUnitTestResults Message: {0}, InnerException {1}", ex.Message, ex.InnerException);
                }
            }
        }

        public void SetAllQueuedTestsToNotRunning()
        {
            using (var context = new TestifyContext(_solutionName))
            {

                foreach (var test in context.TestQueue)
                {
                    test.TestRunId = 0;
                }

                context.SaveChanges();
            }
        }

        public void UpdateTrackedMethods(IList<Poco.TrackedMethod> trackedMethods)
        {
            using (var context = new TestifyContext(_solutionName))
            {

                foreach (var currentTrackedMethod in trackedMethods)
                {
                    //var existingTrackedMethod = context.TrackedMethods.Find(currentTrackedMethod.MetadataToken);
                    var existingTrackedMethod = context.TrackedMethods.Where(x => x.Name == currentTrackedMethod.Name).FirstOrDefault();

                    if (existingTrackedMethod == null)
                    {
                        context.TrackedMethods.Add(currentTrackedMethod);
                    }

                }

                _sw.Stop();

                Log.DebugFormat("UpdateTrackedMethods Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);

                try
                {
                    context.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            Log.ErrorFormat("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in UpdateTrackedMethods Message: {0}", ex.InnerException);
                }
            }
        }

        protected virtual void OnClassChanged(List<string> changedClasses)
        {

            var args = new ClassChangedEventArgs();

            args.ChangedClasses = changedClasses;

            if (ClassChanged != null)
            {
                ClassChanged(this, args);
            }
        }

        private static CoveredLinePocoDto ConstructCoveredLine(LineCoverageInfo line)
        {

            var newCoverage = new CoveredLinePocoDto
            {
                LineNumber = line.LineNumber,
                IsCode = line.IsCode,
                IsCovered = line.IsCovered,
                Module_CodeModuleId = line.Module.CodeModuleId,
                Class_CodeClassId = line.Class.CodeClassId,
                Method_CodeMethodId = line.Method.CodeMethodId,
                FileName = line.FileName

            };

            return newCoverage;
        }

        private static void DoBulkCopy(string tableName, List<CoveredLinePocoDto> coveredLines, TestifyContext context)
        {
            SqlCeBulkCopyOptions options = new SqlCeBulkCopyOptions();

            using (SqlCeBulkCopy bc = new SqlCeBulkCopy(context.Database.Connection.ConnectionString))
            {
                bc.DestinationTableName = tableName;
                bc.WriteToServer(coveredLines);
            }
        }

        private async Task< Poco.CoveredLinePoco> GetCoveredLinesByClassAndLine(ILookup<int, Poco.CoveredLinePoco> existingCoveredLines, LineCoverageInfo line)
        {
            Poco.CoveredLinePoco existingLine = null;
            try
            {
                if (line.Method != null)
                {
// Null reference exception, line.lineNumber, line.Method and line.Class all have value,
                    //maybe line.linenumber doesn't exist
                    existingLine = existingCoveredLines[line.LineNumber].FirstOrDefault(x => x.Method != null && x.Method.Equals(line.Method)
                                                && x.Class.Equals(line.Class));
                }
            }
            catch (Exception ex)
            {

                Log.DebugFormat("Error in GetCoveredLinesByClassAndLine, Method is null for Class: {0}, Method: {1}, Error: {2}", line.ClassName, line.MethodName, ex);
            }

            return existingLine;
        }

        private static ILookup<int, Poco.CoveredLinePoco> GetCoveredLinesForModule(string moduleName, TestifyContext context)
        {
            var existingCoveredLines = (from line in context.CoveredLines
                                        where line.Module.Name.Equals(moduleName)
                                        select line).ToLookup(x => x.LineNumber);

            return existingCoveredLines;
        }

        private static List<TestQueue> MarkTestAsInProgress(int testRunId, TestifyContext context, QueuedTest nextItem, List<TestQueue> testsToMarkInProgress)
        {
            nextItem.TestRunId = testRunId;

            // if the queued item has individual tests, we will remove all of these individual tests from queue.
            if (nextItem.IndividualTests == null)
            {
                testsToMarkInProgress = context.TestQueue.Where(x => x.ProjectName == nextItem.ProjectName).ToList();
            }
            else if (nextItem.IndividualTests.Any())
            {
                // if we are running all the tests for the project, we can remove all the individual and Project tests 
                foreach (var testToRun in nextItem.IndividualTests)
                {
                    testsToMarkInProgress = context.TestQueue.Where(x => x.IndividualTest == testToRun).ToList();
                }
            }

            foreach (var test in testsToMarkInProgress.ToList())
            {
                test.TestRunId = testRunId;
                test.TestStartedDateTime = DateTime.Now;
            }

            context.SaveChanges();

            return testsToMarkInProgress;
        }

        private static List<Poco.UnitTest> SelectUnitTestByName(string name, TestifyContext context)
        {
            var query = (from test in context.UnitTests
                         where test.TestMethodName.Equals(name)
                         select test);

            return query.ToList();
        }

        private Poco.UnitTest ConstructUnitTest(testcaseType testcase)
        {

            var unitTest = new Poco.UnitTest
            {
                TestDuration = testcase.time,
                TestMethodName = testcase.name,
                Executed = testcase.executed == bool.TrueString,
                Result = testcase.result,
                NumberOfAsserts = Convert.ToInt32(testcase.asserts),
                IsSuccessful = testcase.success == bool.TrueString
            };

            if (testcase.success == Boolean.TrueString)
            {
                unitTest.LastSuccessfulRunDatetime = DateTime.Now;
            }

            return unitTest;
        }

        private List<string> GetChangedMethods(TestifyContext context)
        {
            List<string> changedMethods = new List<string>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Unchanged)
                {
                    var methodName = entry.CurrentValues.GetValue<string>("Method");

                    if (!changedMethods.Contains(methodName))
                    {
                        changedMethods.Add(methodName);
                    }

                }
            }

            return changedMethods;
        }

        private List<string> GetChangedMethods(List<Poco.CoveredLinePoco> coveredLines)
        {
            var changedMethods = coveredLines.GroupBy(i => i.Method.Name)
                                                           .Select(i => i.Key)
                                                           .ToList();
            return changedMethods;
        }

        private List<Poco.UnitTest> GetUnitTests(object element)
        {
            var unitTests = new List<Poco.UnitTest>();

            if (element.GetType() == typeof(testcaseType))
            {
                testcaseType testcase = (testcaseType)element;

                var unitTest = ConstructUnitTest(testcase);

                unitTest.TestMethodName = testcase.name;

                unitTests.Add(unitTest);
            }
            else
            {

                if (element is testsuiteType)
                {
                    testsuiteType testsuite = (testsuiteType)element;

                    foreach (var item in testsuite.results.Items)
                    {
                        unitTests.AddRange(GetUnitTests(item));
                    }
                }
            }

            return unitTests;
        }

        private void RefreshUnitTestIds(IList<LineCoverageInfo> newCoveredLineInfos, Module module, Module testModule)
        {
            var trackedMethodLists = (from testInfo in newCoveredLineInfos
                                      where testInfo.TrackedMethods != null
                                      select testInfo.TrackedMethods);

            var trackedMethods = trackedMethodLists.SelectMany(x => x).ToList();

            //UpdateModulesClassesMethodsSummaries(module);

            if (trackedMethods.Any())
            {
                var distinctTrackedMethods = trackedMethods.GroupBy(x => x.MetadataToken).Select(y => y.First()).ToList();

                UpdateUnitTests(module, testModule);

                UpdateTrackedMethods(distinctTrackedMethods);
                var sw = new Stopwatch();
                sw.Start();
                UpdateCoveredLines(module, distinctTrackedMethods, newCoveredLineInfos);
                Log.DebugFormat("UpdateCoveredLines took {0}", sw.ElapsedMilliseconds);
                sw.Stop();

            }
        }

        private void UpdateModulesClassesMethodsSummaries(Module module)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var classLookup = context.CodeClass.ToLookup(clas => clas.Name, clas => clas);

                var methodLookup = context.CodeMethod.ToLookup(m => m.Name, m => m);
                var codeModule = context.CodeModule.FirstOrDefault(x=> x.Name == module.ModuleName);
                if (codeModule != null)
                {
                    UpdateSummary(module.Summary, codeModule.Summary);

                }
                else 
                {
                    codeModule = new Poco.CodeModule(module);
                    context.CodeModule.Add(codeModule);
                }


                UpdateCodeClasses(module, codeModule, context, classLookup, methodLookup);

                context.SaveChanges();

            }
        }

        private void UpdateCodeClasses(Module module, CodeModule codeModule, TestifyContext context, ILookup<string, CodeClass> classLookup, ILookup<string, CodeMethod> methodLookup)
        {
            foreach (var moduleClass in module.Classes)
            {
                //var pocoCodeClass = context.CodeClass.FirstOrDefault(x => x.Name == moduleClass.FullName);
                var pocoCodeClass = classLookup[ moduleClass.FullName].FirstOrDefault();
                          
                if (!moduleClass.FullName.Contains("__"))
                {
                    if (pocoCodeClass != null)
                    {
                        UpdateSummary(moduleClass.Summary, pocoCodeClass.Summary);

                    }
                    else
                    {
                        pocoCodeClass = new Poco.CodeClass(moduleClass);
                        codeModule.Classes.Add(pocoCodeClass);
                    }

                    UpdateCodeMethods(moduleClass, pocoCodeClass, context, methodLookup);
                    context.SaveChanges();
                }
            }
        }

        private static void UpdateSummary(Leem.Testify.Model.Summary newSummary, Leem.Testify.Poco.Summary existing)
        {
            existing.BranchCoverage = newSummary.BranchCoverage;
            existing.MaxCyclomaticComplexity = newSummary.MaxCyclomaticComplexity;
            existing.MinCyclomaticComplexity = newSummary.MinCyclomaticComplexity;
            existing.NumBranchPoints = newSummary.NumBranchPoints;
            existing.NumSequencePoints = newSummary.NumSequencePoints;
            existing.SequenceCoverage = newSummary.SequenceCoverage;
            existing.VisitedBranchPoints = newSummary.VisitedBranchPoints;
            existing.VisitedSequencePoints = newSummary.VisitedSequencePoints;
        }

        private void UpdateCodeMethods(Class codeClass, Poco.CodeClass pocoCodeClass, TestifyContext context, ILookup<string, CodeMethod> methodLookup)
        {
            foreach (var moduleMethod in codeClass.Methods)
            {
               // var codeMethod = context.CodeMethod.FirstOrDefault(x => x.Name == moduleMethod.Name);
                var codeMethod = methodLookup[moduleMethod.Name].FirstOrDefault();
                if (!moduleMethod.Name.Contains("__")
                    && !moduleMethod.Name.Contains("::get_")
                    && !moduleMethod.Name.Contains("::set_")
                    && moduleMethod.FileRef != null)
                { 

                    if (codeMethod != null)
                    {
                        UpdateSummary(moduleMethod.Summary, codeMethod.Summary);
                                           }
                    else
                    {
                        codeMethod = new Poco.CodeMethod(moduleMethod);
                        pocoCodeClass.Methods.Add(codeMethod);
                    }
                    
                }
            }
        }

        private void UpdateCoveredLines(Module module, List<Poco.TrackedMethod> trackedMethods, IList<LineCoverageInfo> newCoveredLineInfos)
        {
            var sw = new Stopwatch();
            sw.Start();
            var newCoveredMethodIds = newCoveredLineInfos.Where(x=>x.Method != null)
                                                        .GroupBy(g => g.Method)
                                                        .Select(m => m.First().Method.CodeMethodId)
                                                        .ToList();

            using (var context = new TestifyContext(_solutionName))
            {

                var coveredLines = context.CoveredLines.Include(x=>x.Class).Include(y=>y.Module).Include(z=>z.Method).Where(w =>w.Module.AssemblyName == module.ModuleName);
                
                var number = coveredLines.Count();
  
                foreach (var coveredLine in coveredLines)
                {
                    var line = new LineCoverageInfo();
                    try
                    {
                         line = newCoveredLineInfos.FirstOrDefault(x => x.Method != null && x.Method.CodeMethodId == coveredLine.Method.CodeMethodId && x.LineNumber == coveredLine.LineNumber);

                    }
                    catch (Exception ex)
                    {

                        Log.DebugFormat("Error Getting Line: Method.Name: ", coveredLine.Method.Name);
                    }                    

                    string testMethodName = string.Empty;

                    if (line != null && line.TrackedMethods.Any())
                    {
                        testMethodName = line.TrackedMethods.FirstOrDefault().NameInUnitTestFormat;

                    }

                    var unitTests = context.UnitTests.Where(x => x.TestMethodName == testMethodName);

                    foreach (var test in unitTests)
                    {
                        coveredLine.UnitTests.Add(test);
                    }

                    if (unitTests.Any(x => x.IsSuccessful == true))
                    {
                        coveredLine.IsSuccessful = true;
                    }

                    if (unitTests.Any(x => x.IsSuccessful == false))
                    {
                        coveredLine.IsSuccessful = false;
                    }


                }

                context.SaveChanges();
                Log.DebugFormat("UpdateCoveredLines for Module.Name: {0}  Elapsed Time = {1}", module.ModuleName, sw.ElapsedMilliseconds);
            }
        }

        
        private void UpdateProjects(IList<Project> projects, TestifyContext context)
        {
            _sw.Restart();

            // Existing projects
            foreach (var currentProject in projects)
            {
                var existingProject = context.Projects.Find(currentProject.UniqueName);
                if (existingProject != null)
                {

                    // update the path
                    if (currentProject.Path != existingProject.Path
                        && !string.IsNullOrEmpty(currentProject.Path))
                    {
                        existingProject.Path = currentProject.Path;
                    }

                    // update the assembly name
                    if (currentProject.AssemblyName != existingProject.AssemblyName
                        && !string.IsNullOrEmpty(currentProject.AssemblyName))
                    {
                        existingProject.AssemblyName = currentProject.AssemblyName;
                    }
                }
            }

            try
            {
                // Add new projects
                var newProjects = (from currentProject in projects
                                   where !(currentProject.UniqueName.Contains(".Test."))
                                   where !(currentProject.UniqueName.Contains("Solution Items"))
                                   where !(currentProject.UniqueName.Contains("Miscellaneous Files"))
                                   where !(from existing in context.Projects
                                           select existing.UniqueName).Contains(currentProject.UniqueName)
                                   select currentProject).ToList();
                newProjects.ForEach(p => context.Projects.Add(p));

                /// Todo - Delete projects from database that no longer exist in solution.

                context.SaveChanges();

                _sw.Stop();

                Log.DebugFormat("UpdateProjects Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error in UpdateProjects Message: {0}", ex);
                throw;
            }

        }
        private void UpdateTestProjects(IList<Project> projects, TestifyContext context)
        {

            // Existing projects
            foreach (var currentProject in projects)
            {
                Log.DebugFormat("Project Name: {0}, AssemblyName: {1}, UniqueName: {2}", currentProject.Name, currentProject.AssemblyName, currentProject.UniqueName);

                var existingProject = context.TestProjects.Find(currentProject.UniqueName);

                if (existingProject != null)
                {
                    if (currentProject.Path != existingProject.Path
                        && !string.IsNullOrEmpty(currentProject.Path))
                    {
                        existingProject.Path = currentProject.Path;
                    }

                    if (string.IsNullOrEmpty(existingProject.AssemblyName)
                        && !string.IsNullOrEmpty(currentProject.AssemblyName))
                    {
                        existingProject.AssemblyName = currentProject.AssemblyName;
                    }
                }

            }

            // Add new projects
            var newProjects = (from currentProject in projects
                               where (currentProject.Name.Contains(".Test"))
                               && !(from existing in context.TestProjects
                                    select existing.UniqueName).Contains(currentProject.UniqueName)
                               select currentProject).ToList();

            foreach (var newProject in newProjects)
            {
                Log.DebugFormat("New Project Name: {0}, UniqueName: {1}", newProject.Name, newProject.UniqueName);
                var targetProjectName = newProject.Name.Replace(".Test", string.Empty);
                var targetProject = context.Projects.FirstOrDefault(x => x.Name.Equals(targetProjectName));
                var existingProject = context.Projects.FirstOrDefault(x => x.Name.Contains(newProject.Name));

                if (targetProject != null)
                {
                    if (existingProject != null)
                    {
                        existingProject.Name = newProject.Name;
                        existingProject.UniqueName = newProject.UniqueName;
                        existingProject.Path = newProject.Path;
                        existingProject.AssemblyName = newProject.AssemblyName;
                    }
                    else
                    {
                        var newTestProject = new Poco.TestProject
                        {
                            Name = newProject.Name,
                            UniqueName = newProject.UniqueName,
                            Path = newProject.Path,
                            ProjectUniqueName = targetProject.UniqueName,
                            OutputPath = newProject.Path,
                            AssemblyName = newProject.AssemblyName
                        };
                        context.TestProjects.Add(newTestProject);
                    }
                }

            }
        }
        private void UpdateUnitTests(Module codeModule, Module testModule)
        {
            _sw.Restart();

            var distinctTrackedMethods = testModule.TrackedMethods.GroupBy(x => x.MetadataToken).Select(y => y.First()).ToList();

            try
            {
                if (testModule != null)
                {
                    using (var context = new TestifyContext(_solutionName))
                    {
                        var testProjectUniqueName = context.TestProjects.Where(x => x.AssemblyName.Equals(testModule.ModuleName)).First().UniqueName;

                        //Create Unit Test objects
                        var unitTests = new List<Poco.UnitTest>();
                        foreach (var trackedMethod in distinctTrackedMethods)
                        {
                            var testMethodName = ConvertTrackedMethodFormatToUnitTestFormat(trackedMethod.Name);

                            //Todo modify the next line to properly handle TestCases, The Tracking method
                            // The TrackedMethod contains the argument Type in parenthesis
                            //"System.Void Quad.QuadMed.QMedClinicalTools.Domain.Test.Services.PatientMergeServiceTest::CanGetHealthAssessmentsByQMedPidNumber(System.String)"
                            // The UnitTest is saved with the actual value of the argument in parenthesis
                            // Quad.QuadMed.QMedClinicalTools.Domain.Test.Services.PatientMergeServiceTest.CanGetHealthAssessmentsByQMedPidNumber("110989")
                            // The Unit test doesn't match because  (System.String) <> ("110989")

                            var matchingUnitTest = context.UnitTests.FirstOrDefault(x => x.TestMethodName.Equals(testMethodName));

                            if (matchingUnitTest != null)
                            {
                                matchingUnitTest.TestProjectUniqueName = testProjectUniqueName;
                                trackedMethod.UnitTestId = matchingUnitTest.UnitTestId;
                                //matchingUnitTest.FilePath = 

                            }
                            else
                            {
                                Log.DebugFormat("ERROR: Could not find Unit test that matched Tracking Method: {0}", trackedMethod.Name);
                            }

                        }

                        context.SaveChanges();

                    }

                }
                else
                {
                    Log.DebugFormat("UpdateUnitTests was called with a Null TestModule");
                }

            }

            catch (Exception ex)
            {
                Log.ErrorFormat("Error in UpdateUnitTests Message: {0} Message: {1}", ex.InnerException, ex.Message);

            }
        }


        public CodeModule[] GetModules()
        {
            using (var context = new TestifyContext(_solutionName) )
            {
                return context.CodeModule.Include(x=>x.Summary).ToArray();
            }

        }


        public CodeClass[] GetClasses(CodeModule module)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.CodeClass.Where(x => x.CodeModule.CodeModuleId == module.CodeModuleId).Include(x => x.Summary).ToArray();
            }
        }


        public CodeMethod[] GetMethods(CodeClass _class)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var methods = context.CodeMethod.Where(x => x.CodeClassId == _class.CodeClassId ).Include(x => x.Summary).ToArray();

                var filteredMethods = methods.Where(x => x.Name.Contains("get_") == false && x.Name.Contains("set_") == false);
                return filteredMethods.ToArray();
            }
        }

        public async Task<CodeModule[]> GetSummaries() 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var result = context.CodeModule
                    .Include(x => x.Summary)
                    .Include(y => y.Classes.Select(c => c.Summary))
                    .Include(y => y.Classes.Select(m => m.Methods))
                    .Include(y => y.Classes.Select(mm => mm.Methods.Select(s=>s.Summary)))
                    
                    .Include(z => z.Summary).ToArray();
                return result;
            }
        }


        public string GetProjectFilePathFromClass(string name)
        {

            using (var context = new TestifyContext(_solutionName))
            {
  
                var result = from clas in context.CodeClass
                                join project in context.Projects on clas.CodeModule.Name equals project.AssemblyName
                                select project.UniqueName;


                return result.FirstOrDefault();
            }

        }

        public string GetProjectFilePathFromMethod(string name)
        {
            using (var context = new TestifyContext(_solutionName))
            {

                var result = from method in context.CodeMethod
                             join project in context.Projects on method.CodeClass.CodeModule.Name equals project.AssemblyName
                             select project.UniqueName;


                return result.FirstOrDefault();
            }
        }

        public void UpdateCodeClassPath(string className, string path, int line, int column)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var matchingClass = context.CodeClass.Where(x => x.Name == className ).FirstOrDefault();
                if (matchingClass != null)
                {
                    matchingClass.FileName = path;
                    matchingClass.Line = line;
                    matchingClass.Column = column;

                }
                context.SaveChanges();
            }
        }

        public void UpdateCodeMethodPath(string methodName, string path, int line, int column)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var modifiedMethodName = methodName.ReplaceAt(methodName.LastIndexOf("."), "::")
                    .Replace(".::ctor", "::.ctor");
           
                var matchingMethod = context.CodeMethod.Where(x => x.Name.Contains(methodName)).FirstOrDefault();
                if (matchingMethod != null)
                {
                    matchingMethod.FileName = path;
                    matchingMethod.Line = line;
                    matchingMethod.Column = column;
                }
                context.SaveChanges();
            }
        }
       



        public IVsTextView GetIVsTextView(string filePath)
        {
            var dte2 = (EnvDTE80.DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE));
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
            ServiceProvider serviceProvider = new Microsoft.VisualStudio.Shell.ServiceProvider(sp);

            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;
            IWpfTextView wpfTextView = null;
            if (VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty,
                                            out uiHierarchy, out itemID, out windowFrame))
            {
                // Get the IVsTextView from the windowFrame.
                return VsShellUtilities.GetTextView(windowFrame);
            }

            return null;
        }

        public IWpfTextView GetWpfTextView(IVsTextView vTextView)
        {
            IWpfTextView view = null;
            IVsUserData userData = vTextView as IVsUserData;

            if (null != userData)
            {
                IWpfTextViewHost viewHost;
                object holder;
                Guid guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out holder);
                viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
            }

            return view;
        }


        public void UpdateMethods(IUnresolvedTypeDefinition fileClass, IEnumerable<IUnresolvedMethod> methods, string fileName)
        {
            List<string> methodsToDelete = new List<string>();
           
            using (var context = new TestifyContext(_solutionName))
            {
                var codeClasses = from clas in context.CodeClass
                                  join method in context.CodeMethod on clas.CodeClassId equals method.CodeClassId
                                  where clas.Name.Equals(fileClass.ReflectionName)
                                select clas;
                foreach (var codeClass in codeClasses)
                {
                    if (codeClass.FileName != fileName
                        || codeClass.Line != fileClass.BodyRegion.BeginLine
                        || codeClass.Column != fileClass.BodyRegion.BeginColumn)
                    {
                        codeClass.FileName = fileName;
                        codeClass.Line = fileClass.BodyRegion.BeginLine;
                        codeClass.Column = fileClass.BodyRegion.BeginColumn;
                    }
                    
                }
                
                string modifiedMethodName = string.Empty;
                foreach (var fileMethod in methods)
                {
                    var rawMethodName = fileMethod.ReflectionName;
                    if (fileMethod.IsConstructor)
                    {
                        rawMethodName = rawMethodName.Replace("..", ".");
                        modifiedMethodName = ConvertUnitTestFormatToFormatTrackedMethod(rawMethodName);
                        modifiedMethodName = modifiedMethodName.Replace("::ctor", "::.ctor");
                    }
                    else 
                    {
                        modifiedMethodName = ConvertUnitTestFormatToFormatTrackedMethod(rawMethodName);
                    }
                    
                    // remove closing paren
                    modifiedMethodName = modifiedMethodName.Substring(0, modifiedMethodName.Length - 1);

                    var codeMethods = from clas in codeClasses
                                     join method in context.CodeMethod on clas.CodeClassId equals method.CodeClassId
                                     where method.Name.Contains(modifiedMethodName)
                                     select method;
                    foreach (var method in codeMethods)
                    {
                        if(method.FileName != fileName
                           || method.Line != fileMethod.BodyRegion.BeginLine
                           || method.Column != fileMethod.BodyRegion.BeginColumn)
                        {
                            method.FileName = fileName;
                            method.Line = fileMethod.BodyRegion.BeginLine;
                            method.Column = fileMethod.BodyRegion.BeginColumn;
                        }
                        
                    }
                 
                    
                }
                context.SaveChanges();
            }
        }

        //public void UpdateMethodLocationInfo(IUnresolvedMethod fileMethod, CodeMethod codeMethod)
        //{
        //    using(var context = new TestifyContext(_solutionName))
        //    {
        //        codeMethod.Line = fileMethod.BodyRegion.BeginLine;
        //        codeMethod.Column = fileMethod.BodyRegion.BeginColumn;
        //        context.SaveChanges();
        //    }
        //}
       
        public CodeMethod GetMethod(string methodName)
        {
            CodeMethod method = null;
           using(var context = new TestifyContext(_solutionName))
           {
               method = context.CodeMethod.FirstOrDefault(x=>x.Name == methodName);
           }
            return method;
        }
    }

}
