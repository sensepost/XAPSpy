using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using System.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XAPspy
{
    public partial class frmMain : Form
    {
        public delegate void AddListItem(string item);
        public delegate void AddText(string text);
        public delegate void ResetButton(string label);        
        public AddListItem listItemDelegate;
        public AddText AddTextDelegate;
        public ResetButton resetButtonDelegate;        
        private List<string> exludedAsms = new List<string>() { "Microsoft.Phone.Controls.dll", "Microsoft.Expression.Interactions.dll", "Microsoft.Phone.Controls.Toolkit.dll", "System.Windows.Interactivity.dll", "System.Windows.Controls.dll", "System.Windows.Controls.DataVisualization.Toolkit.dll", "Microsoft.Advertising.Mobile.UI.dll", "Newtonsoft.Json.WindowsPhone.dll", "Microsoft.Phone.Controls.Maps.dll", "System.Windows.Controls.Layout.Toolkit.dll", "System.Windows.Controls.Toolkit.dll", "System.Xml.Serialization.dll", "System.ServiceModel.Syndication.dll", "Microsoft.Practices.ServiceLocation.dll", "Microsoft.Advertising.Mobile.dll", "Microsoft.Expression.Interactions.dll","Microsoft.Phone.Controls.Maps.dll" };        
        private static XAP targetXAP = null;
        static internal Boolean IsMonitoring = true;
        static internal Queue<MonitorEntry> MonitorQueue = new Queue<MonitorEntry>();

        public frmMain()
        {
            InitializeComponent();           
            listItemDelegate = new AddListItem(AddListItemMethod);
            AddTextDelegate = new AddText(AddtextMethod);
            resetButtonDelegate = new ResetButton(ResetButtonMethod);            
        }

        public void AddListItemMethod(string item)
        {             
            int i=listAsms.Items.Add(item);
            if (!exludedAsms.Contains(item)) listAsms.SetSelected(i, true);
            listAsms.Update();
        }
        public void AddtextMethod(string txt)
        {
            txtOutput.AppendText(txt);
            txtOutput.Update();
        }
        public void ResetButtonMethod(string target)
        {
            switch (target)
            {
                case "deploy":
                    btnDeploy.Enabled = true;
                    break;                
                case "run":
                    xDEMonitorToolStripMenuItem.Enabled = true;
                    btnBrowse.Enabled = true;
                    //btnDeploy.Enabled = true;
                    break;
            }
        }
        private void CheckXDEConsole()
        {
            RegistryKey rk = Registry.LocalMachine;
            RegistryKey rk1 = rk.OpenSubKey(@"SOFTWARE\Microsoft\XDE",true);
            string keyName=@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\XDE";
            if (rk1 != null)
            {              
                if (Registry.GetValue(keyName,"EnableConsole","Not Exists")=="Not Exists")
                {                     
                    
                    rk1.SetValue("EnableConsole", 1, RegistryValueKind.DWord);
                                      
                }
                else
                {
                    int b = (int)rk1.GetValue("EnableConsole");
                    if(b!=1) rk1.SetValue("EnableConsole", 1, RegistryValueKind.DWord);
                }
            }
            else
            {
                MessageBox.Show("Phone Emulator is not installed!", "Error", MessageBoxButtons.OK);
                Application.Exit();
            }
            
        }
        private void label1_Click(object sender, EventArgs e)
        {
            
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlgFile = new OpenFileDialog();
            dlgFile.Title = "Select Target XAP File";
            dlgFile.DefaultExt = ".xap";
            dlgFile.Filter = "XAP (*.xap)|*.xap|All files (*.*)|*.*";
            if (dlgFile.ShowDialog() == DialogResult.OK)
            {
                listAsms.Enabled = true;
                txtFilePath.Text = dlgFile.FileName;
                listAsms.Items.Clear();
                txtOutput.Clear();
                txtOutput.AppendText("Parsing target XAP file..." + Environment.NewLine);
                XAP xap = new XAP(dlgFile.FileName);                          
                XAPParserThreadClass threadClass = new XAPParserThreadClass(this, xap);
                new Thread(new ThreadStart(threadClass.Run)).Start();
            }
        }
             
        private void btnDeploy_Click(object sender, EventArgs e)
        {
            //txtOutput.Clear();
            btnBrowse.Enabled = false;
            listAsms.Enabled = false;
            btnDeploy.Enabled = false;
            KillXDEMonitor();
            targetXAP = new XAP(txtFilePath.Text);
            foreach (string item in listAsms.SelectedItems)
            {
                targetXAP.AddTarget(item);
            }
            DeployerThreadConfig config = new DeployerThreadConfig(Convert.ToInt32(ConfigurationManager.AppSettings["Shell32WaitTime"]), ConfigurationManager.AppSettings["ConMonitorFolder"], ConfigurationManager.AppSettings["MonitorProgram"]);
            
            XAPPatcherThreadClass deployThreadClass = new XAPPatcherThreadClass(this, targetXAP,config);
            Thread deployerThread = new Thread(new ThreadStart(deployThreadClass.Run));
            deployerThread.Start();
              
        }

        private void txtFilePath_TextChanged(object sender, EventArgs e)
        {
           
        }         
        private void txtOutput_TextChanged(object sender, EventArgs e)
        {

        }
                      
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void listAsms_SelectedIndexChanged(object sender, EventArgs e)
        {
            //exludedAsms.Add(listAsms.SelectedItem.ToString());
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            CheckXDEConsole();            
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillXDEMonitor();                              
        }
        private void KillXDEMonitor()
        {
            Process[] ps = Process.GetProcessesByName("XDEMonitor");
            if (ps.Length != 0) ps[0].Kill();  
        }
        private void btnRun_Click(object sender, EventArgs e)
        {
            XAPRunnerThreadClass runThreadClass = new XAPRunnerThreadClass(this, targetXAP);
            Thread runnerThread = new Thread(new ThreadStart(runThreadClass.Run));
            runnerThread.Start();
            //btnStop.Enabled = true;   
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            
        }

        private void OnToolsXDEMonitor_Clicked(object sender, EventArgs e)
        {
            
            Process ps = new Process();
            ps.StartInfo.FileName = Directory.GetCurrentDirectory().ToString()+"\\"+ConfigurationManager.AppSettings["ConMonitorFolder"] + "\\" + ConfigurationManager.AppSettings["MonitorProgram"];
            ps.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory().ToString() + "\\" + ConfigurationManager.AppSettings["ConMonitorFolder"];
            try
            {
                ps.Start();
            }
            catch
            {
                MessageBox.Show("Error while launching XDEMonitor, please check the tool path and name in the config file", "Error");
            }
            
        }

        private void OnExit(object sender, EventArgs e)
        {
            KillXDEMonitor();
            Application.Exit();
        }                         
    }    
}
