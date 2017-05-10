using Amib.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        Queue<KeyValuePair<string, string>[]> _runsArgs;

        object _lock = new object();
        int _worksToRun;

        public LeanSTP(string algorithm, string outputFolder)
        {
            _runsArgs = GenerateRunsArguments(algorithm, outputFolder);
            _worksToRun = _runsArgs.Count;
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
        /// Generates the arguments for each run.
        /// </summary>
        /// <param name="algorithm">The algorithm.</param>
        /// <param name="outputFolder">The output folder.</param>
        /// <returns></returns>
        private static Queue<KeyValuePair<string, string>[]> GenerateRunsArguments(string algorithm, string outputFolder)
        {
            string[] brokers = { "fxcm", "oanda" };
            decimal[] maxExposure = { .2m, .3m, .4m, .5m, .8m };
            int[] leverages = { 1, 5, 10, 20, 50 };
            int[] cash = { 10000, 50000, 100000, 500000, 1000000 };
            int[] pairstoTrade = { 1, 2, 3, 4, 5 };

            var args = new List<KeyValuePair<string, string>[]>();

            foreach (var broker in brokers)
            foreach (var max_exposure in maxExposure)
            foreach (var leverage in leverages)
            foreach (var initial_cash in cash)
            foreach (var pairs_to_trade in pairstoTrade)
            {
                args.Add(new[]
                {
                    new KeyValuePair<string, string>("algorithm", algorithm),
                    new KeyValuePair<string, string>("outputFolder", outputFolder),
                    new KeyValuePair<string, string>("broker", broker),
                    new KeyValuePair<string, string>("max_exposure", max_exposure.ToString()),
                    new KeyValuePair<string, string>("leverage", leverage.ToString()),
                    new KeyValuePair<string, string>("initial_cash", initial_cash.ToString()),
                    new KeyValuePair<string, string>("pairs_to_trade", pairs_to_trade.ToString()),
                });
            }
            return new Queue<KeyValuePair<string, string>[]>(args);
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
            KeyValuePair<string, string>[] args = null;
            lock (_lock)
            {
                args = _runsArgs.Dequeue();
            }
            leanWorker.RunAlgorithm(args);
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