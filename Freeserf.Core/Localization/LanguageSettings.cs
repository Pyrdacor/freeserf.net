using System;
using System.Globalization;
using System.Threading;

namespace Freeserf.Localization
{
    internal static class LanguageSettings
    {
        internal static CultureInfo SystemCulture { get; } = CultureInfo.CurrentUICulture;
        static string preference = "system";
        static int revision;

        public static string Preference => preference;
        public static int Revision => Volatile.Read(ref revision);

        public static bool IsSupported(string value) => value is "system" or "en" or "de";

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

        public static string Next(string value) => value switch
        {
            "system" => "en",
            "en" => "de",
            "de" => "system",
            _ => throw new ArgumentException($"Unsupported language preference: '{value}'.", nameof(value))
        };

        public static string DisplayName => preference switch
        {
            "system" => Strings.Popup_LanguageSystem,
            "en" => Strings.Popup_LanguageEnglish,
            "de" => Strings.Popup_LanguageGerman,
            _ => throw new InvalidOperationException("Invalid language preference.")
        };
    }
}
