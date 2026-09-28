using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 instrument: ByteBufCodecs.holder(INSTRUMENT, Instrument.DIRECT_STREAM_CODEC).
/// 0 = inline Instrument (SoundEvent holder, float useDuration, float range,
/// VarInt durabilityDamage (new in 26.3), Component description); non-zero = registry id + 1.
/// </summary>
public class InstrumentComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int HolderId { get; set; }
    public SoundEventSubComponent? Sound { get; set; }
    public float UseDuration { get; set; }
    public float Range { get; set; }
    public int DurabilityDamage { get; set; }
    public Dictionary<string, object>? DescriptionNbt { get; set; }

    public override void Parse(Queue<byte> data)
    {
        HolderId = DataTypes.ReadNextVarInt(data);
        if (HolderId != 0)
            return;

        Sound = (SoundEventSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
        UseDuration = DataTypes.ReadNextFloat(data);
        Range = DataTypes.ReadNextFloat(data);
        DurabilityDamage = DataTypes.ReadNextVarInt(data);
        DescriptionNbt = DataTypes.ReadNextNbt(data); // ComponentSerialization.STREAM_CODEC
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetVarInt(HolderId));
        if (HolderId != 0)
            return new Queue<byte>(bytes);

        if (Sound is null)
            throw new ArgumentNullException(nameof(Sound), "Direct instrument payload requires a sound event.");

        bytes.AddRange(Sound.Serialize());
        bytes.AddRange(DataTypes.GetFloat(UseDuration));
        bytes.AddRange(DataTypes.GetFloat(Range));
        bytes.AddRange(DataTypes.GetVarInt(DurabilityDamage));
        bytes.AddRange(DataTypes.GetNbt(DescriptionNbt));
        return new Queue<byte>(bytes);
    }
}
