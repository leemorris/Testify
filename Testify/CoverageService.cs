using EnvDTE;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using Leem.Testify.Model;
using Leem.Testify.Poco;
using log4net;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CodeClass = Leem.Testify.Poco.CodeClass;
using File = Leem.Testify.Model.File;
using Summary = Leem.Testify.Poco.Summary;
using TrackedMethod = Leem.Testify.Model.TrackedMethod;

namespace Leem.Testify
{
    public class CoverageService : ICoverageService
    {
        private static CoverageService _instance;
        private readonly ILog _log = LogManager.GetLogger(typeof (CoverageService));
        private IList<CodeElement> _classes;
        private DTE _dte;
        private IList<CodeElement> _methods;
        private ITestifyQueries _queries;
        private string _solutionDirectory;
        private string _solutionName;
        const string Xmlnode = "XmlNode";
        const string SystemXmlXmlnode = "System.Xml.XmlNode";
        const string Int32 = "Int32";
        const string Integer = "int";
        const string Dataset = "DataSet";
        const string SystemDataDataset = "System.Data.DataSet";
        const string Exception = "Exception";
        const string SystemException = "System.Exception";
        const string SystemCollectionsGenericIlist = "System.Collections.Generic.IList`1";
        const string Ilist = "IList";
        const string SystemCollectionsGenericList = "System.Collections.Generic.List`1";
        const string List = "List";
        const string SystemCollectionsGenericIenumerable = "System.Collections.Generic.IEnumerable`1";
        const string Ienumerable = "IEnumerable";
        const string SystemFunc1 = "System.Func`1";
        const string Func = "Func";
        const string SystemFunc = "System.Func";
        const string SystemNullable = "System.Nullable`1";
        const string Stringy = "String";
        const string Nullable = "Nullable";
        const string SystemString = "System.String";
        const string SystemGuid = "System.Guid";
        const string Guid = "Guid";
        const string SystemObject = "System.Object";
        const string Objecty = "Object";
        const string NullableBoolean = "Nullable<Boolean>";
        const string SystemNullableBool = "System.Nullable[[bool]]";
        const string NullableInt = "Nullable<int>";
        const string SystemNullableInt = "System.Nullable[[int]]";
        const string NullableDouble = "Nullable<Double>";
        const string SystemNullableDouble = "System.Nullable[[Double]]";

        public static CoverageService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CoverageService();

                    _instance._classes = new List<CodeElement>();

                    _instance._methods = new List<CodeElement>();
                }

