using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using System.Xml;

namespace XAPspy
{
    public class XAP
    {
        private string _XAPFilePath;
        public bool isUnpacked;
        public string appID;
        public string iconPath;
        public readonly List<string> _targetAsms = new List<string>();
        private string _UnpackPath;
        public string[] dllFiles;

        public delegate void XAPFileUnzippedHandler(object xap, XAPFileInfoEventArgs fileInfo);
        //public delegate void XAPFileReadHandler(object xap, XAPFileInfoEventArgs fileInfo);

        public event XAPFileUnzippedHandler FileUnzipped;
        //public event XAPFileReadHandler FileRead;

        protected void OnXAPFileUnzipped(object xap, XAPFileInfoEventArgs fileInfo)
        {
            if (FileUnzipped != null) FileUnzipped(this, fileInfo);
        }     
        
       
        public XAP(string path)
        {
            if (File.Exists(path))
            {
                _XAPFilePath = path;
                isUnpacked = false;
                appID = "";
                iconPath = "";
            }
            
            else
            {
                string err = "XAP file " + path + " does not exists!";
                throw new Exception(err);
            }
        }
        public string UnZip(string filePath)
        {
            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tmpPath);
            try
            {
                using (ZipInputStream zipStream = new ZipInputStream(File.OpenRead(filePath)))
                {
                    ZipEntry zipThing;
                    while ((zipThing = zipStream.GetNextEntry()) != null)
                    {
                        if (zipThing.IsFile)
                        {
                            if (zipThing.Name != "")
                            {
                                if (zipThing.Name.Contains("\\") || zipThing.Name.Contains("/"))
                                {
                                    string tmpFilename = zipThing.Name.Replace("/", "\\");
                                    int pos = tmpFilename.LastIndexOf("\\");
                                    string str = tmpFilename.Substring(0, pos);
                                    string cdir = "";
                                    string[] dirs = str.Split(new char[] { '\\' });
                                    foreach (string dir in dirs)
                                    {
                                        if (!Directory.Exists(tmpPath + "\\" + cdir + "\\" + dir)) Directory.CreateDirectory(tmpPath + "\\" + cdir + "\\" + dir);
                                        cdir = cdir + "\\" + dir;
                                    }
                                }

                                string strNewFile = @"" + tmpPath + @"\" + zipThing.Name;
                                using (FileStream streamWriter = File.Create(strNewFile))
                                {
                                    XAPFileInfoEventArgs fileInfo = new XAPFileInfoEventArgs(strNewFile);
                                    OnXAPFileUnzipped(this,fileInfo);

                                    byte[] yData = new byte[2048];
                                    while (true)
                                    {
                                        int nSize = zipStream.Read(yData, 0, yData.Length);
                                        if (nSize > 0)
                                            streamWriter.Write(yData, 0, nSize);
                                        else
                                            break;
                                    }
                                    streamWriter.Close();
                                }

                            }
                        }
                        else if (zipThing.IsDirectory)
                        {
                            string strNewDir = @"" + tmpPath + @"\" + zipThing.Name;
                            if (!Directory.Exists(strNewDir)) Directory.CreateDirectory(strNewDir);
                        }

                    }
                    zipStream.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Exception occured while decompressing the source XAP file: " + ex.Message);               
            }
            this.isUnpacked = true;
            this._UnpackPath = tmpPath;
            return tmpPath;
        }
        public string[] ParseXAP()
        {
            if (!isUnpacked)
            {
                _UnpackPath = UnZip(_XAPFilePath);  
            }
            string[] asmFiles = Directory.GetFiles(_UnpackPath, "*.dll");
            this.dllFiles = asmFiles;

            //Read ProductID
            XmlTextReader Reader = new XmlTextReader(_UnpackPath + "\\" + "WMAppManifest.xml");
            while (Reader.Read())
            {
                if (Reader.NodeType == XmlNodeType.Element && Reader.Name == "App")
                {
                    appID = Reader.GetAttribute("ProductID");
                    while (Reader.NodeType != XmlNodeType.EndElement)
                    {
                        if (Reader.Name == "IconPath")
                        {
                            iconPath = Reader.ReadElementContentAsString();
                            //iconPath = Reader.Value;
                        }

                        Reader.Read();
                    }
                }
                
            }

            return asmFiles;        
        }
        public bool StripDRM()
        {
            if (isUnpacked)
            {
                string strDRMFilename = @"" + _UnpackPath + @"\" + "WMAppPRHeader.xml";
                if (File.Exists(strDRMFilename))
                {                    
                    File.Delete(strDRMFilename);
                }
                else return false;
            }
            return true;
        }
        public void ReplaceSignature()
        {
            if (dllFiles != null)
            {
                foreach (string filepath in dllFiles)
                {
                    Process processObj = new Process();
                    processObj.StartInfo.FileName = "signcode.exe";
                    processObj.StartInfo.Arguments = "-spc pubkeycert.cer -v privkey.pvk -a md5 " + filepath;
                    processObj.StartInfo.UseShellExecute = false;
                    processObj.StartInfo.CreateNoWindow = true;
                    processObj.StartInfo.RedirectStandardOutput = true;
                    processObj.Start();
                    processObj.WaitForExit();
                    string strCmdResult = processObj.StandardOutput.ReadToEnd();
                }
                
            }
          
        }
        public bool IsTarget(string filename)
        {
            if (_targetAsms.Contains(filename)) return true;
            return false;
        }
        public string GetPath()
        {
            return _UnpackPath;
        }
        public void AddTarget(string filename)
        {
            _targetAsms.Add(filename);
        }
        
    }
    public class XAPFileInfoEventArgs : EventArgs
    {
        public readonly string path;
        public XAPFileInfoEventArgs(string path)
        {
            this.path=path;
        }
    }
}
