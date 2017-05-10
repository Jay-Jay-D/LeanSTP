

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Util;
using System.Threading.Tasks;

namespace QuantConnect.Lean.Caller
{
    public class ParameterizedAlgorithmRunner : MarshalByRefObject
    {
        public void RunBacktest(string algorithm, string outputFolder, Dictionary<string, string> parameters)
        {
            Composer.Instance.Reset();
            try
            {
                // Setup NEATrader stuff.
                Config.Set("algorithm-type-name", algorithm);
                // Setup Lean stuff.
                Config.Set("live-mode", "false");
                Config.Set("environment", "");
                Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
                Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
                Config.Set("api-handler", "QuantConnect.Api.Api");
                Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.BacktestingResultHandler");
                Config.Set("algorithm-language", Language.CSharp.ToString());
                Config.Set("algorithm-location", "QuantConnect.Algorithm." + Language.CSharp.ToString() + ".dll");
                Config.Set("data-folder", "D:/AlgorithmicTrading/DATA/ForexQuotes");
                //Config.Set("data-provider", "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider");
                Config.Set("parameters", JsonConvert.SerializeObject(parameters));


                var fileIdSb = new StringBuilder(algorithm);
                var guid = Guid.NewGuid().ToString();
                //fileIdSb.Append(guid);

                foreach (var parameter in parameters)
                {
                    fileIdSb.Append("_" + parameter.Value);
                }
                var logFilePath = Path.Combine(outputFolder, string.Format("Backtest_{0}.log", fileIdSb));

                var debugEnabled = Log.DebuggingEnabled;
                //var logHandlers = new ILogHandler[] { new ConsoleLogHandler(), new FileLogHandler(logFilePath, false) };
                var logHandlers = new ILogHandler[] { new FileLogHandler(logFilePath, false) };
                using (Log.LogHandler = new CompositeLogHandler(logHandlers))
                using (var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance))
                using (var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance))
                {
                    Log.DebuggingEnabled = false;

                    Log.LogHandler.Trace(new String('=', 120));
                    Log.LogHandler.Trace("\tTesting algorithm " + algorithm);
                    foreach (var parameter in parameters)
                    {
                        Log.LogHandler.Trace(string.Format("\t\t => {0}: {1}", parameter.Key, parameter.Value));
                    }
                    Log.LogHandler.Trace(new String('=', 120));

                    // run the algorithm in its own thread

                    var engine = new QuantConnect.Lean.Engine.Engine(systemHandlers, algorithmHandlers, false);
                    Task.Factory.StartNew(() =>
                    {
                        string algorithmPath;
                        var job = systemHandlers.JobQueue.NextJob(out algorithmPath);
                        engine.Run(job, algorithmPath);
                    }).Wait();

                    var backtestingResultHandler = (BacktestingResultHandler)algorithmHandlers.Results;

                    var backtestStatistics = backtestingResultHandler.FinalStatistics;

                    var algorithmCustomStatistics = backtestingResultHandler.Algorithm.RuntimeStatistics;

                    dynamic results = new ExpandoObject();
                    results.BacktestStatistics = backtestStatistics;
                    results.CustomStatistics = algorithmCustomStatistics;
                    var resultsFileName = string.Format("BacktestResults_{0}.json", fileIdSb);
                    File.WriteAllText(Path.Combine(outputFolder, resultsFileName), JsonConvert.SerializeObject(results, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Log.LogHandler.Error("{0} {1}", ex.Message, ex.StackTrace);
            }
        }

    }
}
