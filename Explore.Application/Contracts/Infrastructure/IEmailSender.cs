using System;
using System.Collections.Generic;
using System.Text;
using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure
{
    public interface IEmailSender
    {
        Task<bool> SendEmail(Email email);
    }
}
