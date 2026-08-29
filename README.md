[![](https://img.shields.io/nuget/v/soenneker.email.sender.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.sender/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.sender/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.email.sender/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.email.sender.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.email.sender/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.email.sender/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.email.sender/actions/workflows/codeql.yml)

# Soenneker.Email.Sender

A high-level utility responsible for orchestrating the creation and delivery of templated email messages.

## Install

```bash
dotnet add package Soenneker.Email.Sender
```

## Quick start

```csharp
using Soenneker.Email.Sender.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddEmailSenderAsSingleton();
```

Adds `IEmailSender` as a singleton service.

## What you get

- `EmailSenderRegistrar` — A high-level utility responsible for orchestrating the creation and delivery of templated email messages.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `EmailSenderRegistrar.AddEmailSenderAsSingleton(services)` | Adds `IEmailSender` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `EmailSenderRegistrar.AddEmailSenderAsScoped(services)` | Adds `IEmailSender` as a scoped service. | The same service collection, so additional registrations can be chained. |
