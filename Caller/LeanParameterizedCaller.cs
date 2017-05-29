using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Caller
{
    internal static class LeanParameterizedCaller
    {
        private static void Main(string[] args)
        {
#if DEBUG
            // Testing run.
            var argsDictionary = new Dictionary<string, string>
            {
                {"algorithm", "TradingStrategiesBasedOnGeneticAlgorithms"},
                {"outputFolder", "C:\\Users\\jjd\\Desktop\\GAExperiment_2017-05-28_0211"},
                { "ID", "1407"},
                {"EntryIndicator1","0"},
                {"EntryIndicator2","2"},
                {"EntryIndicator3","3"},
                {"EntryIndicator4","1"},
                {"EntryIndicator5","5"},
                {"EntryIndicator1Direction","0"},
                {"EntryIndicator2Direction","0"},
                {"EntryIndicator3Direction","0"},
                {"EntryIndicator4Direction","1"},
                {"EntryIndicator5Direction","1"},
                {"EntryOperator1","1"},
                {"EntryOperator2","0"},
                {"EntryOperator3","1"},
                {"EntryOperator4","0"},
                {"ExitIndicator1","4"},
                {"ExitIndicator2","3"},
                {"ExitIndicator3","2"},
                {"ExitIndicator4","1"},
                {"ExitIndicator5","2"},
                {"ExitIndicator1Direction","1"},
                {"ExitIndicator2Direction","0"},
                {"ExitIndicator3Direction","1"},
                {"ExitIndicator4Direction","0"},
                {"ExitIndicator5Direction","0"},
                {"ExitOperator1","0"},
                {"ExitOperator2","1"},
                {"ExitOperator3","0"},
                {"ExitOperator4","1"}
            };
#else
            var argsDictionary = args.Select(a => a.Split(new[] {'='}, 2))
                .GroupBy(a => a[0], a => a.Length == 2 ? a[1] : null)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());
#endif

            var algorithm = argsDictionary["algorithm"];
            argsDictionary.Remove("algorithm");
            var outputFolder = argsDictionary["outputFolder"];
            argsDictionary.Remove("outputFolder");

            var r = new ParameterizedAlgorithmRunner();

            r.RunBacktest(algorithm, outputFolder, argsDictionary);
        }
    }
}