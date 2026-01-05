using System;
using System.ComponentModel.DataAnnotations.Schema;

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
        public long Size { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        [ForeignKey("Actor")]
        public Guid? ActorId { get; set; }
        public Actor? Actor { get; set; }
    }
}
