using System;

namespace Explore.Application.DTOs.StorageObject
{
    public class StorageObjectListDto
    {
        public Guid Id { get; set; }
        public int FileTypeId { get; set; }
        public string? FileTypeFullName { get; set; }
        public string Uri { get; set; }
        public string FullName { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public Guid TenantId { get; set; }
    }
}
