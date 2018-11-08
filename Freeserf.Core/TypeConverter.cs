using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Freeserf
{
    class TypeConverter : IConvertible
    {
        public TypeConverter(ulong Value)
        {
            field.UlongValue = Value;
        }

        public TypeConverter() : this(0)
        {
        }

        public ulong Field
        {
            set { field.UlongValue = value; }
            get { return field.UlongValue; }
        }


        ULongStruct field;

        [StructLayout(LayoutKind.Explicit)]
        struct ULongStruct
        {
            [FieldOffset(0)]
            public ulong UlongValue;

            [FieldOffset(0)]
            public float FloatValue;

            [FieldOffset(0)]
            public double DoubleValue;

            [FieldOffset(0)]
            public uint UIntValue;

            [FieldOffset(0)]
            public int IntValue;

            [FieldOffset(0)]
            public ushort UShortValue;

            [FieldOffset(0)]
            public short ShortValue;

            [FieldOffset(0)]
            public byte ByteValue;

            [FieldOffset(0)]
            public sbyte SByteValue;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return field.ByteValue != 0;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return field.ByteValue;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return (char)field.SByteValue;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(field.UlongValue);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(field.DoubleValue);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return field.DoubleValue;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return field.ShortValue;
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return field.IntValue;
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return (long)field.UlongValue;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return field.SByteValue;
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return field.FloatValue;
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return String.Format("({0})", field.UlongValue);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(field.UlongValue, conversionType);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return field.UShortValue;
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return field.UIntValue;
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return field.UlongValue;
        }
    }
}
