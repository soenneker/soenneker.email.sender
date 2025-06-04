using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Dtos.Email;
using Soenneker.Email.Mime.Abstract;
using Soenneker.Messages.Email;
using Soenneker.Utils.Json;
using Soenneker.Utils.Template.Abstract;
using Soenneker.Extensions.Dtos.Email;
using System.Threading;
using Soenneker.Email.Senders.Abstract;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.ValueTask;
using Soenneker.Extensions.Messages.Email;

namespace Soenneker.Email.Sender;

///<inheritdoc cref="IEmailSender"/>
public sealed class EmailSender : IEmailSender
{
    private readonly bool _enabled;

    private readonly ILogger<EmailSender> _logger;
    private readonly IMimeUtil _mimeUtil;
    private readonly ITemplateUtil _templateUtil;

    private const string _defaultTemplate = "default.html";

    private readonly string? _senderEmail;
    private readonly string? _senderName;

    public EmailSender(IConfiguration config, ILogger<EmailSender> logger, IMimeUtil mimeUtil, ITemplateUtil templateUtil)
    {
        _logger = logger;
        _mimeUtil = mimeUtil;
        _templateUtil = templateUtil;

        _enabled = config.GetValue<bool>("Smtp:Enable");

        if (!_enabled)
            return;

        _senderEmail = config.GetValueStrict<string>("Smtp:EmailAddress");
        _senderName = config.GetValueStrict<string>("Smtp:Name");
    }

    public async Task<bool> Send(string messageContent, Type? type, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_enabled)
            {
                _logger.LogDebug("{name} has been disabled from config", nameof(EmailSender));
                return false;
            }

            if (type == null)
                throw new Exception("Service bus message did not have a type");

            object? msgModel = JsonUtil.Deserialize(messageContent, type);

            if (msgModel is not EmailMessage message)
                throw new Exception($"Service bus message was not a {typeof(EmailMessage)}");

            EmailDto emailDto = await BuildEmailDto(message, cancellationToken).NoSync();

            var mimeMessage = emailDto.ToMimeMessage(_logger);

            await _mimeUtil.Send(mimeMessage, cancellationToken).NoSync();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Unable to send email: {content}", messageContent);
            throw;
        }

        return true;
    }

    private async ValueTask<EmailDto> BuildEmailDto(EmailMessage message, CancellationToken cancellationToken)
    {
        message.TemplateFile ??= _defaultTemplate;

        string templateFilePath = Path.Combine("Resources", "Email", "Templates", message.TemplateFile);
        string bodyFilePath = Path.Combine("Resources", "Email", "Contents", message.BodyFile);

        Dictionary<string, string> tokensToReplace = message.ToTokenDictionary();

        var tokenObjects = new Dictionary<string, object>();

        foreach (KeyValuePair<string, string> kvp in tokensToReplace)
        {
            tokenObjects[kvp.Key] = kvp.Value;
        }

        string renderedBody = await _templateUtil.Render(
            templateFilePath,
            tokenObjects,
            bodyFilePath,
            cancellationToken: cancellationToken).NoSync();

        return new EmailDto
        {
            Body = renderedBody,
            Subject = message.Subject,
            Name = _senderName!,
            Address = _senderEmail!,
            To = message.To,
            Cc = message.Cc,
            Bcc = message.Bcc
        };
    }
}