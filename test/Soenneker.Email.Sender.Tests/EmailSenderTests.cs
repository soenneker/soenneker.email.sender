using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Email.Sender.Tests;

[Collection("Collection")]
public class EmailSenderTests : FixturedUnitTest
{
    public EmailSenderTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {

    }
}
