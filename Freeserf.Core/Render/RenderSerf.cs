using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Render
{
    public class RenderSerf
    {
        Serf serf = null;
        ISprite sprite = null;

        public RenderSerf(Serf serf, ISprite sprite)
        {
            this.serf = serf;
            this.sprite = sprite;
        }

        public void Delete()
        {
            sprite.Delete();
            sprite = null;
            serf = null;
        }

        public void Draw(Rect visibleMapArea)
        {
            if (sprite == null || serf == null)
                return;

            // TODO: if serf is outside the visible map area -> return
            // TODO: set position and texture coords based on:
            // serf.Position
            // serf.Animation
            // serf.Counter
        }
    }
}
