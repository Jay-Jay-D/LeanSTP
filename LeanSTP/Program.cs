using System;
using System.IO;

namespace QuantConnect.Lean.LeanSTP
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string algorithm;
            try
            {
                algorithm = args[0];
            }
            catch (Exception e)
            {
                Console.WriteLine("No Algorithm was selected, so the default ForexVanillaMomentum will be run");
                algorithm = "ForexVanillaMomentum";
            }

            var baseFolder = args.Length > 1
                ? args[1]
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var experimentFolder = Path.Combine(baseFolder,
                string.Format("LeanExperiment_{0:yyyy-MM-dd_hhmm}", DateTime.Now));
            Directory.CreateDirectory(experimentFolder);

            var leanSTP = new LeanSTP(algorithm, experimentFolder);
            leanSTP.Start();
        }
    }
}