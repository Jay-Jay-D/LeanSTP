using System.Collections.Generic;

namespace QuantConnect.Lean.Caller
{
    internal class LeanParameterizedCaller
    {
        private static void Main(string[] args)
        {
#if DEBUG
            var argsDictionary = new Dictionary<string, string>
            {
                {"algorithm", "ForexVanillaMomentum"},
                {"outputFolder", "C:\\Users\\jjd\\Desktop\\LeanExperiment_2017-05-08_0552"},
                {"broker", "fxcm"},
                {"max_exposure", "0.2"},
                {"leverage", "10"},
                {"initial_cash", "100000"},
                {"pairs_to_trade", "1"}
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