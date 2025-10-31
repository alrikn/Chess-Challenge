using Raylib_cs;
using System.Numerics;
using System;
using static ChessChallenge.Application.ConsoleHelper;

namespace ChessChallenge.Application
{
    public static class MatchStatsUI
    {
        public static void DrawMatchStats(ChallengeController controller)
        {
            if (controller.PlayerWhite.IsBot && controller.PlayerBlack.IsBot)
            {
                int nameFontSize = UIHelper.ScaleInt(40);
                int regularFontSize = UIHelper.ScaleInt(35);
                int headerFontSize = UIHelper.ScaleInt(45);
                Color col = new(180, 180, 180, 255);
                Vector2 startPos = UIHelper.Scale(new Vector2(1500, 250));
                float spacingY = UIHelper.Scale(35);

                DrawNextText($"Game {controller.CurrGameNumber} of {controller.TotalGameCount}", headerFontSize, Color.WHITE);
                startPos.Y += spacingY * 2;

                DrawStats(controller.BotStatsA);
                startPos.Y += spacingY * 2;
                DrawStats(controller.BotStatsB);

                if (controller.CurrGameNumber > 4)
                {
                    Log($"Bot: {controller.BotStatsA.BotName}");
                    Log($"Num Wins: {controller.BotStatsA.NumWins}");
                    Log($"Num Losses: {controller.BotStatsA.NumLosses}");
                    Log($"Num Draws: {controller.BotStatsA.NumDraws}");
                    Log($"Num Timeouts: {controller.BotStatsA.NumTimeouts}");
                    Log($"Num Illegal Moves: {controller.BotStatsA.NumIllegalMoves}");

                    Log($"Bot: {controller.BotStatsB.BotName}");
                    Log($"Num Wins: {controller.BotStatsB.NumWins}");
                    Log($"Num Losses: {controller.BotStatsB.NumLosses}");
                    Log($"Num Draws: {controller.BotStatsB.NumDraws}");
                    Log($"Num Timeouts: {controller.BotStatsB.NumTimeouts}");
                    Log($"Num Illegal Moves: {controller.BotStatsB.NumIllegalMoves}");
                    System.Environment.Exit(0);
                }

                void DrawStats(ChallengeController.BotMatchStats stats)
                {
                    DrawNextText(stats.BotName + ":", nameFontSize, Color.WHITE);
                    DrawNextText($"Num Wins: {stats.NumWins}", regularFontSize, col);
                    DrawNextText($"Num Losses: {stats.NumLosses}", regularFontSize, col);
                    DrawNextText($"Num Draws: {stats.NumDraws}", regularFontSize, col);
                    DrawNextText($"Num Timeouts: {stats.NumTimeouts}", regularFontSize, col);
                    DrawNextText($"Num Illegal Moves: {stats.NumIllegalMoves}", regularFontSize, col);
                }
           
                void DrawNextText(string text, int fontSize, Color col)
                {
                    UIHelper.DrawText(text, startPos, fontSize, 1, col);
                    startPos.Y += spacingY;
                }
            }
        }
    }
}