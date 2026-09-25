using System.Threading.Tasks;
using Soenneker.Enums.Email.Format;
using Soenneker.Enums.Email.Priority;
using Soenneker.Messages.Email;
using Soenneker.Utils.Json;

namespace Soenneker.Email.Sender.Tests;

public class JsonMetadataTests
{
    [Test]
    public async Task Email_contract_includes_package_enum_metadata()
    {
        const string json = """
            {"type":"email","id":"1","queue":"email","sender":"test","createdAt":"2026-01-01T00:00:00Z",
             "to":["recipient@example.com"],"subject":"test","format":"Html","priority":"Normal"}
            """;
        var metadata = LibraryJsonContext.Get<EmailMessage>();
        EmailMessage value = JsonUtil.Deserialize(json, metadata)!;
        await Assert.That(value.Format).IsEqualTo(EmailFormat.Html);
        await Assert.That(value.Priority).IsEqualTo(EmailPriority.Normal);
        EmailMessage roundTrip = JsonUtil.Deserialize(JsonUtil.Serialize(value, metadata), metadata)!;
        await Assert.That(roundTrip.Subject).IsEqualTo("test");
        await Assert.That(roundTrip.Format).IsEqualTo(EmailFormat.Html);
    }
}
