using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf
{
    using MapPos = UInt32;

    internal class Viewport : GuiObject
    {
        public Viewport(Interface interf, Map map)
        {

        }

        public void Update()
        {

        }

        public MapPos GetCurrentMapPos()
        {
            return 0;
        }

        public void MoveToMapPos(MapPos pos)
        {

        }

        public void RedrawMapPos(MapPos pos)
        {

        }

        public void SwitchLayer(Layer layer)
        {

        }

        protected override void InternalDraw()
        {
            throw new NotImplementedException();
        }
    }
}
