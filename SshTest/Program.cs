using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            string a; 
            var res = sshViewer.connect(ip, user, pw, out error);

            var xml = sshViewer.getFileStructXML(out error);

            //res = sshViewer.createFlashContainer("d:/casdev/GIT/BR213IC-GC-EntryLine-MY16/adapt/gen/e009_40_silviu", "/mnt/intflash/flashfiles/", "e007_10_silviu", out error);
            //res = sshViewer.removeFlashContainer("/mnt/intflash/flashfiles/e007_10_silviu", out error);
            //res = sshViewer.addFlashFile("d:/casdev/GIT/BR213IC-GC-EntryLine-MY16/adapt/gen/e009_40_silviu/BR213IC-GC_MY16_E015_EL_15.00_pre80.cfx", "/mnt/intflash/flashfiles/", out error);
            //res = sshViewer.removeFlashFile("/mnt/intflash/flashfiles/BR213IC-GC_MY16_E015_EL_15.00_pre80.cfx", out error);
            res = sshViewer.downloadFile("/mnt/intflash/flashfiles/e94_pre20_ac/sequencing.cfg", "C:/Users/uidr1801/Desktop", out error);
            sshViewer.disconnect(out error);
        }
    }
}
