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

        public List<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage, ProjectInfo projectInfo,
                                                                        List<TrackedMethodMap> methodMapper, TestifyContext context)
        {
            var coveredLines = new List<LineCoverageInfo>();
            var projectAssemblyName = string.Empty;
            List<Module> sessionModules = codeCoverage.Modules;

            _log.DebugFormat("GetCoveredLines for project: {0}", projectInfo.ProjectAssemblyName);
            _log.DebugFormat("Summary.NumSequencePoints: {0}", codeCoverage.Summary.NumSequencePoints);
            _log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
            _log.DebugFormat("Summary.VisitedSequencePoints: {0}", codeCoverage.Summary.VisitedSequencePoints);
            _log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
            _log.DebugFormat("Number of Modules: {0}", sessionModules.Count());

            foreach (Module sessionModule in sessionModules)
            {
                _log.DebugFormat("Module Name: {0}", sessionModule.ModuleName);
                if(sessionModule.ModuleName.EndsWith(".Test"))
                {
                    projectAssemblyName = projectInfo.TestProject.AssemblyName;
                }
                else
                {
                    projectAssemblyName = projectInfo.ProjectAssemblyName;
                }
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

                        // Checking for FileName is not null, prevents us from adding methods that have no covered lines like empty constructors
                        var methodsFromClasses = classesFromModule.SelectMany(y => y.Methods).ToList();

                        var uniqueMethodsFromClasses = methodsFromClasses.GroupBy(x => x.Name).Select(y => y.FirstOrDefault()).ToList();
                        uniqueMethodsFromClasses.ForEach(x => x.Name = RemoveNamespaces(x.Name));
                        var codeMethodDictionary = new Dictionary<string,Leem.Testify.Poco.CodeMethod>();
                        foreach (var method in uniqueMethodsFromClasses)
                        {
                           

                            try
                            {
                                codeMethodDictionary.Add(method.Name, method);
                                //var codeMethodDictionary = uniqueMethodsFromClasses.ToDictionary(item => string.Concat(item.CodeClass.Name, ".", item.Name));
                            }
                            catch (Exception ex) 
                            {
                                _log.ErrorFormat("Error creating CodeMethodDictionary Class: {0}, Method: {1}", method.CodeClass.Name, method.Name);
                        }
                        }
                    //var codeMethodDictionary = uniqueMethodsFromClasses.ToDictionary(item => string.Concat(item.CodeClass.Name, ".", item.Name));

                    var typeDefinitions = GetTypeDefinitionsFromRefactory(sessionModule.AssemblyName, fileDictionary);

                    foreach (var codeClass in classes)
                    {

                        _log.DebugFormat("Class Name: {0}", codeClass.FullName);
                        List<Method> methods = codeClass.Methods;

                        foreach (var method in methods.Where(x=>x.FileRef != null).ToList())
                        {
                            method.Name = RemoveNamespaces(method.Name);
                            //if ( !method.IsGetter && !method.IsSetter)
                            //{
                                var indexOfOpEquality = method.Name.IndexOf("::op_Equality(");
      
                                if (indexOfOpEquality>0)
                                {
                                    method.Name = method.Name.Substring(0,indexOfOpEquality);
                                     method.Name = string.Concat(method.Name , "::Equals(Object)");
                                }
                                method.Name = method.Name.Replace("&", string.Empty);
                                //var indexOfOpInEquality = method.Name.IndexOf("::op_Inequality(");
                                //if ( indexOfOpInEquality > 0)
                                //{
                                //    method.Name = method.Name.Substring(0, indexOfOpInEquality);
                                //    method.Name = string.Concat(method.Name, "::Equals(Object)");
                                //}

                                string fileName = string.Empty;
                                if (method.FileRef != null)
                                {
                                    fileName = fileDictionary[method.FileRef.UniqueId].FullPath;
                                }

                               
                                if (fileName != string.Empty)
                                {

                                    if (fileName.Contains(@"\WebServices\") == false
                                        && fileName.Contains(@"\Web References\") == false
                                        && fileName.Contains(@"\Service References") == false)
                                    {
                                       // var methodNameWithoutNamespaces = RemoveNamespaces(method.Name);
                                        if (method.Name.Contains("`1"))
                                        {
                                            var returnType = method.Name.Substring(0, method.Name.IndexOf(" "));
                                            var modifiedReturnType = CoverageService.Instance.RemoveNamespaceFromType(returnType, isReturnType: true);
                                            method.Name = method.Name.Replace(returnType, modifiedReturnType);
                                        }

                                       // var trackedMethodUnitTestMap = methodMapper.FirstOrDefault(x => methodNameWithoutNamespaces.EndsWith(x.TrackedMethodNameWithoutNamespaces));
                                        var trackedMethodUnitTestMap = methodMapper.FirstOrDefault(x => method.Name.Equals(x.MethodName));
                                       
                                        if (trackedMethodUnitTestMap != null)
                                        {
                                            trackedMethodUnitTestMap.MethodName = method.Name;
                                        }

                                        if (trackedMethodUnitTestMap == null && method.Name.Contains(".ctor") == false && method.Name.Contains(".cctor") == false)
                                        {
                                            _log.ErrorFormat("Did not find Map object for: <{0}>", method.Name);
                                        }

                                        CodeMethodInfo methodInfo = UpdateMethodLocation(method, fileName, trackedMethodUnitTestMap, typeDefinitions);

                                        Queries.UpdateCodeMethodPath(context, methodInfo, codeMethodDictionary);
                                        //method.Name = methodNameWithoutNamespaces;
                                        ProcessSequencePoints(coveredLines, sessionModule, tests, codeClass, method, fileName, trackedMethodUnitTestMap);

                                    }

                                    
                                }
                           // }


                        }

                       context.SaveChanges();//12.4%
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

        public List<string> UpdateMethodsAndClassesFromCodeFile(List<Module> modules,List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            var changedClasses = new List<string>();

            

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

                                changedClasses.AddRange(UpdateMethods(typeDef, methods, typeDef.UnresolvedFile.FileName, trackedMethodUnitTestMapper));
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
            return changedClasses;
        }

        public List<string> UpdateMethods(IUnresolvedTypeDefinition fileClass, IEnumerable<IUnresolvedMethod> methods, string fileName, List<TrackedMethodMap> trackedMethodUnitTestMapper)
        {
            //todo remove trackedmethodmapper from arguments 
            var methodsToDelete = new List<string>();
            var changedClasses = new List<string>();
            using (var context = new TestifyContext(_solutionName))
            {
                var codeClasses = from clas in context.CodeClass
                                  join method in context.CodeMethod on clas.CodeClassId equals method.CodeClassId
                                  where clas.Name.Equals(fileClass.UnresolvedFile.FileName)
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
                        changedClasses.Add(codeClass.Name);
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
                    }

                    if (modifiedMethodName.EndsWith(")"))
                    {
                        // remove closing paren
                        modifiedMethodName = modifiedMethodName.Substring(0, modifiedMethodName.Length - 1);
                    }

// Can we use equals instead of contains, this 4.0%
                    var codeMethods = (from clas in codeClasses
                                      join method in context.CodeMethod on clas.CodeClassId equals method.CodeClassId
                                      where method.Name.Contains(modifiedMethodName)
                                      select method).ToList();
                    codeMethods = context.CodeMethod.Where(x => x.Name == modifiedMethodName).ToList();
                    foreach (var method in codeMethods)
                    {
                        if (method.FileName != fileName
                           || method.Line != fileMethod.BodyRegion.BeginLine
                           || method.Column != fileMethod.BodyRegion.BeginColumn)
                        {
                            _log.DebugFormat("UpdateMethods - Modifing {0}", method.Name);
                            method.FileName = fileName;
                            method.Line = fileMethod.BodyRegion.BeginLine;
                            method.Column = fileMethod.BodyRegion.BeginColumn;
                            changedClasses.Add(method.CodeClass.Name);
                        }

                    }


                }
                var hasChanges = context.ChangeTracker.HasChanges();
                if (hasChanges)
                {
                    _log.DebugFormat("UpdateMethods - Changes were made = {0} in FileName {1}", hasChanges, fileName);
                }
               

                context.SaveChanges();
            }
            return changedClasses;
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
                var branchPointForThisLine = branchPoints.Where(x => x.StartLine == sequencePoint.StartLine).ToList();
                decimal branchCoverage = 0;
                if (branchPointForThisLine.Any(x => x.VisitCount > 0)) 
                {
                    branchCoverage = ((decimal)branchPointForThisLine.Count(x => x.VisitCount > 0) / (decimal)branchPointForThisLine.Count) * 100;
                    _log.DebugFormat("Method :{0}, LineNumber {1}, branchCoverage: {2}%", method.Name, sequencePoint.StartLine, branchCoverage.ToString("G"));
                }
                
               
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
                    IsBranch = branchPointForThisLine.Any(x => x.VisitCount > 0),
                    BranchCoverage= branchCoverage
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

        public CodeMethodInfo UpdateMethodLocation(Method codeMethod, string fileName, TrackedMethodMap methodMapper,IEnumerable<IUnresolvedTypeDefinition> typeDefinitions)
        {

         
            try
            {
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
                               // codeMethodInfo.RawMethodName = string.Concat(typeDef.ReflectionName, ".", codeMethod.Name);
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

        private IEnumerable<IUnresolvedTypeDefinition> GetTypeDefinitionsFromRefactory(string assemblyName,Dictionary<uint,Testify.Model.File> fileDictionary)
        {
            IProjectContent project = new CSharpProjectContent();
            project.SetAssemblyName(assemblyName);
            foreach (var file in fileDictionary)
            {
                var syntaxTree = GetSyntaxTree(file.Value.FullPath);
                project = AddFileToProject(project, syntaxTree);
            }


            var typeDefinitions = project.TopLevelTypeDefinitions;
            return typeDefinitions;
        }

     

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

                if (locationOfCloseParen - locationOfOpenParen > 1)
                {
                    var argumentString = methodNameWithArgsAndReturnType.Substring(methodNameWithArgsAndReturnType.IndexOf("(") + 1, methodNameWithArgsAndReturnType.IndexOf(")") - 1 - methodNameWithArgsAndReturnType.IndexOf("("));
                    if (locationOfOpenParen > 0)
                    {
                        baseMethodName = baseMethodName.Substring(0, baseMethodName.IndexOf("("));
                    }


                    // todo need to parse arguments to see if the argument contains a Lync expression like "System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>>"
                    // failing Method = System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>> Quad.QuadMed.QMedClinicalTools.Domain.Services.Util.PredicateBuilder::Or(System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>>,System.Linq.Expressions.Expression`1<System.Func`2<T,System.Boolean>>)

                    argumentStringWithoutNamespaces =  "(" + ParseArguments(argumentString) + ")";

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

        }

        private string ParseArguments(string argumentString)
        {
            string argumentStringWithoutNamespaces = string.Empty;
            string[] argumentsWithoutNamespaces=null ;
            const string _Comma_ = "#Comma#";

            var arguments = GetArguments(argumentString);

            var results = new List<string>();
            foreach (var argument in arguments)
            {
                results.Add( RemoveNamespaceFromType(argument, isReturnType: false));
            }

            argumentStringWithoutNamespaces = string.Join(",", results);

            return argumentStringWithoutNamespaces;
        }

        private List<string> GetArguments(string argumentString)
        {
            int level = 0;
            int lastCommaPosition = 0;
            var argumentCharArray = argumentString.ToCharArray();
            var argumentList = new List<string>();
            for (int i = 0; i < argumentCharArray.Length; i++)
            {

                if (level == 0 && ((argumentCharArray[i] == ',') ))
                {

                    var segment = new ArraySegment<char>(argumentCharArray, lastCommaPosition, i - lastCommaPosition );

                    argumentList.Add(new string(segment.ToArray()));
                    lastCommaPosition = i + 1;
                }
                else if (argumentCharArray[i] == '<')
                {
                     --level;
                }
                else if (argumentCharArray[i] == '>')
                {
                    level++;
                }

                if (i == argumentCharArray.Length - 1)
                {
                    var segment = new ArraySegment<char>(argumentCharArray, lastCommaPosition, argumentCharArray.Length - lastCommaPosition);
                    argumentList.Add(new string(segment.ToArray()));
                    level = 0;
                }
            }
            return argumentList;

        }

        public string RemoveNamespaceFromType(string returnType, bool isReturnType)
        {

             if (returnType.Contains("<") && returnType.Contains(">"))
            {
                var positionOfOpenBracket = returnType.IndexOf("<") + 1;
                var collectionTypeWithNamespace = returnType.Substring(0, positionOfOpenBracket - 1);
                var collectionTypeWithoutNamespace = collectionTypeWithNamespace.Substring(collectionTypeWithNamespace.LastIndexOf(".") + 1);
                collectionTypeWithoutNamespace = ConvertNameToCSharpName(collectionTypeWithoutNamespace);
                collectionTypeWithoutNamespace = collectionTypeWithoutNamespace.Replace("`2", string.Empty);
                var positionOfCloseBracket = returnType.LastIndexOf(">");
                var collectedTypes = returnType.Substring(positionOfOpenBracket, positionOfCloseBracket - positionOfOpenBracket);
                var collectedTypesSplitByComa = collectedTypes.Split(',');
                var collectedTypeWithoutNamespace = new string[collectedTypesSplitByComa.Length];
                int i=0;

                foreach (var collectedType in collectedTypesSplitByComa)
                {
                    collectedTypeWithoutNamespace[i] =  RemoveNamespaceFromType(collectedType, false);
                    i++;
                }

                var reconstructedCollectedTypes=string.Join(",", collectedTypeWithoutNamespace);

                return string.Concat(collectionTypeWithoutNamespace, "<", reconstructedCollectedTypes, ">");

            }
            else 
            {
                var typeWithoutNamespace = returnType.Substring(returnType.LastIndexOf(".") + 1);
                typeWithoutNamespace = ConvertNameToCSharpName(typeWithoutNamespace);

                return typeWithoutNamespace;
            }

            return null;
        }

        private List<string> ConvertNamesToCSharpNames(List<string> typeNames)
        {
            var result = new List<string>();
            foreach(var typeName in typeNames)
            {
                result.Add(ConvertNameToCSharpName(typeName));
            }

            return result;
        }
        public  string ConvertNameToCSharpName(string typeName)
        {

            if (typeName == KnownTypeCode.Boolean.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Boolean);

            if (typeName == KnownTypeCode.Byte.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Byte);

            if (typeName == KnownTypeCode.Char.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Byte);

            if (typeName == KnownTypeCode.Decimal.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Decimal);

            if (typeName == KnownTypeCode.Double.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Double);

            if (typeName == KnownTypeCode.Int16.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Int16);

            if (typeName == KnownTypeCode.Int32.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Int32);

            if (typeName == KnownTypeCode.Int64.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Int64);

            if (typeName == KnownTypeCode.Object.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Object);

            if (typeName == KnownTypeCode.SByte.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.SByte);

            if (typeName == KnownTypeCode.Single.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Single);

            if (typeName == KnownTypeCode.String.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.String);

            if (typeName == KnownTypeCode.UInt16.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.UInt16);

            if (typeName == KnownTypeCode.UInt32.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.UInt32);

            if (typeName == KnownTypeCode.UInt64.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.UInt64);

            if (typeName == KnownTypeCode.Void.ToString())
                return KnownTypeReference.GetCSharpNameByTypeCode(KnownTypeCode.Void);
            if (typeName.Contains("`"))
                return typeName.Substring(0,typeName.IndexOf("`"));
            if (typeName.Contains("Byte[]"))
                return "byte[]";
            if (typeName.Contains("System.Nullable[[") && typeName.Contains("]]"))
            {
                return typeName.Replace("System.Nullable[[", "Nullable<")
                                .Replace("]]", ">");
            }

            return typeName;
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