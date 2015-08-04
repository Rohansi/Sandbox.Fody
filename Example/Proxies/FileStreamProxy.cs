using System.IO;
using System.Text;

namespace Proxies
{
    [FodyProxy(typeof(FileStream))]
    public class FileStreamProxy : MemoryStream
    {
        public FileStreamProxy(string path, FileMode mode)
            : base(Encoding.UTF8.GetBytes("nope"))
        {
            
        }
    }
}
