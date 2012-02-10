using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace XDEMonitor
{
    internal class MonitorEntry
    {
        public readonly String writeBuffer;
        public readonly DateTime timeStamp = DateTime.Now;


        public MonitorEntry(String writeBuffer)
        {
            this.writeBuffer = writeBuffer;

        }

    }
    public class HookInterface : MarshalByRefObject
    {
        public void ReportException(Exception InInfo)
        {
            Console.WriteLine("The target process has reported an error:\r\n" + InInfo.ToString());
        }

        public void Ping()
        {
        }
        public void OnWriteConsole(Int32 InClientPID, String[] Buffers)
        {
            if (Form1.isMonitoring)
            {
                lock (Form1.monitorQueue)
                {
                    for (int i = Buffers.Length - 1; i>= 0;i--)
                    {
                        string pattern = @"PID:\w+\sTID:\w+\s";
                        string result = Regex.Replace(Buffers[i], pattern, "");
                        Form1.monitorQueue.Enqueue(new MonitorEntry(result));
                    }
                }
            }

        }
    }
}