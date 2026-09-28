using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_1;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 mob_visibility: HolderSet&lt;EntityType&gt; targetingEntityTypes + float visibility.
/// </summary>
public class MobVisibilityComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : HolderSetComponent261(dataTypes, itemPalette, subComponentRegistry)
{
    public float Visibility { get; set; }

    public override void Parse(Queue<byte> data)
    {
        base.Parse(data);
        Visibility = DataTypes.ReadNextFloat(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = base.Serialize().ToList();
        bytes.AddRange(DataTypes.GetFloat(Visibility));
        return new Queue<byte>(bytes);
    }
}
