﻿using ErikEJ.SqlCe;
using ICSharpCode.NRefactory.TypeSystem;
using System.Reflection;
using Leem.Testify.Model;
using Leem.Testify.Poco;
using log4net;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharp = System.Threading.Tasks;
using TrackedMethod = Leem.Testify.Poco.TrackedMethod;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using NUnit.Framework;
using System.Collections.Concurrent;


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
        private static readonly ILog Log = LogManager.GetLogger(typeof(TestifyQueries));

        const string Underscores = "__";
        const string GetUnderscore = "::get_";
        const string SetUnderscore = "::set_";

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
            int locationOfSpace = trackedMethodName.IndexOf(' ') + 1;

            int locationOfParen = trackedMethodName.IndexOf('(');

            var testMethodName = trackedMethodName.Substring(locationOfSpace, locationOfParen - locationOfSpace);
            //var testMethodName = trackedMethodName.Substring(locationOfSpace, trackedMethodName.Length - locationOfSpace);
            testMethodName = testMethodName.Replace("::", ".");

            return testMethodName;
        }

        public string ConvertUnitTestFormatToFormatTrackedMethod(string testMethodName)
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

               // testMethodName = testMethodName + "()";

                return testMethodName;
            }

        }

        public void AddToTestQueue(string projectName)
        {
            try
            {
                // make sure this is not a test project. We will build the Test project when we process the TestQueue containing the Code project
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
                            var unitTests = context.UnitTests.Where (u => u.TestProjectUniqueName == projectInfo.TestProject.UniqueName);
                            //if (!unitTests.Any())
                            //{
                                var testQueueItem = new TestQueue
                                {
                                    ProjectName = projectName,
                                    Priority = 1,
                                    QueuedDateTime = DateTime.Now
                                };
                                context.TestQueue.Add(testQueueItem);
                            //}
                            //foreach(var test in unitTests)
                            //{
                            //    var testQueueItem = new TestQueue
                            //    {
                            //        ProjectName = projectName,
                            //        IndividualTest = test.TestMethodName,
                            //        Priority = 1,
                            //        QueuedDateTime = DateTime.Now
                            //    };
                            //    context.TestQueue.Add(testQueueItem);
                            //}
                      
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

        public void AddTestsCoveringFileToTestQueue(string fileName, EnvDTE.Project project) 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                //if (project.Name.EndsWith(".Test"))
                //{
                    var testQueueItem = new TestQueue
                    {
                        ProjectName = project.UniqueName,
                      
                        Priority = 1000,
                        QueuedDateTime = DateTime.Now
                    };

                    context.TestQueue.Add(testQueueItem);
                //}
                //else 
                //{
                //    var coveredLines = context.CoveredLines.Include(u => u.UnitTests).Where(x => x.FileName == fileName);

                //    foreach (var line in coveredLines.ToList())
                //    {
                //        foreach (var method in line.TrackedMethods)
                //        {
                //            var testQueueItem = new TestQueue
                //            {
                //                ProjectName = project.UniqueName,
                //                IndividualTest =method.NameInUnitTestFormat,
                //                Priority = 1000,
                //                QueuedDateTime = DateTime.Now
                //            };

                //            context.TestQueue.Add(testQueueItem);
                //        }

                //    }
                
                //}


                context.SaveChanges();
            }
        } 

        public IEnumerable<CoveredLinePoco> GetCoveredLines(TestifyContext context, string className)
        {
            var module = new Poco.CodeModule();
            IEnumerable<CodeMethod> methods = new List<CodeMethod>();
            IEnumerable<CoveredLinePoco> coveredLines = new List<CoveredLinePoco>();
            IEnumerable<UnitTest> unitTests;

            var clas = context.CodeClass.FirstOrDefault(c=> c.Name == className);
            if (clas != null)
            {
                var sw = Stopwatch.StartNew();
                module = context.CodeModule.FirstOrDefault(mo => mo.CodeModuleId == clas.CodeModule.CodeModuleId);
                context.Database.Log = L => Log.Debug(L);
                coveredLines = (context.CoveredLines
                    .Where(line => line.Class.CodeClassId == clas.CodeClassId)
                    .Include(u => u.UnitTests))
                    //.Include(me => me.Method)
                    //.Include(me => me.TrackedMethods))
                    .ToList();
                   
                coveredLines.Select(x => { x.Module = module; return x; });
                Log.DebugFormat("Get CoveredLines from database Elapsed Time : {0}", sw.ElapsedMilliseconds);
            }
            
            return coveredLines;
        }

        public QueuedTest GetIndividualTestQueue(int testRunId)
        {
            using (
                var context = new TestifyContext(_solutionName))
            {

                List<TestQueue> batchOfTests = new List<TestQueue>();

                if (context.TestQueue.All(x => x.TestRunId == 0))// there aren't any Individual tests currently running  .Where(i => i.IndividualTest != null)
                {
                    // Get the ten highest priority tests from each project
                    var projectGroups = (from t in context.TestQueue
                                        group t by new { t.ProjectName } into g
                                         select new { ProjectName = g.Key, TestQueueItems = g.OrderByDescending(o => o.Priority).Take(10) });

                    if(projectGroups.Count() > 0)
                    {
                        // Get the tests from the group which has the highest priority tests by summing the priorities
                        batchOfTests = projectGroups.OrderByDescending(group => group.TestQueueItems.Sum(i => i.Priority)).FirstOrDefault().TestQueueItems.ToList();
                    }


                }
                QueuedTest queuedTest = null;

                if (batchOfTests != null && batchOfTests.Any())
                {
                    MarkTestAsInProgress(testRunId, context, batchOfTests);
                    queuedTest = new QueuedTest
                    {
                        IndividualTests = batchOfTests.Where(x => x.IndividualTest != null).Select(s => s.IndividualTest).ToList(),
                                                  TestRunId=testRunId,
                                                  ProjectName= batchOfTests.First().ProjectName,
                                                    Priority = batchOfTests.Max(p=>p.Priority)};
                }

                return queuedTest;
            }

        }

        public QueuedTest GetProjectTestQueue(int testRunId)  
        {
            using (var context = new TestifyContext(_solutionName))
            {
                QueuedTest nextItem = null;

                if (context.TestQueue.All(x => x.TestRunId == 0))// there aren't any tests currently running
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
                    testsToMarkInProgress = MarkTestAsInProgress(testRunId, context, testsToMarkInProgress);
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
                Log.ErrorFormat("Error Getting Project Info, error: {0}", ex);
                return null;
            }

        }

        public ProjectInfo GetProjectInfoFromTestProject(string uniqueName)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var result = from project in context.Projects
                             join testProject in context.TestProjects on project.UniqueName equals testProject.ProjectUniqueName
                             where testProject.UniqueName == uniqueName || project.UniqueName == uniqueName
                             select new ProjectInfo
                             {
                                 ProjectName = project.Name,
                                 ProjectAssemblyName = project.AssemblyName,
                                 TestProject = testProject,
                                 UniqueName = project.UniqueName,
                                 Path = project.Path
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


        public IList<TestProject> GetTestProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.TestProjects.ToList();
            }
        }

        public IEnumerable<UnitTest> GetUnitTestByName(string name)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return SelectUnitTestByName(name, context);
            }
        }

        //public void GetUnitTestsCoveringMethod(string modifiedMethod)
        //{
        //    using (var context = new TestifyContext(_solutionName))
        //    {

        //        var query = from unitTest in context.TrackedMethods
        //                    where unitTest.Name.Contains(modifiedMethod)
        //                    select unitTest.UnitTests;

        //    }
        //}

        public List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber)
        {
            string methodNameFragment = className + "::" + methodName;

            var tests = new List<UnitTest>();

            using (var context = new TestifyContext(_solutionName))
            {
                var query = (from line in context.CoveredLines.Include(x => x.UnitTests)

                             where line.Method.Name.Contains(methodNameFragment)
                             select line.UnitTests);

                tests = query.SelectMany(x => x).ToList();
            }

            var testNames = new List<string>();

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

                    context.SaveChanges();
                }

                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in MaintainProjects Message: {0}", ex);
                }

                _sw.Stop();


            }
        }

        public void RemoveFromQueue(QueuedTest testQueueItem)
        {
            Log.DebugFormat("NUnit Completed for:  {0} Elapsed Time {1} ms", testQueueItem.ProjectName, DateTime.Now - testQueueItem.TestStartTime);

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
                ProjectInfo projectInfo;
                if (projectName.EndsWith(".Test.csproj"))
                {
                    projectInfo = GetProjectInfoFromTestProject(projectName);
                }

                else 
                {
                    projectInfo = GetProjectInfo(projectName);
                }
                
                
                if (projectInfo != null && projectInfo.TestProject != null)
                {
                    var testQueueItem = new QueuedTest { ProjectName = projectName, IndividualTests = unitTestNames };

                    using (var context = new TestifyContext(_solutionName))
                    {

                        if (testQueueItem.IndividualTests.Any())
                        {
                            foreach (var test in testQueueItem.IndividualTests)
                            {
                                var testQueue = new TestQueue { ProjectName = testQueueItem.ProjectName,
                                                                Priority=1000,
                                                                IndividualTest = test, 
                                                                QueuedDateTime = DateTime.Now };

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

       public async Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, resultType testOutput, ProjectInfo projectInfo, List<string> individualTests)
        {
            Log.DebugFormat("SaveCoverageSessionResults for ModuleName {0} ", projectInfo.ProjectName);

            var coverageService = CoverageService.Instance;
            coverageService.Queries = this;
            coverageService.SolutionName = _solutionName;

            var changedClasses = new List<string>();

            List<LineCoverageInfo> newCoveredLineInfos = new List<LineCoverageInfo>();

            if (coverageSession.Modules.Count < 2)
            {
                Log.ErrorFormat("SaveCoverageSessionResults - CoverageSession does not contain 2 Modules, Module Count:{0}", coverageSession.Modules.Count);
                return new List<string>();

            }

            var sessionModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName));
            sessionModule.AssemblyName = projectInfo.ProjectAssemblyName;

            var testModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName));
            testModule.AssemblyName = projectInfo.TestProject.AssemblyName;

            var methodMapper = CreateMethodMap(coverageSession);
 
            coverageService.UpdateMethodsAndClassesFromCodeFile(coverageSession.Modules, methodMapper);
            //UpdateTrackedMethods(trackedMethodUnitTestMapper);
            var changedUnitTestClasses = SaveUnitTestResults(testOutput, testModule, methodMapper);

            try
            {
                if (individualTests == null || !individualTests.Any())
                {
                    // Tests have been run on the whole module, so any line not in CoverageSession is not "Covered"
                    foreach (var module in coverageSession.Modules) 
                    {
                        UpdateModulesClassesMethodsSummaries(module);
                    }

                    newCoveredLineInfos = coverageService.GetCoveredLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName, methodMapper);
                    var newCoveredLineList = new List<CoveredLinePocoDto>();

                   // UpdateUnitTests(testModule);

                    using (var context = new TestifyContext(_solutionName))
                    {

                        try
                        {
                             var existingCoveredLines = GetCoveredLinesForModule(sessionModule.ModuleName, context);

                            var module = context.CodeModule.FirstOrDefault(x => x.Name.Equals(sessionModule.ModuleName));
                            // The module has a list of file names and each method has a file uid. Need to use this to set the file name of the method 
                            // and use NRefactory to get Line number of the Method
                            
                            var modifiedLines = await AddOrUpdateCoveredLine(changedClasses, newCoveredLineInfos, context, existingCoveredLines, module, methodMapper);
                            var coveredLinePocos = new List<CoveredLinePoco>();
                            foreach (var item in modifiedLines)
                            {

                                coveredLinePocos.Add(ConstructCoveredLinePoco(item));

                                changedClasses.Add(item.ClassName);
                            }
                           context.CoveredLines.AddRange(coveredLinePocos);

                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error in SaveCoverageSessionResults Inner Exception: {0} Message: {1} StackTrace {2}", ex.InnerException, ex.Message, ex.StackTrace);
                        }

 
                        try
                        {
                            var sw = Stopwatch.StartNew();
                            context.SaveChanges();
                            Log.DebugFormat("context.SaveChanges() for project: (0) Elapsed Time:{1}", projectInfo.UniqueName,sw.ElapsedMilliseconds);
                        }

                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error in UpdateTrackedMethods Message: {0}", ex.InnerException);
                        }

                    }
                }
                else
                {
                    // Tests have been run on the whole module, so any line not in CoverageSession is not "Covered"
                    foreach (var module in coverageSession.Modules)
                    {
                        UpdateModulesClassesMethodsSummaries(module);
                    }

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

                    using (var context = new TestifyContext(_solutionName))
                    {
                        var unitTests = context.UnitTests.Where(x => individualTests.Contains(x.TestMethodName));

                        List<int> unitTestIds = unitTests.Select(x => x.UnitTestId).ToList();

                        if (unitTestIds != null)
                        {
                            newCoveredLineInfos = coverageService.GetRetestedLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName, individualTestUniqueIds);
                            newCoveredLineInfos.AddRange(coverageService.GetRetestedLinesFromCoverageSession(coverageSession, projectInfo.TestProject.AssemblyName, individualTestUniqueIds));
                            changedClasses = newCoveredLineInfos.Select(x => x.Class.Name).Distinct().ToList();
                        }

                        var module = context.CodeModule.FirstOrDefault(x => x.Name.Equals(sessionModule.ModuleName));
                        var existingCoveredLines = GetCoveredLinesForModule(sessionModule.ModuleName, context);
                        var modifiedLines = await AddOrUpdateCoveredLine(changedClasses, newCoveredLineInfos, context, existingCoveredLines, module, methodMapper);

                        foreach (var item in modifiedLines)
                        {
                            context.CoveredLines.Add(ConstructCoveredLinePoco(item));
                        }
                        context.SaveChanges();
                    }

                }

                RefreshUnitTestIds(newCoveredLineInfos, sessionModule, testModule, methodMapper);
               
                OnClassChanged(changedClasses.Distinct().ToList());
                Log.DebugFormat("SaveCoverageSessionResults exiting ");
                return changedClasses;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in SaveCoverageSessionResults Inner Exception: {0} Message: {1} StackTrace {2}", ex.InnerException, ex.Message, ex.StackTrace);
                return new List<string>();
            }
        }

       private List<TrackedMethodMap> CreateMethodMap(CoverageSession coverageSession)
       {
           var methodMapper = new List<TrackedMethodMap>();
           foreach (var module in coverageSession.Modules)
           {
               foreach (var clas in module.Classes)
               {
                   try
                   {
                       if (clas.FullName.Contains("<>c__DisplayClass") == false
                           && clas.FullName.Contains("<>f__AnonymousType") == false)
                       {
                           IProjectContent project = new CSharpProjectContent();
                           var hasCoveredMethods = clas.Methods.Any(x => x.SkippedDueTo == 0);
                           if (hasCoveredMethods && clas.Methods.Any(y=>y.FileRef != null))
                           {
                               var uniqueId = clas.Methods.FirstOrDefault(x => x.FileRef != null).FileRef.UniqueId;
                               string filePath = module.Files.FirstOrDefault(x => x.UniqueId == uniqueId).FullPath;
                               var syntaxTree = GetSyntaxTree(filePath);
                               methodMapper.AddRange(GetTestCaseMethods(syntaxTree));
                           }

                       }
                   }
                   catch (Exception ex)
                   {
                       Log.ErrorFormat("Error building TrackedMethod UnitTest Map ClassName: {0} Message: {1}", clas.FullName, ex);
                   }
               } 
           }
           return methodMapper;
       }

        private async CSharp.Task<List<LineCoverageInfo>> AddOrUpdateCoveredLine(IList<string> changedClasses, IList<LineCoverageInfo> newCoveredLineList, TestifyContext context, ILookup<int, CoveredLinePoco> existingCoveredLines, CodeModule module, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            var modifiedLineCoverageInfos = new List<LineCoverageInfo>();
            ILookup<string, CodeMethod> methodLookup = context.CodeMethod.ToLookup(m => m.Name, m => m);
            ILookup<string, CodeClass> classLookup = context.CodeClass.ToLookup(c => c.Name, c => c);
            ILookup<string, UnitTest> unitTestLookup = context.UnitTests.ToLookup(c => c.TestMethodName, c => c);
         
            //if (trackedMethodMap != null)
            //{
            //    var unitTests = trackedMethodMap.MethodInfos;
            //    var xy = unitTests.Count;
            //}
            //var tmm = RemoveNamespaces(tests.First().Name);
            //var tm = tests.Where(x => RemoveNamespaces(x.Name) == trackedMethodMap.TrackedMethodName).ToList();
            foreach (var line in newCoveredLineList)
            {
                await GetModuleClassMethodForLine(context, line, methodLookup, classLookup);
                line.Module = module;
                foreach (var trackedMethod in line.TrackedMethods)
                {
                   
                    var trackedMethodUnitTestMap = trackedMethodUnitTestMapper.FirstOrDefault(x => x.CoverageSessionName == trackedMethod.Name);
                    if (trackedMethodUnitTestMap == null) 
                    {
                        var trackedMethodNameWithoutNamespaces = CoverageService.Instance.RemoveNamespaces(trackedMethod.Name);
                        trackedMethodUnitTestMap = trackedMethodUnitTestMapper.FirstOrDefault(x => x.CoverageSessionName == trackedMethodNameWithoutNamespaces);
                    }
                    if (trackedMethodUnitTestMap != null)
                    {
                        if (!line.UnitTests.Any(x => trackedMethodUnitTestMap.MethodInfos.Any(y => y.MethodName.Contains(x.TestMethodName))))
                        {
                            foreach (var unitTestMethodInfo in trackedMethodUnitTestMap.MethodInfos)
                            {
                                var unitTest = unitTestLookup[unitTestMethodInfo.MethodName].FirstOrDefault();
                                if (unitTest != null)
                                {
                                    line.UnitTests.Add(unitTest);
                                }

                            }

                        }
                    }

                    
                }

                CoveredLinePoco existingLine = null;

                existingLine = await GetExistingCoveredLineByMethodAndLineNumber(existingCoveredLines, line);

                if (existingLine != null)
                {
                    var changedClass = await ProcessExistingLine(unitTestLookup, line, existingLine, trackedMethodUnitTestMapper);
                    if(changedClass != string.Empty)
                        changedClasses.Add(changedClass);
                }
                else
                {
                    var methodName = line.MethodName.ToString();
                    var isNotAnonOrGetterSetter = !methodName.Contains("__")
                                                    && !methodName.Contains("::get_")
                                                    && !methodName.Contains("::set_");
                    if (isNotAnonOrGetterSetter)
                    {
                        modifiedLineCoverageInfos.Add(line);
                    }

                }

          

                ///todo remove  deleted Unit Tests
                ///

            }
            context.SaveChanges();
            // Remove lines that no longer exist
            var lines = GetCoveredLines(context, "UnitTestExperiment.Domain.ThingsToDo").ToList();
            var methodsTested = newCoveredLineList.Select(x => x.MethodName).Distinct();
            List<CoveredLinePoco> linesToBeDeleted = new List<CoveredLinePoco>();
            foreach (var method in methodsTested)
            {
                var currentLineNumbers = newCoveredLineList.Where(m=>m.Method.Name == method).Select(x => x.LineNumber).Distinct();
                var linesFromThisMethodToBeDeleted = context.CoveredLines.Where(x => x.Method.Name == method && !currentLineNumbers.Contains(x.LineNumber));
                linesToBeDeleted.AddRange(linesFromThisMethodToBeDeleted);
            }
            var testMethodLinesToBeDeleted = context.CoveredLines.Where(x => x.Module.AssemblyName.Contains(".Test") ).ToList();
            foreach (var line in linesToBeDeleted) 
            {
                Log.DebugFormat("Deleting covered line for Method: {0}, Line number: {1} ",line.Method.Name,line.LineNumber);
                context.CoveredLines.Remove(line);
            }
            context.SaveChanges();

            return modifiedLineCoverageInfos;
        }

        private async CSharp.Task GetModuleClassMethodForLine(TestifyContext context, LineCoverageInfo line, ILookup<string,CodeMethod> methodLookup, ILookup<string,CodeClass> classLookup)
        {
            line.Class = classLookup[line.ClassName].FirstOrDefault();
            line.Method = methodLookup[line.MethodName].FirstOrDefault();

        }

        private async CSharp.Task<string> ProcessExistingLine(ILookup<string, UnitTest> unitTestLookup, LineCoverageInfo line, Poco.CoveredLinePoco existingLine, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            var classThatChanged = string.Empty;

            if (  existingLine.IsCode != line.IsCode
               || existingLine.IsCovered != line.IsCovered
               || existingLine.IsBranch != line.IsBranch
               || existingLine.FileName != line.FileName)
            {
                existingLine.IsCode = line.IsCode;
                existingLine.IsCovered = line.IsCovered;
                existingLine.IsBranch = line.IsBranch;
                existingLine.FileName = line.FileName;
                classThatChanged = existingLine.Class.Name;

            }

             

            // Todo Profile and refactor to improve performance
            foreach (var trackedMethod in line.TrackedMethods)
            {
                var trackedMethodUnitTestMap = trackedMethodUnitTestMapper.FirstOrDefault(x => x.CoverageSessionName == trackedMethod.Name);
                if (trackedMethodUnitTestMap != null)
                {
                    foreach (var unitTestMethodInfo in trackedMethodUnitTestMap.MethodInfos)
                    {
                        var matchingUnitTest = unitTestLookup[unitTestMethodInfo.MethodName].FirstOrDefault();
                        if (matchingUnitTest != null)
                        {
                            if (!existingLine.UnitTests.Any(x => x.UnitTestId == matchingUnitTest.UnitTestId))
                            {
                                existingLine.UnitTests.Add(matchingUnitTest);
                                classThatChanged = existingLine.Class.Name;
                            }
                        }
                    } 
                }



            }
            return classThatChanged;
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

        public List<string> SaveUnitTestResults(resultType testOutput, Leem.Testify.Model.Module testModule, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            var changedUnitTestClasses = new List<string>();


            string runDate = testOutput.date;
            string runTime = testOutput.time;
            string fileName = testOutput.name;

             try 
	        {	        
		       var extractedMethods = testModule.Classes.SelectMany(c => c.Methods);
                var trackedMethods = testModule.TrackedMethods.Select(t => t);
                var filePathDictionary = (from m in extractedMethods
                                          join t in testModule.TrackedMethods on m.MetadataToken equals t.MetadataToken
                                          join f in testModule.Files on m.FileRef.UniqueId equals f.UniqueId
                                          select new { t.Name, f.FullPath })
                                        .ToDictionary(mc => mc.Name.Substring(mc.Name.IndexOf(" ") + 1),
                                                      mc => mc.FullPath);

                var unitTests = GetUnitTests(testOutput.testsuite);

                var trackedMethodDictionary = new ConcurrentDictionary<string, TrackedMethod>();


                using (var context = new TestifyContext(_solutionName))
                {

                    foreach (var test in unitTests)
                    {
                        try
                        {
                            Poco.TrackedMethod existingTrackedMethodFromDictionary;
                            Poco.TrackedMethod trackedMethodJustAdded = null;

                            var trackedMethodUnitTestMap = trackedMethodUnitTestMapper.FirstOrDefault(y => y.MethodInfos.Any(z => z.MethodName.EndsWith(test.TestMethodName,StringComparison.OrdinalIgnoreCase)));

                            var isTrackedMethodInDictionary = trackedMethodDictionary.TryGetValue(trackedMethodUnitTestMap.TrackedMethodName, out existingTrackedMethodFromDictionary);
                            if (filePathDictionary.ContainsKey(test.TestMethodName))
                            {
                               test.FilePath = filePathDictionary[test.TestMethodName];
                            }
                           
                            if (isTrackedMethodInDictionary == false)
                            {
                                var existingTrackedMethod = context.TrackedMethods.FirstOrDefault(x => x.Name.Equals(trackedMethodUnitTestMap.TrackedMethodName));

                                if (existingTrackedMethod == null)
                                {
                                    var trackedMethodToAdd = trackedMethods.FirstOrDefault(x => CoverageService.Instance.RemoveNamespaces(x.Name) == trackedMethodUnitTestMap.TrackedMethodName);
                                    if (trackedMethodToAdd != null)
                                    {
                                        var pocoTrackedMethod = new Poco.TrackedMethod
                                        {
                                            UniqueId = (int)trackedMethodToAdd.UniqueId,
                                            //UnitTestId = trackedMethod.UnitTestId,
                                            Strategy = trackedMethodToAdd.Strategy,
                                            Name = CoverageService.Instance.RemoveNamespaces(trackedMethodToAdd.Name),
                                            MetadataToken = trackedMethodToAdd.MetadataToken
                                        };
                                        pocoTrackedMethod.UnitTests.Add(test);
                                        context.TrackedMethods.Add(pocoTrackedMethod);
                                        context.SaveChanges();
                                        trackedMethodJustAdded = context.TrackedMethods.Local.FirstOrDefault(x => x.Name.Equals(trackedMethodToAdd.Name));
                                        trackedMethodDictionary.TryAdd(trackedMethodJustAdded.Name, trackedMethodJustAdded);
                                    }


                                }
                                else
                                {
                                    trackedMethodDictionary.TryAdd(existingTrackedMethod.Name, existingTrackedMethod);
                                }
                                if (trackedMethodJustAdded != null)
                                {
                                    test.TrackedMethod = trackedMethodJustAdded;
                                    context.SaveChanges();
                                }


                            }

                            var trackedMethodFromContext = context.TrackedMethods.FirstOrDefault(x => x.Name.Equals(trackedMethodUnitTestMap.TrackedMethodName));

                   

                            if (trackedMethodFromContext != null)
                            {
                                test.TrackedMethod = context.TrackedMethods.FirstOrDefault(x => x.Name == trackedMethodUnitTestMap.TrackedMethodName);

                                var existingTest = context.UnitTests.FirstOrDefault(y => y.TestMethodName.Equals(test.TestMethodName));
                                test.LastRunDatetime = runDate + " " + runTime;

                                test.AssemblyName = fileName;

                                if (test.IsSuccessful)
                                {
                                    test.LastSuccessfulRunDatetime = DateTime.Parse(test.LastRunDatetime);
                                }

                                var className = test.TestMethodName.Substring(0, test.TestMethodName.LastIndexOf("."));

                                if (existingTest == null)
                                {

                                    test.LineNumber = 1;

                                    var testName = ConvertUnitTestFormatToFormatTrackedMethod(test.TestMethodName);
                                    string filePath;
                                    filePathDictionary.TryGetValue(testName, out filePath);
                                    test.FilePath = filePath;

                                    context.UnitTests.Add(test);
                                    if (!trackedMethodFromContext.UnitTests.Any(x => x.TestMethodName == test.TestMethodName))
                                    {
                                        // Remove this is never hit

                                        trackedMethodFromContext.UnitTests.Add(test);
                                    }
                                    changedUnitTestClasses.Add(className);
                                }
                                else
                                {
                                    existingTest.LastSuccessfulRunDatetime = test.LastSuccessfulRunDatetime;
                                    existingTest.TestDuration = test.TestDuration;
                                    existingTest.FilePath = test.FilePath;
                                    if (existingTest.IsSuccessful != test.IsSuccessful || existingTest.Result != test.Result)
                                    {
                                        existingTest.IsSuccessful = test.IsSuccessful;
                                        existingTest.Result = test.Result;
                                        changedUnitTestClasses.Add(className);
                                    }

                                } 
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error in SaveUnitTestResults UnitTestName: {0}, InnerException {1}", test.TestMethodName, ex);
                        }
                    }

                    //var classesWithRemovedTests = RemoveDeletedUnitTests(trackedMethodUnitTestMapper, context);
                    //changedUnitTestClasses.AddRange(classesWithRemovedTests);
                    context.SaveChanges();
                }

	            }
	            catch (Exception ex)
	            {
		
                            Log.ErrorFormat("Error building FilePathDictionary Error: {0}", ex);
	            }


                return changedUnitTestClasses;
            
        }

        private SyntaxTree GetSyntaxTree(string fileName)
        {
            string code = string.Empty;
            try
            {
                code = System.IO.File.ReadAllText(fileName);
            }
            catch (Exception)
            {
                Log.ErrorFormat("Could not find file to GetSyntaxTree, Name: {0}", fileName);
            }

            SyntaxTree syntaxTree = new CSharpParser().Parse(code, fileName);
            return syntaxTree;
        }
        //private IProjectContent AddFileToProject(IProjectContent project, SyntaxTree syntaxTree)
        //{
        //    CecilLoader loader = new CecilLoader();
        //    System.Reflection.Assembly[] assembliesToLoad = {
               
        //        typeof(TestCaseAttribute).Assembly
        //       };

        //    IUnresolvedAssembly[] projectAssemblies = new IUnresolvedAssembly[assembliesToLoad.Length];
        //    for (int i = 0; i < assembliesToLoad.Length; i++)
        //    {
        //        projectAssemblies[i] = loader.LoadAssemblyFile(assembliesToLoad[i].Location);
        //    }


        //    project = project.AddAssemblyReferences(projectAssemblies);

        //    CSharpUnresolvedFile unresolvedFile = syntaxTree.ToTypeSystem();

        //    if (syntaxTree.Errors.Count == 0)
        //    {
        //        project = project.AddOrUpdateFiles(unresolvedFile);
        //    }
        //    return project;
        //}
        private static List<string> RemoveDeletedUnitTests(IList<TrackedMethodMap> trackedMethodUnitTestMapper, TestifyContext context)
        {
            var changedUnitTestClasses = new List<string>();
            var methodsInCoverageResult = trackedMethodUnitTestMapper.SelectMany(m => m.MethodInfos).ToList();
            //var extractedMethodNames = new List<string>();
            //foreach (var method in extractedMethods)
            //{
            //    var methodName = method.Name.Substring(method.Name.IndexOf(" ") + 1);
            //    methodName = methodName.Replace("()", string.Empty);
            //    methodName = methodName.Replace("::", ".");
            //    extractedMethodNames.Add(methodName);
            //}
            // = extractedMethods.Select(y => y.Name.Substring(y.Name.IndexOf(" ") + 1)).ToList();
            var unitTestsToBeDeleted = new  List<UnitTest>();// = context.UnitTests.Where(x => !methodsInCoverageResult.Any( y=>y.UnitTestMethodName == TestMethodName)).ToList();
            foreach (var test in unitTestsToBeDeleted)
            {
                 var linesInUnitTestToBeDeleted = context.CoveredLines.SelectMany(x => x.UnitTests)
                                             .Where(x => x.TestMethodName == test.TestMethodName && x.FilePath == test.FilePath);

                var coveredLines = linesInUnitTestToBeDeleted.SelectMany(x=>x.CoveredLines).Include(y=>y.Class).Distinct().ToList();


                foreach (var line in coveredLines)
                {
                    changedUnitTestClasses.Add(line.Class.Name);
                    changedUnitTestClasses.Add(line.Class.Name);
                    context.CoveredLines.Remove(line);
                }
                context.UnitTests.Remove(test);
                var className = test.TestMethodName.Substring(0, test.TestMethodName.LastIndexOf("."));
                changedUnitTestClasses.Add(className);
            }
            return changedUnitTestClasses.Distinct().ToList();
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

        private void UpdateTrackedMethods(IEnumerable<TrackedMethod> trackedMethods)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                foreach (var currentTrackedMethod in trackedMethods)
                {
                    var existingTrackedMethod = context.TrackedMethods.FirstOrDefault(x => x.Name.Equals(currentTrackedMethod.Name));

                    if (existingTrackedMethod == null)
                    {
                        context.TrackedMethods.Add(currentTrackedMethod);
                    }

                }

                try
                {
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in UpdateTrackedMethods Message: {0}", ex.InnerException);
                }
            }
        }
        
        private void UpdateTrackedMethods(List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                TrackedMethod trackedMethodFromContext = null;
                foreach (var trackedMethodMap in trackedMethodUnitTestMapper)
                {
                    var existingTrackedMethod = context.TrackedMethods.FirstOrDefault(x => x.Name.Equals(trackedMethodMap.TrackedMethodName));

                    if (existingTrackedMethod == null)
                    {
                        context.TrackedMethods.Add(existingTrackedMethod);
                    }                    

                    trackedMethodFromContext = context.TrackedMethods.FirstOrDefault(x => x.Name.Equals(trackedMethodMap.TrackedMethodName));

                    foreach (var unitTestMethodInfo in trackedMethodMap.MethodInfos) 
                    {
                        var existingUnitTest = trackedMethodFromContext.UnitTests.FirstOrDefault(x => x.TestMethodName == unitTestMethodInfo.MethodName);
                        if (existingUnitTest == null)
                        {
                           // trackedMethodFromContext.Add();
                        }

                    }
                }

                try
                {
                    context.SaveChanges();
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

        private static CoveredLinePocoDto ConstructCoveredLinePocoDto(LineCoverageInfo line)
        {

            var newCoverage = new CoveredLinePocoDto
            {
                LineNumber = line.LineNumber,
                IsCode = line.IsCode,
                IsCovered = line.IsCovered,
                Module_CodeModuleId = line.Module.CodeModuleId,
                Class_CodeClassId = line.Class.CodeClassId,
                Method_CodeMethodId = line.Method.CodeMethodId,
                FileName = line.FileName,
                IsBranch= line.IsBranch,
                UnitTests=line.UnitTests

            };

            return newCoverage;
        }

        private static CoveredLinePoco ConstructCoveredLinePoco(LineCoverageInfo line)
        {
            if (line.Class == null) 
            {
                Log.ErrorFormat("Line.Class is null for {0}",line.MethodName);
            }
            var coveredLine = new CoveredLinePoco
            {
                Class = line.Class,
                FileName = line.FileName,
                //IsBranch=line.IsBranch,
                IsCode = line.IsCode,
                IsCovered = line.IsCovered,
                IsSuccessful = line.UnitTests.All(y => y.IsSuccessful),
                LineNumber = line.LineNumber,
                Method = line.Method,
                Module = line.Module,
                TrackedMethods = line.TrackedMethods,
                UnitTests=line.UnitTests
            };

            return coveredLine;
        }
        //private static void DoBulkCopy(string tableName, IEnumerable<CoveredLinePocoDto> coveredLines, TestifyContext context)
        //{
        //    //var options = new SqlCeBulkCopyOptions();

        //    using (var bc = new SqlCeBulkCopy(context.Database.Connection.ConnectionString))
        //    {
        //        bc.DestinationTableName = tableName;
        //        bc.WriteToServer(coveredLines);
        //    }
        //}

        private async Task< CoveredLinePoco> GetExistingCoveredLineByMethodAndLineNumber(ILookup<int, Poco.CoveredLinePoco> existingCoveredLines, LineCoverageInfo line)
        {
            CoveredLinePoco existingLine = null;
            try
            {
                if (line.Method != null)
                {
                    existingLine = existingCoveredLines[line.LineNumber].FirstOrDefault(x => x.Method != null && x.Method.Equals(line.Method));
                }
            }
            catch (Exception ex)
            {

                Log.ErrorFormat("Error in GetCoveredLinesByClassAndLine, Method is null for Class: {0}, Method: {1}, Error: {2}", line.ClassName, line.MethodName, ex);
            }

            return existingLine;
        }

        private static ILookup<int, CoveredLinePoco> GetCoveredLinesForModule(string moduleName, TestifyContext context)
        {
            var existingCoveredLines = (from line in context.CoveredLines
                                        where line.Module.Name.Equals(moduleName)
                                        select line).ToLookup(x => x.LineNumber);

            return existingCoveredLines;
        }

        private static List<TestQueue> MarkTestAsInProgress(int testRunId, TestifyContext context,  List<TestQueue> testsToRun)
        {
            List<TestQueue> testsToMarkInProgress = new List<TestQueue>();
            foreach (var test in testsToRun)
            {
                
                if (string.IsNullOrEmpty(test.IndividualTest))
                {
                    // if we are running all the tests for the project, we can remove all the individual and Project tests 
                    testsToMarkInProgress.AddRange(context.TestQueue.Where(x => x.ProjectName.Equals(test.ProjectName)).ToList());
                }
                else 
                {
                    // if the queued item has individual tests, we will remove all of these individual tests from queue.
                    testsToMarkInProgress.AddRange(context.TestQueue.Where(x => x.IndividualTest.Equals(test.IndividualTest)).ToList());

                }

            }

            foreach (var test in testsToMarkInProgress.ToList())
            {
                test.TestRunId = testRunId;
                test.TestStartedDateTime = DateTime.Now;
            }

            context.SaveChanges();

            //// if the queued item has individual tests, we will remove all of these individual tests from queue.
            //if (nextItem.IndividualTests == null)
            //{
            //    testsToMarkInProgress = context.TestQueue.Where(x => x.ProjectName.Equals(nextItem.ProjectName)).ToList();
            //}
            //else if (nextItem.IndividualTests.Any())
            //{
            //    // if we are running all the tests for the project, we can remove all the individual and Project tests 
            //    foreach (var testToRun in nextItem.IndividualTests)
            //    {
            //        testsToMarkInProgress.Add(context.TestQueue.FirstOrDefault(x => x.IndividualTest.Equals(testToRun)));
            //    }
            //}

            //foreach (var test in testsToMarkInProgress.ToList())
            //{
            //    test.TestRunId = testRunId;
            //    test.TestStartedDateTime = DateTime.Now;
            //}

            //context.SaveChanges();

            return testsToMarkInProgress;
        }

        private static List<UnitTest> SelectUnitTestByName(string name, TestifyContext context)
        {
            var query = (from test in context.UnitTests
                         where test.TestMethodName.Equals(name)
                         select test);

            return query.ToList();
        }

        private UnitTest ConstructUnitTest(testcaseType testcase)
        {

             var unitTest = new Poco.UnitTest
            {
                TestDuration = testcase.time,
                TestMethodName = testcase.name,
                Executed = testcase.executed.Equals(bool.TrueString),
                Result = testcase.result,
                NumberOfAsserts = Convert.ToInt32(testcase.asserts),
                IsSuccessful = testcase.success == bool.TrueString
            };
          
            if (testcase.success != null && testcase.success.Equals(Boolean.TrueString))
            {
                unitTest.LastSuccessfulRunDatetime = DateTime.Now;
            }

            return unitTest;
        }

        private List<string> GetChangedMethods(TestifyContext context)
        {
            var changedMethods = new List<string>();

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

        private List<string> GetChangedMethods(IEnumerable<CoveredLinePoco> coveredLines)
        {
            var changedMethods = coveredLines.GroupBy(i => i.Method.Name)
                                                           .Select(i => i.Key)
                                                           .ToList();
            return changedMethods;
        }

        private List<Poco.UnitTest> GetUnitTests(object element)
        {
            var unitTests = new List<UnitTest>();

            try
            {
                if (element.GetType() == typeof(testcaseType))
                {
                    var testcase = (testcaseType)element;

                    var unitTest = ConstructUnitTest(testcase);

                    //unitTest.TestMethodName = testcase.name;

                    unitTests.Add(unitTest);
                }
                else
                {
                    var type = element as testsuiteType;
                    if (type != null)
                    {
                        var testsuite = type;

                        foreach (var item in testsuite.results.Items)
                        {
                            unitTests.AddRange(GetUnitTests(item));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in GetUnitTests Error: {0}",ex);
               
            }

            return unitTests;
        }

        private void RefreshUnitTestIds(IList<LineCoverageInfo> newCoveredLineInfos, Leem.Testify.Model.Module module, Leem.Testify.Model.Module testModule,List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            var trackedMethodLists = (from testInfo in newCoveredLineInfos
                                      where testInfo.TrackedMethods != null
                                      select testInfo.TrackedMethods);

            var trackedMethods = trackedMethodLists.SelectMany(x => x).ToList();

            if (trackedMethods.Any())
            {
                var distinctTrackedMethods = trackedMethods.GroupBy(x => x.MetadataToken).Select(y => y.First()).ToList();

                UpdateUnitTests(testModule, distinctTrackedMethods, trackedMethodUnitTestMapper);

                UpdateTrackedMethods(distinctTrackedMethods);
                var sw = new Stopwatch();
                sw.Start();
                UpdateCoveredLines(module, distinctTrackedMethods, newCoveredLineInfos);

                Log.DebugFormat("UpdateCoveredLines took {0} seconds", sw.ElapsedMilliseconds/1000);
                sw.Stop();

            }
        }

        private void UpdateModulesClassesMethodsSummaries(Leem.Testify.Model.Module module)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var classLookup = context.CodeClass.ToLookup(clas => clas.Name, clas => clas);
                var methodLookup = context.CodeMethod.ToLookup(m => m.Name.ToString(), m => m);
         
                var codeModule = context.CodeModule.FirstOrDefault(x=> x.Name.Equals(module.ModuleName));
                if (codeModule != null)
                {
                    UpdateSummary(module.Summary, codeModule.Summary);

                }
                else 
                {
                    codeModule = new CodeModule(module);
                    context.CodeModule.Add(codeModule);
                }

                UpdateCodeClasses(module, codeModule, context, classLookup, methodLookup);
                //RemoveMissingClasses(context, codeModule);

                context.SaveChanges();
            }
        }

        private void UpdateCodeClasses(Leem.Testify.Model.Module module, CodeModule codeModule, TestifyContext context, ILookup<string, CodeClass> classLookup, ILookup<string, CodeMethod> methodLookup)
        {
            foreach (var moduleClass in module.Classes)
            {
                var pocoCodeClass = classLookup[ moduleClass.FullName].FirstOrDefault();
                          
                if (!moduleClass.FullName.Contains("__"))
                {
                    if (pocoCodeClass != null)
                    {
                        UpdateSummary(moduleClass.Summary, pocoCodeClass.Summary);

                    }
                    else
                    {
                        pocoCodeClass = new CodeClass(moduleClass);
                        codeModule.Classes.Add(pocoCodeClass);
                    }

                    UpdateCodeMethods(moduleClass, pocoCodeClass, methodLookup);
                   // RemoveMissingMethods(context, moduleClass);
                    
                    context.SaveChanges();
                }
            }

        }

        public void RemoveMissingClasses(Leem.Testify.Model.Module module, List<string> currentClassNames, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                //var classesInDatabase = from l in context.CoveredLines
                //                        join m in context.CodeMethod on l.Method.CodeMethodId equals m.CodeMethodId
                //                        join c in context.CodeClass on m.CodeClassId equals c.CodeClassId
                //                        where m.CodeClass.CodeModule.Name == module.FullName
                //                        select c;

                var classesInDatabase = from c in context.CodeClass 
                                        where c.CodeModule.Name == module.FullName
                                        select c;
                var missingClassIds = (from c in classesInDatabase
                                       where !currentClassNames.Contains(c.Name)
                                        select c.CodeClassId).Distinct();

                if (missingClassIds.Count() > 0)
                {
                    foreach (var missingClassId in missingClassIds)
                    {
                        Log.DebugFormat(" CodeClassId {0}  should be deleted from DB", missingClassId);
                        var classToBeDeleted = context.CodeClass.Find(missingClassId);
                        context.CodeClass.Remove(classToBeDeleted);
                    }
                }
                context.SaveChanges();
            }
        }

        public void RemoveMissingMethods(Leem.Testify.Model.Module module, List<string> currentMethodNames, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                try
                {
                    var methodsInDatabase = (from m in context.CodeMethod
                                            join c in context.CodeClass on m.CodeClassId equals c.CodeClassId
                                            where m.CodeClass.CodeModule.Name == module.ModuleName
                                            select m).Distinct().ToList();
                    //var methodNamesInDatabase = methodsInDatabase.Select(m => new { Name = m.Name.Substring(m.Name.IndexOf(" ") + 1).Replace("()", string.Empty).Replace("::", "."), CodeMethodId = m.CodeMethodId }).Distinct().ToList();
                    var methodNamesInDatabase = methodsInDatabase.Select(m => new { Name = m.Name.Substring(m.Name.IndexOf(" ") + 1).Replace("()", string.Empty).Replace("::", "."), CodeMethodId = m.CodeMethodId }).Distinct().ToList();
                    var methodNames = trackedMethodUnitTestMapper.SelectMany(x => x.MethodInfos.Select(y => y.MethodName)).ToList();
                    var missingMethodIds = from m in methodNamesInDatabase
                                           where !methodNames.Contains(m.Name)
                                            select m.CodeMethodId;
                    Log.DebugFormat("RemoveMissingMethods methodNamesInDatabase.Count = {0}  currentMethodNames.Count = {1} ",methodsInDatabase.Count(),methodNamesInDatabase.Count(),currentMethodNames.Count);

                    if (missingMethodIds.Count() > 0)
                    {
                        foreach (var missingMethodId in missingMethodIds)
                        {
                            Log.DebugFormat(" MissingMethodId {0}  should be deleted from DB", missingMethodId);
                            var methodToBeDeleted = context.CodeMethod.FirstOrDefault(x => x.CodeMethodId == missingMethodId);
                            //context.CodeMethod.Remove(methodToBeDeleted);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("ERROR in RemoveMissingMethods  Error: {0}", ex);
    
                }
            }
        }

        private static void UpdateSummary(Model.Summary newSummary, Poco.Summary existing)
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

        private void UpdateCodeMethods(Class codeClass, CodeClass pocoCodeClass, ILookup<string, CodeMethod> methodLookup)
        {
            foreach (var moduleMethod in codeClass.Methods.Where(x=>x.SkippedDueTo != SkippedMethod.AutoImplementedProperty))
            {
                var moduleMethodName = moduleMethod.Name;
                var codeMethod = methodLookup[moduleMethodName].FirstOrDefault();
 
                    if (codeMethod != null)
                    {
                        UpdateSummary(moduleMethod.Summary, codeMethod.Summary);
                                           }
                    else
                    {

                        if (!moduleMethodName.Contains(Underscores)
                            && !moduleMethodName.Contains(GetUnderscore)
                            && !moduleMethodName.Contains(SetUnderscore)
                            && moduleMethod.FileRef != null)
                        { 
                            codeMethod = new CodeMethod(moduleMethod);
                            pocoCodeClass.Methods.Add(codeMethod);
                        }
                    }
            }

        }

        private void UpdateCoveredLines(Leem.Testify.Model.Module module, List<TrackedMethod> trackedMethods, IList<LineCoverageInfo> newCoveredLineInfos)
        {
            var sw = new Stopwatch();
            sw.Start();

            using (var context = new TestifyContext(_solutionName))
            {
                var coveredLines = context.CoveredLines.Include(x=>x.Class)
                                                       .Include(y=>y.Module)
                                                       .Include(z=>z.Method)
                                                       .Where(w =>w.Module.AssemblyName.Equals(module.ModuleName));
                var unitTestLookup = context.UnitTests.ToLookup(x=>x.TestMethodName);

                foreach (var coveredLine in coveredLines)
                {
                    var line = newCoveredLineInfos.FirstOrDefault(x => x.Method != null && x.Method.CodeMethodId.Equals(coveredLine.Method.CodeMethodId) && x.LineNumber.Equals(coveredLine.LineNumber));

                    if (line != null && line.TrackedMethods.Any())
                    {
                        string testMethodName  = line.TrackedMethods.FirstOrDefault().NameInUnitTestFormat;
                        coveredLine.UnitTests = unitTestLookup[testMethodName].ToList();
                        if (coveredLine.UnitTests.All(x => x.IsSuccessful == true))
                        {
                            coveredLine.IsSuccessful = true;
                        }
                        else
                        {
                            coveredLine.IsSuccessful = false;
                        }
                    }

                }

                Log.DebugFormat("UpdateCoveredLines for Module.Name: {0}  Elapsed Time = {1}", module.ModuleName, sw.ElapsedMilliseconds);
                context.SaveChanges();
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

                // Todo - Delete projects from database that no longer exist in solution.

                context.SaveChanges();

                _sw.Stop();

                Log.DebugFormat("UpdateProjects Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in UpdateProjects Message: {0}", ex);
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



        private void UpdateUnitTests(Leem.Testify.Model.Module testModule, List<Poco.TrackedMethod> trackedMethods, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            _sw.Restart();
            var coverageService = CoverageService.Instance;
            var testCaseMethodsList = new List<TrackedMethodMap>();

            var extractedMethods = testModule.Classes.SelectMany(c => c.Methods);
            var filePathDictionary = (from m in extractedMethods
                                      join t in testModule.TrackedMethods on m.MetadataToken equals t.MetadataToken
                                      join f in testModule.Files on m.FileRef.UniqueId equals f.UniqueId
                                      select new { t.MetadataToken, f.FullPath })
                                    .ToDictionary(mc => mc.MetadataToken,
                                                  mc => mc.FullPath);
            try
            {
                if (testModule != null)
                {
                    using (var context = new TestifyContext(_solutionName))
                    {
                        var testProjectUniqueName = context.TestProjects.First(x => x.AssemblyName.Equals(testModule.ModuleName)).UniqueName;

                        //Create Unit Test objects
                        var unitTests = new List<UnitTest>();
                        foreach (var trackedMethod in trackedMethods)
                        {
                            string filePath;
                            filePathDictionary.TryGetValue((int)trackedMethod.MetadataToken, out filePath);
                            var method = extractedMethods.FirstOrDefault(x => x.MetadataToken == trackedMethod.MetadataToken);

                           

                            List<UnitTest> matchingUnitTests = new List<UnitTest>();
                            //if (trackedMethod.Name.Contains("()"))
                            //{
                            //   var matchingTest = context.UnitTests.FirstOrDefault(x => x.TrackedMethod.TrackedMethodId.Equals(trackedMethod.TrackedMethodId));
                            //   if (matchingTest != null)
                            //   {
                            //       matchingUnitTests.Add(matchingTest);
                            //   }
                               
                            //}
                            //else
                            //{
                            //}
                            var trackedMethodUnitTestMap = trackedMethodUnitTestMapper.FirstOrDefault(x => x.CoverageSessionName == trackedMethod.Name);
                            if (trackedMethodUnitTestMap != null)
                            {
                                var methodInfo = coverageService.UpdateMethodLocation(method, filePath, trackedMethodUnitTestMap);
                                var unitTestNames = trackedMethodUnitTestMap.MethodInfos.Select(x => x.ReflectionName).ToList();
                                var unitTestsToAdd = context.UnitTests.Where(x => unitTestNames.Contains(x.TestMethodName)).ToList();
                                matchingUnitTests.AddRange(unitTestsToAdd);  

                  

                                foreach (var matchingUnitTest in matchingUnitTests)
                                {

                                    matchingUnitTest.TestProjectUniqueName = testProjectUniqueName;

                                    // May need to make this a list of UnitTestIds on TrackedMethod
                                    //trackedMethod.UnitTestId = matchingUnitTest.UnitTestId;
                                    matchingUnitTest.FilePath = filePath;

                                    matchingUnitTest.LineNumber = methodInfo.Line;
                                
                                }
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

        private List<TrackedMethodMap> GetTestCaseMethods(SyntaxTree syntaxTree)
        {
            var unitTestCasesList = new List<TrackedMethodMap>();
            foreach (var element in syntaxTree.Children)
            {
               
                if (element.GetType() == typeof(ICSharpCode.NRefactory.CSharp.NamespaceDeclaration) && element.HasChildren)
                {
                    var x = ((ICSharpCode.NRefactory.CSharp.NamespaceDeclaration)(element)).FullName;
                    var unitTestCasesFromNameSpace = CheckNamespaceForTestCaseMethods(syntaxTree, (NamespaceDeclaration)element);
                    if (unitTestCasesFromNameSpace.Any())
                    {
                        unitTestCasesList.AddRange(unitTestCasesFromNameSpace);
                    }
                }
            }
            return unitTestCasesList;
        }

        private List<TrackedMethodMap> CheckNamespaceForTestCaseMethods(SyntaxTree syntaxTree, NamespaceDeclaration namespaceDeclarationNode)
        {
            var unitTestCasesList = new List<TrackedMethodMap>();
            foreach (var element in namespaceDeclarationNode.Children)
            {
                if (element.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeDeclaration) && element.HasChildren)
                {
                    var xx = ((ICSharpCode.NRefactory.CSharp.EntityDeclaration)(element)).Name;
                    var unitTestCasesFromClass = CheckClassForTestCaseAttribute(syntaxTree, (TypeDeclaration)element);
                    if (unitTestCasesFromClass.Any())
                    {
                        unitTestCasesList.AddRange(unitTestCasesFromClass);
                    }
                }
            }
            return unitTestCasesList;
        }

        private List<TrackedMethodMap> CheckClassForTestCaseAttribute(SyntaxTree syntaxTree, TypeDeclaration typeDeclarationNode)
        {
            var unitTestCasesList = new List<TrackedMethodMap>();
            foreach (var element in typeDeclarationNode.Children)
            {
                if (element.GetType() == typeof(ICSharpCode.NRefactory.CSharp.MethodDeclaration) && element.HasChildren)
                {
                    var xxx = ((ICSharpCode.NRefactory.CSharp.EntityDeclaration)(element)).Name;
                    var unitTestCasesFromMethod = CheckMethodForTestCaseAttribute(syntaxTree, (MethodDeclaration)element);
                    if (unitTestCasesFromMethod != null)
                    {
                        unitTestCasesList.Add(unitTestCasesFromMethod);
                    }
                }
            }
            return unitTestCasesList;
        }

        private TrackedMethodMap CheckMethodForTestCaseAttribute(SyntaxTree syntaxTree, MethodDeclaration methodDeclarationNode)
        {
            var arguments = string.Empty;
            var parameters = "(";

            IProjectContent project = new CSharpProjectContent();
            var unresolvedFile = syntaxTree.ToTypeSystem();
            project = project.AddOrUpdateFiles(unresolvedFile);

            ICompilation compilation = project.CreateCompilation();

            var resolver = new ICSharpCode.NRefactory.CSharp.Resolver.CSharpAstResolver(compilation, syntaxTree, unresolvedFile);

            var result = resolver.Resolve(methodDeclarationNode);
            var member = (ICSharpCode.NRefactory.Semantics.MemberResolveResult)result;
            var memberDefinition = member.Member;

            var unresolvedParameters = ((ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedMethod)(memberDefinition.UnresolvedMember)).Parameters;
            if (unresolvedParameters.Any())
            {

                foreach (var parameter in unresolvedParameters)
                {
                    var extractedParameter = string.Empty;
                    if (parameters.Length > 1)
                    {
                        parameters = parameters + ",";
                    }
                    if (parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedParameter)
                        || parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference))
                    {
                        var typeArguments = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(parameter.Type)).TypeArguments.FirstOrDefault();

                        if (((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(parameter.Type)).Identifier == "List"
                            && typeArguments != null
                            && typeArguments.GetType() == typeof(ICSharpCode.NRefactory.TypeSystem.KnownTypeReference))
                        {
                          // var lookupMode = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(typeArguments)).LookupMode;
                           var abbreviatedName = typeArguments.ToString();
                           var fullName = ((ICSharpCode.NRefactory.TypeSystem.KnownTypeReference)(typeArguments)).Name;
                           extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(parameter.Type)).ToString().Replace(abbreviatedName, fullName);
                        }
                        else 
                        {
                            extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(parameter.Type)).ToString();
                        }
                     
                       
                    }
                    else if (parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.TypeSystem.KnownTypeCode))
                    {
                        extractedParameter = parameter.Name;
                    }
                    else if (parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.TypeSystem.KnownTypeReference))
                    {
                        extractedParameter = ((ICSharpCode.NRefactory.TypeSystem.KnownTypeReference)(parameter.Type)).Name;
                    }
                    else if (parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.TypeSystem.ParameterizedTypeReference)
                        || parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeSystem.MemberTypeOrNamespaceReference))
                    {
                        var parameterType = parameter.Type.ToString();
                        parameterType = parameterType.Replace("System.Nullable[[int]]", "Nullable<Int32>");
                        extractedParameter = parameterType;

                    }
                    else if (parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.TypeSystem.ArrayTypeReference))
                    {
                        extractedParameter = ((ICSharpCode.NRefactory.TypeSystem.KnownTypeReference)(((ICSharpCode.NRefactory.TypeSystem.ArrayTypeReference)(parameter.Type)).ElementType)).Name + "[]";
                    }
                    else if (parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.TypeSystem.ByReferenceTypeReference))
                    {
                        extractedParameter = ((ICSharpCode.NRefactory.TypeSystem.KnownTypeReference)(((ICSharpCode.NRefactory.TypeSystem.ByReferenceTypeReference)(parameter.Type)).ElementType)).Name;
                    }
                    else
                    {
                        Log.ErrorFormat("Error Can't handle parameter of type: {0} ", parameter.Type.GetType().ToString());
                    }
                    extractedParameter = extractedParameter.Replace("System.Nullable[[int]]", "Nullable<Int32>");
                    if (extractedParameter.Contains("System.Nullable[[") && extractedParameter.Contains("]]"))
                    {
                        extractedParameter = extractedParameter.Replace("System.Nullable[[", "Nullable<");
                        extractedParameter = extractedParameter.Replace("]]", ">");
                        extractedParameter = extractedParameter.Replace("bool", "Boolean");
                    }
                    extractedParameter = extractedParameter.Replace("[[DateTime]]", "Nullable<DateTime>");
                    extractedParameter = extractedParameter.Replace("[[Double]]", "Nullable<Double>");
                    extractedParameter = extractedParameter.Replace("Nullable<double>", "Nullable<Double>");   
                    parameters = parameters + extractedParameter;
                }
                
            }
            parameters = parameters + ")";
            var modifiedMemberDefinitionName = memberDefinition.ReflectionName.ReplaceAt(memberDefinition.ReflectionName.LastIndexOf("."), "::");

            string returnType;

            returnType = memberDefinition.UnresolvedMember.ReturnType.ToString();


            returnType = returnType.Replace("string","String");
            returnType = returnType.Replace("Void", "System.Void");
            returnType = returnType.Replace("void", "System.Void");
            returnType = returnType.Replace("bool", "System.Boolean");
            returnType = returnType.Replace("double", "System.Double");
            returnType = returnType.Replace("object", "Object");
                
            returnType = returnType.Replace("short", "Int16");
            //returnType = returnType.Replace("int", "System.Int32"); // causes the name Appointment to become AppoSystem.Int32mentDto
            returnType = returnType.Replace("System.Nullable[[int]]", "Nullable<Int32>");
            if (returnType.Contains("System.Nullable[[") && returnType.Contains("]]"))
            {
                returnType = returnType.Replace("System.Nullable[[", "Nullable<");
                returnType = returnType.Replace("]]", ">");
            }
            returnType = returnType.Replace("[[DateTime]]", "Nullable<DateTime>");
            returnType = returnType.Replace("[[Double]]", "Nullable<Double>");
                
            if(returnType == "IList<int>")
            {
                var returnTypeArgument = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(memberDefinition.UnresolvedMember.ReturnType)).TypeArguments[0];
                returnType = "IList<" + ((ICSharpCode.NRefactory.TypeSystem.KnownTypeReference)(returnTypeArgument)).Name + ">";
            }

            var unresolvedMemberReturnType = memberDefinition.UnresolvedMember.ReturnType.Resolve(compilation);
            

            var unitTestCases = new TrackedMethodMap
            {
                TrackedMethodName = returnType + " " + modifiedMemberDefinitionName.Trim() + parameters,
                MethodInfos = new List<MethodInfo>()
            };

            var methodDefinition = project.TopLevelTypeDefinitions.Where(x => x.Methods.Any(m => m.ReflectionName == memberDefinition.ReflectionName));
            MethodInfo methodInfo;

            foreach (var element in methodDeclarationNode.Children)
            {
                arguments = string.Empty;
                if (element.GetType() == typeof(ICSharpCode.NRefactory.CSharp.AttributeSection) && element.HasChildren)
                {
                    methodInfo =
                           new MethodInfo
                           {
                               MethodName = memberDefinition.ReflectionName,
                               ReflectionName = memberDefinition.ReflectionName,
                               BeginLine = methodDefinition.FirstOrDefault().BodyRegion.BeginLine,
                               BeginColumn = methodDefinition.FirstOrDefault().BodyRegion.BeginColumn,
                               FileName = unresolvedFile.FileName
                           };
                    arguments = GetArgumentsFromTestCaseAttribute((AttributeSection)element);
                    methodInfo.MethodName = memberDefinition.ReflectionName + arguments;
                    if (!unitTestCases.MethodInfos.Any(x => x.MethodName == methodInfo.MethodName)) 
                    {
                        unitTestCases.MethodInfos.Add(methodInfo);
                    }

                }
                
            }

            return unitTestCases;

        }

        private string GetArgumentsFromTestCaseAttribute(AttributeSection attributeSection)
        {
            var arguments = string.Empty;
            foreach(var attribute in attributeSection.Attributes)
            {
                if (attribute.Type.ToString() == "TestCase") 
                {
                    arguments = "(" + string.Join(",", attribute.Arguments) + ")";
                }
                
            }
            return arguments;
        }

        public CodeModule[] GetModules()
        {
            using (var context = new TestifyContext(_solutionName) )
            {
                return context.CodeModule.Include(x=>x.Summary).ToArray();
            }

        }

        public IEnumerable<CodeClass> GetClasses(CodeModule module)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.CodeClass.Where(x => x.CodeModule.CodeModuleId == module.CodeModuleId).Include(x => x.Summary).ToArray();
            }
        }

        public IEnumerable<CodeMethod> GetMethods(CodeClass _class)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var methods = context.CodeMethod.Where(x => x.CodeClassId == _class.CodeClassId ).Include(x => x.Summary).ToArray();

                var filteredMethods = methods.Where(x => x.Name.ToString().Contains("get_") == false && x.Name.ToString().Contains("set_") == false);
                return filteredMethods.ToArray();
            }
        }

        public async Task<CodeModule[]> GetSummaries() 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                //context.Database.Log = L => Log.Debug(L);
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
                var matchingClass = context.CodeClass.FirstOrDefault(x => x.Name.Equals(className ));
                if (matchingClass != null)
                {
                    matchingClass.FileName = path;
                    matchingClass.Line = line;
                    matchingClass.Column = column;

                }
                context.SaveChanges();
            }
        }

        public void UpdateCodeMethodPath(CodeMethodInfo methodInfo)
        {
            if (methodInfo != null)
            {
                using (var context = new TestifyContext(_solutionName))
                {
                    var rawMethodNameString = methodInfo.RawMethodName.ToString();
                    var modifiedMethodName = rawMethodNameString.ReplaceAt(rawMethodNameString.LastIndexOf("."), "::")
                    .Replace(".::ctor", "::.ctor");

                    var matchingMethod = context.CodeMethod.FirstOrDefault(x => x.Name.Contains(methodInfo.RawMethodName));
                    if (matchingMethod != null)
                    {
                        matchingMethod.FileName = methodInfo.FileName;
                        matchingMethod.Line = methodInfo.Line;
                        matchingMethod.Column = methodInfo.Column;
                    }
   
                    context.SaveChanges();
                }
            }
        }
       
        public IVsTextView GetIVsTextView(string filePath)
        {
            var dte2 = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(SDTE));
            var sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
            var serviceProvider = new ServiceProvider(sp);

            IVsUIHierarchy uiHierarchy;
            uint itemId;
            IVsWindowFrame windowFrame;

            if (VsShellUtilities.IsDocumentOpen(serviceProvider, filePath, Guid.Empty,
                                            out uiHierarchy, out itemId, out windowFrame))
            {
                // Get the IVsTextView from the windowFrame.
                return VsShellUtilities.GetTextView(windowFrame);
            }

            return null;
        }

        public IWpfTextView GetWpfTextView(IVsTextView vTextView)
        {
            IWpfTextView view = null;
            var userData = vTextView as IVsUserData;

            if (null != userData)
            {
                object holder;
                var guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out holder);
                var viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
            }

            return view;
        }
       
        public CodeMethod GetMethod(string methodName)
        {
            CodeMethod method;
           using(var context = new TestifyContext(_solutionName))
           {
               method = context.CodeMethod.FirstOrDefault(x=>x.Name.Equals(methodName));
           }
            return method;
        }
    }

}
