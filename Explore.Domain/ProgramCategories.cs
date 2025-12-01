using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class ProgramCategories
    {
        public Guid Id { get; set; }
        [ForeignKey("Program")]
        public Guid ProgramId { get; set; }
        public Program Program { get; set; }
        [ForeignKey("Category")]
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
