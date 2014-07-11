using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;
using Leem.Testify.Model;
using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using log4net;
using Leem.Testify.Poco;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace Leem.Testify
{
    
    public  class CoverageService : ICoverageService
    {
        private IList<CodeElement> _classes;
        private IList<CodeElement> _methods;
        private ITextDocument _document;
        private string _solutionDirectory;
        private static CoverageService instance;
        private ILog Log = LogManager.GetLogger(typeof(CoverageService));
        private DTE _dte; 
        private string _solutionName;
        private ITestifyQueries _queries;


        public static CoverageService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CoverageService();

                    instance._classes = new List<CodeElement>();

                    instance._methods = new List<CodeElement>();
                }

                return instance;
            }
        }


        public DTE DTE { set { _dte = value; } }

        public ITestifyQueries Queries { get; set; }

        public string SolutionName
        {
            get { return _solutionName; }
            set
            {
                _solutionName = value;

                _solutionDirectory = System.IO.Path.GetDirectoryName(_solutionName);
            }
        }
        public ITextDocument Document { get { return _document; } set { _document = value; } }

        public List<LineCoverageInfo> CoveredLines { get; set; }

        public IList<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage, string projectAssemblyName)
        {

            var coveredLines = new List<LineCoverageInfo>();

            var sessionModules = codeCoverage.Modules;

            Log.DebugFormat("GetCoveredLines for project: {0}", projectAssemblyName);
            Log.DebugFormat("Summary.NumSequencePoints: {0}", codeCoverage.Summary.NumSequencePoints);
            Log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
            Log.DebugFormat("Summary.VisitedSequencePoints: {0}", codeCoverage.Summary.VisitedSequencePoints);
            Log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
            Log.DebugFormat("Number of Modules: {0}", sessionModules.Count());

            foreach (var sessionModule in sessionModules)
            {
                Log.DebugFormat("Module Name: {0}", sessionModule.ModuleName);
            }

            var module = sessionModules.FirstOrDefault(x => x.ModuleName.Equals(projectAssemblyName));

            var tests = sessionModules.Where(x => x.TrackedMethods.Count() > 0).SelectMany(y => y.TrackedMethods);
            
            if (module != null)
            {
                var classes = module.Classes;

                Log.DebugFormat("First Module Name: {0}", module.ModuleName);
                Log.DebugFormat("Number of Classes: {0}", classes.Count());

                foreach (var codeClass in classes)
                {
                    var methods = codeClass.Methods;

                    foreach (var method in methods)
                    {
                        if (!method.Name.Contains("__") && !method.IsGetter && !method.IsSetter)
                        {
                            
                            var fileNames = new List<Leem.Testify.Model.File>();
                            if (method.FileRef != null)
                            {
                                fileNames = module.Files.Where(x => x.UniqueId == method.FileRef.UniqueId).ToList();
                            }
                            
                            var fileName = string.Empty;
                            if (fileNames.Any())
                            { 
                                fileName = fileNames.FirstOrDefault().FullPath;


                                // remove closing paren
                               // modifiedMethodName = modifiedMethodName.Substring(0, modifiedMethodName.Length - 1);
                                // Raw: System.Void Quad.QuadMed.WebPortal.Domain.App_LocalResources.AssessmentQuestions::.ctor()
                                // modified:        Quad.QuadMed.WebPortal.Domain.App_LocalResources.AssessmentQuestions..ctor
                                //Needed:           Quad.QuadMed.WebPortal.Domain.App_LocalResources.AssessmentQuestions..ctor



                                UpdateMethodLocation(method, fileName);
                                ProcessSequencePoints(coveredLines, module, tests, codeClass, method, fileName);
                            }
                           
                            
                        }

                        //else
                        //{
                        //    Log.DebugFormat("Skipping  Class: {0}   Method {1}: ",codeClass.FullName, method.Name);
                        //}
                        
                    }
                    
                }
            }

            return coveredLines;
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

                var testMethodName = trackedMethodName.Substring(locationOfSpace);

                testMethodName = testMethodName.Replace("::", ".");

                return testMethodName;
            }

        }
        private void ProcessSequencePoints(List<LineCoverageInfo> coveredLines, Module module, IEnumerable<Model.TrackedMethod> tests, Class modelClass, Method method,string fileName)
        {


            var sequencePoints = method.SequencePoints;
            foreach (var sequencePoint in sequencePoints)
            {
                var coveredLine = new LineCoverageInfo
                {
                    IsCode = true,
                    LineNumber = sequencePoint.StartLine,
                    IsCovered = (sequencePoint.VisitCount > 0),
                    ModuleName = module.ModuleName,
                    ClassName = modelClass.FullName,
                    MethodName = method.Name,
                    FileName = fileName,
                    UnitTests = new List<UnitTest>()
                };

                if (tests.Any())
                {
                    var coveringTests = new Poco.TrackedMethod();

                    foreach (var trackedMethodRef in sequencePoint.TrackedMethodRefs)
                    {
                        var trackedMethod = tests.FirstOrDefault(y => y.UniqueId.Equals(trackedMethodRef.UniqueId));

                        coveredLine.IsCode = true;

                        coveredLine.IsCovered = (sequencePoint.VisitCount > 0);
                        coveredLine.FileName = fileName;
                        coveredLine.TrackedMethods.Add(new Poco.TrackedMethod
                        {
                            UniqueId = (int)trackedMethod.UniqueId,
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

        public void UpdateMethodLocation(Method codeMethod, string fileName)
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
                                   Log.DebugFormat("Method Parameter Type: " + typeName + " should equal " + parameters[i]);

                                }
                                if (parametersMatch)
                                {
                                    Queries.UpdateCodeMethodPath(rawMethodName, fileName, method.BodyRegion.BeginLine, method.BodyRegion.BeginColumn);
                                    break;
                                }

                            }

                      
                        }
                    }
                    else if(methods.Count() == 1)
                    {
                        Queries.UpdateCodeMethodPath(rawMethodName, fileName, methods.First().BodyRegion.BeginLine, methods.First().BodyRegion.BeginColumn);
                    }
              
                }

            }

        }

        internal List<string> ParseArguments(string modifiedMethodName)
        {
            var result = new List<string>();
            try
            {
                Log.DebugFormat("Method = {0}", modifiedMethodName);
                int locationOfParen = modifiedMethodName.IndexOf('(') + 1;
                string argumentString;
                if (locationOfParen > -1)
                {
                    argumentString = modifiedMethodName.Substring(locationOfParen, modifiedMethodName.Length - locationOfParen - 1);
                }
                else 
                {
                    argumentString = modifiedMethodName;
                }
                int locationOfOpenAngleBracket = modifiedMethodName.IndexOf('<') + 1;

                var arguments = argumentString.Split(',');
                foreach(var argument in arguments)
                {
                    string name = RemoveNamespaces(argument);

                    name =  name.Replace("XmlNode", "System.Xml.XmlNode")
                                .Replace("Int32", "int")
                                .Replace("DataSet", "System.Data.DataSet")
                                .Replace("Exception", "System.Exception")
                                .Replace("System.Collections.Generic.List`1", "List")
                                .Replace("System.Collections.Generic.IList`1", "IList")
                                .Replace("System.Collections.Generic.IEnumerable`1", "IEnumerable")
                                .Replace("System.Func`1", "Func")
                                .Replace("System.Func", "Func")
                                .Replace("System.Nullable`1", "Nullable")
                                .Replace("System.String", "String")
                                .Replace("System.Guid", "Guid")
                                .Replace("System.Object", "Object")
                                .Replace("Nullable<Boolean>", "System.Nullable[[bool]]")
                                .Replace("Nullable<int>", "System.Nullable[[int]]")
                                .Replace("Nullable<Double>", "System.Nullable[[Double]]")
                                

                                ;
                    
                    result.Add(name);
                }

                var parsedMethodName = modifiedMethodName.Substring(0, locationOfParen ) + string.Join(",", result) + ")";

            }
            catch(Exception ex)
            {
                Log.DebugFormat("Error Parsing Arguments:{0}", ex);
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


                var classPlusNamespace = argument.Substring(positionOfFirstOpeningAngle + 1, positionOfFirstClosingAngle - positionOfFirstOpeningAngle);
                var classOnly = RemoveNamespaceFromMethod(classPlusNamespace);
                argument = argument.Replace(classPlusNamespace, classOnly);

                // method
                if(positionOfFirstOpeningParen > -1)
                {
                    var methodName = argument.Substring(0, positionOfFirstOpeningParen);
                    var methodMinusNamespace = RemoveNamespaceFromMethod(methodName);
                    argument = argument.Replace(methodName, methodMinusNamespace);
                }

                
               
            }
            else if (positionOfFirstOpeningAngle > positionOfLastOpeningAngle && positionOfLastOpeningAngle > positionOfFirstClosingAngle)
            {
                // Nested
            }
            else
            {
                // Multiple
            }
            return argument;

        }

        private string RemoveNamespaceFromMethod(string argumentPart)
        {

            int locationOfPeriod = argumentPart.LastIndexOf('.');

            var name = argumentPart.Substring(locationOfPeriod + 1);
            if (name.Substring(name.Length - 1) == ")")
            {
                name = name.ReplaceAt(name.Length - 1, string.Empty);
            }
            return name;
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

        public List<LineCoverageInfo> GetRetestedLinesFromCoverageSession(CoverageSession coverageSession, string projectAssemblyName, List<int> uniqueIds)
        {

            /// todo need to figure out how to remove a CoveredLine if an Edit has made it uncovered, 
            /// without removing all lines that were not covered by the subset of tests that were run
            /// One option: check all previously covered lines from this group of tests with the current covered lines list from this group.
            /// 
            //todo Handle case where a line that was "Code" and was covered is now not "Code"
            var coveredLines = new List<LineCoverageInfo>();
            
            var sessionModules = coverageSession.Modules;
            var module = sessionModules.FirstOrDefault(x => x.ModuleName.Equals(projectAssemblyName));
            var tests = sessionModules.Where(x => x.TrackedMethods.Count() > 0).SelectMany(y => y.TrackedMethods);

            if (module != null)
            {
                var codeModule = new Poco.CodeModule
                {
                    Name = module.FullName,
                    Summary = new Poco.Summary(module.Summary)
                };

                var classes = module.Classes;

                Log.DebugFormat("First Module Name: {0}", module.ModuleName);
                Log.DebugFormat("Number of Classes: {0}", classes.Count());

                foreach (var moduleClass in classes)
                {
                    if (!moduleClass.FullName.Contains("_"))
                    {
                        var codeClass = new Poco.CodeClass
                        {
                            Name = moduleClass.FullName,
                            Summary = new Poco.Summary(moduleClass.Summary)
                        };

                        var methods = moduleClass.Methods;

                        foreach (var method in methods)
                        {
                            if (!method.Name.Contains("_")
                                && !method.Name.StartsWith("get_")
                                && !method.Name.StartsWith("set_"))
                            {
                                var codeMethod = new Poco.CodeMethod
                                {
                                    Name = method.Name,
                                    Summary = new Poco.Summary(method.Summary)
                                };

                                var sequencePoints = method.SequencePoints;
                                foreach (var sequencePoint in sequencePoints)
                                {
                                    if (tests.Any())
                                    {
                                        var coveringTests = new List<Poco.TrackedMethod>();

                                        foreach (var trackedMethodRef in sequencePoint.TrackedMethodRefs)
                                        {
                                            var coveredLine = new LineCoverageInfo
                                            {
                                                IsCode = true,
                                                LineNumber = sequencePoint.StartLine,
                                                IsCovered = (sequencePoint.VisitCount > 0),
                                                Module = codeModule,
                                                Class = codeClass,
                                                Method = codeMethod
                                            };

                                            var testsThatCoverLine = tests.Where(y => y.UniqueId.Equals(trackedMethodRef.UniqueId));

                                            foreach (var test in testsThatCoverLine)
                                            {
                                                coveredLine.IsCode = true;

                                                coveredLine.IsCovered = (sequencePoint.VisitCount > 0);

                                                coveredLine.TrackedMethods.Add(new Poco.TrackedMethod
                                                {
                                                    UniqueId = (int)test.UniqueId,
                                                    MetadataToken = method.MetadataToken,
                                                    Strategy = test.Strategy,
                                                    Name = test.Name
                                                });
                                            }

                                            coveredLines.Add(coveredLine);
                                        }

                                    }

                                }
                            }

                        } 
                    }
                }
            }

            return coveredLines;
        }
    }
}
