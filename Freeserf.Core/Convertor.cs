using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf
{
    internal abstract class Convertor
    {
        protected Buffer buffer;

        public Convertor(Buffer buffer)
        {
            this.buffer = buffer;
        }

        public abstract Buffer Convert();
    }
}
