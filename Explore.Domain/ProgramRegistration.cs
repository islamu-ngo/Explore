using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class ProgramRegistration
    {
        public Guid Id { get; set; }
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }
        [ForeignKey("Program")]
        public Guid ProgramId { get; set; }
        public Program Program { get; set; }
        // for later, form link for external registration form, status, registrationtype and so on TODO
        //[ForeignKey("StatusType")]
        //public int StatusTypeId { get; set; }
        //public StatusType StatusType { get; set; }
    }
}
