using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Microsoft.Extensions.Options;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace Explore.Infrastructure.Mail
{
    public class EmailSender : IEmailSender
    {
        private EmailSettings _emailSettings { get; }
        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task<bool> SendEmail(Email email)
        {
            // I'm not going to use SendGrid to Expensive for scale, rather AWS SES
            //var client = new SendGridClient(_emailSettings.ApiKey);
            //var to = new EmailAddress(email.To);
            //var from = new EmailAddress
            //{
            //    Email = _emailSettings.FromAddress,
            //    Name = _emailSettings.FromName
            //};

            //var message = MailHelper.CreateSingleEmail(from, to, email.Subject, email.Body, email.Body);
            //var response = await client.SendEmailAsync(message);

            //return response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted;
            return false;
        }
    }
}
