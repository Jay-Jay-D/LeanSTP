using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Lean.LeanSTP
{
    class Program
    {
        static void Main(string[] args)
        {
            var leanSTP = new LeanSTP("ParameterizedAlgorithm", Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));
            leanSTP.Start();
        }
    }
}
