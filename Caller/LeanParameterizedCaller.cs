using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Lean.Caller
{
    public class LeanParameterizedCaller
    {
        static void Main(string[] args)
        {
            var argsDictionary = args.Select(a => a.Split(new[] {'='}, 2))
                .GroupBy(a => a[0], a => a.Length == 2 ? a[1] : null)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());

            var algorithm = argsDictionary["algorithm"];
            argsDictionary.Remove("algorithm");
            var outputFolder = argsDictionary["outputFolder"];
            argsDictionary.Remove("outputFolder");

            var r = new ParameterizedAlgorithmRunner();
            r.RunBacktest(algorithm, outputFolder, argsDictionary);
        }
    }
}
