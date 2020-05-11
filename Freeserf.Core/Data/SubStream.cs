/*
 * SubStream.cs - Partial stream
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.Data
{
    internal class SubStream : Stream
    {
        Stream baseStream;
        readonly long offset;
        long length;
        long position;
        readonly bool disposeBaseStream;

        public SubStream(Stream baseStream, long length, bool disposeBaseStream = false)
            : this(baseStream, baseStream.Position, length, disposeBaseStream)
        {

        }

        public SubStream(Stream baseStream, long offset, long length, bool disposeBaseStream = false)
        {
            if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanSeek) throw new ArgumentException("Base stream does not support seeking.");
            if (!baseStream.CanRead) throw new ArgumentException("Base stream does not support reading.");
            if (offset < 0 || offset >= baseStream.Length) throw new ArgumentOutOfRangeException(nameof(offset));

            this.baseStream = baseStream;
            this.length = length;
            this.disposeBaseStream = disposeBaseStream;
            this.offset = offset;
            position = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            long remaining = length - position;
            if (remaining <= 0) return 0;
            if (remaining < count) count = (int)remaining;

            long oldPosition = baseStream.Position;

            baseStream.Position = position;
            int read = baseStream.Read(buffer, offset, count);
            position += read;

            baseStream.Position = oldPosition;

            return read;
        }

        public override void Close()
        {
            // do nothing
        }

        private void CheckDisposed()
        {
            if (baseStream == null)
                throw new ObjectDisposedException(GetType().Name);
        }

        public override long Length
        {
            get { CheckDisposed(); return length; }
        }

        public override bool CanRead
        {
            get { CheckDisposed(); return true; }
        }

        public override bool CanWrite
        {
            get { CheckDisposed(); return false; }
        }

        public override bool CanSeek
        {
            get { CheckDisposed(); return true; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;

                if (position < 0 || offset + position >= baseStream.Length)
                    throw new ArgumentOutOfRangeException(nameof(Position));
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            if (offset + value > baseStream.Length)
                throw new ArgumentOutOfRangeException(nameof(value));

            length = value;
        }

        public override void Flush()
        {
            CheckDisposed();
            baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (baseStream != null)
                {
                    if (disposeBaseStream)
                    {
                        try { baseStream.Dispose(); }
                        catch { }
                    }

                    baseStream = null;
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
