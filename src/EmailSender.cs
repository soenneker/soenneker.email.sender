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

/// <inheritdoc cref="IEmailSender"/>
public sealed class EmailSender : IEmailSender
{
    private readonly bool _enabled;
    private readonly ILogger<EmailSender> _logger;
    private readonly IMimeUtil _mimeUtil;
    private readonly ITemplateUtil _templateUtil;

    private const string _defaultTemplate = "default.html";

    private readonly string _defaultAddress;
    private readonly string _defaultName;

    // Cache common roots (avoids repeated Path.Combine chains + repeated BaseDirectory lookups)
    private readonly string _templatesRoot;
    private readonly string _contentsRoot;

    public EmailSender(
        IConfiguration configuration,
        ILogger<EmailSender> logger,
        IMimeUtil mimeUtil,
        ITemplateUtil templateUtil)
    {
        _logger = logger;
        _mimeUtil = mimeUtil;
        _templateUtil = templateUtil;

        _enabled = configuration.GetValueStrict<bool>("Email:Enabled");
        _defaultAddress = configuration.GetValueStrict<string>("Email:DefaultAddress");
        _defaultName = configuration.GetValueStrict<string>("Email:DefaultName");

        string baseDir = AppContext.BaseDirectory;
        string localResources = Path.Combine(baseDir, "LocalResources", "Email");
        _templatesRoot = Path.Combine(localResources, "Templates");
        _contentsRoot = Path.Combine(localResources, "Contents");
    }

    public async Task<bool> Send(string messageContent, Type? type, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("{Name} is disabled by config", nameof(EmailSender));
            return false;
        }

        if (type is null)
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
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("{Name} is disabled by config", nameof(EmailSender));
            return false;
        }

        ArgumentNullException.ThrowIfNull(message);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            // ToCommaSeparatedString allocates; do it only if needed
            string toList = message.To.ToCommaSeparatedString();
            _logger.LogInformation("Building and sending email (Subject: {Subject}) to {ToList}", message.Subject, toList);
        }

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

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                string toList = emailDto.To.ToCommaSeparatedString(true);
                _logger.LogDebug("Sending MIME message (Subject: {Subject}) to {ToList}", emailDto.Subject, toList);
            }

            await _mimeUtil.Send(mimeMessage, cancellationToken).NoSync();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                string toList = emailDto.To.ToCommaSeparatedString(true);
                _logger.LogInformation("Email (Subject: {Subject}) successfully sent to {ToList}", emailDto.Subject, toList);
            }
        }
        catch (Exception ex)
        {
            // Only build ToList when we actually log it
            if (_logger.IsEnabled(LogLevel.Error))
            {
                string toList = emailDto.To.ToCommaSeparatedString(true);
                _logger.LogError(ex, "SMTP send failed for message (Subject: {Subject}) to {ToList}", emailDto.Subject, toList);
            }
            else
            {
                _logger.LogError(ex, "SMTP send failed for message (Subject: {Subject})", emailDto.Subject);
            }

            throw;
        }

        return true;
    }

    private async ValueTask<EmailDto> BuildEmailDto(EmailMessage message, CancellationToken cancellationToken)
    {
        message.TemplateFileName ??= _defaultTemplate;
        message.Name ??= _defaultName;
        message.Address ??= _defaultAddress;

        string templateFilePath = Path.Combine(_templatesRoot, message.TemplateFileName);

        // Only build content path when needed
        string? contentFilePath = message.ContentFileName is null
            ? null
            : Path.Combine(_contentsRoot, message.ContentFileName);

        // Avoid allocating an empty dictionary twice; pre-size a bit since we add "subject".
        Dictionary<string, object> tokens;
        if (message.Tokens is null)
        {
            tokens = new Dictionary<string, object>(capacity: 1);
        }
        else
        {
            tokens = message.Tokens.ToObjectDictionary();

            // If ToObjectDictionary might create a tight dictionary, adding capacity here is usually not worth it,
            // but if you control it, consider over-allocating by +1 when constructing inside that method.
        }

        // Use indexer to overwrite rather than throw if already present (safer + avoids exception path)
        tokens["subject"] = message.Subject;

        string renderedBody = contentFilePath is not null
            ? await _templateUtil.RenderWithContent(templateFilePath, tokens, contentFilePath, "bodyText", message.Partials, cancellationToken).NoSync()
            : await _templateUtil.Render(templateFilePath, tokens, message.Partials, cancellationToken).NoSync();

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
