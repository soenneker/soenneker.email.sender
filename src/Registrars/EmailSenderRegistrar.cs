using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Email.Mime.Registrars;
using Soenneker.Email.Sender.Abstract;
using Soenneker.Utils.Template.Registrars;

namespace Soenneker.Email.Sender.Registrars;

/// <summary>
/// A high-level utility responsible for orchestrating the creation and delivery of templated email messages
/// </summary>
public static class EmailSenderRegistrar
{
    /// <summary>
    /// Adds <see cref="IEmailSender"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddEmailSenderAsSingleton(this IServiceCollection services)
    {
        services.AddMimeUtilAsSingleton().AddTemplateUtilAsSingleton().TryAddSingleton<IEmailSender, EmailSender>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IEmailSender"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddEmailSenderAsScoped(this IServiceCollection services)
    {
        services.AddMimeUtilAsScoped().AddTemplateUtilAsScoped().TryAddScoped<IEmailSender, EmailSender>();

        return services;
    }
}