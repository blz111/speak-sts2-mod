using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace Sts2Speak.Messages;

public struct ChatBroadcastMessage : INetMessage, IPacketSerializable
{
    public string MessageId;
    public ulong SenderId;
    public string Text;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(MessageId ?? string.Empty);
        writer.WriteULong(SenderId);
        writer.WriteString(Text ?? string.Empty);
    }

    public void Deserialize(PacketReader reader)
    {
        MessageId = reader.ReadString();
        SenderId = reader.ReadULong();
        Text = reader.ReadString();
    }
}
