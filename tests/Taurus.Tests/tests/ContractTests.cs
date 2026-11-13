using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Taurus.Server.Transport.Grpc.Administration;
using Taurus.Server.Transport.Grpc.Serialization;
using Taurus.Shared;

namespace Taurus.Tests.tests;

public class ContractTests
{
    private const int UnknownFieldTag = 120;
    private const int UnknownValue = 1;
    private const int ExpectedSessionFields = 6;
    private const int UserIdIndex = 0;
    private const int UsernameIndex = 1;
    private const int LocaleIndex = 2;
    private const int TimezoneIndex = 3;
    private const int SessionIdIndex = 4;
    private const int CsrfSeedIndex = 5;

    [Fact]
    public void UnknownRequestFieldsAreRejected()
    {
        using var output = new MemoryStream();
        using var writer = new CodedOutputStream(output, true);

        writer.WriteRawTag(UnknownFieldTag);
        writer.WriteInt32(UnknownValue);
        writer.Flush();

        Assert.Throws<Grpc.Core.RpcException>(() =>
            StrictProtobufMarshaller.Parse<EmptyRequest>(output.ToArray()));
    }

    [Fact]
    public void UnknownFieldsInsideNestedMessagesAreRejected()
    {
        using var output = new MemoryStream();
        using var writer = new CodedOutputStream(output, true);

        writer.WriteRawTag(UnknownFieldTag);
        writer.WriteInt32(UnknownValue);
        writer.Flush();

        var message = new Struct();

        message.Fields.Add(nameof(Value), Value.Parser.ParseFrom(output.ToArray()));

        Assert.Throws<Grpc.Core.RpcException>(() =>
            StrictProtobufMarshaller.Parse<Struct>(message.ToByteArray()));
    }

    [Fact]
    public void AdministrationAccessUsesExplicitEmptyContracts()
    {
        var method = AdministrationReflection.Descriptor.Services[default(int)]
            .Methods[default(int)];

        Assert.Equal(nameof(AdministrationRpc.CheckAccess), method.Name);
        Assert.Same(EmptyRequest.Descriptor, method.InputType);
        Assert.Same(EmptyResponse.Descriptor, method.OutputType);
        Assert.Empty(new EmptyResponse().ToByteArray());
    }

    [Fact]
    public void SessionResponseHasOnlyDocumentedFields()
    {
        var fields = SessionResponse.Descriptor.Fields.InFieldNumberOrder();

        Assert.Equal(ExpectedSessionFields, fields.Count);
        Assert.Equal(nameof(SessionResponse.UserId), fields[UserIdIndex].PropertyName);
        Assert.Equal(nameof(SessionResponse.Username),
            fields[UsernameIndex].PropertyName);
        Assert.Equal(nameof(SessionResponse.Locale), fields[LocaleIndex].PropertyName);
        Assert.Equal(nameof(SessionResponse.Timezone),
            fields[TimezoneIndex].PropertyName);
        Assert.Equal(nameof(SessionResponse.SessionId),
            fields[SessionIdIndex].PropertyName);
        Assert.Equal(nameof(SessionResponse.CsrfSeed),
            fields[CsrfSeedIndex].PropertyName);
        Assert.Empty(new EmptyResponse().ToByteArray());
    }
}
