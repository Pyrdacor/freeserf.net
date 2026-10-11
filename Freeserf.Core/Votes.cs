/*
 * Votes.cs - Votes of the players in multiplayer games
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Freeserf
{
    /// <summary>
    /// Things the players of a multiplayer game vote on.
    /// New types (e.g. surrender or saving) can be added here.
    /// </summary>
    public enum VoteType : byte
    {
        /// <summary>
        /// Pause the game.
        /// </summary>
        Pause,
        /// <summary>
        /// Resume the paused game.
        /// </summary>
        Resume,
        /// <summary>
        /// Change the game speed. The parameter is the new speed.
        /// </summary>
        GameSpeed
    }

    public enum VoteResult : byte
    {
        /// <summary>
        /// More players agreed than declined.
        /// </summary>
        Accepted,
        /// <summary>
        /// At least as many players declined as agreed.
        /// </summary>
        Rejected,
        /// <summary>
        /// The vote was not started (e.g. the player has to wait before the next request).
        /// </summary>
        Refused
    }

    /// <summary>
    /// Times of votes. They can be adjusted here.
    /// </summary>
    public static class VoteSettings
    {
        /// <summary>
        /// A player can only request a vote (of any type) once in this time.
        /// </summary>
        public static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(10);
        /// <summary>
        /// After a vote was rejected, the same player can't request the same vote again for this time.
        /// </summary>
        public static readonly TimeSpan RejectedRequestBlockTime = TimeSpan.FromSeconds(30);
        /// <summary>
        /// Players who don't answer in this time decline the vote.
        /// </summary>
        public static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(5);
        /// <summary>
        /// How long the result of a vote is shown.
        /// </summary>
        public static readonly TimeSpan ResultDisplayTime = TimeSpan.FromSeconds(3);
    }

    public class Vote
    {
        readonly Dictionary<uint, bool?> answers = new Dictionary<uint, bool?>();

        public Vote(uint id, uint requester, VoteType type, int parameter, IEnumerable<uint> voters, DateTime startTime)
        {
            Id = id;
            Requester = requester;
            Type = type;
            Parameter = parameter;
            StartTime = startTime;

            foreach (var voter in voters)
                answers[voter] = null;

            // The requester agrees of course.
            answers[requester] = true;
        }

        public uint Id { get; }
        public uint Requester { get; }
        public VoteType Type { get; }
        public int Parameter { get; }
        public DateTime StartTime { get; }
        public int YesCount => answers.Values.Count(answer => answer == true);
        // Missing answers count as "no" when the vote is finished.
        public int NoCount => answers.Values.Count(answer => answer != true);
        public bool AllAnswered => answers.Values.All(answer => answer != null);
        public bool Accepted => YesCount > NoCount;

        public bool IsVoter(uint player) => answers.ContainsKey(player);

        internal void SetAnswer(uint player, bool yes)
        {
            if (answers.ContainsKey(player) && player != Requester)
                answers[player] = yes;
        }

        internal void RemoveVoter(uint player)
        {
            if (player != Requester)
                answers.Remove(player);
        }

        /// <summary>
        /// Two votes are the same request if they have the same type and parameter.
        /// </summary>
        public bool IsSameRequest(VoteType type, int parameter) => Type == type && Parameter == parameter;

        public static string GetPlayerName(uint player) => $"Player {player + 1}";

        /// <summary>
        /// Text for the players who are asked (lines separated by '\n').
        /// </summary>
        public static string GetQuestion(uint requester, VoteType type, int parameter)
        {
            string name = GetPlayerName(requester);

            return type switch
            {
                VoteType.Pause => $"{name} wants\nto pause the\ngame.",
                VoteType.Resume => $"{name} wants\nto resume the\ngame.",
                VoteType.GameSpeed => $"{name} wants\nto set the game\nspeed to {parameter}.",
                _ => $"{name} wants\nsomething."
            };
        }

        /// <summary>
        /// Text for the requester (lines separated by '\n').
        /// </summary>
        public static string GetOwnRequest(VoteType type, int parameter)
        {
            return type switch
            {
                VoteType.Pause => "You want to\npause the game.",
                VoteType.Resume => "You want to\nresume the game.",
                VoteType.GameSpeed => $"You want to set\nthe game speed\nto {parameter}.",
                _ => "You want\nsomething."
            };
        }
    }

    /// <summary>
    /// Manages the votes of a multiplayer game. Only the server uses it.
    /// </summary>
    public class VoteManager
    {
        readonly List<Vote> activeVotes = new List<Vote>();
        readonly Dictionary<uint, DateTime> lastRequestTimes = new Dictionary<uint, DateTime>();
        readonly List<(uint Player, VoteType Type, int Parameter, DateTime Time)> rejectedRequests = new();
        uint nextVoteId = 1;

        /// <summary>
        /// Raised when a vote has finished (all answered or timeout).
        /// </summary>
        public event Action<Vote, VoteResult> VoteFinished;

        public IReadOnlyList<Vote> ActiveVotes => activeVotes;

        /// <summary>
        /// Checks if the player may request the vote now.
        /// </summary>
        public bool CanRequest(uint player, VoteType type, int parameter, DateTime now)
        {
            if (lastRequestTimes.TryGetValue(player, out var lastRequestTime) &&
                now - lastRequestTime < VoteSettings.RequestInterval)
                return false;

            return !rejectedRequests.Any(request => request.Player == player && request.Type == type &&
                request.Parameter == parameter && now - request.Time < VoteSettings.RejectedRequestBlockTime);
        }

        /// <summary>
        /// Starts a vote. Returns null if the player can't request it now.
        /// The vote may be finished right away if there are no other voters.
        /// </summary>
        public Vote Start(uint requester, VoteType type, int parameter, IEnumerable<uint> voters, DateTime now)
        {
            if (!CanRequest(requester, type, parameter, now))
                return null;

            lastRequestTimes[requester] = now;

            var vote = new Vote(nextVoteId++, requester, type, parameter, voters, now);

            if (nextVoteId == 0) // 0 is not used as an id
                nextVoteId = 1;

            activeVotes.Add(vote);

            return vote;
        }

        public void Answer(uint voteId, uint player, bool yes)
        {
            var vote = activeVotes.FirstOrDefault(vote => vote.Id == voteId);

            if (vote == null)
                return; // the vote may have timed out already

            vote.SetAnswer(player, yes);
        }

        /// <summary>
        /// A player who has left the game no longer votes.
        /// </summary>
        public void RemoveVoter(uint player)
        {
            foreach (var vote in activeVotes)
                vote.RemoveVoter(player);
        }

        /// <summary>
        /// Finishes all votes which are answered by all voters or which have timed out.
        /// </summary>
        public void Update(DateTime now)
        {
            foreach (var vote in activeVotes.ToList())
            {
                if (vote.AllAnswered || now - vote.StartTime >= VoteSettings.AnswerTimeout)
                    Finish(vote, now);
            }

            rejectedRequests.RemoveAll(request => now - request.Time >= VoteSettings.RejectedRequestBlockTime);
        }

        void Finish(Vote vote, DateTime now)
        {
            activeVotes.Remove(vote);

            var result = vote.Accepted ? VoteResult.Accepted : VoteResult.Rejected;

            if (result == VoteResult.Rejected)
                rejectedRequests.Add((vote.Requester, vote.Type, vote.Parameter, now));

            VoteFinished?.Invoke(vote, result);
        }
    }
}
