using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Renci.SshNet;
using System.Xml.XPath;


namespace SshV
{
    public class SshExplorer : ISshExplorer
    {
        SshClient sshClient;
        string ip;
        string user;
        string pw;
        string path;

        public SshExplorer()
        {
            //ip = "192.168.2.0";
            //user = "alarm";
            //pw = "alarm";
            path = "/mnt/intflash/flashfiles";    
        }

        public bool connect(string ip, string user, string pw, out string error)
        {
            error = null;
            try
            {
                this.ip = ip;
                this.user = user;
                this.pw = pw;
                sshClient = new SshClient(ip, user, pw);
                sshClient.Connect();
            }
            catch(Exception e)
            {
                error = e.Message;
                return false;
            }
            return true;
        }
        public bool disconnect(out string error)
        {
            error = null;
            try
            {
                sshClient.Disconnect();
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
            return true;
        }

        public bool removeFlashContainer(string path, out string error)
        {
            bool result = true;
            error = null;
            try {
                SshCommand sc = sshClient.CreateCommand("rm -R " + path);
                sc.Execute();
                if(sc.Error != string.Empty)
                {
                    error = sc.Error;
                    result = false;
                }
            } catch(Exception e){
                Console.Write(e.Message);
                return false;
            }
            return result;
        }

        public void createFlashContainer(string destination, string containerName, List<string> appFlashFiles, List<string> loaderFlashFiles, List<string> flashOrder)
        {
            ScpClient scpClient = new ScpClient(ip,user, pw);
            scpClient.Connect();

            scpClient.Upload(new FileInfo("C:/fileSample.txt"), "/mnt/intflash/flashfiles");
            scpClient.Disconnect();
        }

        public bool createFlashContainer(string origin, string destination, string containerName, out string error)
        {
            bool result = true;
            error = null;
            try
            {
                string name = containerName;

                ScpClient scpClient = new ScpClient(ip, user, pw);
                scpClient.Connect();

                if (name == null)
                {
                    name = origin.Split('/').Last();
                }
                sshClient.CreateCommand("mkdir " + destination + name);

                scpClient.Upload(new DirectoryInfo(origin), destination + name);
                scpClient.Disconnect();
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
            return result;
        }

        List<string> getFiles(string path)
        {
            List<string> files;
            SshCommand sc = sshClient.CreateCommand("ls " + path);
            sc.Execute();

            files = sc.Result.Split('\n').Where(i => i != string.Empty).ToList();
            return files;
        }

        public XElement getFileStructXML(out string error)
        {
            error = null;
            XElement xRoot = new XElement("root");
            try
            {
                SshCommand sc = sshClient.CreateCommand("ls -R " + path);
                sc.Execute();
                if(sc.Error != string.Empty)
                {
                    error = sc.Error;
                }
                var answer = sc.Result;

                List<string> containers = Regex.Split(sc.Result, @"(?:\n){2,}").ToList();
                foreach (var container in containers)
                {
                    XElement xTemp = new XElement("xTemp");
                    var flashFiles = container.Split('\n');

                    string fName = flashFiles.First().Replace(":", "").Split('/').Last();
                    string fPath = flashFiles.First().Replace(":", "");

                    XElement xTempEl = xRoot.Descendants("xTemp").Where(i => i.Attribute("path").Value == fPath).FirstOrDefault();
                    if (xRoot.Descendants("xTemp").Where(i => i.Attribute("path").Value == fPath).Count() > 0)
                    {
                        xTemp = xTempEl;
                    }
                    else
                    {
                        xTemp = xRoot;
                    }

                    foreach (string flashFile in flashFiles.Skip(1).ToList())
                    {
                        XElement xFlashFile = new XElement("xTemp");
                        xFlashFile.Add(new XAttribute("name", flashFile));
                        xFlashFile.Add(new XAttribute("path", fPath + "/" + flashFile));
                        xTemp.Add(xFlashFile);
                    }
                }
            }
            catch(Exception e)
            {
                Console.Write("Error reading file flashbox file structure : " + e.Message);
            }
            
            return xRoot;
        }
    }
}
