using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Data
{
    public class DataSourceMixed : DataSource
    {
        public enum DataUsage
        {
            /// <summary>
            /// Always use Amiga data. If not present throw exception.
            /// </summary>
            ForceAmiga,
            /// <summary>
            /// Always use DOS data. If not present throw exception.
            /// </summary>
            ForceDos,
            /// <summary>
            /// Use Amiga data if present. Otherwise use DOS data.
            /// </summary>
            PreferAmiga,
            /// <summary>
            /// Use DOS data if present. Otherwise use Amiga data.
            /// </summary>
            PreferDos
        }

        DataSourceAmiga amiga = null;
        DataSourceDos dos = null;
        bool amigaOk = false;
        bool dosOk = false;

        public override string Name => "Mixed";
        public override bool IsLoaded => amiga.IsLoaded || dos.IsLoaded;

        public DataUsage GraphicDataUsage { get; set; } = DataUsage.PreferDos;
        public DataUsage SoundDataUsage { get; set; } = DataUsage.PreferDos;
        public DataUsage MusicDataUsage { get; set; } = DataUsage.PreferAmiga;

        public DataSourceMixed(string path)
            : base(path)
        {
            amiga = new DataSourceAmiga(path);
            dos = new DataSourceDos(path);
        }

        public override bool Check()
        {
            amigaOk = amiga.Check();
            dosOk = dos.Check();

            return amigaOk || dosOk;
        }

        public override Buffer GetMusic(uint index)
        {
            switch (MusicDataUsage)
            {
                case DataUsage.ForceAmiga:
                    if (!amigaOk)
                        throw new ExceptionFreeserf("data", "Amiga music data not available.");
                    return amiga.GetMusic(index);
                case DataUsage.ForceDos:
                    if (!dosOk)
                        throw new ExceptionFreeserf("data", "DOS music data not available.");
                    return dos.GetMusic(index);
                case DataUsage.PreferAmiga:
                default:
                    if (amigaOk)
                        return amiga.GetMusic(index);
                    else
                        return dos.GetMusic(index);
                case DataUsage.PreferDos:
                    if (dosOk)
                        return dos.GetMusic(index);
                    else
                        return amiga.GetMusic(index);
            }
        }

        public override Buffer GetSound(uint index)
        {
            switch (SoundDataUsage)
            {
                case DataUsage.ForceAmiga:
                    if (!amigaOk)
                        throw new ExceptionFreeserf("data", "Amiga sound data not available.");
                    return amiga.GetSound(index);
                case DataUsage.ForceDos:
                    if (!dosOk)
                        throw new ExceptionFreeserf("data", "DOS sound data not available.");
                    return dos.GetSound(index);
                case DataUsage.PreferAmiga:
                    if (amigaOk)
                        return amiga.GetSound(index);
                    else
                        return dos.GetSound(index);
                case DataUsage.PreferDos:
                default:
                    if (dosOk)
                        return dos.GetSound(index);
                    else
                        return amiga.GetSound(index);
            }
        }

        public override Tuple<Sprite, Sprite> GetSpriteParts(Data.Resource resource, uint index)
        {
            switch (GraphicDataUsage)
            {
                case DataUsage.ForceAmiga:
                    if (!amigaOk)
                        throw new ExceptionFreeserf("data", "Amiga graphic data not available.");
                    return amiga.GetSpriteParts(resource, index);
                case DataUsage.ForceDos:
                    if (!dosOk)
                        throw new ExceptionFreeserf("data", "DOS graphic data not available.");
                    return dos.GetSpriteParts(resource, index);
                case DataUsage.PreferAmiga:
                    if (amigaOk)
                        return amiga.GetSpriteParts(resource, index);
                    else
                        return dos.GetSpriteParts(resource, index);
                case DataUsage.PreferDos:
                default:
                    if (dosOk)
                        return dos.GetSpriteParts(resource, index);
                    else
                        return amiga.GetSpriteParts(resource, index);
            }
        }

        public override Sprite GetSprite(Data.Resource resource, uint index, Sprite.Color color)
        {
            switch (GraphicDataUsage)
            {
                case DataUsage.ForceAmiga:
                    if (!amigaOk)
                        throw new ExceptionFreeserf("data", "Amiga graphic data not available.");
                    return amiga.GetSprite(resource, index, color);
                case DataUsage.ForceDos:
                    if (!dosOk)
                        throw new ExceptionFreeserf("data", "DOS graphic data not available.");
                    return dos.GetSprite(resource, index, color);
                case DataUsage.PreferAmiga:
                    if (amigaOk)
                        return amiga.GetSprite(resource, index, color);
                    else
                        return dos.GetSprite(resource, index, color);
                case DataUsage.PreferDos:
                default:
                    if (dosOk)
                        return dos.GetSprite(resource, index, color);
                    else
                        return amiga.GetSprite(resource, index, color);
            }
        }

        public override Animation GetAnimation(int animation, int phase)
        {
            switch (GraphicDataUsage)
            {
                case DataUsage.ForceAmiga:
                    if (!amigaOk)
                        throw new ExceptionFreeserf("data", "Amiga graphic data not available.");
                    return amiga.GetAnimation(animation, phase);
                case DataUsage.ForceDos:
                    if (!dosOk)
                        throw new ExceptionFreeserf("data", "DOS graphic data not available.");
                    return dos.GetAnimation(animation, phase);
                case DataUsage.PreferAmiga:
                    if (amigaOk)
                        return amiga.GetAnimation(animation, phase);
                    else
                        return dos.GetAnimation(animation, phase);
                case DataUsage.PreferDos:
                default:
                    if (dosOk)
                        return dos.GetAnimation(animation, phase);
                    else
                        return amiga.GetAnimation(animation, phase);
            }
        }

        public override int GetAnimationPhaseCount(int animation)
        {
            switch (GraphicDataUsage)
            {
                case DataUsage.ForceAmiga:
                    if (!amigaOk)
                        throw new ExceptionFreeserf("data", "Amiga graphic data not available.");
                    return amiga.GetAnimationPhaseCount(animation);
                case DataUsage.ForceDos:
                    if (!dosOk)
                        throw new ExceptionFreeserf("data", "DOS graphic data not available.");
                    return dos.GetAnimationPhaseCount(animation);
                case DataUsage.PreferAmiga:
                    if (amigaOk)
                        return amiga.GetAnimationPhaseCount(animation);
                    else
                        return dos.GetAnimationPhaseCount(animation);
                case DataUsage.PreferDos:
                default:
                    if (dosOk)
                        return dos.GetAnimationPhaseCount(animation);
                    else
                        return amiga.GetAnimationPhaseCount(animation);
            }
        }

        public override uint BPP
        {
            get
            {
                switch (GraphicDataUsage)
                {
                    case DataUsage.ForceAmiga:
                        if (!amigaOk)
                            throw new ExceptionFreeserf("data", "Amiga graphic data not available.");
                        return amiga.BPP;
                    case DataUsage.ForceDos:
                        if (!dosOk)
                            throw new ExceptionFreeserf("data", "DOS graphic data not available.");
                        return dos.BPP;
                    case DataUsage.PreferAmiga:
                        if (amigaOk)
                            return amiga.BPP;
                        else
                            return dos.BPP;
                    case DataUsage.PreferDos:
                    default:
                        if (dosOk)
                            return dos.BPP;
                        else
                            return amiga.BPP;
                }
            }
        }

        public override uint Scale
        {
            get
            {
                switch (GraphicDataUsage)
                {
                    case DataUsage.ForceAmiga:
                        if (!amigaOk)
                            throw new ExceptionFreeserf("data", "Amiga graphic data not available.");
                        return amiga.Scale;
                    case DataUsage.ForceDos:
                        if (!dosOk)
                            throw new ExceptionFreeserf("data", "DOS graphic data not available.");
                        return dos.Scale;
                    case DataUsage.PreferAmiga:
                        if (amigaOk)
                            return amiga.Scale;
                        else
                            return dos.Scale;
                    case DataUsage.PreferDos:
                    default:
                        if (dosOk)
                            return dos.Scale;
                        else
                            return amiga.Scale;
                }
            }
        }

        public override bool Load()
        {
            if (amigaOk)
                amigaOk = amiga.Load();

            if (dosOk)
                dosOk = dos.Load();

            return amigaOk || dosOk;
        }

        public bool UseDosGraphics => dosOk && (GraphicDataUsage == DataUsage.ForceDos || GraphicDataUsage == DataUsage.PreferDos);
        public bool UseDosSounds => dosOk && (SoundDataUsage == DataUsage.ForceDos || SoundDataUsage == DataUsage.PreferDos);
        public bool UseDosMusic => dosOk && (MusicDataUsage == DataUsage.ForceDos || MusicDataUsage == DataUsage.PreferDos);
    }
}
