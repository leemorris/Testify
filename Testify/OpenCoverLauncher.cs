using log4net;
using OpenCover.Framework;
using OpenCover.Framework.Manager;
using OpenCover.Framework.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Leem.Testify
{
    public class CoverageParameters
    {
        public string ProjectName { get; set; }
        public string TestProjectName { get; set; }
    }

    public class OpenCoverLauncher
    {
        public OpenCoverLauncher(string[] args)
        {
            var logger = LogManager.GetLogger(typeof(Bootstrapper));
            var returnCode = 0;

            //var args = new string[]{"",""};
            var parser = new CommandLineParser(args);
            try
            {
                parser.ExtractAndValidateArguments();
            }
            catch (Exception ex)
            {
                var x = 4;
            }

            string outputFile;
            IPerfCounters perfCounter = new NullPerfCounter();
            var persistance = new CoverageSessionPersistance(parser, logger);
            var filter = BuildFilter(parser);
            using (var container = new Bootstrapper(logger))
            {
                try
                {
                    container.Initialise(filter, parser, persistance, perfCounter);
                }
                catch (Exception ex)
                {
                    var x = 4;
                }
                //persistance.Initialise(outputFile, parser.MergeExistingOutputFile);
                var registered = false;

                try
                {
                    if (parser.Register)
                    {
                        ProfilerRegistration.Register(parser.Registration);
                        registered = true;
                    }
                    try { var harness = container.Resolve<IProfilerManager>(); }
                    catch (Exception ex)
                    {
                        int x = 6;
                    }

                    string serviceAccountName = GetServiceAccountName(parser.Target);
                    if (serviceAccountName.StartsWith(@".\"))
                    {
                        serviceAccountName = Environment.MachineName + serviceAccountName.Substring(1);
                    }
                    else if (serviceAccountName.ToLower().Contains("localsystem"))
                    {
                        serviceAccountName = "NT Authority\\SYSTEM";
                    }

                    var servicePrincipal =
                        (parser.Service
                            ? new[] { serviceAccountName }
                            : new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                    //harness.RunProcess(environment =>
                    //{
                    //    returnCode = 0;
                    //    //if (parser.Service)
                    //    //{
                    //    //    RunService(parser, environment, logger);
                    //    //}
                    //    //else
                    //    //{
                    //        returnCode = RunProcess(parser, environment);
                    //    //}
                    //}, servicePrincipal);

                    //DisplayResults(persistance, parser, logger);
                    //var communicationManager = new OpenCover.Framework.Communication.CommunicationManager(new OpenCover.Framework.Communication.MessageHandler());
                    //OpenCover.Framework.ICommandLine commandLine = null;
                    //var sessionPersistence = new CoverageSessionPersistance(commandLine, Log);
                    //var memoryManager = new MemoryManager();

                    //var perfCounters = new OpenCover.Framework.Utility.PerfCounters();
                    //var profilerManager = new OpenCover.Framework.Manager.ProfilerManager(communicationManager, sessionPersistence, memoryManager, commandLine, perfCounters);
                    ////var servicePrincipal = (parser.Service
                    ////                ? new[] { ServiceEnvironmentManagement.MachineQualifiedServiceAccountName(parser.Target) }
                    ////                : new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    //ar servicePrincipal =
                    //profilerManager.RunProcess(args, servicePrincipal);
                    //DisplayResults(persistance, parser, logger);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("Exception: {0}\n{1}", ex.Message, ex.InnerException));
                    throw;
                }
                finally
                {
                    if (parser.Register && registered)
                        ProfilerRegistration.Unregister(parser.Registration);
                }
            }
        }

        //private static void RunService(CommandLineParser parser, Action<StringDictionary> environment, ILog logger)
        //{
        //    var entry = GetServiceKey(parser.Target);
        //    var isServiceDisabled =  (int)entry.GetValue("Start") == 4;

        //    if (isServiceDisabled)
        //    {
        //        logger.ErrorFormat("The service '{0}' is disabled. Please enable the service.",
        //            parser.Target);
        //        return;
        //    }

        //    var service = new ServiceController(parser.Target);
        //    try
        //    {
        //        if (service.Status != ServiceControllerStatus.Stopped)
        //        {
        //            logger.ErrorFormat(
        //                "The service '{0}' is already running. The profiler cannot attach to an already running service.",
        //            parser.Target);
        //            return;
        //        }

        //        // now to set the environment variables
        //        var profilerEnvironment = new StringDictionary();
        //        environment(profilerEnvironment);

        //        //var serviceEnvironment = new ServiceEnvironmentManagement();

        //        try
        //        {
        //            serviceEnvironment.PrepareServiceEnvironment(
        //                parser.Target,
        //                    parser.ServiceEnvironment,
        //                (from string key in profilerEnvironment.Keys
        //                 select string.Format("{0}={1}", key, profilerEnvironment[key])).ToArray());

        //            // now start the service
        //            var old = service;
        //            service = new ServiceController(parser.Target);
        //            old.Dispose();

        //            if (parser.Target.ToLower().Equals("w3svc"))
        //            {
        //                // Service will not automatically start
        //                if (!TerminateCurrentW3SvcHost(logger) ||
        //                    !ServiceEnvironmentManagementEx.IsServiceStartAutomatic(parser.Target))
        //                {
        //                    service.Start();
        //                }
        //            }
        //            else
        //            {
        //                service.Start();
        //            }
        //            logger.InfoFormat("Service starting '{0}'", parser.Target);
        //            service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
        //            logger.InfoFormat("Service started '{0}'", parser.Target);
        //        }
        //        catch (InvalidOperationException fault)
        //        {
        //            logger.FatalFormat("Service launch failed with '{0}'", fault);
        //        }
        //        finally
        //        {
        //            // once the serice has started set the environment variables back - just in case
        //            serviceEnvironment.ResetServiceEnvironment();
        //        }

        //        // and wait for it to stop
        //        service.WaitForStatus(ServiceControllerStatus.Stopped);
        //        logger.InfoFormat("Service stopped '{0}'", parser.Target);

        //        // Stopping w3svc host
        //        if (parser.Target.ToLower().Equals("w3svc"))
        //        {
        //            logger.InfoFormat("Stopping svchost to clean up environment variables for w3svc", parser.Target);
        //            if (ServiceEnvironmentManagementEx.IsServiceStartAutomatic(parser.Target))
        //            {
        //                logger.InfoFormat("Please note that the 'w3svc' service may automatically start");
        //            }
        //            TerminateCurrentW3SvcHost(logger);
        //        }
        //    }
        //    finally
        //    {
        //        service.Dispose();
        //    }
        //}
        private static string GetServiceAccountName(string serviceName)
        {
            Microsoft.Win32.RegistryKey key = GetServiceKey(serviceName);
            if (key != null)
                return key.GetValue("ObjectName") as string;
            return null;
        }

        protected static Microsoft.Win32.RegistryKey GetServiceKey(string serviceName)
        {
            Microsoft.Win32.RegistryKey localMachine = Microsoft.Win32.Registry.LocalMachine;
            Microsoft.Win32.RegistryKey key = localMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + serviceName, true);
            return key;
        }

        private static int RunProcess(CommandLineParser parser, Action<StringDictionary> environment)
        {
            var returnCode = 0;

            var targetPathname = ResolveTargetPathname(parser);
            System.Console.WriteLine("Executing: {0}", Path.GetFullPath(targetPathname));

            var startInfo = new ProcessStartInfo(targetPathname);
            environment(startInfo.EnvironmentVariables);

            if (parser.OldStyleInstrumentation)
                startInfo.EnvironmentVariables[@"OpenCover_Profiler_Instrumentation"] = "oldSchool";

            startInfo.Arguments = parser.TargetArgs;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = parser.TargetDir;

            var process = Process.Start(startInfo);
            process.WaitForExit();

            if (parser.ReturnTargetCode)
                returnCode = process.ExitCode;
            return returnCode;
        }

        //private static void RunService(CommandLineParser parser, Action<StringDictionary> environment, ILog logger)
        //{
        //    if (OpenCover.Console.ServiceEnvironmentManagementEx.IsServiceDisabled(parser.Target))
        //    {
        //        logger.ErrorFormat("The service '{0}' is disabled. Please enable the service.",
        //            parser.Target);
        //        return;
        //    }

        //    var service = new ServiceController(parser.Target);

        //    if (service.Status != ServiceControllerStatus.Stopped)
        //    {
        //        logger.ErrorFormat("The service '{0}' is already running. The profiler cannot attach to an already running service.",
        //            parser.Target);
        //        return;
        //    }

        //    // now to set the environment variables
        //    var profilerEnvironment = new StringDictionary();
        //    environment(profilerEnvironment);

        //    var serviceEnvironment = new ServiceEnvironmentManagement();

        //    try
        //    {
        //        serviceEnvironment.PrepareServiceEnvironment(parser.Target,
        //            (from string key in profilerEnvironment.Keys select string.Format("{0}={1}", key, profilerEnvironment[key])).ToArray());

        //        // now start the service
        //        service = new ServiceController(parser.Target);
        //        service.Start();
        //        logger.InfoFormat("Service starting '{0}'", parser.Target);
        //        service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
        //        logger.InfoFormat("Service started '{0}'", parser.Target);
        //    }
        //    finally
        //    {
        //        // once the serice has started set the environment variables back - just in case
        //        serviceEnvironment.ResetServiceEnvironment();
        //    }

        //    // and wait for it to stop
        //    service.WaitForStatus(ServiceControllerStatus.Stopped);
        //    logger.InfoFormat("Service stopped '{0}'", parser.Target);
        //}

        public int LaunchCoverage(string[] args)
        {
            var returnCode = 0;
            var returnCodeOffset = 0;
            var logger = LogManager.GetLogger(typeof(Bootstrapper));

            IPerfCounters perfCounter = new NullPerfCounter();
            try
            {
                CommandLineParser parser;
                if (!ParseCommandLine(args, out parser)) return parser.ReturnCodeOffset + 1;

                returnCodeOffset = parser.ReturnCodeOffset;
                var filter = BuildFilter(parser);

                using (var container = new Bootstrapper(logger))
                {
                    var persistance = new CoverageSessionPersistance(parser, logger);
                    container.Initialise(filter, parser, persistance, perfCounter);
                    //persistance.Initialise(outputFile);
                    var registered = false;

                    try
                    {
                        if (parser.Register)
                        {
                            ProfilerRegistration.Register(parser.Registration);
                            registered = true;
                        }
                        var harness = container.Resolve<IProfilerManager>();

                        //var servicePrincipal =
                        //    (parser.Service
                        //        ? new[] { ServiceEnvironmentManagement.MachineQualifiedServiceAccountName(parser.Target) }
                        //        : new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                        //harness.RunProcess((environment) =>
                        //                       {
                        //                           returnCode = 0;
                        //                           if (parser.Service)
                        //                           {
                        //                               RunService(parser, environment, logger);
                        //                           }
                        //                           else
                        //                           {
                        //                               returnCode = RunProcess(parser, environment);
                        //                           }
                        //                       }, servicePrincipal);

                        //DisplayResults(persistance, parser, logger);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(string.Format("Exception: {0}\n{1}", ex.Message, ex.InnerException));
                        throw;
                    }
                    finally
                    {
                        if (parser.Register && registered)
                            ProfilerRegistration.Unregister(parser.Registration);
                    }
                }

                perfCounter.ResetCounters();
            }
            catch (Exception ex)
            {
                if (logger.IsFatalEnabled)
                {
                    logger.FatalFormat("An exception occured: {0}", ex.Message);
                    logger.FatalFormat("stack: {0}", ex.StackTrace);
                }

                returnCode = returnCodeOffset + 1;
            }

            return returnCode;
        }

        private static bool ParseCommandLine(string[] args, out CommandLineParser parser)
        {
            try
            {
                parser = new CommandLineParser(args);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(
                    "An error occurred whilst parsing the command line; try -? for command line arguments.");
            }

            try
            {
                parser.ExtractAndValidateArguments();

                if (parser.PrintUsage)
                {
                    System.Console.WriteLine(parser.Usage());
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(parser.TargetDir) && !Directory.Exists(parser.TargetDir))
                {
                    System.Console.WriteLine("TargetDir '{0}' cannot be found - have you specified your arguments correctly?", parser.TargetDir);
                    return false;
                }

                if (parser.Service)
                {
                    try
                    {
                        var service = new System.ServiceProcess.ServiceController(parser.Target);
                        var name = service.DisplayName;
                    }
                    catch (Exception)
                    {
                        System.Console.WriteLine("Service '{0}' cannot be found - have you specified your arguments correctly?", parser.Target);
                        return false;
                    }
                }
                else if (!File.Exists(ResolveTargetPathname(parser)))
                {
                    System.Console.WriteLine("Target '{0}' cannot be found - have you specified your arguments correctly?", parser.Target);
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Incorrect Arguments: {0}", ex.Message);
                System.Console.WriteLine(parser.Usage());
                return false;
            }
            return true;
        }

        private static IFilter BuildFilter(CommandLineParser parser)
        {
            var filter = new Filter();

            // apply filters
            if (!parser.NoDefaultFilters)
            {
                filter.AddFilter("-[mscorlib]*");
                filter.AddFilter("-[mscorlib.*]*");
                filter.AddFilter("-[System]*");
                filter.AddFilter("-[System.*]*");
                filter.AddFilter("-[Microsoft.VisualBasic]*");
            }

            if (parser.Filters.Count == 0 && string.IsNullOrEmpty(parser.FilterFile))
            {
                filter.AddFilter("+[*]*");
            }
            else
            {
                if (!string.IsNullOrEmpty(parser.FilterFile))
                {
                    if (!File.Exists(parser.FilterFile))
                        System.Console.WriteLine("FilterFile '{0}' cannot be found - have you specified your arguments correctly?", parser.FilterFile);
                    else
                    {
                        var filters = File.ReadAllLines(parser.FilterFile);
                        filters.ToList().ForEach(filter.AddFilter);
                    }
                }
                if (parser.Filters.Count > 0)
                {
                    parser.Filters.ForEach(filter.AddFilter);
                }
            }

            filter.AddAttributeExclusionFilters(parser.AttributeExclusionFilters.ToArray());
            filter.AddFileExclusionFilters(parser.FileExclusionFilters.ToArray());
            filter.AddTestFileFilters(parser.TestFilters.ToArray());

            return filter;
        }

        private static IEnumerable<string> GetSearchPaths(string targetDir)
        {
            return (new[] { Environment.CurrentDirectory, targetDir }).Concat((Environment.GetEnvironmentVariable("PATH") ?? Environment.CurrentDirectory).Split(Path.PathSeparator));
        }

        private static string ResolveTargetPathname(CommandLineParser parser)
        {
            var expandedTargetName = Environment.ExpandEnvironmentVariables(parser.Target);
            var expandedTargetDir = Environment.ExpandEnvironmentVariables(parser.TargetDir ?? string.Empty);
            return Path.IsPathRooted(expandedTargetName) ? Path.Combine(Environment.CurrentDirectory, expandedTargetName) :
                    GetSearchPaths(expandedTargetDir).Select(dir => Path.Combine(dir.Trim('"'), expandedTargetName)).FirstOrDefault(File.Exists) ?? expandedTargetName;
        }
    }
}