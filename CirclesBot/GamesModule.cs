using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclesBot
{
    public class GamesModule : Module
    {
        public override string Name => "Games Module";

        public override int Order => 3;

        public abstract class Game
        {
            public ISocketMessageChannel channel;
            public SocketUser[] Players;

            public bool IsGameDone { get; protected set; }

            public void Setup(ISocketMessageChannel channel, params SocketUser[] players)
            {
                this.channel = channel;
                this.Players = players;

                OnStart();
            }

            public abstract void OnStart();

            public abstract void OnMessageReceived(SocketMessage sMsg);
        }

        private class TicTacToe : Game
        {
            enum Piece
            {
                Empty,
                X,
                O
            }

            private SocketUser currentPlayer;

            private Piece[,] board = new Piece[3, 3];

            private bool saidInvalidInput;

            private Piece getPiece(int i)
            {
                return board[i % 3, i / 3];
            }

            private string emote(Piece piece)
            {
                switch (piece)
                {
                    case Piece.Empty:
                        return ":heavy_minus_sign:";
                    case Piece.X:
                        return ":x:";
                    case Piece.O:
                        return ":o:";
                    default:
                        return "WTF";
                }
            }

            private bool checkForWin(Piece p)
            {
                ///P - P - P
                ///P - P - P
                ///P - p - P

                //Rows
                if (board[0, 0] == p && board[1, 0] == p && board[2, 0] == p)
                    return true;

                if (board[0, 1] == p && board[1, 1] == p && board[2, 1] == p)
                    return true;

                if (board[0, 2] == p && board[1, 2] == p && board[2, 2] == p)
                    return true;

                //Columns
                if (board[0, 0] == p && board[0, 1] == p && board[0, 2] == p)
                    return true;

                if (board[1, 0] == p && board[1, 1] == p && board[1, 2] == p)
                    return true;

                if (board[2, 0] == p && board[2, 1] == p && board[2, 2] == p)
                    return true;

                //Diagonals
                if (board[0, 0] == p && board[1, 1] == p && board[2, 2] == p)
                    return true;

                if (board[0, 2] == p && board[1, 1] == p && board[2, 0] == p)
                    return true;


                return false;
            }

            private bool checkForTie()
            {
                for (int i = 0; i < board.Length; i++)
                {
                    if (getPiece(i) == Piece.Empty)
                        return false;
                }

                return true;
            }

            //lol
            public (int, int) getIndex(string text)
            {
                string[] args = text.ToLower().Split(' ');

                int xIndex = -1;
                int yIndex = -1;

                if (args.Length == 2)
                {
                    switch (args[0])
                    {
                        case "top":
                            yIndex = 0;
                            break;
                        case "middle":
                            yIndex = 1;
                            break;
                        case "bottom":
                            yIndex = 2;
                            break;
                    }

                    switch (args[1])
                    {
                        case "left":
                            xIndex = 0;
                            break;
                        case "middle":
                            xIndex = 1;
                            break;
                        case "right":
                            xIndex = 2;
                            break;
                    }
                }
                else if (args.Length == 1)
                {
                    switch (args[0])
                    {
                        case "a1":
                            xIndex = 0;
                            yIndex = 0;
                            break;
                        case "a2":
                            xIndex = 1;
                            yIndex = 0;
                            break;
                        case "a3":
                            xIndex = 2;
                            yIndex = 0;
                            break;
                        case "b1":
                            xIndex = 0;
                            yIndex = 1;
                            break;
                        case "b2":
                            xIndex = 1;
                            yIndex = 1;
                            break;
                        case "b3":
                            xIndex = 2;
                            yIndex = 1;
                            break;

                        case "c1":
                            xIndex = 0;
                            yIndex = 2;
                            break;
                        case "c2":
                            xIndex = 1;
                            yIndex = 2;
                            break;
                        case "c3":
                            xIndex = 2;
                            yIndex = 2;
                            break;

                        case "1a":
                            xIndex = 0;
                            yIndex = 0;
                            break;
                        case "2a":
                            xIndex = 1;
                            yIndex = 0;
                            break;
                        case "3a":
                            xIndex = 2;
                            yIndex = 0;
                            break;
                        case "1b":
                            xIndex = 0;
                            yIndex = 1;
                            break;
                        case "2b":
                            xIndex = 1;
                            yIndex = 1;
                            break;
                        case "3b":
                            xIndex = 2;
                            yIndex = 1;
                            break;

                        case "1c":
                            xIndex = 0;
                            yIndex = 2;
                            break;
                        case "2c":
                            xIndex = 1;
                            yIndex = 2;
                            break;
                        case "3c":
                            xIndex = 2;
                            yIndex = 2;
                            break;

                        case "middle":
                            xIndex = 1;
                            yIndex = 1;
                            break;
                    }
                }

                return (xIndex, yIndex);
            }

            private void sendGameEmbed()
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithAuthor($"Tic Tac Toe: {Players[0].Username} vs {Players[1].Username}", Players[0].GetAvatarUrl());
                builder.WithThumbnailUrl(Players[1].GetAvatarUrl());

                builder.Description += $":black_large_square: :one: :two: :three:\n";
                builder.Description += $":regional_indicator_a: {emote(getPiece(0))} {emote(getPiece(1))} {emote(getPiece(2))}\n";
                builder.Description += $":regional_indicator_b: {emote(getPiece(3))} {emote(getPiece(4))} {emote(getPiece(5))}\n";
                builder.Description += $":regional_indicator_c: {emote(getPiece(6))} {emote(getPiece(7))} {emote(getPiece(8))}\n";

                builder.WithFooter($"It's currently {currentPlayer}'s turn");

                channel.SendMessageAsync("", false, builder.Build());
            }

            public override void OnMessageReceived(SocketMessage sMsg)
            {
                if (sMsg.Author.Id == currentPlayer.Id && sMsg.Channel.Id == channel.Id)
                {
                    var indices = getIndex(sMsg.Content);

                    int xIndex = indices.Item1;
                    int yIndex = indices.Item2;

                    if (xIndex == -1 || yIndex == -1)
                    {
                        if (saidInvalidInput == false)
                        {
                            saidInvalidInput = true;
                            sMsg.Channel.SendMessageAsync("invalid input");
                            return;
                        }
                    }

                    if (board[xIndex, yIndex] != Piece.Empty)
                    {
                        sMsg.Channel.SendMessageAsync("Theres already a piece there");
                        return;
                    }

                    saidInvalidInput = false;

                    if (currentPlayer == Players[0])
                    {
                        board[xIndex, yIndex] = Piece.X;
                        if (checkForWin(Piece.X))
                            IsGameDone = true;
                        else
                            currentPlayer = Players[1];
                    }
                    else
                    {
                        board[xIndex, yIndex] = Piece.O;
                        if (checkForWin(Piece.O))
                            IsGameDone = true;
                        else
                            currentPlayer = Players[0];
                    }

                    sendGameEmbed();

                    if (IsGameDone)
                        sMsg.Channel.SendMessageAsync($"**{currentPlayer.Username}** Wins!");

                    if (checkForTie())
                    {
                        sMsg.Channel.SendMessageAsync($"**It's a tie!!!**");
                        IsGameDone = true;
                    }
                }
            }

            public override void OnStart()
            {
                if (Utils.GetRandomNumber(1, 2) == 1)
                    currentPlayer = Players[0];
                else
                    currentPlayer = Players[1];

                sendGameEmbed();
            }
        }

        private List<Game> activeGames = new List<Game>();

        public GamesModule()
        {
            AddCMD("Play tic tac toe against someone", async (sMsg, buffer) =>
            {
                if (sMsg.MentionedUsers.Count > 0)
                {
                    var userToDuel = sMsg.MentionedUsers.First();

                    if (activeGames.Any((a) => a.Players.Contains(userToDuel)))
                    {
                        await sMsg.Channel.SendMessageAsync("You or that person is already in a game");
                        return;
                    }

                    var msg = await sMsg.Channel.SendMessageAsync($"{userToDuel.Mention} You have been challenged to a game of Tic Tac Toe by {sMsg.Author.Mention}\nDo you want to accept?");
                    msg.CreateReactionCollector((userID, emote, wasAdded) =>
                    {
                        if (userID == userToDuel.Id)
                        {
                            if (activeGames.Any((a) => a.Players.Contains(userToDuel)))
                            {
                                sMsg.Channel.SendMessageAsync("You or that person is already in a game");
                                msg.DeleteReactionCollector();
                                return;
                            }

                            sMsg.Channel.SendMessageAsync("You have accepted the duel!");
                            TicTacToe game = new TicTacToe();
                            game.Setup(sMsg.Channel, userToDuel, sMsg.Author);
                            activeGames.Add(game);
                        }

                    }, new Emoji("⚔️"));
                }

            }, ".ttt");

            CoreModule.OnMessageReceived += (s) =>
            {
                for (int i = 0; i < activeGames.Count; i++)
                {
                    activeGames[i].OnMessageReceived(s);

                    if (activeGames[i].IsGameDone)
                    {
                        activeGames.RemoveAt(i);
                        Console.WriteLine("A game of tictactoe has been done.");
                    }
                }
            };
        }
    }
}
