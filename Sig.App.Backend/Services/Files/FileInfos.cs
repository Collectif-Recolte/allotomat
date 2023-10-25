using System.Collections.Generic;
using System.IO;

namespace Sig.App.Backend.Services.Files
{
    public class FileInfos
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public IDictionary<string, string> Metadata { get; set; }
        public Stream Content { get; set; }
    }
}