using Raylib_cs;
using System.Numerics;
using System.IO;
using System.Text;
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
                if (controller.CurrGameNumber > 200)
                {
                    var statsA = controller.BotStatsA;
                    var statsB = controller.BotStatsB;


                    // Parseable version for file (key=value format)
                    StringBuilder parseable = new StringBuilder();
                    parseable.AppendLine($"{statsA.BotName}_Wins={statsA.NumWins}");
                    parseable.AppendLine($"{statsA.BotName}_Losses={statsA.NumLosses}");
                    parseable.AppendLine($"{statsA.BotName}_Draws={statsA.NumDraws}");
                    parseable.AppendLine($"{statsA.BotName}_Timeouts={statsA.NumTimeouts}");
                    parseable.AppendLine($"{statsA.BotName}_IllegalMoves={statsA.NumIllegalMoves}");
                    parseable.AppendLine($"{statsB.BotName}_Wins={statsB.NumWins}");
                    parseable.AppendLine($"{statsB.BotName}_Losses={statsB.NumLosses}");
                    parseable.AppendLine($"{statsB.BotName}_Draws={statsB.NumDraws}");
                    parseable.AppendLine($"{statsB.BotName}_Timeouts={statsB.NumTimeouts}");
                    parseable.AppendLine($"{statsB.BotName}_IllegalMoves={statsB.NumIllegalMoves}");

                    string filePath = "bot_match_results.txt";
                    File.WriteAllText(filePath, parseable.ToString());

                    Log(parseable.ToString()); // This goes to console - human readable

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