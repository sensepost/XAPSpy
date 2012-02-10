using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Microsoft.SmartDevice.Connectivity;
using System.IO;
using EasyHook;
using System.Windows.Forms;
using System.Runtime.Remoting;


namespace XAPspy
{
    public struct DeployerThreadConfig
    {
        public int zipWaitTime;
        public string conMonitorFolder;
        public string monitorProgram;
        public DeployerThreadConfig(int zipWaitTime, string conMonitorFolder, string monitorProgram)
        {
            this.zipWaitTime = zipWaitTime;
            this.conMonitorFolder = conMonitorFolder;
            this.monitorProgram = monitorProgram;
        }
    }
    public abstract class XAPThreadClass
    {
        protected frmMain _mainfrm;
        protected XAP _xap;
        public XAPThreadClass(frmMain mainfrm, XAP xap)
        {
            _mainfrm = mainfrm;
            _xap = xap;
        }
        public abstract void Run();
    }
    public class XAPParserThreadClass : XAPThreadClass
    {
        public XAPParserThreadClass(frmMain mainfrm, XAP xap) : base(mainfrm, xap) { }
        public override void Run()
        {
            string[] fileNames = _xap.ParseXAP();

            foreach (string fileName in fileNames)
            {
                _mainfrm.BeginInvoke(_mainfrm.listItemDelegate, fileName.Substring(fileName.LastIndexOf("\\") + 1));
            }
            _mainfrm.Invoke(_mainfrm.AddTextDelegate, "Application GUID: " + _xap.appID + Environment.NewLine);
            _mainfrm.Invoke(_mainfrm.AddTextDelegate, "Application Icon File: " + _xap.iconPath + Environment.NewLine);
            _mainfrm.Invoke(_mainfrm.resetButtonDelegate, "deploy");
        }
    }
    public class XAPPatcherThreadClass : XAPThreadClass
    {        
        private DeployerThreadConfig _config;
        public XAPPatcherThreadClass(frmMain mainfrm, XAP xap, DeployerThreadConfig config) : base(mainfrm, xap) { _config = config; }
        public override void Run()
        {
            if (!_xap.isUnpacked) _xap.ParseXAP();
            if (_xap.StripDRM())
            {
                _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "DRM file was removed." + Environment.NewLine);
            }
            foreach (string item in _xap.dllFiles)
            {
                if (!_xap.IsTarget(item.Substring(item.LastIndexOf("\\") + 1)))
                {
                    _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "Skipping excluded assembly: " + item + Environment.NewLine);
                    continue;
                }
                try
                {
                    System.Reflection.AssemblyName.GetAssemblyName(item);
                    _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "Patching " + item + Environment.NewLine);
                    XAPAssembly asmTarget = new XAPAssembly(item);
                    asmTarget.InjectProlouge();
                }
                catch (System.BadImageFormatException)
                {
                    _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "Skipping native dll file:" + item + Environment.NewLine);
                }
            }
            _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "Finished patching target assebmlies." + Environment.NewLine);
            _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "Signing dll files...");
            _xap.ReplaceSignature();
            _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "(Done)" + Environment.NewLine + "Creating new XAP file...");
            string tmp = Directory.GetParent(_xap.GetPath()).ToString() + "\\" + _xap.appID;
            string zipfilename = tmp + ".zip";
            string newfilename = tmp + ".xap";
            System.IO.File.Delete(zipfilename);
            System.IO.File.Delete(newfilename);
            ZipFolder(_xap.GetPath(), zipfilename);
            System.Threading.Thread.Sleep(_config.zipWaitTime);    //wait for shell32.dll
            System.IO.File.Move(zipfilename, newfilename);
            _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "(Done)" + Environment.NewLine + "Connecting to emulator...");
            DatastoreManager dsmgr = new DatastoreManager(1033);
            Platform platform = dsmgr.GetPlatforms().Single(p => p.Name == "Windows Phone 7");
            Device WP7emu = platform.GetDevices().Single(d => d.Name == "Windows Phone 7 Emulator");
            WP7emu.Connect();

            Guid appGUID = new Guid(_xap.appID);
            RemoteApplication app;

            if (WP7emu.IsApplicationInstalled(appGUID))
            {
                _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "(Done)" + Environment.NewLine + "Uninstalling previous version...");
                app = WP7emu.GetApplication(appGUID);
                app.Uninstall();
            }
            _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "(Done)" + Environment.NewLine + "Deploying application....");
            app = WP7emu.InstallApplication(appGUID, appGUID, "NormalApp", _xap.iconPath, newfilename);
            _mainfrm.BeginInvoke(_mainfrm.AddTextDelegate, "(Done)" + Environment.NewLine + "You can now launch the XDEMonitor from tools menu and then run the application on the phone."+Environment.NewLine);
            RunXDEMonitor();
            _mainfrm.BeginInvoke(_mainfrm.resetButtonDelegate, "run");
        }
        public void ZipFolder(string sourceFolder, string dstFile)
        {
            byte[] emptyzip = new byte[] { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            try
            {
                FileStream fs = File.Create(dstFile);
                fs.Write(emptyzip, 0, emptyzip.Length);
                fs.Flush();
                fs.Close();
                fs = null;
                Shell32.Shell sc = new Shell32.Shell();
                Shell32.Folder SrcFlder = sc.NameSpace(sourceFolder);
                Shell32.Folder DestFlder = sc.NameSpace(dstFile);
                Shell32.FolderItems items = SrcFlder.Items();
                DestFlder.CopyHere(items, 20);
            }
            catch
            {

            }
        }        
        public void RunXDEMonitor()
        {

        }
    }
    public class XAPRunnerThreadClass : XAPThreadClass
    {
        public XAPRunnerThreadClass(frmMain mainfrm, XAP xap) :base(mainfrm, xap){}
        public override void Run()
        {
            DatastoreManager dsmgr = new DatastoreManager(1033);
            Platform platform = dsmgr.GetPlatforms().Single(p => p.Name == "Windows Phone 7");
            Device WP7emu = platform.GetDevices().Single(d => d.Name == "Windows Phone 7 Emulator");
            WP7emu.Connect();
            Guid appGUID = new Guid(_xap.appID);
            RemoteApplication app;

            if (WP7emu.IsApplicationInstalled(appGUID))
            {
                app = WP7emu.GetApplication(appGUID);
                app.Launch();
            }
            else
            {
                MessageBox.Show("Application was not found!", "Error");
                return;
            }
        }
    }
}