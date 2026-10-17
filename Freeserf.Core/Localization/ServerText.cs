using System.Globalization;

namespace Freeserf.Localization
{
    internal static class ServerText
    {
        public static string NotFound(string address) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Server_NotFound, address);

        public static string Online(string name, string host, int currentPlayers, int maxPlayers, bool inGame, bool allowsRejoin)
        {
            string template = inGame ? Strings.Server_Running : allowsRejoin ? Strings.Server_Saved : Strings.Server_Online;
            return string.Format(CultureInfo.CurrentCulture, template, name, host, currentPlayers, maxPlayers);
        }
    }
}
