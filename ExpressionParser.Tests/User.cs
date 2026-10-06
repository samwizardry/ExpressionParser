namespace ExpressionParser.Tests;

public class User
{
    public int Id { get; set; }

    public string String { get; set; } = string.Empty;

    public string? NullableString { get; set; }

    // byte / sbyte
    public byte ByteVal { get; set; }

    public byte? NullableByteVal { get; set; }

    public sbyte SByteVal { get; set; }

    public sbyte? NullableSByteVal { get; set; }

    // short / ushort
    public short ShortVal { get; set; }

    public short? NullableShortVal { get; set; }

    public ushort UShortVal { get; set; }

    public ushort? NullableUShortVal { get; set; }

    // int / uint
    public int IntVal { get; set; }

    public int? NullableIntVal { get; set; }

    public uint UIntVal { get; set; }

    public uint? NullableUIntVal { get; set; }

    // long / ulong
    public long LongVal { get; set; }

    public long? NullableLongVal { get; set; }

    public ulong ULongVal { get; set; }

    public ulong? NullableULongVal { get; set; }

    // float
    public float FloatVal { get; set; }

    public float? NullableFloatVal { get; set; }

    // double
    public double DoubleVal { get; set; }

    public double? NullableDoubleVal { get; set; }

    // decimal
    public decimal DecimalVal { get; set; }

    public decimal? NullableDecimalVal { get; set; }

    // char
    public char CharVal { get; set; }

    public char? NullableCharVal { get; set; }

    // date / time types
    public DateOnly DateOnlyVal { get; set; }

    public DateOnly? NullableDateOnlyVal { get; set; }

    public DateTime DateTimeVal { get; set; }

    public DateTime? NullableDateTimeVal { get; set; }

    public TimeOnly TimeOnlyVal { get; set; }

    public TimeOnly? NullableTimeOnlyVal { get; set; }

    // bool
    public bool BoolVal { get; set; }

    public bool? NullableBoolVal { get; set; }
}
