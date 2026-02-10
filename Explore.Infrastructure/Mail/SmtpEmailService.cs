// ABOUTME: MailKit-based email service implementation. Resolves SMTP config per-tenant
// from the cascading settings engine. Works with any standard SMTP provider.

using System.Diagnostics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Polly;

namespace Explore.Infrastructure.Mail;

/// <summary>
/// Sends emails via any SMTP server using MailKit.
/// Configuration is resolved per-tenant from the cascading settings engine.
/// Creates a new SmtpClient per send (MailKit is not thread-safe).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ISmtpConfigResolver _configResolver;
    private readonly ILogger<SmtpEmailService> _logger;
    private static readonly ResiliencePipeline<EmailResult> RetryPipeline =
        EmailResiliencePipelines.CreateSendPipeline();

    public SmtpEmailService(
        ISmtpConfigResolver configResolver,
        ILogger<SmtpEmailService> logger)
    {
        _configResolver = configResolver;
        _logger = logger;
    }

    public async Task<EmailResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var config = await _configResolver.ResolveAsync(cancellationToken);
        if (config is null)
        {
            return EmailResult.Fail("SMTP is not configured. Configure email settings in the admin panel.");
        }

        return await RetryPipeline.ExecuteAsync(
            async ct => await SendCoreAsync(message, config, ct),
            cancellationToken);
    }

    public async Task<EmailResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configResolver.ResolveAsync(cancellationToken);
        if (config is null)
        {
            return EmailResult.Fail("SMTP is not configured. Set email.smtp_host in admin settings.");
        }

        var sw = Stopwatch.StartNew();

        try
        {
            using var client = new SmtpClient();
            ConfigureClient(client, config);

            await client.ConnectAsync(
                config.Host,
                config.Port,
                MapSecurityMode(config.Security),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(config.Username))
            {
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            }

            await client.DisconnectAsync(quit: true, cancellationToken);

            sw.Stop();
            _logger.LogInformation(
                "SMTP connection test successful: {Host}:{Port} in {Duration}ms",
                config.Host, config.Port, sw.ElapsedMilliseconds);

            return EmailResult.Ok("Connection successful", sw.Elapsed);
        }
        catch (AuthenticationException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SMTP authentication failed for {Host}:{Port}", config.Host, config.Port);
            return EmailResult.Fail($"Authentication failed: {ex.Message}", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SMTP connection test failed for {Host}:{Port}", config.Host, config.Port);
            return EmailResult.Fail($"Connection test failed: {ex.Message}", sw.Elapsed);
        }
    }

    private async Task<EmailResult> SendCoreAsync(
        EmailMessage message,
        SmtpConfiguration config,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            using var mimeMessage = BuildMimeMessage(message, config);

            using var client = new SmtpClient();
            ConfigureClient(client, config);

            await client.ConnectAsync(
                config.Host,
                config.Port,
                MapSecurityMode(config.Security),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(config.Username))
            {
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            }

            var response = await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            sw.Stop();
            _logger.LogInformation(
                "Email sent to {To} via {Host}:{Port} in {Duration}ms",
                message.To, config.Host, config.Port, sw.ElapsedMilliseconds);

            return EmailResult.Ok(response, sw.Elapsed);
        }
        catch (SmtpCommandException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "SMTP command error sending to {To}: {StatusCode} {Message}",
                message.To, ex.StatusCode, ex.Message);
            return EmailResult.Fail($"SMTP error ({ex.StatusCode}): {ex.Message}", sw.Elapsed);
        }
        catch (SmtpProtocolException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "SMTP protocol error sending to {To}: {Message}",
                message.To, ex.Message);
            return EmailResult.Fail($"SMTP protocol error: {ex.Message}", sw.Elapsed);
        }
        catch (AuthenticationException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "SMTP authentication failed sending to {To} via {Host}:{Port}",
                message.To, config.Host, config.Port);
            return EmailResult.Fail($"Authentication failed: {ex.Message}", sw.Elapsed);
        }
        catch (Exception ex) when (ex is TimeoutException
            or OperationCanceledException
            or System.IO.IOException)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Connection error sending to {To} via {Host}:{Port}",
                message.To, config.Host, config.Port);
            return EmailResult.Fail($"Connection error: {ex.Message}", sw.Elapsed);
        }
    }

    private static void ConfigureClient(SmtpClient client, SmtpConfiguration config)
    {
        if (config.SkipCertificateValidation)
        {
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        client.Timeout = config.TimeoutSeconds * 1000;
    }

    private static SecureSocketOptions MapSecurityMode(SmtpSecurityMode mode) => mode switch
    {
        SmtpSecurityMode.None => SecureSocketOptions.None,
        SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
        SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
        SmtpSecurityMode.Auto => SecureSocketOptions.Auto,
        _ => SecureSocketOptions.StartTls
    };

    private static MimeMessage BuildMimeMessage(EmailMessage message, SmtpConfiguration config)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(
            message.FromName ?? config.FromName,
            message.FromAddress ?? config.FromAddress));

        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        foreach (var cc in message.Cc)
            mimeMessage.Cc.Add(MailboxAddress.Parse(cc));

        foreach (var bcc in message.Bcc)
            mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));

        mimeMessage.Subject = message.Subject;

        foreach (var (key, value) in message.CustomHeaders)
            mimeMessage.Headers.Add(key, value);

        var builder = new BodyBuilder();

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            builder.HtmlBody = message.HtmlBody;

        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            builder.TextBody = message.PlainTextBody;

        foreach (var attachment in message.Attachments)
        {
            if (attachment.IsInline && !string.IsNullOrWhiteSpace(attachment.ContentId))
            {
                var inline = builder.LinkedResources.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
                inline.ContentId = attachment.ContentId;
            }
            else
            {
                builder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }
        }

        mimeMessage.Body = builder.ToMessageBody();
        return mimeMessage;
    }
}
