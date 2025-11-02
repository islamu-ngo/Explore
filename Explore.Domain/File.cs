using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain
{
    public class File
    {
        public int Id { get; set; }
        public string Uri { get; set; }
        public string FullName { get; set; }
        public string Extension { get; set; }
        public int Size { get; set; }
        public Guid CreatedBy { get; set; }
    }
}
