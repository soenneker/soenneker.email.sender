using Soenneker.Email.Sender.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Email.Sender.Tests;

[Collection("Collection")]
public class EmailSenderTests : FixturedUnitTest
{
    private readonly IEmailSender _util;

    public EmailSenderTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IEmailSender>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
