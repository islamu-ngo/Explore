using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain
{
    public class FileType // profile picture, document, video...
    {
        public int Id { get; set; }
        public string MasterCode { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
    }
}
