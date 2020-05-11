/*
 * Buffer.cs - Memory buffer implementation
 *
 * Copyright (C) 2017       Wicked_Digger <wicked_digger@mail.ru>
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Freeserf.Data
{
    using Endianess = Endian.Endianess;

    unsafe public class BufferStream
    {
        byte[] data;
        byte* start;

        public static BufferStream CreateFromValues<T>(T value, uint count)
        {
            var size = (uint)Marshal.SizeOf<T>() * count;

            var stream = new BufferStream(size);
            stream.Size = size;
            dynamic v = value;

            fixed (byte* pointer = stream.data)
            {
                switch (Marshal.SizeOf<T>())
                {
                    case 0:
                        break;
                    case 1:
                        Misc.CopyByte(pointer, (byte)v, count);
                        break;
                    case 2:
                        Misc.CopyWord(pointer, (ushort)v, count);
                        break;
                    case 4:
                        Misc.CopyDWord(pointer, (uint)v, count);
                        break;
                    case 8:
                        Misc.CopyQWord(pointer, (ulong)v, count);
                        break;
                }
            }

            return stream;
        }

        public BufferStream(uint capacity)
        {
            data = new byte[capacity];

            fixed (byte* pointer = data)
            {
                start = pointer;
            }
        }

        public BufferStream(byte* data, uint size)
        {
            start = data;
            this.Size = size;
        }

        public BufferStream(byte[] data)
            : this(data, 0u, (uint)data.Length)
        {

        }

        public BufferStream(ushort[] data)
        {
            var size = (uint)data.Length << 1;

            this.data = new byte[size];

            fixed (ushort* dataPointer = data)
            fixed (byte* pointer = this.data)
            {
                byte* src = (byte*)dataPointer;
                byte* end = src + size;
                byte* dst = pointer;

                while (src < end)
                    *dst++ = *src++;

                start = pointer;
            }

            Size = size;
        }

        public BufferStream(byte[] data, uint offset, uint size)
        {
            if (offset + size > data.Length)
                throw new IndexOutOfRangeException("The combination of offset and size exceeds base stream size.");

            this.data = new byte[size];

            System.Buffer.BlockCopy(data, (int)offset, this.data, 0, (int)size);

            fixed (byte* pointer = this.data)
            {
                start = pointer;
            }

            Size = size;
        }

        public BufferStream(BufferStream baseStream, uint offset, uint size)
        {
            if (offset + size > baseStream.Size)
                throw new IndexOutOfRangeException("The combination of offset and size exceeds base stream size.");

            start = baseStream.start + offset;
            Size = size;
        }

        public void Realloc(uint newSize)
        {
            if (data == null)
            {
                Size = newSize;
                return;
            }

            var temp = new byte[Size];
            System.Buffer.BlockCopy(data, 0, temp, 0, temp.Length);

            data = new byte[newSize];

            fixed (byte* pointer = data)
            {
                start = pointer;
            }

            System.Buffer.BlockCopy(temp, 0, data, 0, Math.Min(temp.Length, data.Length));

            temp = null;
        }

        public uint Size { get; private set; }

        unsafe public byte* GetPointer()
        {
            return start;
        }

        internal byte[] GetData()
        {
            if (this.data != null)
                return this.data;

            byte[] data = new byte[Size];

            fixed (byte* ptr = data)
            {
                System.Buffer.MemoryCopy(start, ptr, data.Length, data.Length);
            }

            return data;
        }

        public void CopyTo(BufferStream stream)
        {
            int offset = 0;

            if (stream.data == null)
            {
                // TODO: should we add data from base stream before the copy action? is there a use case?

                stream.data = new byte[Size];
                stream.Size = Size;

                fixed (byte* pointer = stream.data)
                {
                    stream.start = pointer;
                }
            }
            else
            {
                offset = (int)stream.Size;
                stream.Size += Size;
            }

            // Note: If stream.data already exists we might want to check for size exceeding here.
            // But as the Buffer class handles this, we omit it here.

            if (data != null)
                System.Buffer.BlockCopy(data, 0, stream.data, offset, (int)Size);
            else
            {
                byte* current = start;
                byte* end = start + Size;
                byte* streamCurrent = stream.start + offset;

                while (current != end)
                    *streamCurrent++ = *current++;
            }
        }

        public void CopyTo(Stream stream)
        {
            byte[] buffer = new byte[Size];

            if (data != null)
                System.Buffer.BlockCopy(data, 0, buffer, 0, (int)Size);
            else
            {
                byte* current = start;
                byte* end = start + Size;

                fixed (byte* bufPointer = buffer)
                {
                    byte* destination = bufPointer;

                    while (current != end)
                        *destination++ = *current++;
                }
            }

            stream.Write(buffer, 0, buffer.Length);
        }
    }

    unsafe public class Buffer : IDisposable
    {
        protected BufferStream data = null;
        protected bool owned;
        protected Buffer parent;
        protected byte* read;
        protected Endianess endianess;

        static Endianess ChooseEndianess(Endianess endianess)
        {
            if (endianess == Endianess.Default)
                return Endian.HostEndianess;

            return endianess;
        }

        public static Buffer CreateFromValues<T>(T value, uint count, Endianess endianess = Endianess.Default)
        {
            var buffer = new Buffer((uint)Marshal.SizeOf<T>() * count, endianess);

            buffer.data = BufferStream.CreateFromValues(value, count);

            return buffer;
        }

        protected Buffer(uint capacity, Endianess endianess = Endianess.Default)
        {
            if (capacity > 0)
            {
                data = new BufferStream(capacity);
                read = data.GetPointer();
            }

            owned = true;
            this.endianess = ChooseEndianess(endianess);
        }

        public Buffer(Endianess endianess = Endianess.Default)
        {
            owned = true;
            this.endianess = ChooseEndianess(endianess);
        }

        public Buffer(byte* data, uint size, Endianess endianess = Endianess.Default)
        {
            this.data = new BufferStream(data, size);
            owned = false;
            this.endianess = ChooseEndianess(endianess);
            read = this.data.GetPointer();
        }

        public Buffer(byte[] data, Endianess endianess = Endianess.Default)
        {
            this.data = new BufferStream(data);
            owned = true;
            this.endianess = ChooseEndianess(endianess);
            read = this.data.GetPointer();
        }

        public Buffer(ushort[] data, Endianess endianess = Endianess.Default)
        {
            this.data = new BufferStream(data);
            owned = true;
            this.endianess = ChooseEndianess(endianess);
            read = this.data.GetPointer();
        }

        public Buffer(Buffer parent, uint start, uint length)
        {
            data = new BufferStream(parent.data, start, length);
            owned = false;
            this.endianess = ChooseEndianess(parent.endianess);
            read = this.data.GetPointer();
        }

        public Buffer(Buffer parent, uint start, uint length, Endianess endianess)
        {
            data = new BufferStream(parent.data, start, length);
            owned = false;
            this.endianess = ChooseEndianess(endianess);
            read = data.GetPointer();
        }

        public Buffer(string path, Endianess endianess = Endianess.Default)
        {
            owned = true;
            this.endianess = ChooseEndianess(endianess);

            Stream fileStream;

            try
            {
                fileStream = File.OpenRead(path);
            }
            catch
            {
                throw new ExceptionFreeserf(ErrorSystemType.Data, $"Failed to open file '{path}'");
            }

            byte[] data = new byte[fileStream.Length];

            fileStream.Read(data, 0, data.Length);

            this.data = new BufferStream(data);

            fileStream.Close();

            read = this.data.GetPointer();
        }

        public uint Size => (data == null) ? 0 : data.Size;
        public byte* Data => (data == null) ? null : data.GetPointer();

        public virtual byte[] Unfix()
        {
            byte[] result = new byte[data.Size];

            System.Buffer.BlockCopy(data.GetData(), 0, result, 0, result.Length);

            data = null;

            return result;
        }

        public void SetEndianess(Endianess endianess)
        {
            this.endianess = endianess;
        }

        public bool Readable()
        {
            return (read - Data) != Size;
        }

        public byte PeekByte()
        {
            return *read;
        }

        public byte PeekByte(uint offset)
        {
            return *(read + offset);
        }

        public void Skip(uint size)
        {
            uint offset = (uint)(read - Data);

            if (offset + size > Size)
                throw new IndexOutOfRangeException("Given size exceeds buffer size.");

            read += size;
        }

        public Buffer Pop(uint size)
        {
            uint offset = (uint)(read - Data);

            if (offset + size > Size)
                throw new IndexOutOfRangeException("Given size exceeds buffer size.");

            var subBuffer = new Buffer(this, offset, size, endianess);

            read += size;

            return subBuffer;
        }

        public Buffer PopTail()
        {
            uint offset = (uint)(read - Data);

            return Pop(Size - offset);
        }

        ulong PopInternal(uint size)
        {
            uint offset = (uint)(read - Data);

            if (offset + size > Size)
                throw new IndexOutOfRangeException("Read beyond buffer size.");

            ulong result = 0;

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
            ulong val = PopInternal((uint)Marshal.SizeOf<T>());

            T value = (T)Convert.ChangeType(new TypeConverter(val), typeof(T));

            return (endianess == Endianess.Big) ? Endian.Betoh(value) : Endian.Letoh(value);
        }

        public Buffer GetSubBuffer(uint offset, uint size)
        {
            return new Buffer(this, offset, size, endianess);
        }

        public Buffer GetTail(uint offset)
        {
            return GetSubBuffer(offset, Size - offset);
        }

        public override string ToString()
        {
            byte* end = Data + Size;

            return new string((sbyte*)read, 0, (int)(end - read));
        }

        public string ToString(int length)
        {
            byte* end = Data + Size;
            int num = Math.Min(length, (int)(end - read));

            return new string((sbyte*)read, 0, num);
        }

        unsafe public byte[] ReinterpretAsArray(uint length)
        {
            byte* end = Data + Size;
            uint remaining = (uint)(end - read);

            if (remaining < length)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Buffer is not large enough.");

            byte[] array = new byte[length];

            fixed (byte* ptr = array)
            {
                System.Buffer.MemoryCopy(read, ptr, length, length);
            }

            return array;
        }

        protected void CopyTo(Buffer other)
        {
            if (data == null)
                throw new ExceptionFreeserf(ErrorSystemType.Data, "Tried to copy an empty buffer.");

            if (other.data == null)
                other.data = new BufferStream(Size);

            data.CopyTo(other.data);
        }

        protected void CopyFrom(Buffer other)
        {
            other.CopyTo(this);
        }

        public bool Write(string path)
        {
            Stream fileStream;

            try
            {
                fileStream = File.Create(path);
            }
            catch
            {
                return false;
            }

            try
            {
                if (!fileStream.CanWrite)
                    throw new ExceptionFreeserf(ErrorSystemType.Data, $"Unable to write to readonly file '{path}'");

                data.CopyTo(fileStream);
            }
            finally
            {
                fileStream.Close();
            }

            return true;
        }

        protected byte* Offset(int offset)
        {
            return Data + offset;
        }


        #region IDisposable Support

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    data = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }

    unsafe public class MutableBuffer : Buffer
    {
        protected uint reserved;
        protected uint growth = 1000u;

        public MutableBuffer(Endianess endianess = Endianess.Default)
            : base(endianess)
        {

        }

        public MutableBuffer(uint reserved, Endianess endianess = Endianess.Default)
            : base(reserved, endianess)
        {
            this.reserved = reserved;
        }

        public override unsafe byte[] Unfix()
        {
            reserved = 0;

            return base.Unfix();
        }

        public void Push(Buffer buffer)
        {
            uint size = buffer.Size;

            CheckSize(Size + size);

            CopyFrom(buffer);
        }

        public void Push(byte* data, uint size)
        {
            Push(new Buffer(data, size, endianess));
        }

        public void Push(string str)
        {
            var bytes = Settings.Encoding.GetBytes(str);

            Push(new Buffer(bytes, endianess));
        }

        public void Push(char* str)
        {
            Push(new string(str));
        }

        public void Push<T>(T value, uint count = 1)
        {
            CheckSize(Size + ((uint)Marshal.SizeOf<T>() * count));

            Push(CreateFromValues(value, count, endianess));
        }

        protected void CheckSize(uint size)
        {
            if (this.reserved >= size)
            {
                return;
            }

            uint reserved = Math.Max(Size + growth, size);

            if (data == null)
            {
                data = new BufferStream(reserved);
                owned = true;
            }
            else
            {
                data.Realloc(reserved);
            }

            read = data.GetPointer();

            this.reserved = reserved;
        }
    }
}