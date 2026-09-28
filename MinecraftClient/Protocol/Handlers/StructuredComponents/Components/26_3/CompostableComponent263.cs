using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 compostable: ResolvableInt layers.
/// </summary>
public class CompostableComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public ResolvableInt263 Layers { get; set; } = new(true, 0, null);

    public override void Parse(Queue<byte> data)
    {
        Layers = ResolvableNumbers263.ReadInt(DataTypes, data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        ResolvableNumbers263.WriteInt(DataTypes, bytes, Layers);
        return new Queue<byte>(bytes);
    }
}
