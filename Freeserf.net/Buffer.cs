/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Freeserf
{
    using Endianess = Endian.Endianess;

    internal static class TypeSize<T>
    {
        public readonly static int Size;

        static TypeSize()
        {
            var dm = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Sizeof, typeof(T));
            il.Emit(OpCodes.Ret);
            Size = (int)dm.Invoke(null, null);
        }
    }

    unsafe public class Buffer
    {
        protected byte* data;
        protected int size;
        protected bool owned;
        protected Buffer parent;
        protected byte* read;
        protected Endianess endianess;

        public Buffer(Endianess endianess)
        {

        }

        public Buffer(byte* data, int size, Endianess endianess)
        {

        }

        public Buffer(Buffer parent, int start, int length)
        {

        }

        public Buffer(Buffer parent, int start, int length, Endianess endianess)
        {

        }

        public Buffer(string path, Endianess endianess)
        {

        }

        public int Size => size;
        public byte* Data => data;

        public byte* Unfix()
        {

        }

        public void SetEndianess(Endianess endianess)
        {
            this.endianess = endianess;
        }

        public bool Readable()
        {

        }

        public Buffer Pop(int size)
        {

        }

        public Buffer PopTail()
        {

        }

        object PopInternal(int size)
        {
            object result = null;

            switch (size)
            {
                case 1:
                    result = *read;
                    break;
                case 2:
                    result = *(ushort*)read;
                    break;
                case 4:
                    result = *(uint*)read;
                    break;
                case 8:
                    result = *(ulong*)read;
                    break;
                default:
                    throw new NotSupportedException("Invalid type size.");
            }

            read += size;

            return result;
        }

        public T Pop<T>() where T : struct
        {
            T value = (T)Convert.ChangeType(PopInternal(TypeSize<T>.Size), typeof(T));

            return (endianess == Endianess.Big) ? Endian.Betoh(value) : Endian.Letoh(value);
        }

        public Buffer GetSubBuffer(int offset, int size)
        {

        }

        public Buffer GetTail(int offset)
        {

        }

        public static explicit operator string(Buffer buffer)
        {
            return new string((char*)data, 0, size);
        }

        public bool Write(string path)
        {

        }

        protected byte* Offset(int offset)
        {
            return (byte*)data + offset;
        }
    }

    unsafe public class MutableBuffer : Buffer
    {
        protected int reserved;
        protected int growth;

        public MutableBuffer(Endianess endianess)
        {

        }

        public MutableBuffer(int size, Endianess endianess)
        {

        }

        public void Push(Buffer buffer)
        {

        }

        public void Push(byte* data, int size)
        {

        }

        public void Push(string str)
        {

        }

        public void Push(char* str)
        {
            Push(new string(str));
        }

        public void Push<T>(T value, int count = 1)
        {
            CheckSize(size + (TypeSize<T>.Size * count));

            for (int i = 0; i < count; i++)
            {
                byte* pointer = Offset(size);

                T pointerValue = (T)Convert.ChangeType(pointer, typeof(T));
                Offset(size)

                *(reinterpret_cast<T*>(offset(size))) = (endianess == EndianessBig) ?
                                                          htobe(value) :
                                                          htole(value);
                size += sizeof(T);
            }
        }

        protected void CheckSize(int size)
        {

        }

        static void ConvertTo<T>(byte* pointer, T value, bool movePointer) where T : struct
        {
            object destination = null;
            int size = TypeSize<T>.Size;

            switch (size)
            {
                case 1:
                    *(byte*)pointer = *(byte*)value;
                    break;
                case 2:
                    destination = *(ushort*)pointer;
                    break;
                case 4:
                    destination = *(uint*)pointer;
                    break;
                case 8:
                    destination = *(ulong*)pointer;
                    break;
                default:
                    throw new NotSupportedException("Invalid type size.");
            }

            destination = value;
        }

        static T ConvertFrom<T>(byte* pointer, bool movePointer)
        {
            object result = null;
            int size = TypeSize<T>.Size;

            switch (size)
            {
                case 1:
                    result = *(byte*)pointer;
                    break;
                case 2:
                    result = *(ushort*)pointer;
                    break;
                case 4:
                    result = *(uint*)pointer;
                    break;
                case 8:
                    result = *(ulong*)pointer;
                    break;
                default:
                    throw new NotSupportedException("Invalid type size.");
            }

            if (movePointer)
                pointer += size;

            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
}
*/