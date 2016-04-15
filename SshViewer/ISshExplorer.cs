using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SshV
{
    interface ISshExplorer
    {
        XElement getFileStructXML(out string error);
        bool connect(string ip, string user, string pw, out string error);
        bool disconnect(out string error);
        bool createFlashContainer(string origin, string destination, string containerName, out string error);
        bool addFlashFile(string origin, string destination, out string error);
        bool removeFlashFile(string path, out string error);
        bool removeFlashContainer(string path, out string error);
        bool downloadFile(string fromPath, string toPath, out string error);
        
    }
}
