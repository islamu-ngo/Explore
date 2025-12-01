using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class ProgramTags
    {
        public Guid Id { get; set; }
        [ForeignKey("Program")]
        public Guid ProgramId { get; set; }
        public Program Program { get; set; }
        [ForeignKey("Tag")]
        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
