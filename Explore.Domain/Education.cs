using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class Education : Program
    {
        [ForeignKey("EducationType")]
        public int EducationTypeId { get; set; }
        public EducationType EducationType { get; set; }
    }
}
