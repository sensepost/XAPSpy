using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using EasyHook;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

namespace XDEMonitor
{
    public partial class Form1 : Form
    {
        private String channelName = null;
        private IpcServerChannel ipcServer;
        static internal Queue<MonitorEntry> monitorQueue = new Queue<MonitorEntry>();
        static internal Boolean isMonitoring = true;

        public Form1()
        {
            InitializeComponent();
            string appID = null;
            /*string[] args = Environment.GetCommandLineArgs();
            try
            {
                appID = args[0];
            }
            catch
            {
                MessageBox.Show("No target application ID is specified!", "Error");
                Application.Exit();
            }*/
            ipcServer = RemoteHooking.IpcCreateServer<HookInterface>(ref channelName, WellKnownObjectMode.Singleton);
            if (!InjectDll())
            {
                Application.Exit();
            }
            TIMER_Tick(null, null);
            
        }

        private void TIMER_Tick(object sender, EventArgs e)
        {
            TIMER.Stop();
            Regex regx1 = new Regex("(\\*Type:)(?<type>.+) (method name:)(?<method>.+)");
            Regex regx2 = new Regex("(\\*Param Name:)(?<varname>[\\w\\W]*)");
            Regex regx3 = new Regex("(?<var>[\\w\\W]*)");
            ListViewItem item = null;
            
            bool isDumpingVars = false;
            int i = 0;
            try
            {
                listViewXDE.BeginUpdate();
                lock (monitorQueue)
                {
                   
                    string strTmp="";
                    while (monitorQueue.Count > 0)
                    {
                        i++;
                        MonitorEntry entry = monitorQueue.Dequeue();  
                         
                        if (regx1.IsMatch(entry.writeBuffer))   //beginning of method dump
                        {
                            if (isDumpingVars&&item!=null)
                            {
                                item.SubItems.Add(strTmp);
                                isDumpingVars = false;  //close previous method dump
                                strTmp = "";
                                listViewXDE.Items[listViewXDE.Items.Count-1].EnsureVisible();
                            }
                            Match m1 = regx1.Match(entry.writeBuffer);
                            item= listViewXDE.Items.Add(entry.timeStamp.ToLongTimeString());
                            string strType = m1.Groups["type"].ToString();
                            item.SubItems.Add(strType);
                            string strMethodName = m1.Groups["method"].ToString();
                            item.SubItems.Add(strMethodName);                            
                            
                        }
                        else if (regx2.IsMatch(entry.writeBuffer))  //beginning of var name
                        {
                            Match m2 = regx2.Match(entry.writeBuffer);
                            strTmp += m2.Groups["varname"].ToString().Replace("\r\n","").Replace("\r","")+":";
                            isDumpingVars = true;
                        }
                        else if(isDumpingVars)
                        {
                            Match m3=regx3.Match(entry.writeBuffer);
                            strTmp += m3.Groups["var"].ToString();
                        }

                                                                      
                    }
                    listViewXDE.EndUpdate();
                    
                }
            }
            finally
            {
                TIMER.Start();
            }

        }
        public bool InjectDll()
        {
            Process[] psArray = Process.GetProcessesByName("XDE");
            if (psArray.Length != 1) { return false; }
            Int32 pid = psArray[0].Id;
            try
            {  
                RemoteHooking.Inject(pid, "XDEHook.dll", "XDEHook.dll", channelName);                
            }
            catch (Exception ExtInfo)
            {
                MessageBox.Show("There was an error while connecting to target:\r\n" + ExtInfo.ToString(), "Error");
                return false;
            }
            labelMsg.Text = "API hook status: Successfull, PID=" + pid.ToString();
            return true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            switch (isMonitoring)
            {
                case false:
                    TIMER.Start();
                    isMonitoring = true;
                    btnStop.Text = "Stop";                    
                    break;
                case true:
                    TIMER.Stop();
                    isMonitoring = false;
                    btnStop.Text = "Start";
                    break;
            }
            
        }

        private void listViewXDE_DouableClick(object sender, EventArgs e)
        {
            string msg = "";
            for (int i = 0; i <listViewXDE.SelectedItems[0].SubItems.Count; i++) msg += listViewXDE.SelectedItems[0].SubItems[i].Text;            
            MessageBox.Show(msg);
        }
       
        private void button1_Click(object sender, EventArgs e)
        {
            string strSearch = textSearchStr.Text.ToLower();
            if (strSearch!= "")
            {
                ResetBkgColor();
                foreach (ListViewItem item1 in listViewXDE.Items)
                {
                    foreach (ListViewItem.ListViewSubItem item2 in item1.SubItems)
                    {
                        if (item2.Text.ToLower().Contains(strSearch)) { item1.BackColor = Color.Blue; item2.BackColor = Color.Blue; listViewXDE.TopItem = item1; }
                    }
                }

            }            
        }
        private void ResetBkgColor()
        {
            foreach (ListViewItem item1 in listViewXDE.Items)
            {
                foreach (ListViewItem.ListViewSubItem item2 in item1.SubItems)
                {
                    item1.BackColor = Color.White; item2.BackColor = Color.White;
                }
            }
        }
        
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void onSaveAs(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Select destination file";
            dlg.DefaultExt = ".txt";
            dlg.Filter = "TXT (*.txt)|*.txt|All files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                TextWriter writer = new StreamWriter(dlg.FileName);
                foreach (ListViewItem item in listViewXDE.Items)
                {
                    writer.WriteLine("========");
                    foreach (ListViewItem.ListViewSubItem subItem in item.SubItems) writer.Write(subItem.Text);
                    writer.WriteLine("========");
                }
                writer.Close();
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
   
}
