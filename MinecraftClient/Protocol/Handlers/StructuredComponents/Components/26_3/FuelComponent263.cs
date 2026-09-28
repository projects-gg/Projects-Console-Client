using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._26_3;

/// <summary>
/// 26.3 cooking_fuel (burnTime, speedMultiplier) and brewing_fuel (uses, speedMultiplier):
/// ResolvableInt + ResolvableFloat.
/// </summary>
public class FuelComponent263(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public ResolvableInt263 Amount { get; set; } = new(true, 0, null);
    public ResolvableFloat263 SpeedMultiplier { get; set; } = new(true, 0f, null);

    public override void Parse(Queue<byte> data)
    {
        Amount = ResolvableNumbers263.ReadInt(DataTypes, data);
        SpeedMultiplier = ResolvableNumbers263.ReadFloat(DataTypes, data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        ResolvableNumbers263.WriteInt(DataTypes, bytes, Amount);
        ResolvableNumbers263.WriteFloat(DataTypes, bytes, SpeedMultiplier);
        return new Queue<byte>(bytes);
    }
}

/// <summary>ResolvableInt: Either&lt;Constant(int), Reference(ResourceKey&lt;ContextIntProvider&gt;)&gt;.</summary>
public sealed record ResolvableInt263(bool IsConstant, int Value, string? Key);

/// <summary>ResolvableFloat: Either&lt;Constant(float), Reference(ResourceKey&lt;ContextFloatProvider&gt;)&gt;.</summary>
public sealed record ResolvableFloat263(bool IsConstant, float Value, string? Key);

/// <summary>
/// Wire format (ByteBufCodecs.either): bool true = left/Constant, false = right/Reference.
/// Int constant is ByteBufCodecs.INT (fixed 4-byte big-endian, not VarInt); float constant is ByteBufCodecs.FLOAT.
/// Reference is an Identifier string.
/// </summary>
internal static class ResolvableNumbers263
{
    public static ResolvableInt263 ReadInt(DataTypes dataTypes, Queue<byte> data)
    {
        return dataTypes.ReadNextBool(data)
            ? new ResolvableInt263(true, dataTypes.ReadNextInt(data), null)
            : new ResolvableInt263(false, 0, dataTypes.ReadNextString(data));
    }

    public static ResolvableFloat263 ReadFloat(DataTypes dataTypes, Queue<byte> data)
    {
        return dataTypes.ReadNextBool(data)
            ? new ResolvableFloat263(true, dataTypes.ReadNextFloat(data), null)
            : new ResolvableFloat263(false, 0f, dataTypes.ReadNextString(data));
    }

    public static void WriteInt(DataTypes dataTypes, List<byte> bytes, ResolvableInt263 value)
    {
        bytes.AddRange(dataTypes.GetBool(value.IsConstant));
        if (value.IsConstant)
        {
            var raw = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(raw, value.Value);
            bytes.AddRange(raw);
        }
        else
        {
            bytes.AddRange(dataTypes.GetString(value.Key ?? throw new ArgumentNullException(nameof(value), "ResolvableInt reference key is missing.")));
        }
    }

    public static void WriteFloat(DataTypes dataTypes, List<byte> bytes, ResolvableFloat263 value)
    {
        bytes.AddRange(dataTypes.GetBool(value.IsConstant));
        if (value.IsConstant)
            bytes.AddRange(dataTypes.GetFloat(value.Value));
        else
            bytes.AddRange(dataTypes.GetString(value.Key ?? throw new ArgumentNullException(nameof(value), "ResolvableFloat reference key is missing.")));
    }
}
