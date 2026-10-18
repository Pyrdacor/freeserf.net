using System;
using System.Globalization;
using System.Threading;

namespace Freeserf.Localization
{
    internal static class LanguageSettings
    {
        static readonly string[] supportedLanguages = ["system", "en", "de"];
        internal static CultureInfo SystemCulture { get; } = CultureInfo.CurrentUICulture;
        static string preference = "system";
        static int revision;

        public static string Preference => preference;
        public static int Revision => Volatile.Read(ref revision);

        public static bool IsSupported(string value) => Array.IndexOf(supportedLanguages, value) >= 0;

        public static void Apply(string value)
        {
            if (!IsSupported(value))
                throw new ArgumentException($"Unsupported language preference: '{value}'.", nameof(value));

            var culture = value == "system" ? SystemCulture : CultureInfo.GetCultureInfo(value);
            if (preference == value && Equals(Strings.Culture, culture))
                return;

            // Use a resource-wide override so Android UI and rendering threads agree.
            Strings.Culture = culture;
            preference = value;
            Interlocked.Increment(ref revision);
        }

        public static string Next(string value) => Move(value, 1);

        public static string Previous(string value) => Move(value, -1);

        static string Move(string value, int direction)
        {
            int index = Array.IndexOf(supportedLanguages, value);
            if (index < 0)
                throw new ArgumentException($"Unsupported language preference: '{value}'.", nameof(value));

            return supportedLanguages[(index + direction + supportedLanguages.Length) % supportedLanguages.Length];
        }

        public static string DisplayName => preference switch
        {
            "system" => Strings.Popup_LanguageSystem,
            "en" => Strings.Popup_LanguageEnglish,
            "de" => Strings.Popup_LanguageGerman,
            _ => throw new InvalidOperationException("Invalid language preference.")
        };
    }
}
