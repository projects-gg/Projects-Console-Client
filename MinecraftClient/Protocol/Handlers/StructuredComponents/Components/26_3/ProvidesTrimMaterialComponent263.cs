using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 provides_trim_material: ByteBufCodecs.holder(TRIM_MATERIAL, TrimMaterial.DIRECT_STREAM_CODEC).
/// 0 = inline TrimMaterial (Identifier palette_id + Component description); non-zero = registry id + 1.
/// </summary>
public class ProvidesTrimMaterialComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int HolderId { get; set; }
    public string? PaletteId { get; set; }
    public Dictionary<string, object>? DescriptionNbt { get; set; }

    public override void Parse(Queue<byte> data)
    {
        HolderId = DataTypes.ReadNextVarInt(data);
        if (HolderId != 0)
            return;

        PaletteId = DataTypes.ReadNextString(data);
        DescriptionNbt = DataTypes.ReadNextNbt(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetVarInt(HolderId));
        if (HolderId != 0)
            return new Queue<byte>(bytes);

        if (PaletteId is null)
            throw new ArgumentNullException(nameof(PaletteId), "Direct trim material payload requires a palette id.");

        bytes.AddRange(DataTypes.GetString(PaletteId));
        bytes.AddRange(DataTypes.GetNbt(DescriptionNbt));
        return new Queue<byte>(bytes);
    }
}
