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
#if CI
                if (controller.CurrGameNumber > 4)
                {
                    var statsA = controller.BotStatsA;
                    var statsB = controller.BotStatsB;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Bot: {statsA.BotName}");
                    sb.AppendLine($"Num Wins: {statsA.NumWins}");
                    sb.AppendLine($"Num Losses: {statsA.NumLosses}");
                    sb.AppendLine($"Num Draws: {statsA.NumDraws}");
                    sb.AppendLine($"Num Timeouts: {statsA.NumTimeouts}");
                    sb.AppendLine($"Num Illegal Moves: {statsA.NumIllegalMoves}");
                    sb.AppendLine();
                    sb.AppendLine($"Bot: {statsB.BotName}");
                    sb.AppendLine($"Num Wins: {statsB.NumWins}");
                    sb.AppendLine($"Num Losses: {statsB.NumLosses}");
                    sb.AppendLine($"Num Draws: {statsB.NumDraws}");
                    sb.AppendLine($"Num Timeouts: {statsB.NumTimeouts}");
                    sb.AppendLine($"Num Illegal Moves: {statsB.NumIllegalMoves}");

                    string filePath = "bot_match_results.txt";
                    File.WriteAllText(filePath, sb.ToString());

                    Log(sb.ToString());

                    System.Environment.Exit(0);
                }
#endif
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