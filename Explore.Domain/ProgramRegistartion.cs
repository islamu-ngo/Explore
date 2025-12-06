using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class ProgramRegistartion
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        [ForeignKey("Program")]
        public Guid ProgramId { get; set; }
        public Program Program { get; set; }
        [ForeignKey("StatusType")]
        public int StatusTypeId { get; set; }
        public StatusType StatusType { get; set; }

        // Guest/User details snapshot
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}
