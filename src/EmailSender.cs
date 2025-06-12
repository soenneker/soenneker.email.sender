using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Dtos.Email;
using Soenneker.Email.Mime.Abstract;
using Soenneker.Email.Senders.Abstract;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.Dictionaries.StringString;
using Soenneker.Extensions.Dtos.Email;
using Soenneker.Extensions.Enumerable.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Messages.Email;
using Soenneker.Utils.Json;
using Soenneker.Utils.Template.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Email.Sender;

///<inheritdoc cref="IEmailSender"/>
public sealed class EmailSender : IEmailSender
{
    private readonly bool _enabled;
    private readonly ILogger<EmailSender> _logger;
    private readonly IMimeUtil _mimeUtil;
    private readonly ITemplateUtil _templateUtil;

    private const string _defaultTemplate = "default.html";

    private readonly string _defaultAddress;
    private readonly string _defaultName;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger, IMimeUtil mimeUtil, ITemplateUtil templateUtil)
    {
        _logger = logger;
        _mimeUtil = mimeUtil;
        _templateUtil = templateUtil;

        _enabled = configuration.GetValueStrict<bool>("Email:Enabled");
        _defaultAddress = configuration.GetValueStrict<string>("Email:DefaultAddress");
        _defaultName = configuration.GetValueStrict<string>("Email:DefaultName");
    }

    public async Task<bool> Send(string messageContent, Type? type, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            _logger.LogDebug("{Name} is disabled by config", nameof(EmailSender));
            return false;
        }

        if (type == null)
            throw new ArgumentException("Service bus message did not have a type", nameof(type));

        object? msgModel = JsonUtil.Deserialize(messageContent, type);
        if (msgModel is not EmailMessage message)
            throw new InvalidOperationException($"Service bus message was not a {nameof(EmailMessage)}");

        return await Send(message, cancellationToken).NoSync();
    }

    public async Task<bool> Send(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            _logger.LogDebug("{Name} is disabled by config", nameof(EmailSender));
            return false;
        }

        if (message == null)
        {
            _logger.LogError("EmailSender.Send called with null EmailMessage");
            throw new ArgumentNullException(nameof(message));
        }

        _logger.LogInformation("Building and sending email (Subject: {Subject}) to {ToList}", message.Subject, message.To.ToCommaSeparatedString());

        EmailDto emailDto;
        try
        {
            emailDto = await BuildEmailDto(message, cancellationToken).NoSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build EmailDto for message (Subject: {Subject})", message.Subject);
            throw;
        }

        try
        {
            var mimeMessage = emailDto.ToMimeMessage(_logger);
            _logger.LogDebug("Sending MIME message (Subject: {Subject}) to {ToList}", emailDto.Subject, emailDto.To.ToCommaSeparatedString(true));
            await _mimeUtil.Send(mimeMessage, cancellationToken).NoSync();
            _logger.LogInformation("Email (Subject: {Subject}) successfully sent to {ToList}", emailDto.Subject, emailDto.To.ToCommaSeparatedString(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for message (Subject: {Subject}) to {ToList}", emailDto.Subject, emailDto.To.ToCommaSeparatedString(true));
            throw;
        }

        return true;
    }

    private async ValueTask<EmailDto> BuildEmailDto(EmailMessage message, CancellationToken cancellationToken)
    {
        message.TemplateFileName ??= _defaultTemplate;
        message.Name ??= _defaultName;
        message.Address ??= _defaultAddress;

        string templateFilePath = Path.Combine(AppContext.BaseDirectory, "LocalResources", "Email", "Templates", message.TemplateFileName);

        string? contentFilePath = null;

        if (message.ContentFileName != null)
            contentFilePath = Path.Combine(AppContext.BaseDirectory, "LocalResources", "Email", "Contents", message.ContentFileName);

        Dictionary<string, object> tokens = message.Tokens != null ? message.Tokens.ToObjectDictionary() : new Dictionary<string, object>();
        tokens.Add("subject", message.Subject);

        string renderedBody;
        if (contentFilePath != null)
        {
            renderedBody = await _templateUtil.RenderWithContent(templateFilePath, tokens, contentFilePath, "bodyText", message.Partials, cancellationToken)
                                              .NoSync();
        }
        else
        {
            renderedBody = await _templateUtil.Render(templateFilePath, tokens, message.Partials, cancellationToken).NoSync();
        }

        return new EmailDto
        {
            Body = renderedBody,
            Subject = message.Subject,
            Name = message.Name,
            Address = message.Address,
            To = message.To,
            Cc = message.Cc,
            Bcc = message.Bcc
        };
    }
}