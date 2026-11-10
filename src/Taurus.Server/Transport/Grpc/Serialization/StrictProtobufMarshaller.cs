using Google.Protobuf;
using Grpc.Core;

namespace Taurus.Server.Transport.Grpc.Serialization;

public static class StrictProtobufMarshaller
{
    public static Marshaller<T> Create<T>() where T : class, IMessage<T>, new() =>
        Marshallers.Create<T>(
            message => message.ToByteArray(),
            bytes => Parse<T>(bytes));

    public static T Parse<T>(byte[] bytes) where T : class, IMessage<T>, new()
    {
        var parser = new MessageParser<T>(() => new T());
        var parsed = parser.ParseFrom(bytes);
        var discarded = parser.WithDiscardUnknownFields(true).ParseFrom(bytes);

        if (!parsed.Equals(discarded))
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Request contains undocumented fields"));

        return discarded;
    }
}
