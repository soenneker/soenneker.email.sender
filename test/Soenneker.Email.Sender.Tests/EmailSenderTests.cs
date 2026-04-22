using Soenneker.Tests.HostedUnit;

namespace Soenneker.Email.Sender.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class EmailSenderTests : HostedUnitTest
{
    public EmailSenderTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
