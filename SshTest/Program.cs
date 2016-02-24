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

            string error;
            
            SshExplorer sshViewer = new SshExplorer();
            var res = sshViewer.connect(ip, user, pw, out error);

            var xml = sshViewer.getFileStructXML(out error);

            res = sshViewer.createFlashContainer("d:/casdev/GIT/BR213IC-GC-EntryLine-MY16/adapt/gen/e009_40_silviu", "/mnt/intflash/flashfiles/213HL/", "e008_20_silviu", out error);
            
            res = sshViewer.removeFlashContainer("/mnt/intflash/flashfiles/213HL/e008_20_silviu", out error);

            sshViewer.disconnect(out error);
        }
    }
}
