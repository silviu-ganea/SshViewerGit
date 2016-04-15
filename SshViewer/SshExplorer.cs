using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Security.Cryptography;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;


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

        public bool createFlashContainer(string origin, string destination, string containerName, out string error)
        {
            bool result = true;
            error = null;
            try
            {
                string name = containerName;
                var a = getSizeOfWinDirectory(origin);
                var b = getFlashBoxAvailableSpace(destination,out error);

                if (a < b)
                {
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
                else
                {
                    error = "Insufficient space on flashbox";
                    return false;
                }
                
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
            
            bool isChkSumCorrect = checkSumDirectories(origin, destination + "/" + containerName, out error);
            if (!isChkSumCorrect)
            {
                return false;
            }
            return result;
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

                string rootPath = containers.First().Split('\n').First().Replace(":","");
                string err;
                var availableSpace = getFlashBoxAvailableSpace(rootPath, out err);
                xRoot.Add(new XAttribute("path", rootPath));
                xRoot.Add(new XAttribute("freeSpace", availableSpace + "Kb"));
            }
            catch(Exception e)
            {
                Console.Write("Error reading file flashbox file structure : " + e.Message);
            }
            
            return xRoot;
        }

        int getFlashBoxAvailableSpace(string linPath, out string error)
        {
            error = null;
            int result = 0;
            try
            {
                SshCommand sc = sshClient.CreateCommand("df " + linPath + " -P | tail -1 | awk '{print $4}' ");
                sc.Execute();
                result = Int32.Parse(sc.Result);
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            
            return result;

        }

        int getSizeOfWinDirectory(string path)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            return (int)DirSize(dirInfo)/1000;
        }

        long DirSize(DirectoryInfo d)
        {
            long Size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                Size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                Size += DirSize(di);
            }
            return (Size);
        }

        public bool checkSumDirectories(string winPath, string linuxPath, out string error)
        {
            bool result = true;
            error = null;
            try
            {
                Dictionary<string, string[]> dict = new Dictionary<string, string[]>();

                //windows stuff;
                List<string> filePaths = Directory.GetFiles(winPath, "*.*", SearchOption.AllDirectories).ToList();
                foreach (string filePath in filePaths)
                {
                    string fileName = Path.GetFileName(filePath);
                    string fileHash = getMD5FromFile(filePath);
                    if (dict.ContainsKey(fileName))
                    {
                        throw new Exception(filePath + " already exists");
                    }
                    dict.Add(fileName, new string[] { fileHash, "" });

                }

                //linux stuff and compare checksums
                SshCommand sc = sshClient.CreateCommand("find " + linuxPath + " -type f");
                sc.Execute();

                if (sc.Error != "")
                {
                    error = sc.Error;
                    return false;
                }
                else
                {
                    string strFiles = sc.Result;
                    foreach (string filePath in strFiles.Split('\n').Where(i => i != ""))
                    {
                        string fileName = filePath.Split('/').Last();
                        sc = sshClient.CreateCommand("md5sum " + filePath + " | awk '{print $1}' ");
                        sc.Execute();
                        if (sc.Error != "")
                        {
                            error = sc.Error;
                            return false;
                        }
                        else
                        {
                            try
                            {
                                string linuxFileSSH = sc.Result.Replace("\n", "");
                                dict[fileName][1] = linuxFileSSH;
                                if (dict[fileName][0] != dict[fileName][1])
                                {
                                    throw new Exception("Checksum failed, errors occured during upload!");
                                }
                            }
                            catch (Exception e)
                            {
                                error = e.Message;
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            
            return result;
        }
        public bool checkSumFiles(string winPath, string linuxPath, out string error)
        {
            bool result = true;
            error = null;

            string winFileHash = getMD5FromFile(winPath);
            string fileName = Path.GetFileName(winPath);
            string linuxFileHash;

            //linux stuff and compare checksums
            SshCommand sc = sshClient.CreateCommand("md5sum " + linuxPath + fileName + " | awk '{print $1}' ");
            sc.Execute();
            if (sc.Error != "")
            {
                error = sc.Error;
                return false;
            }
            else
            {
                linuxFileHash = sc.Result.Replace("\n","");
                if(winFileHash != linuxFileHash)
                {
                    error = "Checksum failed, upload failed.";
                    return false;
                }
            }
            return result;
        }
        string getMD5FromFile(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return toHex(md5.ComputeHash(stream), false);
                }
            }
        }

        string toHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        public bool addFlashFile(string origin, string destination, out string error)
        {
            error = null;
            bool res = true;
            //check if there is enough space
            FileInfo flashFileInfo = new FileInfo(origin);
            int winSizeKB = (Int32)flashFileInfo.Length / 1000;
            int linuxSpace = getFlashBoxAvailableSpace(destination,out error);

            //upload flashfile
            if (winSizeKB < linuxSpace)
            {
                ScpClient scpClient = new ScpClient(ip, user, pw);
                scpClient.Connect();

                scpClient.Upload(new FileInfo(origin), destination);
                scpClient.Disconnect();
            }
            else
            {
                error = "Insufficient space on flashbox";
                return false;
            }
            //checksum
            res = checkSumFiles(origin, destination, out error);

            return res;
        }

        public bool removeFlashFile(string path, out string error)
        {
            bool result = true;
            error = null;
            try
            {
                SshCommand sc = sshClient.CreateCommand("rm " + path);
                sc.Execute();
                if (sc.Error != string.Empty)
                {
                    error = sc.Error;
                    result = false;
                }
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
                return false;
            }
            return result;
        }

        public bool downloadFile(string fromPath, string toPath, out string error)
        {
            bool result = true;
            error = null;
            try
            {
                ScpClient scpClient = new ScpClient(ip, user, pw);
                scpClient.Connect();

                scpClient.Download(fromPath, new DirectoryInfo(toPath));
                scpClient.Disconnect();
            }
            catch (Exception e)
            {
                result = false;
                error = e.Message;
            }
            

            return result;
        }
    }
}
