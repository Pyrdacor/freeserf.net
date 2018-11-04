using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class Graphics
    {
        static Graphics instance = null;

        public static Graphics Instance
        {
            get
            {
                if (instance == null)
                    instance = new Graphics();

                return instance;
            }
        }

        Graphics()
        {

        }

        public IFrame CreateFrame(uint width, uint height)
        {

        }
    }
}
