using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Lean.Caller
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var algorithm = args[0];
            var outputFolder = args[1];
            var parameters = new Dictionary<string, string>
            {
                {"ema-fast", args[2]},
                {"ema-slow", args[3]}
            };

            var r = new ParameterizedAlgorithmRunner();
            r.RunBacktest(algorithm, outputFolder, parameters);
        }
    }
}
