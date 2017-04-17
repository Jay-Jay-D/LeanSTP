using System;
using System.IO;
using System.Diagnostics;

namespace QuantConnect.Lean.LeanSTP
{
    public class LeanWorker : MarshalByRefObject 
    {
        public void RunAlgorithm(object[] args)
        {
            var command = string.Format("Caller {0} {1} {2} {3}", args[0], args[1], args[2], args[3]);
            using (var p = new Process())
            {
                p.StartInfo.WorkingDirectory = @"D:\REPOS\LeanSTP\Caller\bin\Debug";
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = "CMD.exe";
                p.Start();
                using (StreamWriter sw = p.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                    {
                        sw.WriteLine(command);
                    }
                }
                p.WaitForExit();
            }

        }
    }
}