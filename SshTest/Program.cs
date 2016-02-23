using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using SshV;

namespace SshTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string ip = "192.168.2.0";
            string user = "alarm";
            string pw = "alarm";

            SShViewer sshViewer = new SShViewer();
            sshViewer.connect(ip, user, pw);
            var xml = sshViewer.getFileStructXML();

            sshViewer.createNewFlashContainer();
        }
    }
}