                return _instance;
            }
        }

        public ITextDocument Document { get; set; }


        public DTE DTE
        {
            set { _dte = value; }
        }

        public ITestifyQueries Queries { get; set; }

        public string SolutionName
        {
            get { return _solutionName; }
            set
            {
                _solutionName = value;

                _solutionDirectory = Path.GetDirectoryName(_solutionName);
            }
        }

        public List<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage,
            string projectAssemblyName)
        {
            var coveredLines = new List<LineCoverageInfo>();

            List<Module> sessionModules = codeCoverage.Modules;

            _log.DebugFormat("GetCoveredLines for project: {0}", projectAssemblyName);
            _log.DebugFormat("Summary.NumSequencePoints: {0}", codeCoverage.Summary.NumSequencePoints);
            _log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
            _log.DebugFormat("Summary.VisitedSequencePoints: {0}", codeCoverage.Summary.VisitedSequencePoints);
            _log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
            _log.DebugFormat("Number of Modules: {0}", sessionModules.Count());

            foreach (Module sessionModule in sessionModules)
            {
                _log.DebugFormat("Module Name: {0}", sessionModule.ModuleName);
           
                IEnumerable<TrackedMethod> tests =
                    sessionModules.Where(x => x.TrackedMethods.Any()).SelectMany(y => y.TrackedMethods);

                if (sessionModule != null)
                {
                    List<Class> classes = sessionModule.Classes;

                    _log.DebugFormat("First Module Name: {0}", sessionModule.ModuleName);
                    _log.DebugFormat("Number of Classes: {0}", classes.Count());

                    foreach (var codeClass in classes)
                    {
                        List<Method> methods = codeClass.Methods;

                        foreach (var method in methods)
                        {
                            if (!method.Name.ToString().Contains("__") && !method.IsGetter && !method.IsSetter)
                            {
                                var fileNames = new List<File>();
                                if (method.FileRef != null)
                                {
                                    fileNames = sessionModule.Files.Where(x => x.UniqueId == method.FileRef.UniqueId).ToList();
                                }

                                string fileName;
                                if (fileNames.Any())
                                {
                                    fileName = fileNames.FirstOrDefault().FullPath;


                                    // remove closing paren
                                    // modifiedMethodName = modifiedMethodName.Substring(0, modifiedMethodName.Length - 1);
                                    // Raw: System.Void Quad.QuadMed.WebPortal.Domain.App_LocalResources.AssessmentQuestions::.ctor()
                                    // modified:        Quad.QuadMed.WebPortal.Domain.App_LocalResources.AssessmentQuestions..ctor
                                    //Needed:           Quad.QuadMed.WebPortal.Domain.App_LocalResources.AssessmentQuestions..ctor


                                    CodeMethodInfo methodInfo = UpdateMethodLocation(method, fileName);

                                    Queries.UpdateCodeMethodPath(methodInfo);

                                    ProcessSequencePoints(coveredLines, sessionModule, tests, codeClass, method, fileName);
                                }
                            }


                        }
                    }
                }
            }
            return coveredLines;
        }


        private static string ConvertTrackedMethodFormatToUnitTestFormat(string trackedMethodName)
        {
            // Convert This:
            // System.Void UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest::TestIt()
            // Into This:
            // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest.TestIt

            if (trackedMethodName == "")
            {
                return trackedMethodName;
            }
            int locationOfSpace = trackedMethodName.IndexOf(' ') + 1;

            int locationOfParen = trackedMethodName.IndexOf('(');

            var testMethodName = trackedMethodName.Substring(locationOfSpace);

                testMethodName = testMethodName.Replace("::", ".");

                return testMethodName;
            }

        private void ProcessSequencePoints(List<LineCoverageInfo> coveredLines, Module module,
            IEnumerable<TrackedMethod> tests, Class modelClass, Method method, string fileName)
        {
            List<SequencePoint> sequencePoints = method.SequencePoints;
            List<BranchPoint> branchPoints = method.BranchPoints;
            foreach (SequencePoint sequencePoint in sequencePoints)
            {
                var branchPoint = branchPoints.FirstOrDefault(x => x.StartLine == sequencePoint.StartLine);
                var coveredLine = new LineCoverageInfo
                {
                    IsCode = true,
                    LineNumber = sequencePoint.StartLine,
                    IsCovered = (sequencePoint.VisitCount > 0),
                    ModuleName = module.ModuleName,
                    ClassName = modelClass.FullName,
                    MethodName = method.Name,
                    FileName = fileName,
                    UnitTests = new List<UnitTest>(),
                    IsBranch = branchPoint != null
                };

                if (tests.Any())
                {
                    foreach (var trackedMethodRef in sequencePoint.TrackedMethodRefs)
                    {
                        TrackedMethod trackedMethod =
                            tests.FirstOrDefault(y => y.UniqueId.Equals(trackedMethodRef.UniqueId));

                        coveredLine.IsCode = true;

                        coveredLine.IsCovered = (sequencePoint.VisitCount > 0);
                        coveredLine.FileName = fileName;

                        coveredLine.TrackedMethods.Add(new Poco.TrackedMethod
                        {
                            UniqueId = (int) trackedMethod.UniqueId,
                            UnitTestId = trackedMethod.UnitTestId,
                            Strategy = trackedMethod.Strategy,
                            Name = trackedMethod.Name,
                            MetadataToken = trackedMethod.MetadataToken
                        });
                    }
                }

                coveredLines.Add(coveredLine);
            }
        }

        public CodeMethodInfo UpdateMethodLocation(Method codeMethod, string fileName)
        {
            var modifiedMethodName = string.Empty;
            var rawMethodName = codeMethod.Name;
          
            modifiedMethodName = ConvertTrackedMethodFormatToUnitTestFormat(rawMethodName);

            if (codeMethod.IsConstructor)
            {
                modifiedMethodName = modifiedMethodName.Replace(".cctor", ".ctor");
            }
            var parameters = new List<string>();
            if (!modifiedMethodName.EndsWith("()"))
            {
                parameters = ParseArguments(modifiedMethodName);

            }

            modifiedMethodName = modifiedMethodName.Substring(0, modifiedMethodName.LastIndexOf('('));
            IProjectContent project = new CSharpProjectContent();

            project.SetAssemblyName(fileName);
            project = AddFileToProject(project, fileName);

            var classes = new List<string>();

            var typeDefinitions = project.TopLevelTypeDefinitions;

            foreach (var typeDef in typeDefinitions)
            {
                classes.Add(typeDef.ReflectionName);
                if (typeDef.Kind == TypeKind.Class)
                {

                    var methods = typeDef.Methods.Where(x => x.ReflectionName == modifiedMethodName);
                    if(methods.ToList().Count > 1)
                    {
                        foreach(var method in methods)
                        {
                            System.Diagnostics.Debug.WriteLine("Method : " + method.Name );
                            if (method.Parameters.Count == parameters.Count)
                            {
                                bool parametersMatch = true;
                                for (int i = 0; i < method.Parameters.Count; i++ )
                                {
                                    var methodParameterType = method.Parameters[i].Type;
                                    string typeName = methodParameterType.ToString();
                                    typeName = typeName.Replace("bool", "Boolean")
                                                       .Replace("System.Nullable[[Boolean]]", "System.Nullable[[bool]]")
                                                       .Replace("System.Guid","Guid");
                                    if (typeName.Equals(parameters[i],StringComparison.OrdinalIgnoreCase) && parametersMatch)
                                    {
                                        parametersMatch = true;
                                    }
                                    else 
                                    {
                                        parametersMatch = false;
                                        break;
                                    }
 
                                }
                                if (parametersMatch)
                                {
                                    return new CodeMethodInfo
                                    {
                                        RawMethodName = rawMethodName,
                                        FileName = fileName,
                                        Line = method.BodyRegion.BeginLine,
                                        Column = method.BodyRegion.BeginColumn
                                    };
                                }
                            }
                        }
                    }
                    else if (methods.Count() == 1)
                    {
                        return new CodeMethodInfo
                        {
                            RawMethodName = rawMethodName,
                            FileName = fileName,
                            Line = methods.First().BodyRegion.BeginLine,
                            Column = methods.First().BodyRegion.BeginColumn
                        };

                    }
                }
            }
            return null;
        }

        internal List<string> ParseArguments(string modifiedMethodName)
        {
            var result = new List<string>();
            try
            {
                int locationOfParen = modifiedMethodName.IndexOf('(') + 1;
                string argumentString;
                if (locationOfParen > -1)
                {
                    argumentString = modifiedMethodName.Substring(locationOfParen,
                        modifiedMethodName.Length - locationOfParen - 1);
                }
                else
                {
                    argumentString = modifiedMethodName;
                }
                int locationOfOpenAngleBracket = modifiedMethodName.IndexOf('<') + 1;

                string[] arguments = argumentString.Split(',');
                foreach (string argument in arguments)
                {
                    string name = string.Empty;
                    try
                    {
                         name = RemoveNamespaces(argument);

                        name = name.Replace(Xmlnode, SystemXmlXmlnode)
                            .Replace(Int32, Integer)
                            .Replace(Dataset, SystemDataDataset)
                            .Replace(Exception, SystemException)
                            .Replace(SystemCollectionsGenericList, List)
                            .Replace(SystemCollectionsGenericIlist, Ilist)
                            .Replace(SystemCollectionsGenericIenumerable, Ienumerable)
                            .Replace(SystemFunc1, Func)
                            .Replace(SystemFunc, Func)
                            .Replace(SystemNullable, Nullable)
                            .Replace(SystemString, Stringy)
                            .Replace(SystemGuid, Guid)
                            .Replace(SystemObject, Objecty)
                            .Replace(NullableBoolean, SystemNullableBool)
                            .Replace(NullableInt, SystemNullableInt)
                            .Replace(NullableDouble, SystemNullableDouble)
                            ;

                        result.Add(name);
                    }
                    catch
                    {
                        _log.ErrorFormat("Error in RemoveNamespaces for: {0}", argument);
                        name = argument;
                    }
                }

                string parsedMethodName = modifiedMethodName.Substring(0, locationOfParen) + string.Join(",", result) +
                                          ")";
            }
            catch (Exception ex)
            {
                _log.DebugFormat("Error Parsing Arguments:{0}", ex);
            }

            return result;
        }


        private string RemoveNamespaces(string argument)
        {
            int positionOfFirstOpeningAngle = argument.IndexOf("<");
            int positionOfLastOpeningAngle = argument.LastIndexOf("<");

            int positionOfFirstClosingAngle = argument.LastIndexOf(">");
            int positionOfLastClosingAngle = argument.LastIndexOf(">");

            int positionOfFirstOpeningParen = argument.IndexOf("(");

            if (positionOfFirstOpeningAngle == -1)
            {
                argument = RemoveNamespaceFromMethod(argument);
            }
            else if (positionOfFirstOpeningAngle == positionOfLastOpeningAngle)
            {
                // Single

                // object


                string classPlusNamespace = argument.Substring(positionOfFirstOpeningAngle + 1,
                    positionOfFirstClosingAngle - positionOfFirstOpeningAngle);
                string classOnly = RemoveNamespaceFromMethod(classPlusNamespace);
                argument = argument.Replace(classPlusNamespace, classOnly);

                // method
                if (positionOfFirstOpeningParen > -1)
                {
                    string methodName = argument.Substring(0, positionOfFirstOpeningParen);
                    string methodMinusNamespace = RemoveNamespaceFromMethod(methodName);
                    argument = argument.Replace(methodName, methodMinusNamespace);
                }
            }
            else if (positionOfFirstOpeningAngle > positionOfLastOpeningAngle &&
                     positionOfLastOpeningAngle > positionOfFirstClosingAngle)
            {
                // Nested
            }
            return argument;
        }

        private string RemoveNamespaceFromMethod(string argumentPart)
        {
            int locationOfPeriod = argumentPart.LastIndexOf('.');

            string name = argumentPart.Substring(locationOfPeriod + 1);
            if (name.Substring(name.Length - 1) == ")")
            {
                name = name.ReplaceAt(name.Length - 1, string.Empty);
            }
            return name;
        }

        private IProjectContent AddFileToProject(IProjectContent project, string fileName)
        {
            string code = string.Empty;
            try
            {
                code = System.IO.File.ReadAllText(fileName);
            }
            catch (Exception)
            {
                _log.ErrorFormat("Could not find file to AddFileToProject, Name: {0}", fileName);
            }

            SyntaxTree syntaxTree = new CSharpParser().Parse(code, fileName);
            CSharpUnresolvedFile unresolvedFile = syntaxTree.ToTypeSystem();

            if (syntaxTree.Errors.Count == 0)
            {
                project = project.AddOrUpdateFiles(unresolvedFile);
            }
            return project;
        }

        public List<LineCoverageInfo> GetRetestedLinesFromCoverageSession(CoverageSession coverageSession,
            string projectAssemblyName, List<int> uniqueIds)
        {
            /// todo need to figure out how to remove a CoveredLine if an Edit has made it uncovered, 
            /// without removing all lines that were not covered by the subset of tests that were run
            /// One option: check all previously covered lines from this group of tests with the current covered lines list from this group.
            /// 
            //todo Handle case where a line that was "Code" and was covered is now not "Code"
            var coveredLines = new List<LineCoverageInfo>();

            List<Module> sessionModules = coverageSession.Modules;
            Module module = sessionModules.FirstOrDefault(x => x.ModuleName.Equals(projectAssemblyName));
            Module testModule = sessionModules.FirstOrDefault(x => x.ModuleName.Equals(projectAssemblyName + ".Test"));
            IEnumerable<TrackedMethod> tests =
                sessionModules.Where(x => x.TrackedMethods.Any()).SelectMany(y => y.TrackedMethods);
            var testsRun = tests.Where(x => uniqueIds.Contains((int)x.UniqueId));
            var methodsTested = module.Classes.SelectMany(m => m.Methods).Where(x=> x.SequenceCoverage > 0);

            if (module != null)
            {
                var codeModule = new CodeModule
                {
                    Name = module.FullName,
                    Summary = new Summary(module.Summary)
                };

                List<Class> classes = module.Classes;

                _log.DebugFormat("First Module Name: {0}", module.ModuleName);
                _log.DebugFormat("Number of Classes: {0}", classes.Count());


                foreach (Method method in methodsTested)
                        {
                            
                            string methodName = method.Name.ToString();
                            if (!methodName.Contains("_")
                                && !methodName.StartsWith("get_")
                                && !methodName.StartsWith("set_"))
                            {
                                var codeMethod = new CodeMethod
                                {
                                    Name = method.Name,
                                    Summary = new Summary(method.Summary)
                                };
                                string modifiedMethodName = methodName;
                                if (method.IsConstructor)
                                {
                                    modifiedMethodName = ConvertTrackedMethodFormatToUnitTestFormat(methodName);
                                    modifiedMethodName = modifiedMethodName.Replace("..","::.");
                                }

                             

                                var modelClass = module.Classes.FirstOrDefault(x=>x.Methods.Any(y=>y.Name.Contains(modifiedMethodName)));
                     


                                var codeClass = new CodeClass
                                {
                                    Name = modelClass.FullName,
                                    Summary = new Summary(modelClass.Summary)
                                };

                                List<SequencePoint> sequencePoints = method.SequencePoints;
                                foreach (SequencePoint sequencePoint in sequencePoints)
                                {
                                    if (testsRun.Any())
                                    {
                                        var coveredLine = new LineCoverageInfo
                                        {
                                            IsCode = true,
                                            LineNumber = sequencePoint.StartLine,
                                            IsCovered = (sequencePoint.VisitCount > 0),
                                            Module = codeModule,
                                            Class = codeClass,
                                            ClassName = modelClass.FullName,
                                            Method = codeMethod,
                                            FileName = module.Files.FirstOrDefault(x => x.UniqueId == method.FileRef.UniqueId).FullPath,
                                            MethodName = method.Name
                                            //   UnitTests = testsRun.ToList()
                                        };

                                        foreach (TrackedMethodRef trackedMethodRef in sequencePoint.TrackedMethodRefs)
                                        {

                                            IEnumerable<TrackedMethod> testsThatCoverLine =
                                                testsRun.Where(y => y.UniqueId.Equals(trackedMethodRef.UniqueId));
                                            var fileNames = module.Files.Where(x => x.UniqueId == method.FileRef.UniqueId).ToList();
                                            foreach (TrackedMethod test in testsThatCoverLine)
                                            {
                                                coveredLine.IsCode = true;

                                                coveredLine.IsCovered = (sequencePoint.VisitCount > 0);

                                                coveredLine.TrackedMethods.Add(new Poco.TrackedMethod
                                                {
                                                    UniqueId = (int) test.UniqueId,
                                                    MetadataToken = method.MetadataToken,
                                                    Strategy = test.Strategy,
                                                    Name = test.Name
                                                });
                                            }

                                            
                                        }
                                        if (coveredLine.Class == null)
                                        {
                                            _log.ErrorFormat("CoveredLine.Class is null for method:{0}", method.Name);
                                        }
                                        coveredLines.Add(coveredLine);
                                    }
                                }
                            }
                        }

            }

            return coveredLines;
        }
    }
}