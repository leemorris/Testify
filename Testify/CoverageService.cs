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
using NUnit.Framework;

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

        public List<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage, string projectAssemblyName, 
                                                                        List<TrackedMethodMap> methodMapper, TestifyContext context)
        {
            //4.42%
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
                    try
                    { 
                    List<Class> classes = sessionModule.Classes.ToList();

                    _log.DebugFormat("First Module Name: {0}", sessionModule.ModuleName);
                    _log.DebugFormat("Number of Classes: {0}", classes.Count());

                    var fileDictionary = sessionModule.Files.ToDictionary(file => file.UniqueId);
                        var classesFromModule = context.CodeClass.Where(x => x.CodeModule.AssemblyName == projectAssemblyName).ToList();
                        var methodsFromClasses = classesFromModule.SelectMany(y => y.Methods).ToList();
                        var uniqueMethodsFromClasses = methodsFromClasses.GroupBy(x => x.Name).Select(y => y.FirstOrDefault()).ToList();
                    var codeMethodDictionary = uniqueMethodsFromClasses.ToDictionary(item => item.Name);
 
                    foreach (var codeClass in classes)
                    {
                       // codeClass.Methods.RemoveAll(x=>x.Name.Contains("__"));
                        _log.DebugFormat("Class Name: {0}", codeClass.FullName);
                        List<Method> methods = codeClass.Methods;

                        foreach (var method in methods)
                        {
                            //if ( !method.IsGetter && !method.IsSetter)
                            //{
                                //var fileNames = new List<File>();
                                string fileName = string.Empty;
                                if (method.FileRef != null)
                                {
                                    fileName = fileDictionary[method.FileRef.UniqueId].FullPath;//.sessionModule.Files.Where(x => x.UniqueId == method.FileRef.UniqueId).ToList();
                                }

                               
                                if (fileName != string.Empty)
                                {
                                    //fileName = fileNames.FirstOrDefault().FullPath;

                                    if (fileName.Contains(@"\WebServices\") == false
                                        && fileName.Contains(@"\Web References\") == false
                                        && fileName.Contains(@"\Service References") == false)
                                    {
                                        var methodNameWithoutNamespaces = RemoveNamespaces(method.Name);
                                        if (methodNameWithoutNamespaces.Contains("`1"))
                                        {
                                            var returnType = methodNameWithoutNamespaces.Substring(0, methodNameWithoutNamespaces.IndexOf(" "));
                                            var modifiedReturnType = CoverageService.Instance.RemoveNamespaceFromType(returnType, isReturnType: true);
                                            methodNameWithoutNamespaces = methodNameWithoutNamespaces.Replace(returnType, modifiedReturnType);
                                        }

                                        var trackedMethodUnitTestMap = methodMapper.FirstOrDefault(x => methodNameWithoutNamespaces.EndsWith(RemoveNamespaces(x.TrackedMethodName)));

                                        if (trackedMethodUnitTestMap != null)
                                        {
                                            trackedMethodUnitTestMap.CoverageSessionName = methodNameWithoutNamespaces;
                                        }

                                        if (trackedMethodUnitTestMap == null && methodNameWithoutNamespaces.Contains(".ctor") == false && methodNameWithoutNamespaces.Contains(".cctor") == false)
                                        {
                                            _log.ErrorFormat("Did not find Map object for: <{0}>", methodNameWithoutNamespaces);
                                        }
                                        CodeMethodInfo methodInfo = UpdateMethodLocation(method, fileName, trackedMethodUnitTestMap);

                                        Queries.UpdateCodeMethodPath(context, methodInfo, codeMethodDictionary);

                                        ProcessSequencePoints(coveredLines, sessionModule, tests, codeClass, method, fileName, trackedMethodUnitTestMap);

                                    }

                                    
                                }
                            }


                        //}
                        context.SaveChanges();
                    }
                    }
                    catch (Exception ex)
                    {
                        _log.ErrorFormat("ERROR in GetCoveredLinesFromCoverageSession {0}", ex);
                    }
                }

            }
            return coveredLines;
        }

        public void UpdateMethodsAndClassesFromCodeFile(List<Module> modules,List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {

            _log.DebugFormat("Entering UpdateMethodsAndClassesFromCodeFile ");

            foreach (var module in modules)
            {
                IProjectContent project = new CSharpProjectContent();
                var classNames = new List<string>();
                var methodNames = new List<string>();

                var fileNames = module.Files.Select(x => x.FullPath);
                foreach (var file in module.Files)
                {
                    project.SetAssemblyName(file.FullPath);
                    var syntaxTree= GetSyntaxTree(file.FullPath);
                    project = AddFileToProject(project, syntaxTree);
                    var classes = new List<string>();
                }
                var typeDefinitions = project.TopLevelTypeDefinitions;


                    foreach (var typeDef in typeDefinitions)
                    {
                        try
                        {
                            if (typeDef.Kind == TypeKind.Class)
                            {
                                classNames.Add(typeDef.ReflectionName);
                                var methods = typeDef.Methods;
                                UpdateMethods(typeDef, methods, typeDef.UnresolvedFile.FileName, trackedMethodUnitTestMapper);
                                methodNames.AddRange(methods.Select(x => x.ReflectionName));
                            }

                        }
                        catch (Exception ex)
                        {
                            _log.ErrorFormat("Error in UpdateMethodsAndClassesFromCodeFile Class Name : {0} Error: {1}", typeDef.Name, ex);
                        }

                    }



                    //Queries.RemoveMissingClasses(module, classNames, trackedMethodUnitTestMapper);
                    //Queries.RemoveMissingMethods(module, methodNames, trackedMethodUnitTestMapper);


            }
            _log.DebugFormat("Leaving UpdateMethodsAndClassesFromCodeFile ");

        }

        public void UpdateMethods(IUnresolvedTypeDefinition fileClass, IEnumerable<IUnresolvedMethod> methods, string fileName, List<TrackedMethodMap>  trackedMethodUnitTestMapper)
        {
            //todo remove trackedmethodmapper from arguments 
            var methodsToDelete = new List<string>();

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

                string modifiedMethodName;
                foreach (var fileMethod in methods)
                {
                    var rawMethodName = fileMethod.ReflectionName;
                    if (fileMethod.IsConstructor)
                    {
                        rawMethodName = rawMethodName.Replace("..", ".");
                        modifiedMethodName = Queries.ConvertUnitTestFormatToFormatTrackedMethod(rawMethodName);
                        modifiedMethodName = modifiedMethodName.Replace("::ctor", "::.ctor");
                    }
                    else
                    {
                        modifiedMethodName = Queries.ConvertUnitTestFormatToFormatTrackedMethod(rawMethodName);
                       // trackedMethodUnitTestMapper.FirstOrDefault(x=>x.TrackedMethodName);
                    }

                    // remove closing paren
                    modifiedMethodName = modifiedMethodName.Substring(0, modifiedMethodName.Length - 1);

                    var codeMethods = from clas in codeClasses
                                      join method in context.CodeMethod on clas.CodeClassId equals method.CodeClassId
                                      where method.Name.Contains(modifiedMethodName)
                                      select method;
                    foreach (var method in codeMethods)
                    {
                        if (method.FileName != fileName
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
            IEnumerable<TrackedMethod> tests, Class modelClass, Method method, string fileName,TrackedMethodMap trackedMethodMap)
        {
            List<SequencePoint> sequencePoints = method.SequencePoints;
            List<BranchPoint> branchPoints = method.BranchPoints;
            var isTestProject = module.AssemblyName.EndsWith(".Test");
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
                    TestMethods = new List<TestMethod>(),
                    IsBranch = branchPoint != null
                };

                if (isTestProject == false && tests.Any())
                {
                    foreach (var trackedMethodRef in sequencePoint.TrackedMethodRefs)
                    {
                        TrackedMethod trackedMethod =
                            tests.FirstOrDefault(y => y.UniqueId.Equals(trackedMethodRef.UniqueId));

                        coveredLine.IsCode = true;

                        coveredLine.IsCovered = (sequencePoint.VisitCount > 0);
                        coveredLine.FileName = fileName;
                        
                        coveredLine.TestMethods.Add(new Poco.TestMethod
                        {
                            UniqueId = (int) trackedMethod.UniqueId,
                            Strategy = trackedMethod.Strategy,
                            Name = trackedMethod.Name,
                            MetadataToken = trackedMethod.MetadataToken,
                            TestMethodName = trackedMethod.Name
                        });
                    }
                }

                coveredLines.Add(coveredLine);
            }
        }

        public CodeMethodInfo UpdateMethodLocation(Method codeMethod, string fileName, TrackedMethodMap methodMapper)
        {

          
            //var modifiedMethodName = string.Empty;
            //var rawMethodName = codeMethod.Name;

            //modifiedMethodName = ConvertTrackedMethodFormatToUnitTestFormat(rawMethodName);

            //if (codeMethod.IsConstructor)
            //{
            //    modifiedMethodName = modifiedMethodName.Replace(".cctor", ".ctor");
            //}
            //var parameters = new List<string>();
            //if (!modifiedMethodName.EndsWith("()"))
            //{
            //    parameters = ParseArguments(modifiedMethodName);

            //}

            //modifiedMethodName = modifiedMethodName.Substring(0, modifiedMethodName.LastIndexOf('('));



            try
            {
                var typeDefinitions = GetTypeDefinitionsFromRefactory(fileName);



                var classes = new List<string>();

                foreach (var typeDef in typeDefinitions)
                {
                    classes.Add(typeDef.ReflectionName);
                    if (typeDef.Kind == TypeKind.Class)
                    {

                        var codeMethodInfo = new CodeMethodInfo
                        {
                            RawMethodName = codeMethod.Name,
                            FileName = fileName
                        };

                        if (methodMapper != null)
                        {
                            // Test Module
                            var method = typeDef.Methods.FirstOrDefault(x => methodMapper.MethodInfos.Any(u => u.MethodName.Contains(x.ReflectionName)));

                            if (method != null)
                            {
                                codeMethodInfo.Line = method.BodyRegion.BeginLine;
                                codeMethodInfo.Column = method.BodyRegion.BeginColumn;
                            }
                        }
                        else 
                        {
                            // Code Module
                            var modifiedMethodName = Queries.ConvertUnitTestFormatToFormatTrackedMethod(codeMethod.Name);
                            var methodRegular = typeDef.Methods.FirstOrDefault(x => x.Name == modifiedMethodName);
                        }


                        return codeMethodInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error in UpdateMethodLocation FileName : {0}, MethodName: {1} Error: {2}", fileName, codeMethod.Name, ex);
                throw;
            }
            return null;
        }

        private IEnumerable<IUnresolvedTypeDefinition> GetTypeDefinitionsFromRefactory(string fileName)
        {
            IProjectContent project = new CSharpProjectContent();

            project.SetAssemblyName(fileName);
            var syntaxTree = GetSyntaxTree(fileName);
            project = AddFileToProject(project, syntaxTree);

            var typeDefinitions = project.TopLevelTypeDefinitions;
            return typeDefinitions;
        }

        internal List<string> ParseArguments(string modifiedMethodName)
        {
            var result = new List<string>();
            try
            {
                int locationOfParen = modifiedMethodName.IndexOf('(') + 1;
                string argumentString;
                if (locationOfParen > 0)
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
                            //.Replace(Exception, SystemException)
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
                _log.ErrorFormat("Error Parsing Arguments:{0}", ex);
            }

            return result;
        }

        //System.Collections.Generic.IList`1<Quad.QuadMed.QMedClinicalTools.Domain.Objects.AccountRequest> Quad.QuadMed.QMedClinicalTools.DataAccess.AccountRequestDao::GetAccountRequestsByRequestCriteria(Quad.QuadMed.QMedClinicalTools.Domain.Objects.AccountRequest)

        public string RemoveNamespaces(string methodNameWithArgsAndReturnType)
        {
            var baseMethodName = methodNameWithArgsAndReturnType;
            string returnTypeWithoutNamespace = string.Empty;
            string returnType = string.Empty;
            string argumentStringWithoutNamespaces = string.Empty;
            //Get return Type
            // substring up to first space
            try
            {
                var locationOfOpenParen = methodNameWithArgsAndReturnType.IndexOf("(");
                var locationOfCloseParen = methodNameWithArgsAndReturnType.IndexOf(")");
                var locationOfFirstSpace = methodNameWithArgsAndReturnType.IndexOf(" ");
                if (locationOfOpenParen > 0)
                {
                    returnType = methodNameWithArgsAndReturnType.Substring(0, methodNameWithArgsAndReturnType.IndexOf(" "));
                    baseMethodName = methodNameWithArgsAndReturnType.Substring(methodNameWithArgsAndReturnType.IndexOf(" ")).TrimStart();
                }
                else
                {
                    baseMethodName = string.Empty;
                }
               
               

                //returnTypeWithoutNamespace = RemoveNamespaceFromType(returnType,isReturnType:true);


                
                if (locationOfCloseParen - locationOfOpenParen > 1)
                {
                    var argumentString = methodNameWithArgsAndReturnType.Substring(methodNameWithArgsAndReturnType.IndexOf("(") + 1, methodNameWithArgsAndReturnType.IndexOf(")") - 1 - methodNameWithArgsAndReturnType.IndexOf("("));
                    if (locationOfOpenParen > 0)
                    {
                        baseMethodName = baseMethodName.Substring(0, baseMethodName.IndexOf("("));
                    }


                    // todo need to parse arguments to see if the argument contains a Lync expression like "System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>>"
                    // failing Method = System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>> Quad.QuadMed.QMedClinicalTools.Domain.Services.Util.PredicateBuilder::Or(System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>>,System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>>)

                    var arguments = argumentString.Split(',');
                    string[] argumentsWithoutNamespaces = new string[arguments.Length];

                    for (int i = 0; i < arguments.Length; i++)
                    {
                        argumentsWithoutNamespaces[i] = RemoveNamespaceFromType(arguments[i], isReturnType: false);
                    }

                    argumentStringWithoutNamespaces = "(" + string.Join(",", argumentsWithoutNamespaces) + ")";

                }

            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error in RemoveNamespaces, methodName: {0} , Error: {1}", methodNameWithArgsAndReturnType,ex);
            }
            if(returnType!= string.Empty)
            {
                returnTypeWithoutNamespace = RemoveNamespaceFromType(returnType, isReturnType: true);
            }
            

            return string.Concat(returnTypeWithoutNamespace, " ", baseMethodName, argumentStringWithoutNamespaces).Trim();
            //return returnTypeWithoutNamespace + " " + baseMethodName + argumentStringWithoutNamespaces;
        }

        public string RemoveNamespaceFromType(string returnType, bool isReturnType)
        {
            if (returnType.Contains("`1"))
            {
                var positionOfApostropheOne = returnType.IndexOf("`1<") + 3;
                var collectionTypeWithNamespace = returnType.Substring(0, positionOfApostropheOne-3);
                var collectionTypeWithoutNamespace = collectionTypeWithNamespace.Substring(collectionTypeWithNamespace.LastIndexOf(".")+1);
                var positionOfGreaterThan = returnType.LastIndexOf(">");        //System.Collections.Generic.IList`1<Quad.QuadMed.QMedClinicalTools.Domain.Objects.AccountRequest>
                var collectedType = returnType.Substring(positionOfApostropheOne, positionOfGreaterThan-positionOfApostropheOne);
                var collectedTypeWithoutNamespace = RemoveNamespaceFromType(collectedType,  false);

                return string.Concat(collectionTypeWithoutNamespace, "<", collectedTypeWithoutNamespace, ">");

                //return collectionTypeWithoutNamespace + "<" + collectedTypeWithoutNamespace + ">";
            }
            else if (returnType.Contains("<") && returnType.Contains(">"))
            {
                var positionOfOpenBracket = returnType.IndexOf("<") + 1;
                var collectionTypeWithNamespace = returnType.Substring(0, positionOfOpenBracket - 1);
                var collectionTypeWithoutNamespace = collectionTypeWithNamespace.Substring(collectionTypeWithNamespace.LastIndexOf(".") + 1);
                var positionOfCloseBracket = returnType.LastIndexOf(">");
                var collectedType = returnType.Substring(positionOfOpenBracket, positionOfCloseBracket - positionOfOpenBracket);
                var collectedTypeWithoutNamespace = RemoveNamespaceFromType(collectedType, false);

                return string.Concat(collectionTypeWithoutNamespace, "<", collectedTypeWithoutNamespace, ">");
               // return collectionTypeWithoutNamespace + "<" + collectedTypeWithoutNamespace + ">";
            }
            else 
            {
                var typeWithoutNamespace = returnType.Substring(returnType.LastIndexOf(".") + 1);
                if (isReturnType)
                {
                    typeWithoutNamespace = typeWithoutNamespace//.Replace("Void", "void")
                                                                .Replace("Boolean", "bool")
                                                                .Replace("Int32", "int");

                }
                return typeWithoutNamespace;
            }

            return null;
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



        private IProjectContent AddFileToProject(IProjectContent project, SyntaxTree syntaxTree)
        {
            CecilLoader loader = new CecilLoader();
            System.Reflection.Assembly[] assembliesToLoad = {
               
                typeof(TestCaseAttribute).Assembly
               };

            IUnresolvedAssembly[] projectAssemblies = new IUnresolvedAssembly[assembliesToLoad.Length];
            for(int i = 0; i < assembliesToLoad.Length; i++)
            {
                projectAssemblies[i] = loader.LoadAssemblyFile(assembliesToLoad[i].Location);
            }

      
            project = project.AddAssemblyReferences(projectAssemblies);

            CSharpUnresolvedFile unresolvedFile = syntaxTree.ToTypeSystem();

            if (syntaxTree.Errors.Count == 0)
            {
                project = project.AddOrUpdateFiles(unresolvedFile);
            }
            return project;
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
                _log.ErrorFormat("Could not find file to GetSyntaxTree, Name: {0}", fileName);
            }

            SyntaxTree syntaxTree = new CSharpParser().Parse(code, fileName);
            return syntaxTree;
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

                                                coveredLine.TestMethods.Add(new Poco.TestMethod
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