using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace VsPlayer
{
    class CommandExcute
    {
        public static string executeCmd(string[] Commands)
        {
            Process process = new Process
            {
                StartInfo = { FileName = "cmd.exe", UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true }
            };
            process.Start();
            foreach (string cmd in Commands)
                process.StandardInput.WriteLine(cmd);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();
            string str = process.StandardOutput.ReadToEnd();
            process.Close();
            return str;
        }
        public static string executeCmd(string Command)
        {
            return executeCmd(new string[] { Command });
        }
    }
}
