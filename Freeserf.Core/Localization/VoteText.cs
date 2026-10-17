using System;
using System.Globalization;

namespace Freeserf.Localization
{
    internal static class VoteText
    {
        public static string PlayerName(uint player) =>
            string.Format(CultureInfo.CurrentCulture, Strings.Vote_PlayerName, player + 1);

        public static string Question(uint requester, VoteType type, int parameter)
        {
            string name = PlayerName(requester);

            return type switch
            {
                VoteType.Pause => string.Format(CultureInfo.CurrentCulture, Strings.Vote_Question_Pause, name),
                VoteType.Resume => string.Format(CultureInfo.CurrentCulture, Strings.Vote_Question_Resume, name),
                VoteType.GameSpeed => string.Format(CultureInfo.CurrentCulture, Strings.Vote_Question_GameSpeed, name, parameter),
                VoteType.Save => string.Format(CultureInfo.CurrentCulture, Strings.Vote_Question_Save, name),
                _ => string.Format(CultureInfo.CurrentCulture, Strings.Vote_Question_Unknown, name)
            };
        }

        public static string OwnRequest(VoteType type, int parameter) => type switch
        {
            VoteType.Pause => Strings.Vote_OwnRequest_Pause,
            VoteType.Resume => Strings.Vote_OwnRequest_Resume,
            VoteType.GameSpeed => string.Format(CultureInfo.CurrentCulture, Strings.Vote_OwnRequest_GameSpeed, parameter),
            VoteType.Save => Strings.Vote_OwnRequest_Save,
            _ => Strings.Vote_OwnRequest_Unknown
        };

        public static string Status(VoteResult? result, bool canAnswer, DateTime deadline, DateTime now)
        {
            if (result != null)
            {
                return result switch
                {
                    VoteResult.Accepted => Strings.Vote_Result_Accepted,
                    VoteResult.Rejected => Strings.Vote_Result_Rejected,
                    _ => Strings.Vote_Result_Refused
                };
            }

            if (!canAnswer)
                return Strings.Vote_Status_Waiting;

            int seconds = Math.Max(0, (int)Math.Ceiling((deadline - now).TotalSeconds));
            return string.Format(CultureInfo.CurrentCulture, Strings.Vote_Status_SecondsLeft, seconds);
        }
    }
}
