[![](https://img.shields.io/nuget/v/soenneker.email.sender.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.sender/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.sender/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.email.sender/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.email.sender.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.sender/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.sender/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.email.sender/actions/workflows/codeql.yml)

# Soenneker.Email.Sender

Renders `EmailMessage` payloads with Scriban templates, converts them to MIME, and sends them through the configured SMTP utility.

## Install

```bash
dotnet add package Soenneker.Email.Sender
```

## Resource layout

Email resources are loaded relative to the application's output directory:

```text
LocalResources/
  Email/
    Templates/
      default.html
    Contents/
      welcome.html
```

When `TemplateFileName` is null, `default.html` is used. `ContentFileName` is optional; when supplied, that content is rendered first and exposed to the outer template as `bodyText`. Message tokens and partials become Scriban globals, and the `subject` token is always overwritten with the message subject.

Template and content names must resolve beneath their respective directories. Absolute paths and traversal outside those roots are rejected.

## Configuration

```json
{
  "Email": {
    "Enabled": true,
    "DefaultAddress": "mailer@example.com",
    "DefaultName": "Example App"
  },
  "Smtp": {
    "Enable": true,
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "mailer@example.com",
    "Password": "use-a-secret-provider",
    "UseSsl": false,
    "UseStartTls": true
  }
}
```

`Email:Enabled`, `Email:DefaultAddress`, `Email:DefaultName`, and `Smtp:Enable` are read when the sender is constructed. A send returns `false` without rendering when either enable flag is false. SMTP connection settings are documented by `Soenneker.Email.Mime` and should come from a secret provider where appropriate.

## Registration

```csharp
using Soenneker.Email.Sender.Registrars;

services.AddEmailSenderAsSingleton();
```

This registers `IEmailSender` and its MIME and template dependencies as singletons. `AddEmailSenderAsScoped()` registers the sender, MIME utility, and template utility as scoped services while their intentionally shared lower-level dependencies remain reusable according to their own registrations.

## Send an email

```csharp
using Soenneker.Email.Senders.Abstract;
using Soenneker.Enums.Email.Format;
using Soenneker.Enums.Email.Priority;
using Soenneker.Messages.Email;

var message = new EmailMessage
{
    Type = "email.welcome.v1",
    Id = Guid.NewGuid().ToString("N"),
    Queue = "email",
    Sender = "accounts-api",
    CreatedAt = DateTimeOffset.UtcNow,
    To = ["recipient@example.net"],
    Subject = "Welcome, Alex",
    Format = EmailFormat.Html,
    Priority = EmailPriority.Normal,
    ContentFileName = "welcome.html",
    Tokens = new Dictionary<string, string>
    {
        ["first_name"] = "Alex"
    }
};

IEmailSender sender = serviceProvider.GetRequiredService<IEmailSender>();
bool sent = await sender.Send(message, cancellationToken);
```

`Name` and `Address` fall back to the configured defaults. `ReplyTo`, `Format`, `Priority`, `To`, `Cc`, and `Bcc` are carried into the MIME message. Rendering, address parsing, or SMTP failures are logged and rethrown.

The string overload deserializes `messageContent` as `EmailMessage`; its `type` argument is transport metadata and does not select an arbitrary CLR type.

Recipient addresses are logged only as counts at information and error levels. Debug logging includes recipient addresses, and the underlying MIME utility can optionally log full message content, so choose production log levels with email privacy in mind.
