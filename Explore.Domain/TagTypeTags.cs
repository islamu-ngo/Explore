using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class TagTypeTags
    {
        [ForeignKey("TagType")]
        public Guid TagTypeId { get; set; }
        public TagType TagType { get; set; }
        [ForeignKey("Tag")]
        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
