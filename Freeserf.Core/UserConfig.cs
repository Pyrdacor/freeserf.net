namespace Freeserf
{
    public static class UserConfig
    {
        public static class Game
        {
            public static int Options { get; set; } = 0x39;
            public static Data.DataSourceMixed.DataUsage GraphicDataUsage { get; set; } = Data.DataSourceMixed.DataUsage.PreferDos;
            public static Data.DataSourceMixed.DataUsage SoundDataUsage { get; set; } = Data.DataSourceMixed.DataUsage.PreferDos;
            public static Data.DataSourceMixed.DataUsage MusicDataUsage { get; set; } = Data.DataSourceMixed.DataUsage.PreferAmiga;
            // TODO: language
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
                var configFile = new ConfigFile();

                if (!configFile.Load(filename))
                {
                    SetDefaults();
                    return false;
                }


                // Game
                if (int.TryParse(configFile.Value("game", "options", "57"), out int options))
                    Game.Options = options | 0x01; // bit 0 must be always set
                Game.GraphicDataUsage = configFile.Value<Data.DataSourceMixed.DataUsage>("game", "graphic_data_usage", Data.DataSourceMixed.DataUsage.PreferDos);
                Game.SoundDataUsage = configFile.Value<Data.DataSourceMixed.DataUsage>("game", "sound_data_usage", Data.DataSourceMixed.DataUsage.PreferDos);
                Game.MusicDataUsage = configFile.Value<Data.DataSourceMixed.DataUsage>("game", "music_data_usage", Data.DataSourceMixed.DataUsage.PreferAmiga);


                // Audio
                Audio.Music = configFile.Value("audio", "music", "0") == "1";
                Audio.Sound = configFile.Value("audio", "sound", "0") == "1";

                if (float.TryParse(configFile.Value("audio", "volume", "0.0"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float volume))
                    Audio.Volume = volume;


                // Video
                if (int.TryParse(configFile.Value("video", "resolution_width", "1280"), out int resolutionWidth))
                    Video.ResolutionWidth = resolutionWidth;
                if (int.TryParse(configFile.Value("video", "resolution_height", "960"), out int resolutionHeight))
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
                var configFile = new ConfigFile();

                // Game
                configFile.SetValue("game", "options", Game.Options);
                configFile.SetValue("game", "graphic_data_usage", Game.GraphicDataUsage);
                configFile.SetValue("game", "sound_data_usage", Game.SoundDataUsage);
                configFile.SetValue("game", "music_data_usage", Game.MusicDataUsage);


                // Audio
                configFile.SetValue("audio", "music", Audio.Music ? 1 : 0);
                configFile.SetValue("audio", "sound", Audio.Sound ? 1 : 0);
                configFile.SetValue("audio", "volume", Audio.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture));


                // Video
                configFile.SetValue("video", "resolution_width", Video.ResolutionWidth);
                configFile.SetValue("video", "resolution_height", Video.ResolutionHeight);
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
            Game.GraphicDataUsage = Data.DataSourceMixed.DataUsage.PreferDos;
            Game.SoundDataUsage = Data.DataSourceMixed.DataUsage.PreferDos;
            Game.MusicDataUsage = Data.DataSourceMixed.DataUsage.PreferAmiga;

            Audio.Music = true;
            Audio.Sound = true;
            Audio.Volume = 1.0f;

            Video.ResolutionWidth = 1280;
            Video.ResolutionHeight = 960;
            Video.Fullscreen = false;
        }
    }
}
