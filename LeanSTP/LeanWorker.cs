using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using QuantConnect.Lean.Caller;

namespace QuantConnect.Lean.LeanSTP
{
    public class LeanWorker : MarshalByRefObject 
    {
        public void RunAlgorithm(KeyValuePair<string, string>[] args)
        {

            var command = new StringBuilder("Caller.exe");
            foreach (var arg in args)
            {
                command.Append(string.Format(" {0}={1}", arg.Key, arg.Value));
            }

            using (var p = new Process())
            {
#if DEBUG
                p.StartInfo.WorkingDirectory = @"D:\REPOS\LeanSTP\Caller\bin\Debug";
#else
                p.StartInfo.WorkingDirectory = @"D:\REPOS\LeanSTP\Caller\bin\Release";
#endif
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