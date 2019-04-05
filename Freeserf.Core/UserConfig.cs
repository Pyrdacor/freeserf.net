using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf
{
    public static class UserConfig
    {
        public static class Game
        {
            public static int Options { get; set; } = 0x39;
            public static string DataFile { get; set; } = "SPAE.PA";
        }

        public static class Audio
        {
            public static bool Music { get; set; } = true;
            public static bool Sound { get; set; } = true;
            public static float Volume { get; set; } = 1.0f;
        }

        public static class Video
        {
            public static int ResolutionWidth { get; set; } = 1280;
            public static int ResolutionHeight { get; set; } = 960;
            public static bool Fullscreen { get; set; } = false;
        }

        public static bool Load(string filename)
        {
            try
            {
                ConfigFile configFile = new ConfigFile();

                if (!configFile.Load(filename))
                {
                    SetDefaults();
                    return false;
                }


                // Game
                if (int.TryParse(configFile.Value("game", "options", "57"), out int options))
                    Game.Options = options | 0x01; // bit 0 must be set always
                Game.DataFile = configFile.Value("game", "datafile", "SPAE.PA");


                // Audio
                Audio.Music = configFile.Value("audio", "music", "0") == "1";
                Audio.Sound = configFile.Value("audio", "sound", "0") == "1";

                if (float.TryParse(configFile.Value("audio", "volume", "0.0"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float volume))
                    Audio.Volume = volume;


                // Video
                if (int.TryParse(configFile.Value("video", "resolutionwidth", "1280"), out int resolutionWidth))
                    Video.ResolutionWidth = resolutionWidth;
                if (int.TryParse(configFile.Value("video", "resolutionheight", "960"), out int resolutionHeight))
                    Video.ResolutionHeight = resolutionHeight;
                Video.Fullscreen = configFile.Value("video", "fullscreen", "0") == "1";

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(string filename)
        {
            try
            {
                ConfigFile configFile = new ConfigFile();


                // Game
                configFile.SetValue("game", "options", Game.Options);
                configFile.SetValue("game", "dataFile", Game.DataFile);


                // Audio
                configFile.SetValue("audio", "music", Audio.Music ? 1 : 0);
                configFile.SetValue("audio", "sound", Audio.Sound ? 1 : 0);
                configFile.SetValue("audio", "volume", Audio.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture));


                // Video
                configFile.SetValue("video", "resolutionwidth", Video.ResolutionWidth);
                configFile.SetValue("video", "resolutionheight", Video.ResolutionHeight);
                configFile.SetValue("video", "fullscreen", Video.Fullscreen ? "1" : "0");


                return configFile.Save(filename);
            }
            catch
            {
                return false;
            }
        }

        public static void SetDefaults()
        {
            Game.Options = 0x39;
            Game.DataFile = "SPAE.PA";

            Audio.Music = true;
            Audio.Sound = true;
            Audio.Volume = 1.0f;

            Video.ResolutionWidth = 1280;
            Video.ResolutionHeight = 960;
            Video.Fullscreen = false;
        }
    }
}
