using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Email.Sender.Abstract;

/// <summary>
/// Interface for sending emails based on a raw service bus message content and its associated type.
/// Wraps logic for rendering templates, transforming content into MIME format, and delivering via SMTP.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email using the given raw message content and type.
    /// </summary>
    /// <param name="messageContent">
    /// The JSON string content of the message. Expected to deserialize into an <see cref="Soenneker.Messages.Email.EmailMessage"/>.
    /// </param>
    /// <param name="type">
    /// The target CLR type to deserialize the message content into. Typically <see cref="Soenneker.Messages.Email.EmailMessage"/>.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous operation, with a boolean indicating whether the send was successful.
    /// </returns>
    Task<bool> Send(string messageContent, Type? type, CancellationToken cancellationToken = default);
}