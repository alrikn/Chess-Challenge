using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class V3_bot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int Value(PieceType type) => type switch
    {
        PieceType.Pawn => 100,
        PieceType.Knight => 300,
        PieceType.Bishop => 300,
        PieceType.Rook => 500,
        PieceType.Queen => 900,
        PieceType.King => 10000,
        _ => 0
    };


    /*
    ** positive if winning,
    ** negative if losing
    ** if you can score a draw when you are losing, then good, otherwise, bad(or is that a natural thing)
    ** is_white is the actual real player, board.IsWhiteToMove is whoever is playing now, on the minmax algo
    */
    long rating(bool is_white, Board board)
    {
        int P = board.GetPieceList(PieceType.Pawn, is_white).Count - board.GetPieceList(PieceType.Pawn, !is_white).Count;
        int N = board.GetPieceList(PieceType.Knight, is_white).Count - board.GetPieceList(PieceType.Knight, !is_white).Count;
        int B = board.GetPieceList(PieceType.Bishop, is_white).Count - board.GetPieceList(PieceType.Bishop, !is_white).Count;
        int R = board.GetPieceList(PieceType.Rook, is_white).Count - board.GetPieceList(PieceType.Rook, !is_white).Count;
        int Q = board.GetPieceList(PieceType.Queen, is_white).Count - board.GetPieceList(PieceType.Queen, !is_white).Count;
        long result = (900 * Q) + (500 * R) + (300 * (B + N)) + (100 * P);
        bool is_player_turn = board.IsWhiteToMove == is_white;
        Span<Move> moveSpan = stackalloc Move[254];
        board.GetLegalMovesNonAlloc(ref moveSpan);
        int mobilityBonus = (board.IsWhiteToMove == is_white) ? moveSpan.Length : -moveSpan.Length;
        mobilityBonus = mobilityBonus / 2;
        result += mobilityBonus;
        //get num of possible moves and reward that 1 move = + 1 point (not just capture)
        int pawnScore = 0;
        foreach (var p in board.GetPieceList(PieceType.Pawn, is_white))
            pawnScore += (is_white ? p.Square.Rank : 7 - p.Square.Rank) * 5;
        result += pawnScore / 2; //pawn 

        if (board.IsInCheckmate()) //losing bad winning good
        {
            return is_player_turn ? int.MinValue : int.MaxValue;
            // if its your turn:(board.IsWhiteToMove == is_white) and the board is in checmate, that means that you are in checkmate, and so very bad
            //if its not your turn, it means the enemy is in checmate and so very good.
        }
        if (board.IsInCheck()) //checking opponent good being checked bad (usually)
        {
            //if (!is_player_turn)
            //    Console.WriteLine("possible check to be made to evil botS");
            if (is_player_turn)
                result += -50;
            else
                result += 50;
        }
        //if (board.HasKingsideCastleRight(is_white) || board.HasQueensideCastleRight(is_white))
        //    result += 20;
        if (board.IsDraw())
        {
            return -110; //if down more tan one pawn
        }
        return result;
    }

    /*
    ** defs:
    ** Move[] all_moves = board.GetLegalMoves(); // get all moves
    ** board.MakeMove(move); //advances board to move
    ** board.UndoMove(move); //resets board to the move that was (undoes board.MakeMove(move))
    ** timer.MillisecondsElapsedThisTurn //when this reaches 2000, exit branch
    ** timer.MillisecondsRemaining //s
    */
    Move iterative_deepening(Board board, Timer timer)
    {
        Move[] all_moves = board.GetLegalMoves();
        Random rng = new();
        Move best_move = all_moves[rng.Next(all_moves.Length)];
        bool is_white = board.IsWhiteToMove;
        int time_limit = timer.MillisecondsRemaining / 80;

        int depth = 1;
        int num_of_update = 0;
        long best_score = int.MinValue;
        while (timer.MillisecondsElapsedThisTurn < time_limit)
        {
            Move moveAtThisDepth = best_move;
            best_score = int.MinValue;
            num_of_update = 0;
            Move[] sub_moves = board.GetLegalMoves();

            foreach (Move move in sub_moves)
            {
                board.MakeMove(move);
                long score = Minimax(board, depth, false, is_white, timer, int.MinValue, int.MaxValue, time_limit);
                board.UndoMove(move);

                if (score > best_score)
                {
                    best_score = score;
                    num_of_update++;
                    moveAtThisDepth = move;
                }
                if (timer.MillisecondsElapsedThisTurn >= time_limit)
                    break;
            }
            if (best_score == int.MaxValue)
            {
                Console.WriteLine($"mate in {depth} found");
                return moveAtThisDepth;
            }

            if (timer.MillisecondsElapsedThisTurn >= time_limit)
                break;

            best_move = moveAtThisDepth;
            depth++; // Try deeper
        }
        Console.WriteLine($"V3 - depth reached: {depth - 1}; num of updates: {num_of_update}; score = "+ best_score + "; time_limit = " + time_limit);
        return best_move;
    }


    /*
    **timer.MillisecondsElapsedThisTurn;
    **timer.MillisecondsRemaining;
    **each player has 60 seconds long. should return somehwere between 4 and 2 depending on how much time is left.
    **2 only is under 7 seconds
    */
    int calculate_best_depth(Timer timer)
    {
        int timeLeft = timer.MillisecondsRemaining;

        if (timeLeft < 5000) // under 5 seconds: panic mode
            return 2;
        return 3;
    }

    /*
    ** if no time or is unplayable, or at end of tree return
    ** if our turn, we look for highest value, if opponet turn, they try to find lowest value
    ** alpha beta pruning explenation:
    ** Max
    ** ├── A → Min
    ** │   ├── A1 → 50
    ** │   └── A2 → 60  ← Best so far: Max sees 60
    ** └── B → Min
    **     ├── B1 → 20
    **     └── B2 → ?
    ** We are at the root (Max’s turn).
    ** 
    ** We first explore branch A, and we find:
    ** 
    **     A1: Max gets 50
    ** 
    **     A2: Max gets 60
    ** 
    ** So, alpha = 60 (best score Max has seen so far)
    ** Now we start exploring branch B.
    ** 
    ** Let’s say it's Min's turn under B. Min is trying to minimize Max’s score. So Min sees:
    ** 
    **     B1: Max would get 20 → Min thinks, “this is good for me”
    ** 
    **     So far, beta = 20 (best Min has seen for itself in branch B)
    ** 
    ** Now we check:
    ** 
    ** if (beta <= alpha) → if (20 <= 60) → TRUE
    ** 
    ** So we prune B2.
    ** explanation:
    ** 
    **    Even if B2 gives Max a huge score, Min won’t allow it.
    **
    **    Because Min is in control under B.
    **
    **    And Min has already seen B1, which limits Max to 20.
    **
    **    So Min would just play B1 and never allow B2 to happen.
    **
    **    Therefore, Max cannot do better than 20 in branch B.
    **
    **    But Max already knows that branch A gives 60.
    **
    **    So Max will never choose branch B, and we don’t need to evaluate B2.
    */
    long Minimax(Board board, int depth, bool isMaximizing, bool is_white, Timer timer, long alpha, long beta, int time_limit)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw() || timer.MillisecondsElapsedThisTurn >= time_limit)
        {
            return rating(is_white, board);
        }
        Move[] all_moves = board.GetLegalMoves();
        if (all_moves.Length == 0)
        {
            return rating(is_white, board);
        }
        Array.Sort(all_moves, (a, b) => { //move ordering makes it fast as fuck
            int Score(Move move) {
                var victim = board.GetPiece(move.TargetSquare);
                var attacker = board.GetPiece(move.StartSquare);
                if (victim.IsNull) return 0;
                return Value(victim.PieceType) * 10 - Value(attacker.PieceType);
            }
            return Score(b).CompareTo(Score(a));
        });


        long bestEval = isMaximizing ? int.MinValue : int.MaxValue;
        foreach (Move move in all_moves)
        {
            board.MakeMove(move);
            long eval = Minimax(board, depth - 1, !isMaximizing, is_white, timer, alpha, beta, time_limit);
            board.UndoMove(move);

            if (isMaximizing)
            {
                bestEval = Math.Max(bestEval, eval);
                if (bestEval > alpha)
                    alpha = bestEval;
            }
            else
            {
                bestEval = Math.Min(bestEval, eval);
                if (bestEval < beta)
                    beta = bestEval;
            }
            if (beta <= alpha)
                break;
            if (timer.MillisecondsElapsedThisTurn >= time_limit)
                    break;
        }
        return bestEval;
    }
    public Move Think(Board board, Timer timer)
    {
        return iterative_deepening(board, timer);
    }
    }
}
