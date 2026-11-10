using Taurus.Infrastructure.Configuration.Http;

namespace Taurus.Tests.tests;

public class HttpOptionsTests
{
    private const string EmptyPrefix = "";
    private const string NestedPrefix = "/identity/taurus";
    private const string RelativePrefix = "identity";
    private const string TrailingSlash = "/identity/";
    private const string QueryPrefix = "/identity?query";
    private const string EmptySegment = "/identity//taurus";
    private const string Loopback = "http://127.0.0.1:5080";
    private const string Public = "http://example.com";

    [Fact]
    public void PrefixCanBeEmptyOrNestedButMustBeCanonical()
    {
        new HttpOptions { RequestPrefix = EmptyPrefix }.Validate();
        new HttpOptions { RequestPrefix = NestedPrefix }.Validate();

        foreach (var prefix in new[]
        {
            RelativePrefix, TrailingSlash, QueryPrefix, EmptySegment
        })
        {
            Assert.Throws<InvalidOperationException>(() =>
                new HttpOptions { RequestPrefix = prefix }.Validate());
        }
    }

    [Fact]
    public void HttpDevelopmentRequiresExplicitLiteralLoopback()
    {
        var options = new HttpOptions();

        Assert.False(options.AllowsDevelopmentOrigin(new Uri(Loopback)));

        options.AllowLoopbackHttp = true;

        Assert.True(options.AllowsDevelopmentOrigin(new Uri(Loopback)));
        Assert.False(options.AllowsDevelopmentOrigin(new Uri(Public)));
    }
}
