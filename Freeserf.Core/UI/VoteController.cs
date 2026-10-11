/*
 * VoteController.cs - Handles the votes of a multiplayer game
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

namespace Freeserf.UI
{
    using Network;

    /// <summary>
    /// Connects the votes of a multiplayer game with the network and the vote box.
    ///
    /// The server decides about all votes (see <see cref="VoteManager"/>). Its own
    /// requests and answers are passed to the vote manager directly. Clients send
    /// them to the server. The server sends the started votes and their results to
    /// all clients. Accepted votes are applied by the server and the clients get the
    /// result with the next game state update (e.g. the game speed).
    /// </summary>
    internal class VoteController : IDisposable
    {
        readonly Interface interf;
        readonly ILocalServer server;
        readonly ILocalClient client;
        readonly VoteManager voteManager;
        readonly List<VoteEntry> entries = new List<VoteEntry>();
        VoteBox voteBox = null;
        bool disposed = false;

        VoteController(Interface interf, ILocalServer server, ILocalClient client)
        {
            this.interf = interf;
            this.server = server;
            this.client = client;

            if (server != null)
            {
                voteManager = new VoteManager();
                voteManager.VoteFinished += VoteManager_VoteFinished;
                server.ClientVote += Server_ClientVote;
            }

            if (client != null)
                client.VoteReceived += Client_VoteReceived;
        }

        public static VoteController CreateForServer(Interface interf, ILocalServer server) => new(interf, server, null);
        public static VoteController CreateForClient(Interface interf, ILocalClient client) => new(interf, null, client);

        uint OwnPlayerIndex => client?.PlayerIndex ?? interf.Player?.Index ?? 0u;

        /// <summary>
        /// The votes which are known on this participant (used by the test driver).
        /// </summary>
        public IReadOnlyList<VoteEntry> Entries => entries;

        /// <summary>
        /// Raised when a vote has finished on this participant (used by the test driver).
        /// </summary>
        public event Action<VoteEntry> VoteFinished;

        /// <summary>
        /// The own player requests a vote.
        /// </summary>
        public void Request(VoteType type, int parameter)
        {
            if (server != null)
            {
                StartVote(OwnPlayerIndex, type, parameter);
            }
            else if (client?.Connected == true)
            {
                client.SendVote(VoteData.CreateRequest(OwnPlayerIndex, type, parameter));
            }
        }

        /// <summary>
        /// The own player answers a vote.
        /// </summary>
        public void Answer(VoteEntry entry, bool yes)
        {
            entry.Answered = true;

            if (server != null)
                voteManager.Answer(entry.VoteId, OwnPlayerIndex, yes);
            else if (client?.Connected == true)
                client.SendVote(VoteData.CreateAnswer(entry.VoteId, OwnPlayerIndex, yes));

            UpdateVoteBox(DateTime.UtcNow);
        }

        /// <summary>
        /// Called each frame.
        /// </summary>
        public void Update()
        {
            var now = DateTime.UtcNow;

            if (voteManager != null)
            {
                // Players who have left the game no longer vote.
                if (interf.Game != null)
                {
                    foreach (var player in interf.Game.Players)
                    {
                        if (player.HasLeftGame)
                            voteManager.RemoveVoter(player.Index);
                    }
                }

                voteManager.Update(now);
            }

            entries.RemoveAll(entry => entry.Result != null && now - entry.FinishTime >= VoteSettings.ResultDisplayTime);

            UpdateVoteBox(now);
        }

        // Only the server: starts a vote of the given player.
        void StartVote(uint requester, VoteType type, int parameter)
        {
            var now = DateTime.UtcNow;
            var vote = voteManager.Start(requester, type, parameter, GetVoters(), now);

            if (vote == null)
            {
                Log.Info.Write(ErrorSystemType.Network, $"Vote request {type} {parameter} of player {requester} refused.");

                if (requester == OwnPlayerIndex)
                    AddRefusedEntry(type, parameter);
                else
                    SendToPlayer(requester, VoteData.CreateResult(0, requester, type, parameter, VoteResult.Refused));

                return;
            }

            Log.Info.Write(ErrorSystemType.Network, $"Vote {vote.Id} started: {type} {parameter} by player {requester}.");

            server.BroadcastVote(VoteData.CreateStart(vote));
            AddEntry(vote.Id, requester, type, parameter, now);

            // There may be no other voters (the vote is finished in Update).
        }

        IEnumerable<uint> GetVoters()
        {
            var game = interf.Game;

            if (game == null)
                yield break;

            var players = new HashSet<uint> { OwnPlayerIndex };

            foreach (var remoteClient in server.Clients)
                players.Add(remoteClient.PlayerIndex);

            foreach (var player in players)
            {
                if (player < game.PlayerCount && game.GetPlayer(player) is Player gamePlayer && !gamePlayer.HasLeftGame)
                    yield return player;
            }
        }

        void SendToPlayer(uint player, VoteData vote)
        {
            var remoteClient = server.Clients.FirstOrDefault(c => c.PlayerIndex == player);

            if (remoteClient != null)
                server.SendVote(remoteClient, vote);
        }

        void VoteManager_VoteFinished(Vote vote, VoteResult result)
        {
            Log.Info.Write(ErrorSystemType.Network, $"Vote {vote.Id} finished: {result} ({vote.YesCount} yes, {vote.NoCount} no).");

            if (result == VoteResult.Accepted)
                Apply(vote);

            server.BroadcastVote(VoteData.CreateResult(vote.Id, vote.Requester, vote.Type, vote.Parameter, result));
            SetResult(vote.Id, result);
        }

        // Only the server: applies an accepted vote. The clients get the changes with the next game state update.
        void Apply(Vote vote)
        {
            var game = interf.Game;

            if (game == null)
                return;

            switch (vote.Type)
            {
                case VoteType.Pause:
                    if (!game.IsPaused)
                    {
                        game.Pause();
                        server.PauseGame();
                    }
                    break;
                case VoteType.Resume:
                    if (game.IsPaused)
                    {
                        game.Resume();
                        server.ResumeGame();
                    }
                    break;
                case VoteType.GameSpeed:
                    {
                        bool wasPaused = game.IsPaused;
                        game.SetSpeed((uint)Math.Clamp(vote.Parameter, 1, Freeserf.Global.MAX_GAME_SPEED));

                        if (wasPaused)
                            server.ResumeGame();
                        break;
                    }
                case VoteType.Save:
                    (interf as ServerInterface)?.SaveMultiplayerGame(false);
                    return; // the game state was not changed
            }

            server.GameDirty = true;
        }

        void Server_ClientVote(ILocalServer server, IRemoteClient remoteClient, VoteData vote)
        {
            switch (vote.Message)
            {
                case VoteMessage.Request:
                    StartVote(remoteClient.PlayerIndex, vote.VoteType, vote.Parameter);
                    break;
                case VoteMessage.Answer:
                    voteManager.Answer(vote.VoteId, remoteClient.PlayerIndex, vote.Yes);
                    break;
                default:
                    Log.Error.Write(ErrorSystemType.Network, $"Unexpected {vote.LogName} from player {remoteClient.PlayerIndex}.");
                    break;
            }
        }

        void Client_VoteReceived(VoteData vote)
        {
            switch (vote.Message)
            {
                case VoteMessage.Start:
                    AddEntry(vote.VoteId, vote.PlayerIndex, vote.VoteType, vote.Parameter, DateTime.UtcNow);
                    break;
                case VoteMessage.Result:
                    if (vote.Result == VoteResult.Refused)
                        AddRefusedEntry(vote.VoteType, vote.Parameter);
                    else
                        SetResult(vote.VoteId, vote.Result);
                    break;
                default:
                    Log.Error.Write(ErrorSystemType.Network, $"Unexpected {vote.LogName} from the server.");
                    break;
            }
        }

        void AddEntry(uint voteId, uint requester, VoteType type, int parameter, DateTime startTime)
        {
            var entry = new VoteEntry
            {
                VoteId = voteId,
                Requester = requester,
                Type = type,
                Parameter = parameter,
                OwnRequest = requester == OwnPlayerIndex,
                Deadline = startTime + VoteSettings.AnswerTimeout
            };

            entries.Add(entry);

            if (!entry.OwnRequest)
                interf.PlaySound(Freeserf.Audio.Audio.TypeSfx.Message);

            UpdateVoteBox(DateTime.UtcNow);
        }

        void AddRefusedEntry(VoteType type, int parameter)
        {
            var entry = new VoteEntry
            {
                Requester = OwnPlayerIndex,
                Type = type,
                Parameter = parameter,
                OwnRequest = true,
                Result = VoteResult.Refused,
                FinishTime = DateTime.UtcNow
            };

            entries.Add(entry);
            VoteFinished?.Invoke(entry);

            interf.PlaySound(Freeserf.Audio.Audio.TypeSfx.NotAccepted);
            UpdateVoteBox(DateTime.UtcNow);
        }

        void SetResult(uint voteId, VoteResult result)
        {
            var entry = entries.FirstOrDefault(entry => entry.VoteId == voteId && entry.Result == null);

            if (entry == null)
                return;

            entry.Result = result;
            entry.FinishTime = DateTime.UtcNow;

            if (entry.OwnRequest)
                interf.PlaySound(result == VoteResult.Accepted ? Freeserf.Audio.Audio.TypeSfx.Accepted : Freeserf.Audio.Audio.TypeSfx.NotAccepted);

            VoteFinished?.Invoke(entry);
            UpdateVoteBox(DateTime.UtcNow);
        }

        // Shows the vote which needs an answer first. Otherwise the oldest other one.
        void UpdateVoteBox(DateTime now)
        {
            if (disposed)
                return;

            var entry = entries.FirstOrDefault(entry => entry.CanAnswer) ?? entries.FirstOrDefault();

            if (entry == null)
            {
                if (voteBox != null)
                    voteBox.Displayed = false;

                return;
            }

            if (voteBox == null)
            {
                voteBox = new VoteBox(interf);
                voteBox.Answered += (answeredEntry, yes) => Answer(answeredEntry, yes);
                interf.AddChild(voteBox, 8, 8);
                voteBox.SetSize(144, 160);
            }

            if (voteBox.Entry != entry || !voteBox.Displayed)
                voteBox.Show(entry);
            else
                voteBox.UpdateStatus(now);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (server != null)
                server.ClientVote -= Server_ClientVote;

            if (client != null)
                client.VoteReceived -= Client_VoteReceived;

            if (voteBox != null)
            {
                voteBox.Displayed = false;
                interf.DeleteChild(voteBox);
                voteBox = null;
            }
        }
    }
}
