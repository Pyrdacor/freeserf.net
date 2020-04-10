using System;
using System.Runtime.InteropServices;
using ManagedBass;

namespace Freeserf.Audio.Bass
{
    internal class WaveMusic : Music
    {
        internal WaveMusic(byte[] data)
            :  base(data, Type.Wave)
        {

        }

        internal class WaveStreamProvider
        {
            const int MAX_BUFFER_SIZE = 4096;
            byte[] data;
            int offset = 0;

            public WaveStreamProvider(byte[] data)
            {
                this.data = data;
            }

            public int StreamProcedure(int handle, IntPtr buffer, int length, IntPtr user)
            {
                if (length > data.Length - offset)
                    length = data.Length - offset;

                if (length == 0)
                {
                    offset = 0;
                    return (int)StreamProcedureType.End;
                }

                length = Math.Min(length, MAX_BUFFER_SIZE);
                Marshal.Copy(data, offset, buffer, length);
                offset += length;

                return length;
            }
        }
    }
}
