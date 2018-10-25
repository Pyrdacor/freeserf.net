using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freeserf
{
    public class ExceptionAudio : ExceptionFreeserf
    {
        public ExceptionAudio(string description)
            : base(description)
        {

        }

        public virtual string Platform => "Abstract";

        public override string System => "audio";

        public override string Message => "[" + System + ":" + Platform + "]" + Description;
    }

    public class Audio
    {
        public enum TypeSfx
        {
            Message = 1,
            Accepted = 2,
            NotAccepted = 4,
            Undo = 6,
            Click = 8,
            Fight01 = 10,
            Fight02 = 14,
            Fight03 = 18,
            Fight04 = 22,
            ResourceFound = 26,
            PickBlow = 28,
            MetalHammering = 30,
            AxBlow = 32,
            TreeFall = 34,
            WoodHammering = 36,
            Elevator = 38,
            HammerBlow = 40,
            Sawing = 42,
            MillGrinding = 43,
            BackswordBlow = 44,
            GeologistSampling = 46,
            Planting = 48,
            Digging = 50,
            Mowing = 52,
            FishingRodReel = 54,
            Unknown21 = 58,
            PigOink = 60,
            GoldBoils = 62,
            Rowing = 64,
            Unknown25 = 66,
            SerfDying = 69,
            BirdChirp0 = 70,
            BirdChirp1 = 74,
            Ahhh = 76,
            BirdChirp2 = 78,
            BirdChirp3 = 82,
            Burning = 84,
            Unknown28 = 86,
            Unknown29 = 88
        }

        public enum TypeMidi
        {
            None = -1,
            Track0 = 0,
            Track1 = 1,
            Track2 = 2,
            Track3 = 4,
            TrackLast = Track3,
        }

        public abstract class VolumeController
        {
            public abstract float GetVolume();
            public abstract void SetVolume(float volume);
            public abstract void VolumeUp();
            public abstract void VolumeDown();
        }

        public abstract class Track
        {
            public abstract void Play();
        }

        public abstract class Player
        {
            protected Dictionary<int, Track> trackCache = new Dictionary<int, Track>();
            protected bool enabled = true;

            public virtual Track PlayTrack(int trackID)
            {
                if (!IsEnabled)
                {
                    return null;
                }

                Track track;

                if (!trackCache.ContainsKey(trackID))
                {
                    track = CreateTrack(trackID);

                    if (track != null)
                    {
                        trackCache[trackID] = track;
                    }
                }
                else
                {
                    track = trackCache[trackID];
                }

                if (track != null)
                {
                    track.Play();
                }

                return track;
            }

            public abstract void Enable(bool enable);

            public virtual bool IsEnabled => enabled;

            public abstract VolumeController GetVolumeController();

            protected abstract Track CreateTrack(int trackID);

            protected abstract void Stop();
        }
    }
}
