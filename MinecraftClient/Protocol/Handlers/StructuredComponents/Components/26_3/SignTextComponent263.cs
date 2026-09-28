using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 sign_text_front / sign_text_back: SignText.STREAM_CODEC =
/// 4 x Component (messages) + Optional&lt;4 x Component&gt; (filtered messages) + DyeColor (VarInt) + bool glowing.
/// </summary>
public class SignTextComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    private const int Lines = 4;

    public List<Dictionary<string, object>> MessagesNbt { get; set; } = [];
    public List<Dictionary<string, object>>? FilteredMessagesNbt { get; set; }
    public int Color { get; set; }
    public bool HasGlowingText { get; set; }

    public override void Parse(Queue<byte> data)
    {
        MessagesNbt = ReadLines(data);
        FilteredMessagesNbt = DataTypes.ReadNextBool(data) ? ReadLines(data) : null;
        Color = DataTypes.ReadNextVarInt(data);
        HasGlowingText = DataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        WriteLines(bytes, MessagesNbt);
        bytes.AddRange(DataTypes.GetBool(FilteredMessagesNbt is not null));
        if (FilteredMessagesNbt is not null)
            WriteLines(bytes, FilteredMessagesNbt);
        bytes.AddRange(DataTypes.GetVarInt(Color));
        bytes.AddRange(DataTypes.GetBool(HasGlowingText));
        return new Queue<byte>(bytes);
    }

    private List<Dictionary<string, object>> ReadLines(Queue<byte> data)
    {
        var lines = new List<Dictionary<string, object>>(Lines);
        for (var i = 0; i < Lines; i++)
            lines.Add(DataTypes.ReadNextNbt(data)); // ComponentSerialization.STREAM_CODEC
        return lines;
    }

    private void WriteLines(List<byte> bytes, List<Dictionary<string, object>> lines)
    {
        // fixedSizeList(4): exactly four entries, no length prefix
        for (var i = 0; i < Lines; i++)
            bytes.AddRange(DataTypes.GetNbt(i < lines.Count ? lines[i] : null));
    }
}
