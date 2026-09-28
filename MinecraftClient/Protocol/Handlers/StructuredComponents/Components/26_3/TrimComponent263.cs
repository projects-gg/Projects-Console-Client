using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 trim uses ArmorTrim.STREAM_CODEC: Holder&lt;TrimMaterial&gt; + Holder&lt;TrimPattern&gt;.
/// Holder value 0 = inline data, non-zero = registry id + 1.
/// 26.3 inline TrimMaterial is Identifier palette_id + Component description
/// (was MaterialAssetGroup + Component before). Inline TrimPattern is unchanged.
/// </summary>
public class TrimComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int MaterialHolderValue { get; set; }
    public DirectTrimMaterial263? DirectMaterial { get; set; }
    public int PatternHolderValue { get; set; }
    public DirectTrimPattern263? DirectPattern { get; set; }

    public override void Parse(Queue<byte> data)
    {
        MaterialHolderValue = DataTypes.ReadNextVarInt(data);
        if (MaterialHolderValue == 0)
        {
            var paletteId = DataTypes.ReadNextString(data);
            var descriptionNbt = DataTypes.ReadNextNbt(data);
            DirectMaterial = new DirectTrimMaterial263(paletteId, descriptionNbt, ChatParser.ParseText(descriptionNbt));
        }

        PatternHolderValue = DataTypes.ReadNextVarInt(data);
        if (PatternHolderValue == 0)
        {
            var assetId = DataTypes.ReadNextString(data);
            var descriptionNbt = DataTypes.ReadNextNbt(data);
            var decal = DataTypes.ReadNextBool(data);
            DirectPattern = new DirectTrimPattern263(assetId, descriptionNbt, ChatParser.ParseText(descriptionNbt), decal);
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(MaterialHolderValue));
        if (MaterialHolderValue == 0)
        {
            if (DirectMaterial is null)
                throw new ArgumentNullException(nameof(DirectMaterial), "Direct trim material payload is required when holder value is 0.");
            data.AddRange(DataTypes.GetString(DirectMaterial.PaletteId));
            data.AddRange(DataTypes.GetNbt(DirectMaterial.DescriptionNbt));
        }

        data.AddRange(DataTypes.GetVarInt(PatternHolderValue));
        if (PatternHolderValue == 0)
        {
            if (DirectPattern is null)
                throw new ArgumentNullException(nameof(DirectPattern), "Direct trim pattern payload is required when holder value is 0.");
            data.AddRange(DataTypes.GetString(DirectPattern.AssetId));
            data.AddRange(DataTypes.GetNbt(DirectPattern.DescriptionNbt));
            data.AddRange(DataTypes.GetBool(DirectPattern.Decal));
        }

        return new Queue<byte>(data);
    }
}

public sealed record DirectTrimMaterial263(
    string PaletteId,
    Dictionary<string, object> DescriptionNbt,
    string Description);

public sealed record DirectTrimPattern263(
    string AssetId,
    Dictionary<string, object> DescriptionNbt,
    string Description,
    bool Decal);
