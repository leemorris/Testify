using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;
using Leem.Testify.Domain.Model;
using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Leem.Testify.Domain.DaoInterfaces;
using StructureMap;
using log4net;

namespace Leem.Testify.Domain
{
    public class CoverageService : ICoverageService
    {
        private IList<CodeElement> _classes;
        private IList<CodeElement> _methods;
        private ITextDocument _document;
        private string _solutionDirectory;
        private static CoverageService instance;
        private ILog Log = LogManager.GetLogger(typeof(CoverageService));
        private DTE _dte; 
        private string _solutionName;

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

        public IList<CoveredLine> GetCoveredLinesForClass(string className)
        {
            if (Queries == null)
            {
                Log.DebugFormat("ERROR TestifyQueries is null");
            }
            return Queries.GetCoveredLines(className);
        }

        public IList<LineCoverageInfo> GetCoveredLinesFromCoverageSession(CoverageSession codeCoverage, string projectAssemblyName)
        {
            Log.DebugFormat("GetCoveredLines for project: {0}", projectAssemblyName);
            Log.DebugFormat("Summary.NumSequencePoints: {0}", codeCoverage.Summary.NumSequencePoints);
            Log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
            Log.DebugFormat("Summary.VisitedSequencePoints: {0}", codeCoverage.Summary.VisitedSequencePoints);
            Log.DebugFormat("Summary.SequenceCoverage: {0}", codeCoverage.Summary.SequenceCoverage);
          
            var coveredLines = new List<LineCoverageInfo>();

            var sessionModules = codeCoverage.Modules;
            Log.DebugFormat("Number of Modules: {0}", sessionModules.Count());
            foreach (var sessionModule in sessionModules)
            {
                Log.DebugFormat("Module Name: {0}", sessionModule.ModuleName);
            }
            var module = sessionModules.FirstOrDefault(x => x.ModuleName.Equals(projectAssemblyName));

            var tests = sessionModules.Where(x => x.TrackedMethods.Count() > 0).SelectMany(y => y.TrackedMethods);
            
            if (module != null)
            {
                Log.DebugFormat("First Module Name: {0}", module.ModuleName);
                var classes = module.Classes;
                Log.DebugFormat("Number of Classes: {0}", classes.Count());
                foreach (var codeClass in classes)
                {
                    //Log.DebugFormat("Class: {0}", codeClass.FullName);
                    var methods = codeClass.Methods;
                    foreach (var method in methods)
                    {
                        ProcessSequencePoints(coveredLines, module, tests, codeClass, method);
                    }
                }
            }
            return coveredLines;
        }

        private static void ProcessSequencePoints(List<LineCoverageInfo> coveredLines, Module module, IEnumerable<Model.TrackedMethod> tests, Class codeClass, Method method)
        {
            //Log.DebugFormat("Class: {0}", method.Name);
            var sequencePoints = method.SequencePoints;
            foreach (var sequencePoint in sequencePoints)
            {
                var coveredLine = new LineCoverageInfo
                {
                    IsCode = true,
                    LineNumber = sequencePoint.StartLine,
                    IsCovered = (sequencePoint.VisitCount > 0),
                    Module = module.FullName,
                    Class = codeClass.FullName,
                    Method = method.Name
                };
                if (tests.Any())
                {
                    var coveringTests = new List<TrackedMethod>();
                    foreach (var trackedMethodRef in sequencePoint.TrackedMethodRefs)
                    {
                        var testsThatCoverLine = tests.Where(y => y.UniqueId.Equals(trackedMethodRef.UniqueId));
                        foreach (var test in testsThatCoverLine)
                        {
                            coveredLine.IsCode = true;
                            coveredLine.IsCovered = (sequencePoint.VisitCount > 0);
                            coveredLine.CoveringTest = new TrackedMethod { UniqueId = (int)test.UniqueId, 
                                                                            UnitTestId = test.UnitTestId, 
                                                                            Strategy = test.Strategy, 
                                                                            Name = test.Name,
                                                                            MetadataToken = test.MetadataToken };
                        }
                    }

                }
                coveredLines.Add(coveredLine);
            }
        }




        public object GetRetestedLinesFromCoverageSession(CoverageSession coverageSession, string projectAssemblyName, int metadataToken)
        {
            var coveredLines = new List<LineCoverageInfo>();
            
            var sessionModules = coverageSession.Modules;
            Log.DebugFormat("Number of Modules: {0}", sessionModules.Count());
            foreach (var sessionModule in sessionModules)
            {
                Log.DebugFormat("Module Name: {0}", sessionModule.ModuleName);
            }
            var module = sessionModules.FirstOrDefault(x => x.ModuleName.Equals(projectAssemblyName));

            var tests = sessionModules.Where(x => x.TrackedMethods.Count() > 0).SelectMany(y => y.TrackedMethods);

            if (module != null)
            {
     
                Log.DebugFormat("First Module Name: {0}", module.ModuleName);
                var classes = module.Classes;
                Log.DebugFormat("Number of Classes: {0}", classes.Count());
                foreach (var codeClass in classes)
                {
                    //Log.DebugFormat("Class: {0}", codeClass.FullName);
                    var methods = codeClass.Methods;
                    foreach (var method in methods)
                    {
                        if (method.MetadataToken.Equals(metadataToken))
                        {
                            //Log.DebugFormat("Class: {0}", method.Name);
                            var sequencePoints = method.SequencePoints;
                            foreach (var sequencePoint in sequencePoints)
                            {
                                var coveredLine = new LineCoverageInfo
                                {
                                    IsCode = true,
                                    LineNumber = sequencePoint.StartLine,
                                    IsCovered = (sequencePoint.VisitCount > 0),
                                    Module = module.FullName,
                                    Class = codeClass.FullName,
                                    Method = method.Name
                                };
                                if (tests.Any())
                                {
                                    var coveringTests = new List<TrackedMethod>();
                                    foreach (var trackedMethodRef in sequencePoint.TrackedMethodRefs)
                                    {
                                        var testsThatCoverLine = tests.Where(y => y.UniqueId.Equals(trackedMethodRef.UniqueId));
                                        foreach (var test in testsThatCoverLine)
                                        {
                                            coveredLine.IsCode = true;
                                            coveredLine.IsCovered = (sequencePoint.VisitCount > 0);
                                            coveredLine.CoveringTest = new TrackedMethod { UniqueId = (int)test.UniqueId, 

                                                                                            Strategy = test.Strategy, 
                                                                                            Name = test.Name };
                                        }
                                    }

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
