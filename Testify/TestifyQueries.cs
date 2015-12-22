using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CSharp = System.Threading.Tasks;

namespace Leem.Testify
{
    [Export(typeof(ITestifyQueries))]
    public class TestifyQueries : ITestifyQueries
    {
        private const string GetUnderscore = "::get_";

        private const string SetUnderscore = "::set_";

        private const string Underscores = "__";

        // static holder for instance, need to use lambda to construct since constructor private
        private static readonly Lazy<TestifyQueries> _instance = new Lazy<TestifyQueries>(() => new TestifyQueries());

        private static readonly ILog Log = LogManager.GetLogger(typeof(TestifyQueries));
        private static string _connectionString;
        private static string _solutionName;
        private static Stopwatch _sw;

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
                _solutionName = value.Replace(".sln", string.Empty);

                var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                var hashCode = _solutionName.GetHashCode();
                hashCode = hashCode > 0 ? hashCode : -hashCode;
                Log.DebugFormat("The hashcode for {0} is {1}", _solutionName, hashCode);
                var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(value), hashCode.ToString(), "TestifyCE.sdf;password=lactose");

                // Set connection string
                _connectionString = string.Format("Data Source={0}", path);
            }
            get
            {
                return _solutionName;
            }
        }

        public void AddTestsCoveringFileToTestQueue(string fileName, EnvDTE.Project project)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var testQueueItem = new TestQueue
                {
                    ProjectName = project.UniqueName,

                    Priority = 1000,
                    QueuedDateTime = DateTime.Now
                };

                context.TestQueue.Add(testQueueItem);
                context.SaveChanges();
            }
        }

        public void AddToTestQueue(string projectName)
        {
            try
            {
                // make sure this is not a test project. We will build the Test project when we process the TestQueue containing the Code project
                //if (!projectName.Contains(".Test"))
                //{
                var projectInfo = GetProjectInfo(projectName);
                if (projectInfo == null)
                {
                    projectInfo = GetProjectInfoFromTestProject(projectName);
                }

                // make sure there is a matching test project
                if (projectInfo != null && projectInfo.TestProject != null && projectInfo.TestProject.Path != null)
                {
                    var testQueue = new TestQueue
                    {
                        ProjectName = projectInfo.ProjectName,
                        QueuedDateTime = DateTime.Now
                    };
                    using (var context = new TestifyContext(_solutionName))
                    {
                        //Todo remove next line
                        var unitTests = context.TestMethods.Where(u => u.TestProjectUniqueName == projectInfo.TestProject.UniqueName);

                        var testQueueItem = new TestQueue
                        {
                            ProjectName = projectName,
                            Priority = 1,
                            QueuedDateTime = DateTime.Now
                        };
                        context.TestQueue.Add(testQueueItem);

                        context.SaveChanges();
                    }
                }

                //}
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
                if (locationOfLastDot > 0)
                {
                    testMethodName = testMethodName.Remove(locationOfLastDot, 1);
                    testMethodName = testMethodName.Insert(locationOfLastDot, "::");
                }
                return testMethodName;
            }
        }

        public IEnumerable<CodeClass> GetClasses(CodeModule module, TestifyContext context)
        {
             return context.CodeClass.Where(x => x.CodeModule.CodeModuleId == module.CodeModuleId).Include(x => x.Summary).ToArray();
        }

        public IEnumerable<CoveredLine> GetCoveredLines(TestifyContext context, string className)
        {
            var module = new Poco.CodeModule();
            IEnumerable<CodeMethod> methods = new List<CodeMethod>();
            IEnumerable<CoveredLine> coveredLines = new List<CoveredLine>();
            IEnumerable<TestMethod> testMethods;

            //The following line throws Exception
            //An unhandled exception of type 'System.AccessViolationException' occurred in System.Data.SqlServerCe.dll
            //Additional information: Attempted to read or write protected memory. This is often an indication that other memory is corrupt.
            var clas = context.CodeClass.FirstOrDefault(c => c.Name == className);

            if (clas != null)
            {
                var sw = Stopwatch.StartNew();
                module = context.CodeModule.FirstOrDefault(mo => mo.CodeModuleId == clas.CodeModule.CodeModuleId);
                coveredLines = (context.CoveredLines
                    .Where(line => line.Class.CodeClassId == clas.CodeClassId))

                    .ToList();

                coveredLines.Select(x => { x.Module = module; return x; });
                Log.DebugFormat("Get CoveredLines for Class: {0} from database Elapsed Time : {1}", className, sw.ElapsedMilliseconds);
            }

            return coveredLines;
        }

        public List<CoveredLine> GetCoveredLinesForDocument(TestifyContext context, string documentName)
        {
            return context.CoveredLines.Where(x => x.FileName == documentName)
                                        .Include(x => x.Class)
                                        .ToList();
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

                    if (projectGroups.Count() > 0)
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
                        TestRunId = testRunId,
                        ProjectName = batchOfTests.First().ProjectName,
                        Priority = batchOfTests.Max(p => p.Priority)
                    };
                }

                return queuedTest;
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

        public CodeMethod GetMethod(string methodName)
        {
            CodeMethod method;
            using (var context = new TestifyContext(_solutionName))
            {
                method = context.CodeMethod.FirstOrDefault(x => x.Name.Equals(methodName));
            }
            return method;
        }

        public IEnumerable<CodeMethod> GetMethods(CodeClass _class, TestifyContext context)
        {
            var methods = _class.Methods;//context.CodeMethod.Where(x => x.CodeClassId == _class.CodeClassId && x.FileName != null).Include(x => x.Summary).ToArray();

            var filteredMethods = methods.Where(x => x.Name.ToString().Contains("get_") == false && x.Name.ToString().Contains("set_") == false);
            return filteredMethods.ToArray();
        }

        public CodeModule[] GetModules(TestifyContext context)
        {
            return context.CodeModule.Where(x => x.Name.EndsWith(".Test") == false).Include(x => x.Summary).ToArray();
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
                                     TestProject = testProject,
                                     Path = project.Path,
                                     UniqueName = project.UniqueName
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

        public async Task<CodeModule[]> GetSummaries()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                //context.Database.Log = L => Log.Debug(L);
                var result = context.CodeModule
                    .Include(x => x.Summary)
                    .Include(y => y.Classes.Select(c => c.Summary))
                    .Include(y => y.Classes.Select(m => m.Methods))
                    .Include(y => y.Classes.Select(mm => mm.Methods.Select(s => s.Summary)))

                    .Include(z => z.Summary).ToArray();
                return result;
            }
        }

        public IList<TestProject> GetTestProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.TestProjects.ToList();
            }
        }

        public IEnumerable<TestMethod> GetUnitTestByName(string name)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return SelectUnitTestByName(name, context);
            }
        }

        public List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber)
        {
            string methodNameFragment = className + "::" + methodName;

            var tests = new List<TestMethod>();

            using (var context = new TestifyContext(_solutionName))
            {
                var query = (from line in context.CoveredLines.Include(x => x.TestMethods)

                             where line.Method.Name.Contains(methodNameFragment)
                             select line.TestMethods);

                tests = query.SelectMany(x => x).ToList();
            }

            var testNames = new List<string>();

            testNames = tests.Select(x => x.TestMethodName).Distinct().ToList();

            return testNames;
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

        public void RemoveAllTestsFromQueue()
        {
            var testsToDelete = new List<TestQueue>();

            using (var context = new TestifyContext(_solutionName))
            {
                foreach (var test in context.TestQueue.ToList())
                {
                    context.TestQueue.Remove(test);
                }

                context.SaveChanges();
            }
        }

        public void RemoveFromQueue(QueuedTest testQueueItem)
        {
            var elapsedTime = DateTime.Now - testQueueItem.TestStartTime;
            Log.DebugFormat("NUnit Completed for:  {0} Elapsed Time {1} min {2} sec", testQueueItem.ProjectName, elapsedTime.Minutes, elapsedTime.Seconds);

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
                                var testQueue = new TestQueue
                                {
                                    ProjectName = testQueueItem.ProjectName,
                                    Priority = 1000,
                                    IndividualTest = test,
                                    QueuedDateTime = DateTime.Now
                                };

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
            Log.DebugFormat("Entering SaveCoverageSessionResults for ModuleName {0} ", projectInfo.ProjectName);

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

            coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName)).Classes.RemoveAll(x => x.FullName.Contains("<>"));
            coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName)).Classes.RemoveAll(x => x.FullName.Contains("__"));
            coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName)).Classes.ForEach(c => c.Methods.RemoveAll(x => x.Name.Contains("__")));

            coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName)).Classes.RemoveAll(x => x.FullName.Contains("<>"));
            coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName)).Classes.RemoveAll(x => x.FullName.Contains("__"));
            coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName)).Classes.ForEach(c => c.Methods.RemoveAll(x => x.Name.Contains("__")));
            coverageSession.Modules.ForEach(m => m.Classes.ForEach(c => c.Methods.ForEach(me => me.Name = me.Name.Replace("::", "."))));
            coverageSession.Modules.ForEach(m => m.TrackedMethods.ForEach(tm => tm.Name = tm.Name.Replace("::", ".")));
            coverageSession.Modules.ForEach(module => module.Classes.ForEach(clas => clas.Methods.ForEach(method => method.Name = CoverageService.Instance.RemoveNamespaces(method.Name))));

            foreach (var module in coverageSession.Modules)
                foreach (var c in module.TrackedMethods)
                {
                    c.Name = c.Name.Replace("System.String", "String").Replace("System.Int32", "Int32").Replace("System.Boolean", "Boolean").Replace("System.Void", "Void");
                }

            var sessionModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName));
            sessionModule.AssemblyName = projectInfo.ProjectAssemblyName;

            var testModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName));
            testModule.AssemblyName = projectInfo.TestProject.AssemblyName;

            var methodMapper = CreateMethodMap(coverageSession);

            CodeModule codeModule;
            try
            {
                //if (individualTests == null || !individualTests.Any())
                //{
                using (var context = new TestifyContext(_solutionName))
                {
                    UpdateModule(sessionModule, context);
                    codeModule = UpdateModule(testModule, context);
                    UpdateClassesMethodsSummaries(context, testModule, methodMapper);
                    UpdateClassesMethodsSummaries(context, sessionModule, methodMapper);

                    changedClasses.AddRange(coverageService.UpdateMethodsAndClassesFromCodeFile(coverageSession.Modules, methodMapper));

                    var testMethodDictionary = SaveUnitTestResults(testOutput, testModule, methodMapper, changedClasses, context);//81.2%

                    newCoveredLineInfos = coverageService.GetCoveredLinesFromCoverageSession(coverageSession, projectInfo, methodMapper, context, testMethodDictionary);

                    var newCoveredLineList = new List<CoveredLinePocoDto>();

                    try
                    {
                        var existingCoveredLines = GetCoveredLinesForModule(sessionModule.ModuleName, context);

                        var module = context.CodeModule.FirstOrDefault(x => x.Name.Equals(sessionModule.ModuleName));

                        var modifiedLines = await AddOrUpdateCoveredLine(changedClasses, newCoveredLineInfos, context, existingCoveredLines, module, methodMapper);//3.28%

                        var coveredLinesLookup = context.CoveredLines.Where(x => x.Module.AssemblyName.Equals(module.AssemblyName)).ToLookup(item => item.Method.Name);

                        var coveredLinePocos = new List<CoveredLine>();
                        foreach (var item in modifiedLines)
                        {
                            if (item == null)
                            {
                                continue;
                            }
                            try
                            {
                                var coveredLines = coveredLinesLookup[item.MethodName];

                                if (item.Method != null && !coveredLines.Any(x => x.LineNumber == item.LineNumber && x.Method.Name == item.Method.Name))
                                {
                                    coveredLinePocos.Add(ConstructCoveredLinePoco(item, context, testMethodDictionary));

                                    changedClasses.Add(item.ClassName);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.ErrorFormat("Error in SaveCoverageSessionResults MethodName: {0} Exception: {1} ", item.MethodName, ex);
                            }
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
                        var hasChanges = context.ChangeTracker.HasChanges();
                        if (hasChanges)
                        {
                            Log.DebugFormat("SaveCoverageSessionResults - Changes were made = {0}", context.ChangeTracker.HasChanges());
                        }

                        context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("Error in UpdateTrackedMethods Message: {0}", ex.InnerException);
                    }
                }
                //}
                //else
                //{
                //    // Tests have been run on the whole module, so any line not in CoverageSession is not "Covered"
                //    foreach (var module in coverageSession.Modules)
                //    {
                //        UpdateClassesMethodsSummaries(testModule,codeModule);
                //    }

                //    // Only a single unit test was run, so only the lines in the CoverageSession will be updated
                //    // Get the MetadataTokens for the unitTests we just ran

                //    var individualTestUniqueIds = new List<int>();

                //    foreach (var test in individualTests)
                //    {
                //        var testMethodName = ConvertUnitTestFormatToFormatTrackedMethod(test);
                //        individualTestUniqueIds.Add((int)testModule.TrackedMethods
                //                                                 .Where(x => x.Name.Contains(testMethodName))
                //                                                 .FirstOrDefault().UniqueId);
                //    }

                //    using (var context = new TestifyContext(_solutionName))
                //    {
                //        var unitTests = context.UnitTests.Where(x => individualTests.Contains(x.TestMethodName));

                //        List<int> unitTestIds = unitTests.Select(x => x.UnitTestId).ToList();

                //        if (unitTestIds != null)
                //        {
                //            newCoveredLineInfos = coverageService.GetRetestedLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName, individualTestUniqueIds);
                //            newCoveredLineInfos.AddRange(coverageService.GetRetestedLinesFromCoverageSession(coverageSession, projectInfo.TestProject.AssemblyName, individualTestUniqueIds));
                //            changedClasses = newCoveredLineInfos.Select(x => x.Class.Name).Distinct().ToList();
                //        }

                //        var module = context.CodeModule.FirstOrDefault(x => x.Name.Equals(sessionModule.ModuleName));
                //        var existingCoveredLines = GetCoveredLinesForModule(sessionModule.ModuleName, context);
                //        var modifiedLines = await AddOrUpdateCoveredLine(changedClasses, newCoveredLineInfos, context, existingCoveredLines, module, methodMapper);

                //        foreach (var item in modifiedLines)
                //        {
                //            context.CoveredLines.Add(ConstructCoveredLinePoco(item));
                //        }
                //        context.SaveChanges();
                //    }

                //}
                //try
                //{
                //    RefreshUnitTestIds(newCoveredLineInfos, sessionModule, testModule, methodMapper);
                //}
                //catch(Exception ex)
                //{
                //    Log.ErrorFormat("Error in RefreshUnitTestIds: {0} ", ex);
                //}

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

        public Dictionary<string, TestMethod> SaveUnitTestResults(resultType testOutput,
                                                                   Leem.Testify.Model.Module testModule,
                                                                   List<TrackedMethodMap> trackedMethodUnitTestMapper,
                                                                   List<string> changedUnitTestClasses,
                                                                   TestifyContext context)
        {
            Log.DebugFormat("Inside SaveUnitTestResults ");
            var trackedMethodDictionary = new Dictionary<string, TestMethod>();

            bool hasChanges = false;
            var testMethods = new List<TestMethod>();
            string runDate = testOutput.date;
            string runTime = testOutput.time;
            string fileName = testOutput.name;

            try
            {
                var extractedMethods = testModule.Classes.SelectMany(c => c.Methods);
                var trackedMethods = testModule.TrackedMethods.Select(t => t);
                foreach (var method in trackedMethods)
                {
                    method.Name = CoverageService.Instance.RemoveNamespaces(method.Name).Replace("::", ".");
                }

                var filePathDictionary = (from m in extractedMethods
                                          join t in testModule.TrackedMethods on m.MetadataToken equals t.MetadataToken
                                          join f in testModule.Files on m.FileRef.UniqueId equals f.UniqueId
                                          select new { t.Name, f.FullPath })
                                        .ToDictionary(mc => CoverageService.Instance.RemoveNamespaces(mc.Name)
                                                                                    .Substring(mc.Name.IndexOf(" ") + 1)
                                            //.Replace("::", ".")
                                                                                    .Replace("()", ""),
                                                      mc => mc.FullPath);

                var unitTests = GetUnitTests(testOutput.testsuite);

                var codeModule = context.CodeModule.FirstOrDefault(x => x.Name.Equals(testModule.ModuleName));

                TestMethod dummy;

                foreach (var testMethod in context.TestMethods.Where(x => x.AssemblyName == testModule.AssemblyName))
                {
                    var isInDictionary = trackedMethodDictionary.TryGetValue(testMethod.Name, out dummy);
                    if (!isInDictionary)
                    {
                        trackedMethodDictionary.Add(testMethod.Name, testMethod);
                    }
                }

                foreach (var test in unitTests)
                {
                    try
                    {
                        Poco.TestMethod existingTestMethodFromDictionary;
                        Poco.TestMethod trackedMethodJustAdded = null;

                        var trackedMethodUnitTestMap = trackedMethodUnitTestMapper.FirstOrDefault(y => y.MethodInfos.Any(z => z.MethodName.Contains(test.TestMethodName)));

                        var isTestMethodInDictionary = trackedMethodDictionary.TryGetValue(trackedMethodUnitTestMap.MethodName, out existingTestMethodFromDictionary);
                        if (filePathDictionary.ContainsKey(test.TestMethodName))
                        {
                            test.FilePath = filePathDictionary[test.TestMethodName];
                            test.LineNumber = trackedMethodUnitTestMap.MethodInfos.First().BeginLine;
                        }

                        if (isTestMethodInDictionary == false)
                        {
                            TestMethod existingTestMethod = null;
                            trackedMethodDictionary.TryGetValue(trackedMethodUnitTestMap.MethodName, out existingTestMethod);// context.TrackedMethods.FirstOrDefault(x => x.Name.Equals(trackedMethodUnitTestMap.TrackedMethodName));

                            if (existingTestMethod == null)
                            {
                                var trackedMethodToAdd = trackedMethods.FirstOrDefault(x => x.Name == trackedMethodUnitTestMap.MethodName);
                                if (trackedMethodToAdd != null)
                                {
                                    test.UniqueId = (int)trackedMethodToAdd.UniqueId;

                                    test.Strategy = trackedMethodToAdd.Strategy;
                                    test.Name = trackedMethodToAdd.Name;
                                    test.MetadataToken = trackedMethodToAdd.MetadataToken;
                                    test.CodeModule = codeModule;
                                    test.AssemblyName = testModule.AssemblyName;

                                    context.TestMethods.Add(test);
                                    trackedMethodDictionary.Add(test.Name, test);
                                    //hasChanges = context.ChangeTracker.HasChanges();
                                    //if (hasChanges)
                                    //{
                                    //    Log.DebugFormat("SaveUnitTestResults - Changes were made = {0},  Added test, name:{1}", hasChanges, test.Name);
                                    //}

                                    context.SaveChanges();

                                    trackedMethodJustAdded = context.TestMethods.Local.FirstOrDefault(x => x.Name != null && x.Name.Equals(trackedMethodToAdd.Name));
                                }
                                else
                                {
                                    Log.ErrorFormat("TrackedMethod not fount to match: {0}", trackedMethodUnitTestMap.MethodName);
                                    int x = 1;
                                }
                            }

                            if (trackedMethodJustAdded != null)
                            {
                                context.SaveChanges();
                            }
                        }

                        if (existingTestMethodFromDictionary != null)
                        {
                            var existingTest = context.TestMethods.FirstOrDefault(y => y.TestMethodName.Equals(test.TestMethodName));
                            test.LastRunDatetime = runDate + " " + runTime;
                            var className = test.TestMethodName.Substring(0, test.TestMethodName.LastIndexOf("."));

                            if (existingTest == null)
                            {
                                var testName = ConvertUnitTestFormatToFormatTrackedMethod(test.TestMethodName);
                                string filePath;
                                filePathDictionary.TryGetValue(testName, out filePath);

                                test.FilePath = filePath;
                                context.TestMethods.Add(test);
                                Log.DebugFormat("Added TestMethod {0}", test.Name);
                                changedUnitTestClasses.Add(className);
                                context.SaveChanges();
                            }
                            else
                            {
                                if (existingTest.FilePath != test.FilePath ||
                                    existingTest.LineNumber != test.LineNumber)
                                {
                                    existingTest.TestDuration = test.TestDuration;
                                    existingTest.FilePath = test.FilePath;
                                    existingTest.LineNumber = test.LineNumber;
                                }

                                if (existingTest.IsSuccessful != test.IsSuccessful || existingTest.Result != test.Result)
                                {
                                    Log.DebugFormat("SaveUnitTestResults - UnitTest result changed from {0} to {1 } for {2}", existingTest.Result, test.Result, existingTest.Name);

                                    existingTest.IsSuccessful = test.IsSuccessful;
                                    existingTest.Result = test.Result;
                                    changedUnitTestClasses.Add(className);
                                    existingTest.TestDuration = test.TestDuration;
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

                DeleteUnitTestsThatAreNoLongerInTrackedMethods(changedUnitTestClasses, trackedMethods, unitTests, context, trackedMethodDictionary);

                hasChanges = context.ChangeTracker.HasChanges();
                Log.DebugFormat("SaveUnitTestResults - Changes were made = {0}", hasChanges);

                context.SaveChanges();
                //}
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error building FilePathDictionary Error: {0}", ex);
            }

            Log.DebugFormat("Leaving SaveUnitTestResults trackedMethodDictionary count : {0}", trackedMethodDictionary.Count);
            return trackedMethodDictionary;
        }

        private static void DeleteUnitTestsThatAreNoLongerInTrackedMethods(List<string> changedUnitTestClasses, IEnumerable<TrackedMethod> trackedMethods, List<TestMethod> unitTests, TestifyContext context, Dictionary<string, TestMethod> trackedMethodLookup)
        {
            var toDelete = new List<TestMethod>();
            foreach (var test in trackedMethodLookup)
            {
                if (unitTests.Any(x => x.Name == test.Key) == false)
                {
                    var trackedMethod = trackedMethods.ToList().FirstOrDefault(x => x.Name == test.Key);
                    if (trackedMethod == null)
                    {
                        toDelete.Add(test.Value);
                        if (test.Value.CoveredLines.Any(x => x.Class != null))
                        {
                            var className = test.Value.CoveredLines.FirstOrDefault(x => x.Class != null).Class.Name;
                            changedUnitTestClasses.Add(className);
                        }
                    }
                }
            }
            foreach (var testToDelete in toDelete)
            {
                context.TestMethods.Remove(testToDelete);
                foreach (var lineCoveredByTest in testToDelete.CoveredLines)
                {
                    lineCoveredByTest.TestMethods.Remove(testToDelete);
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

        public void UpdateCodeClassPath(string className, string path, int line, int column)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var matchingClass = context.CodeClass.FirstOrDefault(x => x.Name.Equals(className));
                if (matchingClass != null)
                {
                    matchingClass.FileName = path;
                    matchingClass.Line = line;
                    matchingClass.Column = column;
                }
                context.SaveChanges();
            }
        }

        public void UpdateCodeMethodPath(TestifyContext context, CodeMethodInfo methodInfo, Dictionary<string, CodeMethod> codeMethodDictionary)
        {
            if (methodInfo != null)
            {
                CodeMethod matchingMethod;
                codeMethodDictionary.TryGetValue(methodInfo.RawMethodName, out matchingMethod);

                if (matchingMethod != null
                    && (matchingMethod.FileName != methodInfo.FileName
                    || matchingMethod.Line != methodInfo.Line
                    || matchingMethod.Column != methodInfo.Column))
                {
                    matchingMethod.FileName = methodInfo.FileName;
                    matchingMethod.Line = methodInfo.Line;
                    matchingMethod.Column = methodInfo.Column;
                }
                else if (matchingMethod == null)
                {
                    int x = 1;
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

        private static CoveredLine ConstructCoveredLinePoco(LineCoverageInfo line, TestifyContext context, Dictionary<string, TestMethod> testMethodDictionary)
        {
            if (line.Class == null)
            {
                Log.ErrorFormat("Line.Class is null for {0}", line.MethodName);
            }
            var coveredLine = new CoveredLine
            {
                Class = line.Class,
                FileName = line.FileName,
                IsBranch = line.IsBranch,
                IsCode = line.IsCode,
                IsCovered = line.IsCovered,

                LineNumber = line.LineNumber,
                Method = line.Method,
                Module = line.Module
            };
            foreach (var test in line.TestMethods)
            {
                TestMethod testMethodFromContext;
                testMethodDictionary.TryGetValue(test.Name, out testMethodFromContext);
                if (testMethodFromContext == null)
                {
                    Log.ErrorFormat("Test Method not found in TestMethodFromContext", test.TestMethodName);
                }
                else
                {
                    if (!coveredLine.TestMethods.Contains(testMethodFromContext))
                    {
                        coveredLine.TestMethods.Add(testMethodFromContext);
                    }
                }
            }
            coveredLine.IsSuccessful = coveredLine.TestMethods.All(y => y.IsSuccessful);
            return coveredLine;
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
                IsBranch = line.IsBranch
            };

            return newCoverage;
        }

        private static ILookup<int, CoveredLine> GetCoveredLinesForModule(string moduleName, TestifyContext context)
        {
            var existingCoveredLines = (from line in context.CoveredLines
                                        where line.Module.Name.Equals(moduleName)
                                        select line).ToLookup(x => x.LineNumber);

            return existingCoveredLines;
        }

        private static List<TestQueue> MarkTestAsInProgress(int testRunId, TestifyContext context, List<TestQueue> testsToRun)
        {
            List<TestQueue> testsToMarkInProgress = new List<TestQueue>();
            foreach (var test in testsToRun)
            {
                var projectInfo = Instance.GetProjectInfo(testsToRun.FirstOrDefault().ProjectName);
                if (projectInfo == null)
                {
                    projectInfo = Instance.GetProjectInfoFromTestProject(testsToRun.FirstOrDefault().ProjectName);
                }
                if (string.IsNullOrEmpty(test.IndividualTest))
                {
                    // if we are running all the tests for the project, we can remove all the individual and Project tests
                    testsToMarkInProgress.AddRange(context.TestQueue.Where(x => x.ProjectName.Equals(projectInfo.UniqueName)).ToList());

                    testsToMarkInProgress.AddRange(context.TestQueue.Where(x => x.ProjectName.Equals(projectInfo.TestProject.UniqueName)).ToList());
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

            return testsToMarkInProgress;
        }

        private static List<string> RemoveDeletedUnitTests(IList<TrackedMethodMap> trackedMethodUnitTestMapper, TestifyContext context)
        {
            var changedUnitTestClasses = new List<string>();
            var methodsInCoverageResult = trackedMethodUnitTestMapper.SelectMany(m => m.MethodInfos).ToList();

            var unitTestsToBeDeleted = new List<TestMethod>();
            foreach (var test in unitTestsToBeDeleted)
            {
                var linesInUnitTestToBeDeleted = context.CoveredLines.SelectMany(x => x.TestMethods)
                                            .Where(x => x.TestMethodName == test.TestMethodName && x.FilePath == test.FilePath);

                var coveredLines = linesInUnitTestToBeDeleted.SelectMany(x => x.CoveredLines).Include(y => y.Class).Distinct().ToList();

                foreach (var line in coveredLines)
                {
                    changedUnitTestClasses.Add(line.Class.Name);
                    changedUnitTestClasses.Add(line.Class.Name);
                    context.CoveredLines.Remove(line);
                }
                context.TestMethods.Remove(test);
                var className = test.TestMethodName.Substring(0, test.TestMethodName.LastIndexOf("."));
                changedUnitTestClasses.Add(className);
            }
            return changedUnitTestClasses.Distinct().ToList();
        }

        private static List<TestMethod> SelectUnitTestByName(string name, TestifyContext context)
        {
            var query = (from test in context.TestMethods
                         where test.TestMethodName.Equals(name)
                         select test);

            return query.ToList();
        }

        private static void UpdateSummary(Model.Summary newSummary, Poco.Summary existing)
        {
            if (existing.BranchCoverage != newSummary.BranchCoverage ||
                existing.MaxCyclomaticComplexity != newSummary.MaxCyclomaticComplexity ||
                existing.MinCyclomaticComplexity != newSummary.MinCyclomaticComplexity ||
                existing.NumBranchPoints != newSummary.NumBranchPoints ||
                existing.NumSequencePoints != newSummary.NumSequencePoints ||
                existing.SequenceCoverage != newSummary.SequenceCoverage ||
                existing.VisitedBranchPoints != newSummary.VisitedBranchPoints ||
                existing.VisitedSequencePoints != newSummary.VisitedSequencePoints)
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
        }

        private async CSharp.Task<List<LineCoverageInfo>> AddOrUpdateCoveredLine(List<string> changedClasses, IList<LineCoverageInfo> newCoveredLineList, TestifyContext context, ILookup<int, CoveredLine> existingCoveredLines, CodeModule module, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            Log.DebugFormat("Entering AddOrUpdateCoveredLine ");

            var modifiedLineCoverageInfos = new List<LineCoverageInfo>();
            var modulesTested = newCoveredLineList.Select(x => x.ModuleName).Distinct();

            ILookup<string, CodeClass> classLookup = context.CodeClass.Where(x => modulesTested.Contains(x.CodeModule.Name)).ToLookup(c => c.Name, c => c);

            var classesTested = classLookup.Select(x => x.Key).ToList();
            ILookup<string, CodeMethod> methodLookup = context.CodeMethod.Where(x => classesTested.Contains(x.CodeClass.Name)).ToLookup(m => m.Name, m => m);
            ILookup<string, TestMethod> unitTestLookup = context.TestMethods.ToLookup(c => c.TestMethodName, c => c);

            var numberOfBatches = (newCoveredLineList.Count / 2000) + 1;
            var batches = LineCoverageInfoExtensions.Split<LineCoverageInfo>(newCoveredLineList, numberOfBatches).ToList();

            var batchNumber = 1;
            foreach (var batch in batches)
            {
                Log.DebugFormat("Processing batch {0}  of {1}", batchNumber, numberOfBatches);
                batchNumber++;
                foreach (var line in batch)
                {
                    try
                    {
                        await GetModuleClassMethodForLine(context, line, methodLookup, classLookup);
                        line.Module = module;

                        CoveredLine existingLine = null;

                        existingLine = await GetExistingCoveredLineByMethodAndLineNumber(existingCoveredLines, line);

                        if (existingLine != null)
                        {
                            var changedClass = await ProcessExistingLine(unitTestLookup, line, existingLine, trackedMethodUnitTestMapper);
                            if (changedClass != string.Empty)
                                changedClasses.Add(changedClass);
                        }
                        else
                        {
                            modifiedLineCoverageInfos.Add(line);
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("Error Saving Changes for Batch: error: {0}", ex);
                    }
                }
                context.SaveChanges();

                Log.DebugFormat("Changes Saved for Batch:");

                context.SaveChanges();
            }

            changedClasses.AddRange(RemoveLinesThatNoLongerExist(newCoveredLineList, context, methodLookup, modulesTested));

            Log.DebugFormat("Leaving AddOrUpdateCoveredLine:");
            return modifiedLineCoverageInfos;
        }

        private List<string> RemoveLinesThatNoLongerExist(IList<LineCoverageInfo> newCoveredLineList, TestifyContext context, ILookup<string, CodeMethod> methodLookup, IEnumerable<string> modulesTested)
        {
            var classesChanged = new List<string>();
            try
            {
                var methodsTested = newCoveredLineList.Select(x => x.MethodName).Distinct();
                List<CoveredLine> linesToBeDeleted = new List<CoveredLine>();

                var coveredLinesLookup = context.CoveredLines.Where(y =>  modulesTested.Contains(y.Module.AssemblyName)).Where(x => methodsTested.Contains(x.Method.Name)).ToLookup(item => item.Method.Name);

                foreach (var method in methodsTested)
                {
                    var currentLineNumbers = newCoveredLineList.Where(m => m.MethodName == method).Select(x => x.LineNumber).Distinct();
                    var coveredLines = coveredLinesLookup[method];
                    var linesFromThisMethodToBeDeleted = coveredLines.Where(x => x.Method.Name == method && !currentLineNumbers.Contains(x.LineNumber));
                    linesToBeDeleted.AddRange(linesFromThisMethodToBeDeleted);
                }
                var testMethodLinesToBeDeleted = context.CoveredLines.Where(x => x.Module.AssemblyName.Contains(".Test")).ToList();
                foreach (var line in linesToBeDeleted)
                {
                    Log.DebugFormat("Deleting covered line for Method: {0}, Line number: {1} ", line.Method.Name, line.LineNumber);
                    context.CoveredLines.Remove(line);
                }
                var methodsToDelete = new List<CodeMethod>();
                foreach (var method in context.CodeMethod.Where(x => modulesTested.Contains(x.CodeClass.CodeModule.Name)))
                {
                    if (!methodsTested.Contains(method.Name))
                    {
                        Log.DebugFormat("Deleting Method: {0}", method.Name);
                        methodsToDelete.Add(method);
                        classesChanged.Add(method.CodeClass.Name);
                    }
                }
                foreach (var method in methodsToDelete)
                {
                    context.CodeMethod.Remove(method);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in RemoveLinesThatNoLongerExist {0}", ex);
            }

            context.SaveChanges();
            return classesChanged;
        }

        private List<TrackedMethodMap> CheckClassForTestCaseAttribute(SyntaxTree syntaxTree, TypeDeclaration typeDeclarationNode)
        {
            var unitTestCasesList = new List<TrackedMethodMap>();
            foreach (var element in typeDeclarationNode.Children)
            {
                if (element.GetType() == typeof(ICSharpCode.NRefactory.CSharp.ConstructorDeclaration) && element.HasChildren || (element.GetType() == typeof(MethodDeclaration) && element.HasChildren))
                {
                    var xxx = ((EntityDeclaration)(element)).Name;
                    var unitTestCasesFromMethod = CheckMethodForTestCaseAttribute(syntaxTree, element);
                    if (unitTestCasesFromMethod != null)
                    {
                        unitTestCasesList.Add(unitTestCasesFromMethod);
                    }
                }
            }
            return unitTestCasesList;
        }

        private TrackedMethodMap CheckMethodForTestCaseAttribute(SyntaxTree syntaxTree, AstNode methodDeclarationNode)
        {
            var arguments = string.Empty;
            var parameters = string.Empty;

            IProjectContent project = new CSharpProjectContent();
            var unresolvedFile = syntaxTree.ToTypeSystem();
            project = project.AddOrUpdateFiles(unresolvedFile);

            ICompilation compilation = project.CreateCompilation();

            var resolver = new ICSharpCode.NRefactory.CSharp.Resolver.CSharpAstResolver(compilation, syntaxTree, unresolvedFile);

            var result = resolver.Resolve(methodDeclarationNode);
            var member = (ICSharpCode.NRefactory.Semantics.MemberResolveResult)result;
            var memberDefinition = member.Member;

            var unresolvedParameters = ((ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedMethod)(memberDefinition.UnresolvedMember)).Parameters;

            parameters = GetParameterList(unresolvedParameters);

            string returnType = memberDefinition.UnresolvedMember.ReturnType.ToString();
            returnType = CoverageService.Instance.ConvertNameToCSharpName(returnType);

            var trackedMethodName = returnType + " " + memberDefinition.ReflectionName.Trim() + parameters;
            var unitTestCases = new TrackedMethodMap
            {
                MethodName = CoverageService.Instance.RemoveNamespaces(trackedMethodName),

                MethodInfos = new List<MethodInfo>()
            };

            var methodDefinition = project.TopLevelTypeDefinitions.Where(x => x.Methods.Any(m => m.ReflectionName == memberDefinition.ReflectionName));
            MethodInfo methodInfo;

            var children = methodDeclarationNode.Children.ToList();
            foreach (var element in methodDeclarationNode.Children)
            {
                arguments = string.Empty;

                if (element.GetType() == typeof(AttributeSection) && element.HasChildren)
                {
                    methodInfo =
                           new MethodInfo
                           {
                               MethodName = memberDefinition.ReflectionName,
                               ReflectionName = memberDefinition.ReflectionName,
                               BeginLine = children.Where(x => x.GetType() == typeof(Identifier)).First().StartLocation.Line,
                               BeginColumn = element.StartLocation.Column,
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
            if (unitTestCases.MethodInfos.Count == 0)
            {
                unitTestCases.MethodInfos.Add(
                           new MethodInfo
                           {
                               MethodName = memberDefinition.ReflectionName,
                               ReflectionName = memberDefinition.ReflectionName,
                               BeginLine = children.Where(x => x.GetType() == typeof(Identifier)).First().StartLocation.Line,
                               BeginColumn = children.Where(x => x.GetType() == typeof(Identifier)).First().StartLocation.Column,
                               FileName = unresolvedFile.FileName
                           });
            }

            return unitTestCases;
        }

        public string GetParameterList(IList<IUnresolvedParameter> unresolvedParameters)
        {
            string parameters = "(";

            foreach (var parameter in unresolvedParameters)
            {
                try
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
                        var identifier = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(parameter.Type)).Identifier;
                        if ((identifier == "List" || identifier == "IDictionary")
                            && typeArguments != null
                            && typeArguments.GetType() == typeof(KnownTypeReference))
                        {
                            var abbreviatedName = typeArguments.ToString();
                            var fullName = ((KnownTypeReference)(typeArguments)).Name;
                            extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(parameter.Type)).ToString().Replace(abbreviatedName, fullName);
                        }
                        else
                        {
                            extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)(parameter.Type)).ToString();
                        }
                    }
                    else if (parameter.Type.GetType() == typeof(KnownTypeCode))
                    {
                        extractedParameter = parameter.Name;
                    }
                    else if (parameter.Type.GetType() == typeof(KnownTypeReference))
                    {
                        extractedParameter = ((KnownTypeReference)(parameter.Type)).Name;
                    }
                    else if (parameter.Type.GetType() == typeof(ParameterizedTypeReference)
                        || parameter.Type.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeSystem.MemberTypeOrNamespaceReference))
                    {
                        var parameterType = parameter.Type.ToString();
                        parameterType = parameterType.Replace("System.Nullable[[int]]", "Nullable<Int32>");
                        extractedParameter = parameterType;
                    }
                    else if (parameter.Type.GetType() == typeof(ArrayTypeReference))
                    {
                        if (((ArrayTypeReference)parameter.Type).ElementType.GetType() == typeof(KnownTypeReference))
                        {
                            extractedParameter = ((KnownTypeReference)(((ArrayTypeReference)(parameter.Type)).ElementType)).Name + "[]";
                        }
                        else if (((ArrayTypeReference)parameter.Type).ElementType.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference))
                        {
                            extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)((ArrayTypeReference)parameter.Type).ElementType).Identifier + "[]";
                        }
                    }
                    else if (parameter.Type.GetType() == typeof(ByReferenceTypeReference))
                    {
                        var elementType = (((ByReferenceTypeReference)(parameter.Type)).ElementType);
                        if (elementType.GetType() == typeof(KnownTypeReference))
                        {
                            extractedParameter = ((KnownTypeReference)(((ByReferenceTypeReference)(parameter.Type)).ElementType)).Name;
                        }
                        else if (elementType.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference))
                        {
                            extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)((ByReferenceTypeReference)parameter.Type).ElementType).Identifier;
                        }
                        else if (elementType.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeSystem.MemberTypeOrNamespaceReference))
                        {
                            extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.MemberTypeOrNamespaceReference)elementType).Identifier;
                            if (((ICSharpCode.NRefactory.CSharp.TypeSystem.MemberTypeOrNamespaceReference)elementType).TypeArguments.Any())
                            {
                                extractedParameter = extractedParameter + "<" + ((ICSharpCode.NRefactory.CSharp.TypeSystem.MemberTypeOrNamespaceReference)(((ICSharpCode.NRefactory.CSharp.TypeSystem.MemberTypeOrNamespaceReference)elementType).TypeArguments.FirstOrDefault())).Identifier + ">";
                            }
                        }
                        else if (elementType.GetType() == typeof(ArrayTypeReference))
                        {
                            if (((ICSharpCode.NRefactory.TypeSystem.ByReferenceTypeReference)parameter.Type).ElementType.GetType() == typeof(KnownTypeReference))
                            {
                                extractedParameter = ((KnownTypeReference)((ArrayTypeReference)elementType).ElementType).Name + "[]";
                            }
                            else if (((ICSharpCode.NRefactory.TypeSystem.ByReferenceTypeReference)parameter.Type).ElementType.GetType() == typeof(ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference))
                            {
                                extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)((ArrayTypeReference)elementType).ElementType).Identifier + "[]";
                            }
                            else if (((ByReferenceTypeReference)parameter.Type).ElementType.GetType() == typeof(ArrayTypeReference))
                            {
                                if (((((ArrayTypeReference)elementType).ElementType)).GetType() == typeof(KnownTypeReference))
                                {
                                    extractedParameter = ((KnownTypeReference)((ArrayTypeReference)elementType).ElementType).Name + "[]";
                                }
                                else
                                {
                                    extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)((ArrayTypeReference)elementType).ElementType).Identifier + "[]";
                                }
                            }
                        }
                        else
                        {
                            extractedParameter = ((ICSharpCode.NRefactory.CSharp.TypeSystem.SimpleTypeOrNamespaceReference)((ArrayTypeReference)((ByReferenceTypeReference)parameter.Type).ElementType).ElementType).Identifier;
                        }
                    }
                    else
                    {
                        Log.ErrorFormat("Error Can't handle parameter of type: {0} ", parameter.Type.GetType().ToString());
                    }

                    extractedParameter = CoverageService.Instance.ConvertNameToCSharpName(extractedParameter);
                    parameters = parameters + extractedParameter;
                }
                catch
                {
                    Log.ErrorFormat("Could not extract parameter type from {0}", parameter.Type);
                }
            }
            return parameters + ")"; ;
        }

        private List<TrackedMethodMap> CheckNamespaceForTestCaseMethods(SyntaxTree syntaxTree, NamespaceDeclaration namespaceDeclarationNode)
        {
            var unitTestCasesList = new List<TrackedMethodMap>();
            foreach (var element in namespaceDeclarationNode.Children)
            {
              
                if (element.GetType() == typeof(TypeDeclaration) && element.HasChildren)
                {
                    var xx = ((EntityDeclaration)(element)).Name;
                    var unitTestCasesFromClass = CheckClassForTestCaseAttribute(syntaxTree, (TypeDeclaration)element);
                    if (unitTestCasesFromClass.Any())
                    {
                        unitTestCasesList.AddRange(unitTestCasesFromClass);
                    }
                }
            }
            return unitTestCasesList;
        }

        private TestMethod ConstructUnitTest(testcaseType testcase)
        {
            var unitTest = new Poco.TestMethod
            {
                TestDuration = testcase.time,
                TestMethodName = testcase.name,
                Executed = testcase.executed.Equals(bool.TrueString),
                Result = testcase.result,
                NumberOfAsserts = Convert.ToInt32(testcase.asserts),
                IsSuccessful = testcase.success == bool.TrueString,
                LastRunDatetime = DateTime.Now.ToString()
            };
            if (testcase.result == "Failure")
            {
                var testCaseItem = (Leem.Testify.failureType)testcase.Item;
                var message = testCaseItem.message;
                int lineNumberFromStackTrace;
                const string lineMarker = ":line ";
                var lineNumberLocation = testCaseItem.stacktrace.IndexOf(lineMarker);
                if (lineNumberLocation > 0)
                {
                    var linenumberstring = testCaseItem.stacktrace.Substring(lineNumberLocation + lineMarker.Length).Replace("\n", string.Empty);
                    var lineNumberString = Int32.TryParse(linenumberstring, out lineNumberFromStackTrace);
                    unitTest.FailureLineNumber = lineNumberFromStackTrace;
                }

                unitTest.FailureMessage = message;
            }
            if (testcase.result == "Exception")
            {
                var ddd = 4;
            }

            if (testcase.success != null && testcase.success.Equals(Boolean.TrueString))
            {
                unitTest.LastSuccessfulRunDatetime = DateTime.Now;
            }

            return unitTest;
        }

        private List<TrackedMethodMap> CreateMethodMap(CoverageSession coverageSession)
        {
            var methodMapper = new List<TrackedMethodMap>();
            var listOfAutoGeneratedClasses = new List<string>();
            foreach (var module in coverageSession.Modules)
            {
                foreach (var clas in module.Classes)
                {
                    try
                    {
                        IProjectContent project = new CSharpProjectContent();
                        var hasCoveredMethods = clas.Methods.Any(x => x.SkippedDueTo == 0);
                        var firstMethod = clas.Methods.FirstOrDefault(x => x.FileRef != null);
                        bool isAutoGenerated = false;
                        bool hasNoComplexity = false;
                        string filePath = string.Empty;
                        uint uniqueId = 0;
                        if (firstMethod != null)
                        {
                            uniqueId = firstMethod.FileRef.UniqueId;
                            filePath = module.Files.FirstOrDefault(x => x.UniqueId == uniqueId).FullPath;
                            isAutoGenerated = System.IO.File.ReadLines(filePath).Skip(1).Take(1).Contains("// <auto-generated>");
                        }
                        else if (clas.Methods.Max(x => x.CyclomaticComplexity == 0))
                        {
                            // This removes Event Handlers from Web Services
                            hasNoComplexity = true;
                        }

                        if (isAutoGenerated || hasNoComplexity)
                        {
                            listOfAutoGeneratedClasses.Add(clas.FullName);
                            clas.Methods.RemoveAll(x => x.FileRef == null || x.FileRef.UniqueId == uniqueId);
                        }
                        if (!isAutoGenerated && clas.Methods.Any(y => y.FileRef != null))
                        {
                            if (filePath.Contains(@"\Web References\") == false
                                && filePath.Contains(@"\Service References\") == false)
                            {
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

                module.Classes.RemoveAll(x => listOfAutoGeneratedClasses.Contains(x.FullName));
            }
            return methodMapper;
        }

        private string GetArgumentsFromTestCaseAttribute(AttributeSection attributeSection)
        {
            var arguments = new System.Text.StringBuilder();
            foreach (var attribute in attributeSection.Attributes)
            {
                if (attribute.Type.ToString() == "TestCase")
                {
                    var attributeArguments = string.Join(",", attribute.Arguments);
                    var indexOfResultKeyword = attributeArguments.IndexOf(",Result");
                    if (indexOfResultKeyword > 0)
                    {
                        attributeArguments = attributeArguments.Remove(indexOfResultKeyword);
                    }

                    arguments.Append("(");
                    arguments.Append(attributeArguments);
                    arguments.Append(")");

                    arguments.Replace("true", "True");
                    arguments.Replace("false", "False");
                }
            }
            var args = CoverageService.Instance.RemoveNamespaces(arguments.ToString());
            return args;
        }

        private async Task<CoveredLine> GetExistingCoveredLineByMethodAndLineNumber(ILookup<int, Poco.CoveredLine> existingCoveredLines, LineCoverageInfo line)
        {
            CoveredLine existingLine = null;
            try
            {
                if (line.Method != null)
                {
                    existingLine = existingCoveredLines[line.LineNumber].FirstOrDefault(x => x.Method != null && x.Method.Name.Equals(line.Method.Name));
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in GetCoveredLinesByClassAndLine, Method is null for Class: {0}, Method: {1}, Error: {2}", line.ClassName, line.MethodName, ex);
            }

            return existingLine;
        }

        private async CSharp.Task GetModuleClassMethodForLine(TestifyContext context, LineCoverageInfo line, ILookup<string, CodeMethod> methodLookup, ILookup<string, CodeClass> classLookup)
        {
            line.Class = classLookup[line.ClassName].FirstOrDefault();
            line.Method = methodLookup[line.MethodName].FirstOrDefault();
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

        private List<Poco.TestMethod> GetUnitTests(object element)
        {
            var unitTests = new List<TestMethod>();

            try
            {
                if (element.GetType() == typeof(testcaseType))
                {
                    var testcase = (testcaseType)element;

                    var unitTest = ConstructUnitTest(testcase);
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
                Log.ErrorFormat("Error in GetUnitTests Error: {0}", ex);
            }

            return unitTests;
        }

        private async CSharp.Task<string> ProcessExistingLine(ILookup<string, TestMethod> unitTestLookup, LineCoverageInfo line, Poco.CoveredLine existingLine, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            var classThatChanged = string.Empty;
            bool previousStatusOfExistingLine = existingLine.IsSuccessful;
            if (existingLine.IsCode != line.IsCode
               || existingLine.IsCovered != line.IsCovered
               || existingLine.IsBranch != line.IsBranch
               || existingLine.FileName != line.FileName
               || existingLine.BranchCoverage != line.BranchCoverage)
            {
                existingLine.IsCode = line.IsCode;
                existingLine.IsCovered = line.IsCovered;
                existingLine.IsBranch = line.IsBranch;
                existingLine.FileName = line.FileName;
                existingLine.BranchCoverage = line.BranchCoverage;
                classThatChanged = existingLine.Class.Name;
            }

            var lineMethodUnitTestMap = trackedMethodUnitTestMapper.FirstOrDefault(x => x.MethodName == line.MethodName);
            if (line.TestMethods.Any() && line.TestMethods.All(x => x.IsSuccessful))
            {
                existingLine.IsSuccessful = true;
            }
            else if (lineMethodUnitTestMap != null)
            {
                TestMethod matchingUnitTest = null;
                foreach (var methodInfo in lineMethodUnitTestMap.MethodInfos)
                {
                    matchingUnitTest = unitTestLookup[methodInfo.MethodName].FirstOrDefault();
                    if (matchingUnitTest != null)
                    {
                        existingLine.IsSuccessful = matchingUnitTest.IsSuccessful;
                        if (existingLine.Method.Name == matchingUnitTest.Name
                            && existingLine.LineNumber == matchingUnitTest.FailureLineNumber)
                        {
                            existingLine.FailureMessage = matchingUnitTest.FailureMessage;
                            existingLine.FailureLineNumber = matchingUnitTest.FailureLineNumber;
                        }

                        continue;
                    }
                    existingLine.IsSuccessful = false;
                }
            }
            else
            {
                existingLine.IsSuccessful = false;
            }
            if (existingLine.IsSuccessful != previousStatusOfExistingLine)
            {
                classThatChanged = existingLine.Class.Name;
            }
            return classThatChanged;
        }

        private void UpdateClassesMethodsSummaries(TestifyContext context, Model.Module module,List<TrackedMethodMap> methodMapper)
        {
            Log.DebugFormat("Inside UpdateModulesClassesMethodsSummaries for Module: {0}", module.AssemblyName);

            var classLookup = context.CodeClass.ToLookup(clas => clas.Name, clas => clas);
            var methodLookup = context.CodeMethod.ToLookup(m => m.Name.ToString(), m => m);

            UpdateCodeClasses(module, context, classLookup, methodLookup, methodMapper);//13.6%

            var hasChanges = context.ChangeTracker.HasChanges();

            if (hasChanges)
            {
                Log.DebugFormat("UpdateClassesMethodsSummaries - Changes were made = {0}", context.ChangeTracker.HasChanges());
            }

            context.SaveChanges();

            Log.DebugFormat("Finished UpdateModulesClassesMethodsSummaries for Module: {0}", module.AssemblyName);
        }

        private void UpdateCodeClasses(Model.Module module, TestifyContext context, ILookup<string, CodeClass> classLookup, ILookup<string, CodeMethod> methodLookup, List<TrackedMethodMap> methodMapper)
        {
            //bool hasChanges = context.ChangeTracker.HasChanges();
            var codeModule = context.CodeModule.FirstOrDefault(x => x.AssemblyName == module.AssemblyName);
            foreach (var moduleClass in module.Classes)
            {
                //hasChanges = context.ChangeTracker.HasChanges();
                var pocoCodeClass = classLookup[moduleClass.FullName].FirstOrDefault();

                if (pocoCodeClass != null)
                {
                    UpdateSummary(moduleClass.Summary, pocoCodeClass.Summary);
                }
                else
                {
                    pocoCodeClass = new CodeClass(moduleClass);
                    codeModule.Classes.Add(pocoCodeClass);
                    Log.DebugFormat("Added Class {0}", pocoCodeClass.Name);
                }

                UpdateCodeMethods(moduleClass, pocoCodeClass, methodLookup, methodMapper);
                if (pocoCodeClass != null && pocoCodeClass.Methods.Any())
                {
                    if (pocoCodeClass.Methods.Any(x => x.FileName != null))
                        pocoCodeClass.FileName = pocoCodeClass.Methods.FirstOrDefault(x => x.FileName != null).FileName;
                }
            }
            context.SaveChanges();
        }

        private void UpdateCodeMethods(Class codeClass, CodeClass pocoCodeClass, ILookup<string, CodeMethod> methodLookup, List<TrackedMethodMap> methodMapper)
        {
            foreach (var moduleMethod in codeClass.Methods.Where(x => x.SkippedDueTo != SkippedMethod.AutoImplementedProperty && !x.IsGetter && !x.IsSetter))
            {
                var moduleMethodName = moduleMethod.Name;
                var codeMethod = methodLookup[moduleMethodName].FirstOrDefault();

                if (codeMethod != null)
                {
                    UpdateSummary(moduleMethod.Summary, codeMethod.Summary);
                }
                else if (moduleMethod.FileRef != null)
                {
                    // Null FileRefs indicate unexecuted methods like empty constructors
                    var mapper = methodMapper.FirstOrDefault(x=>x.MethodName == moduleMethodName);
                    if (mapper != null)
                    {
                        codeMethod = new CodeMethod(moduleMethod);
                        pocoCodeClass.Methods.Add(codeMethod);
                    }

                }
            }
        }

        private CodeModule UpdateModule(Model.Module module, TestifyContext context)
        {
            Log.DebugFormat("Entering UpdateModule for Module: {0}", module.AssemblyName);
            var codeModule = context.CodeModule.FirstOrDefault(x => x.Name.Equals(module.ModuleName));
            if (codeModule != null)
            {
                UpdateSummary(module.Summary, codeModule.Summary);
            }
            else
            {
                codeModule = new CodeModule(module);
                context.CodeModule.Add(codeModule);

                context.SaveChanges();
            }

            if (codeModule.TestMethods != null)
            {
                try
                {
                    var testMethodsToRemove = new List<TestMethod>();
                    // Remove TrackedMethods that are no longer in the Session Module
                    foreach (var trackedMethod in codeModule.TestMethods.Where(x => x.AssemblyName == module.AssemblyName))
                    {
                        if (trackedMethod.Name == null)
                        {
                            Log.ErrorFormat("TestMethod has a NULL Name , fileName = {0}", trackedMethod.FileName);
                        }
                        if (!module.TrackedMethods.Any(n => CoverageService.Instance.RemoveNamespaces(n.Name) == trackedMethod.Name))
                        {
                            Log.DebugFormat("TestMethod marked for Removal , Name = {0}", trackedMethod.Name);
                            testMethodsToRemove.Add(trackedMethod);
                        }
                    }

                    testMethodsToRemove.ForEach(x => codeModule.TestMethods.Remove(x));
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("ERROR in UpdateModule  Error: {0}", ex);
                }
            }
            context.SaveChanges();

            return codeModule;
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
            context.SaveChanges();
        }

        //}
    }
}