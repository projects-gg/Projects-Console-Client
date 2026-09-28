using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 pot_decorations: four fixed slots (back, left, right, front),
/// each Optional&lt;ItemStackTemplate&gt; (bool present + ItemStackTemplate).
/// Before 26.3 this was a list (max 4) of item registry ids.
/// </summary>
public class PotDecorationsComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public Item? Back { get; set; }
    public Item? Left { get; set; }
    public Item? Right { get; set; }
    public Item? Front { get; set; }

    public override void Parse(Queue<byte> data)
    {
        Back = ReadOptionalTemplate(data);
        Left = ReadOptionalTemplate(data);
        Right = ReadOptionalTemplate(data);
        Front = ReadOptionalTemplate(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        WriteOptionalTemplate(data, Back);
        WriteOptionalTemplate(data, Left);
        WriteOptionalTemplate(data, Right);
        WriteOptionalTemplate(data, Front);
        return new Queue<byte>(data);
    }

    private Item? ReadOptionalTemplate(Queue<byte> data)
    {
        return DataTypes.ReadNextBool(data) ? DataTypes.ReadNextItemStackTemplate(data, ItemPalette) : null;
    }

    private void WriteOptionalTemplate(List<byte> data, Item? item)
    {
        data.AddRange(DataTypes.GetBool(item is not null));
        if (item is not null)
            data.AddRange(DataTypes.GetItemStackTemplate(item, ItemPalette));
    }
}
