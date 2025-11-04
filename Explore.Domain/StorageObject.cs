using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class StorageObject
    {
        public Guid Id { get; set; }
        [ForeignKey("FileType")]
        public int FileTypeId { get; set; }
        public FileType FileType { get; set; }
        public string Uri { get; set; }
        public string FullName { get; set; }
        public string Extension { get; set; }
        public int Size { get; set; }
        public Guid CreatedBy { get; set; }
    }
}
