using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Validation;
using System.Diagnostics;
using Leem.Testify.Domain;
using System.Data;
using log4net;
using Leem.Testify.Domain.DaoInterfaces;
using Leem.Testify.Domain.Model;
using ErikEJ.SqlCe;


namespace Leem.Testify.DataLayer
{
    public class TestifyQueries :ITestifyQueries
    {
        private Stopwatch _sw;
        private ILog Log = LogManager.GetLogger(typeof(TestifyQueries));
        private string _solutionName;
        public TestifyQueries()
        {
            _sw = new Stopwatch();
        }
        public TestifyQueries(string solutionName)
        {
            _solutionName = solutionName;
            _sw = new Stopwatch();
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

        public ProjectInfo GetProjectInfoFromTestProject(string uniqueName)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var result = from project in context.Projects
                             from testProject in context.TestProjects
                             where testProject.UniqueName == uniqueName
                             && testProject.ProjectUniqueName == project.UniqueName
                             select new ProjectInfo { ProjectName = project.Name,
                                                         ProjectAssemblyName = project.AssemblyName,
                                                         TestProject = testProject};
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
                                 from testProject in context.TestProjects
                                 where project.UniqueName == uniqueName
                                 && testProject.ProjectUniqueName == project.UniqueName
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
                Log.DebugFormat("Error Getting Project Info, error: {0}",ex);
                return null;
            }
           
        }
        public IList<TestProject> GetTestProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.TestProjects.ToList();
            }
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
                catch(Exception ex)
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
                    Debug.WriteLine("Error: {0} ", ex.InnerException);
                }
                _sw.Stop();
                Log.DebugFormat("MaintainProjects Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);
            }
        }

        private void UpdateProjects(IList<Project> projects, TestifyContext context)
        {
            _sw.Restart();
            // Existing projects
            foreach (var currentProject in projects)
            {
                var existingProject = context.Projects.Find(currentProject.UniqueName);
                if( existingProject != null){ 

                    // update the path
                    if(currentProject.Path != existingProject.Path 
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

        private bool CheckForChanges(TestifyContext context)
        {
            context.ChangeTracker.DetectChanges();
            var isModified = context.ChangeTracker.Entries().Any(e => e.State == EntityState.Modified);
            var isAdded = context.ChangeTracker.Entries().Any(e => e.State == EntityState.Added);
            var isDeleted = context.ChangeTracker.Entries().Any(e => e.State == EntityState.Deleted);
            var isDirty = context.ChangeTracker.Entries()
                      .Any(e => e.State == EntityState.Added
                             || e.State == EntityState.Deleted
                             || e.State == EntityState.Modified);
            Debug.WriteLine("IsDirty = ", isDirty);
            return isDirty;
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

        private List<string> GetChangedMethods(List<CoveredLine> coveredLines)
        {
            var changedMethods = coveredLines.GroupBy(i => i.Method)
                                                           .Select(i => i.Key)
                                                           .ToList();
            return changedMethods;
        }
        private void UpdateTestProjects(IList<Project> projects, TestifyContext context)
        {

            // Existing projects
            foreach (var currentProject in projects)
            {
                Log.DebugFormat("Project Name: {0}, AssemblyName: {1}, UniqueName: {2}", currentProject.Name, currentProject.AssemblyName,currentProject.UniqueName);
                var existingProject = context.TestProjects.Find(currentProject.UniqueName);
                if (existingProject != null)
                {

                     if(currentProject.Path != existingProject.Path
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

            foreach(var newProject in newProjects)
            {
                Log.DebugFormat("New Project Name: {0}, UniqueName: {1}", newProject.Name, newProject.UniqueName);
                var targetProjectName = newProject.Name.Replace(".Test", string.Empty);
                var targetProject = context.Projects.FirstOrDefault(x => x.Name.Equals(targetProjectName));
                var existingProject = context.Projects.FirstOrDefault(x => x.Name.Contains(newProject.Name));

                if( targetProject != null)
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
                        var newTestProject = new TestProject
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

        public IQueryable<Project> GetProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.Projects;
            }
        }

        public IList<CoveredLine> GetCoveredLines(string className) 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                List<CoveredLine> coveredLines = new List<CoveredLine>();

                var results = context.CoveredLines.Where(x => x.Class.Equals(className));
                foreach(var result in results)
                {
                   // Log.DebugFormat("TestMethod {0} ,  IsSuccessful = {1}", result.line.Method, result.line.IsSuccessful);
                    coveredLines.Add(result);
                }
                return coveredLines;
            }
        }

        public async Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, ProjectInfo projectInfo, List<string> individualTests)
        {
            var coverageService = CoverageService.Instance;
            var changedClasses = new List<string>();

            IList<LineCoverageInfo> newCoveredLineInfos = new List<LineCoverageInfo>();
            var module = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName));
            var moduleName = module != null ? module.FullName : string.Empty;

            var testModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName));

            try{
                    //if (individualTests.Any())
                    //{
                        // Tests have been run on the whole module, so any line not in CoverageSession is not "Covered"
                        newCoveredLineInfos = coverageService.GetCoveredLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName);

                        var newCoveredLineList = new List<CoveredLine>();
                        using (var context = new TestifyContext(_solutionName))
                        {
                            var existingCoveredLines = (from line in context.CoveredLines
                                                        where line.Module.Equals(moduleName)
                                                        select line).ToLookup(x => x.LineNumber);

                            foreach (var line in newCoveredLineInfos)
                            {
                                var existingTest = existingCoveredLines[line.LineNumber].FirstOrDefault(x => x.Method.Equals(line.Method)
                                                                    && x.Class.Equals(line.Class)
                                                                    && x.Module.Equals(line.Module));

                                if (existingTest != null)
                                {
                                    existingTest.IsCode = line.IsCode;
                                    existingTest.IsCovered = line.IsCovered;
                                    //existingTest.UnitTestId = line.CoveringTest.UnitTestId;
                                }
                                else
                                {
                                    var newCoverage = ConstructCoveredLine(line);
                                    newCoveredLineList.Add(newCoverage);
                                }

                            }

                            DoBulkCopy("CoveredLines", newCoveredLineList, context);
                            var isDirty = CheckForChanges(context);

                            changedClasses = GetChangedMethods(newCoveredLineList);
                            changedClasses.AddRange(GetChangedMethods(context));
                            context.SaveChanges();

                            var trackedMethods = (from testInfo in newCoveredLineInfos
                                                  where testInfo.CoveringTest != null && testInfo.CoveringTest.UniqueId > 0
                                                  select testInfo.CoveringTest);

                            if (trackedMethods.Any())
                            {
                                var distinctTrackedMethods = trackedMethods.GroupBy(x => x.MetadataToken).Select(y => y.First()).ToList();
                                UpdateUnitTests(distinctTrackedMethods, testModule);
                                UpdateTrackedMethods(distinctTrackedMethods);

                                UpdateCoveredLines(module.FullName, distinctTrackedMethods, newCoveredLineInfos);

                            }                     
                        }
                    //}
                    //else 
                    //{
                    //    // Only a single unit test was run, so only the lines in the CoverageSession will be updated
                    //    int? metadataToken; 
                    //    using (var context = new TestifyContext()) 
                    //    {
                    //        metadataToken = context.UnitTests.FirstOrDefault(x => x.TestMethodName.Equals(individualTest)).MetadataToken;
                    //    }
                    //    if (metadataToken != null)
                    //    {
                    //        var testedLines = coverageService.GetRetestedLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName, (int)metadataToken);
                    //    }
                    //}
                    return changedClasses;
  
            }

            catch(Exception ex)
            {
                Log.ErrorFormat("Error in SaveCoverageSessionResults Inner Exception: {0} Message: {1} StackTrace {2}", ex.InnerException, ex.Message,ex.StackTrace);
                 return new List<string>();
            }
        }

               

        private static void DoBulkCopy(string tableName, List<CoveredLine> coveredLines, TestifyContext context)
        {
            SqlCeBulkCopyOptions options = new SqlCeBulkCopyOptions();

            using (SqlCeBulkCopy bc = new SqlCeBulkCopy(context.Database.Connection.ConnectionString))
            {
                bc.DestinationTableName = tableName;
                bc.WriteToServer(coveredLines);
            }
        }

        private void UpdateCoveredLines(string moduleName, List<TrackedMethod> trackedMethods, IList<LineCoverageInfo> newCoveredLineInfos)
        {

            using (var context = new TestifyContext(_solutionName))
            {
                //var results = (from line in context.CoveredLines
                //               // 2/7 join test in context.UnitTests on line.MetadataToken equals test.MetadataToken into allLines
                //               join test in context.UnitTests on line.UnitTestId equals test.UnitTestId into allLines
                //               from test in allLines.DefaultIfEmpty()
                //               where line.Module.Equals(moduleName)
                //               select new { line, IsSuccessful = test == null ? false : test.IsSuccessful }).ToList();

//var covering testc = line.CoveringTest
                var coveredLines = (from line in context.CoveredLines
                                // 2/7 join test in context.UnitTests on line.MetadataToken equals test.MetadataToken into allLines
                                //join lineInfo in newCoveredLines on new { line.Method, line.LineNumber } equals new { lineInfo.Method, lineInfo.LineNumber } //into allLines
                                //join test in context.UnitTests on lineInfo.CoveringTest.Name equals test.TestMethodName into allLines
                                //from test in allLines.DefaultIfEmpty()
                                where line.Module.Equals(moduleName)
                                select line);

                foreach (var coveredLine in coveredLines)
                {
                    var unitTestName = newCoveredLineInfos.FirstOrDefault(x => x.Method == coveredLine.Method && x.LineNumber == coveredLine.LineNumber).CoveringTest.Name;
                    var testMethodName = ConvertTrackedMethodFormatToUnitTestFormat(unitTestName);
                    var unitTest = context.UnitTests.FirstOrDefault(x => x.TestMethodName == testMethodName);
                    if (unitTest != null)
                    {
                        coveredLine.UnitTestId = unitTest.UnitTestId; 
                        coveredLine.IsSuccessful = unitTest.IsSuccessful;
                    }


                    //Log.DebugFormat("TestMethod {0} ,  IsSuccessful = {1}", result.line.Method, result.IsSuccessful);
                   // var metaDataToken = newCoveredLines.Where(x=>x.Method == result.Method && x.LineNumber == result.LineNumber).FirstOrDefault().CoveringTest.MetadataToken;
                }
                context.SaveChanges();
            }
            //{"Unable to create a constant value of type 'Leem.Testify.Domain.LineCoverageInfo'. Only primitive types or enumeration types are supported in this context."}{"Unable to create a constant value of type 'Leem.Testify.Domain.LineCoverageInfo'. Only primitive types or enumeration types are supported in this context."}         
        }

        private void UpdateUnitTests(IList<TrackedMethod> trackedMethods, Module testModule)
        {
            _sw.Restart();
 
            try
            {
                if (testModule != null)
                {
                    using (var context = new TestifyContext(_solutionName))
                    {

                        var testProjectUniqueName = context.TestProjects.Where(x => x.AssemblyName.Equals(testModule.ModuleName)).First().UniqueName;

                        //Create Unit Test objects
                        var unitTests = new List<UnitTest>();
                        foreach (var trackedMethod in trackedMethods)
                        {
                            var testMethodName = ConvertTrackedMethodFormatToUnitTestFormat(trackedMethod.Name);

                            var matchingUnitTest = context.UnitTests.Single(x => x.TestMethodName.Equals(testMethodName));
                            if (matchingUnitTest != null)
                            {
                                // 2/7 matchingUnitTest.MetadataToken = trackedMethod.MetadataToken;
 //                               matchingUnitTest.UnitTestId = trackedMethod.UnitTestId;
                                matchingUnitTest.TestProjectUniqueName = testProjectUniqueName;
                                trackedMethod.UnitTestId = matchingUnitTest.UnitTestId;
                                
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

        public static string ConvertTrackedMethodFormatToUnitTestFormat(string trackedMethodName)
        {
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

 

        private static CoveredLine ConstructCoveredLine(LineCoverageInfo line)
        {
            var newCoverage = new CoveredLine
            {
                LineNumber = line.LineNumber,
                Method = line.Method,
                Class = line.Class,
                Module = line.Module,
                IsCode = line.IsCode,
                IsCovered = line.IsCovered,

                UnitTestId = line.CoveringTest != null ? line.CoveringTest.UnitTestId : 0
            };
            return newCoverage;
        }

        public void UpdateTrackedMethods(IList<TrackedMethod> trackedMethods)
        {
            using (var context = new TestifyContext(_solutionName))
            {
  
                foreach(var currentTrackedMethod in trackedMethods)
                {
                    //var existingTrackedMethod = context.TrackedMethods.Find(currentTrackedMethod.MetadataToken);
                    var existingTrackedMethod = context.TrackedMethods.Where(x => x.Name == currentTrackedMethod.Name).FirstOrDefault();

                    if (existingTrackedMethod == null)
                    {
                        context.TrackedMethods.Add(currentTrackedMethod);
                    }
                    // 2/7 
                    //else if (currentTrackedMethod.Name != existingTrackedMethod.Name)
                    // {
                    //    existingTrackedMethod = currentTrackedMethod;
                    // }
                    else
                    {
                        existingTrackedMethod.UnitTestId = currentTrackedMethod.UnitTestId;
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

        public void SaveUnitTests()
        {
 
        }


        public void GetUnitTestsCoveringMethod(string modifiedMethod)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                
                var query = from unitTest in context.TrackedMethods
                            where unitTest.Name.Contains(modifiedMethod)
                            // 2/7 select unitTest.MetadataToken;
                            select unitTest.UnitTestId;

            }
        }


        public void SaveUnitTestResults(resultType testOutput)
        {

            string runDate = testOutput.date;
            string runTime = testOutput.time;
            string fileName = testOutput.name;
            var x = testOutput.testsuite;
            var unitTests = new List<UnitTest>();
            unitTests.AddRange(GetUnitTests(testOutput.testsuite));

            foreach(var test in unitTests)
            {
                test.LastRunDatetime = runDate + " " + runTime;
                test.AssemblyName = fileName;
                if (test.IsSuccessful)
                {
                    test.LastSuccessfulRunDatetime = test.LastRunDatetime;
   
                }
            }

            using (var context = new TestifyContext(_solutionName))
            {
                
                try{
                    foreach(var test in unitTests)
                    {
                        var existingTest = context.UnitTests.SingleOrDefault(y => y.TestMethodName == test.TestMethodName);
                        if (existingTest == null)
                        {
                            context.UnitTests.Add(test);
                            Log.DebugFormat("Added UnitTest to Context: Name: {0}, IsSucessful : {1}",test.TestMethodName, test.IsSuccessful);
                        }
                        else 
                        {
                            test.UnitTestId = existingTest.UnitTestId;
                            test.LastSuccessfulRunDatetime = existingTest.LastSuccessfulRunDatetime;
                            context.Entry(existingTest).CurrentValues.SetValues(test);
                        }

                    }

                    context.SaveChanges();
                }
              catch (Exception ex)
              {
                  Log.ErrorFormat("Error in SaveUnitTestResults Message: {0}, InnerException {1}",ex.Message, ex.InnerException);
              }
            }
        }

        private List<UnitTest> GetUnitTests(object element)
        {
            var unitTests = new List<UnitTest>();
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
                   
                    foreach (var item in testsuite.results.Items )
                    {
                        unitTests.AddRange(GetUnitTests(item));
                    }
                }
            }

            return unitTests;
        }

        private UnitTest ConstructUnitTest(testcaseType testcase)
        {
 
            var unitTest = new UnitTest
            {
                TestDuration = testcase.time,
                IsSuccessful = testcase.success == bool.TrueString,
                TestMethodName = testcase.name,
                Executed = testcase.executed == bool.TrueString,
                Result = testcase.result,
                NumberOfAsserts = Convert.ToInt32( testcase.asserts)
            };
            return unitTest;
        }

        public List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber)
        {
            string methodNameFragment = className + "::" + methodName;
            List<string> testNames;
            using (var context = new TestifyContext(_solutionName))
            {
                var query = (from line in context.CoveredLines
                            join test in context.UnitTests
                            on line.UnitTestId equals test.UnitTestId
                            where line.Method.Contains(methodNameFragment)
                            select test.TestMethodName);
                testNames = query.Distinct().ToList();
            }

            return testNames;
        }
    }

}
