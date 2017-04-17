using Amib.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using QuantConnect.Lean.LeanSTP;

namespace QuantConnect.Lean.LeanSTP
{
    public class LeanSTP
    {
        private static string _exeAssembly;
        private static AppDomainSetup _ads;
        private SmartThreadPool _smartThreadPool;
        Thread _workGeneratorThread;
        Queue<object[]> _runGenomeTesterArgs;

        object _lock = new object();
        int _worksToRun;

        public LeanSTP(string algorithm, string outputFolder)
        {
            _runGenomeTesterArgs = GenerateTesterArguments(algorithm, outputFolder);
            _worksToRun = _runGenomeTesterArgs.Count;
            _exeAssembly = Assembly.GetEntryAssembly().FullName;
            _ads = SetupAppDomain();

            var stpStartInfo = new STPStartInfo
            {
                EnableLocalPerformanceCounters = true,
                StartSuspended = true,
                DisposeOfStateObjects = true,
                MaxWorkerThreads = 8 // Default: 25
            };

            _smartThreadPool = new SmartThreadPool(stpStartInfo);
            _workGeneratorThread = new Thread(new ThreadStart(WorkGenerator));
        }

        public void Start()
        {
            _workGeneratorThread.Start();
            while (_workGeneratorThread.ThreadState == ThreadState.Running)
            {
                Console.Write(".");
            }
            Console.WriteLine();
            _smartThreadPool.Start();
            //SmartThreadPool.WaitAll(_workResutls.ToArray());
            _smartThreadPool.WaitForIdle();

        }

        /// <summary>
        /// Generates the genome tester arguments. That is the path for all genomes with the risk
        /// manager enabled and the combinations of trailing orders and ma cross.
        /// </summary>
        /// <param name="trainingSessionPath">The training session path.</param>
        /// <returns></returns>
        private static Queue<object[]> GenerateTesterArguments(string algorithm, string trainingSessionPath)
        {
            int[] fastMaPeriods = { 10, 20 };
            int[] slowMaPeriods = { 60, 120};

            var args = (from fastMa in fastMaPeriods from slowMa in slowMaPeriods select new object[] {algorithm, trainingSessionPath, fastMa, slowMa}).ToList();
            return new Queue<object[]>(args);
        }

        /// <summary>
        /// Creates a new AppDomain, instantiate a LeanWorker in it.
        /// </summary>
        /// <param name="ad">A null AppDomain object.</param>
        /// <returns>A LeanWorker in a AppDomin</returns>
        private LeanWorker CreateLeanWorkerInAppDomain(ref AppDomain ad)
        {
            var name = Guid.NewGuid().ToString();
            ad = AppDomain.CreateDomain(name, null, _ads);
            var leanWorker = (LeanWorker)ad.CreateInstanceAndUnwrap(_exeAssembly, typeof(LeanWorker).FullName);
            return leanWorker;
        }

        /// <summary>
        /// Makes work a LeanWorker in an AppDomain and when finished, unload the AppDomain.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        private object LeanWorkerRunner(object obj)
        {
            AppDomain ad = null;
            var leanWorker = CreateLeanWorkerInAppDomain(ref ad);
            object[] args = null;
            lock (_lock)
            {
                args = _runGenomeTesterArgs.Dequeue();
            }
            leanWorker.RunAlgorithm(args);
            //leanWorker.RunGenomeTester((string)args[0], (string)args[1], (string)args[2], (string)args[3]);
            AppDomain.Unload(ad);
            return null;
        }

        /// <summary>
        /// Populates the SmartThreadingPool with the 
        /// </summary>
        private void WorkGenerator()
        {
            for (int i = 0; i < _worksToRun; i++)
            {
                var workItemCallback = new WorkItemCallback(LeanWorkerRunner);
                _smartThreadPool.QueueWorkItem(workItemCallback);
            }
        }

        /// <summary>
        /// Construct and initialize settings for the subsequents AppDomain.
        /// </summary>
        /// <returns></returns>
        private static AppDomainSetup SetupAppDomain()
        {
            return new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };
        }
    }
}