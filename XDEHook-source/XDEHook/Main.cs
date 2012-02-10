using System;
using System.Collections.Generic;
using System.Text;
using EasyHook;
using System.Threading;
using System.Runtime.InteropServices;



namespace XDEMonitor
{
    public class Main : EasyHook.IEntryPoint
    {
        public HookInterface Interface=null;
        public LocalHook WriteConsoleHook=null;
        Stack<String> Queue = new Stack<String>();
        
        public Main(
            RemoteHooking.IContext InContext,
            String InChannelName)
        {
            Interface = RemoteHooking.IpcConnectClient<HookInterface>(InChannelName);

            Interface.Ping();
        }

        public void Run(RemoteHooking.IContext InContext,String InChannelName)
        {
            // install hook...
            try
            {

                WriteConsoleHook = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "WriteFile"),
                    new DWriteFile(WriteFile_Hooked),
                    this);

                WriteConsoleHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            }
            catch (Exception ExtInfo)
            {
                Interface.ReportException(ExtInfo);

                return;
            }

            //Interface.IsInstalled(RemoteHooking.GetCurrentProcessId());

            RemoteHooking.WakeUpProcess();

            // wait for host process termination...
            try
            {
                while (true)
                {
                    Thread.Sleep(500);

                    // transmit newly monitored file accesses...
                    if (Queue.Count > 0)
                    {
                        String[] Package = null;

                        lock (Queue)
                        {
                            Package = Queue.ToArray();

                            Queue.Clear();
                        }

                        Interface.OnWriteConsole(RemoteHooking.GetCurrentProcessId(), Package);
                    }
                    else
                        Interface.Ping();
                }
            }
            catch
            {
                // Ping() will raise an exception if host is unreachable
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall,
            CharSet = CharSet.Auto,
            SetLastError = true)]
        delegate bool DWriteFile(
            IntPtr hFile,
            IntPtr lpBuffer, 
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            [In] IntPtr lpOverlapped);

        [DllImport("kernel32.dll",
            CharSet = CharSet.Auto,
            SetLastError = true,
            CallingConvention = CallingConvention.StdCall)]
        static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer, 
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            [In] IntPtr lpOverlapped);

         static bool WriteFile_Hooked(
            IntPtr hFile,
            IntPtr lpBuffer, //changed
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            [In] IntPtr lpOverlapped)
        {
            byte[] bytes = new byte[nNumberOfBytesToWrite];

            try
            {
                Main This = (Main)HookRuntimeInfo.Callback;
                
                lock (This.Queue)
                {
                    System.Diagnostics.ProcessModule module=HookRuntimeInfo.CallingUnmanagedModule;
                    if (module.ModuleName == "XDE.exe")
                    {
                        for (uint i = 0; i < nNumberOfBytesToWrite; i++)
                            bytes[i] = Marshal.ReadByte(lpBuffer, (int)i);
                        //string tmpStr = InBuffer.ToString().Substring(0, (int)InNumberOfBytesToWrite).Replace("\r\n", " ");
                        //System.Text.Encoding encoding=new System.Text.ASCIIEncoding();
                        //String tmpStr = bytes.ToString().Substring(0, (int)nNumberOfBytesToWrite).Replace("\r\n", " ");
                        string output="";
                        for (uint i = 0; i < nNumberOfBytesToWrite; i++)
                        {
                            output += (char)bytes[i];
                        }


                        This.Queue.Push(output);
                    }                    
                      
                }
            }
            catch
            {
            }

            // call original API...
            return WriteFile(hFile, bytes, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, lpOverlapped);
        }

    
    }
}
