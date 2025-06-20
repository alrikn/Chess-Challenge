using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    bool move_is_checkmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool is_mate = board.IsInCheckmate();
        board.UndoMove(move);
        return is_mate;
    }

    /*
    ** when bot is smarter we'll remove this cus it won't need it
    ** that or we'll only consider it bad when we are losing
    */
    bool move_is_draw(Board board, Move move)
    {
        board.MakeMove(move);
        bool is_draw = board.IsDraw();
        board.UndoMove(move);
        return is_draw;
    }

    /*
    ** positive if winning,
    ** negative if losing
    ** if you can score a draw when you are losing, then good, otherwise, bad(or is that a natural thing)
    ** is_white is the actual real player, board.IsWhiteToMove is whoever is playing now, on the minmax algo
    */
    int rating(bool is_white, Board board)
    {
        int P = board.GetPieceList(PieceType.Pawn, is_white).Count - board.GetPieceList(PieceType.Pawn, !is_white).Count;
        int N = board.GetPieceList(PieceType.Knight, is_white).Count - board.GetPieceList(PieceType.Knight, !is_white).Count;
        int B = board.GetPieceList(PieceType.Bishop, is_white).Count - board.GetPieceList(PieceType.Bishop, !is_white).Count;
        int R = board.GetPieceList(PieceType.Rook, is_white).Count - board.GetPieceList(PieceType.Rook, !is_white).Count;
        int Q = board.GetPieceList(PieceType.Queen, is_white).Count - board.GetPieceList(PieceType.Queen, !is_white).Count;
        int result = (900 * Q) + (500 * R) + (300 * (B + N)) + (100 * P);

        if (board.IsInCheckmate()) //losing bad winning good
        {
            result = board.IsWhiteToMove == is_white ? -10000 : 10000;
            return result;
        }
        if (board.IsInCheck()) //checking opponent good being checked bad (usually)
        {
            result += board.IsWhiteToMove == is_white ? -50 : 50;
            return result;
        }
        if (board.IsDraw())
        {
            if (result < -100)
                result = 0;
            else
                result = board.IsWhiteToMove == is_white ? -100 : 0;
        }
        return result;
    }

    /*
    ** defs:
    ** Move[] all_moves = board.GetLegalMoves(); // get all moves
    ** board.MakeMove(move); //advances board to move
    ** board.UndoMove(move); //resets board to the move that was (undoes board.MakeMove(move))
    ** timer.MillisecondsElapsedThisTurn //when this reaches 2000, exit branch
    ** 
    */
    Move min_max_handle(Board board, Timer timer)
    {
        Move[] all_moves = board.GetLegalMoves();
        Random rng = new();
        Move best_move = all_moves[rng.Next(all_moves.Length)];
        int best_score = int.MinValue;
        bool is_white = board.IsWhiteToMove;

        foreach (Move move in all_moves)
        {
            board.MakeMove(move);
            int score = Minimax(board, depth: 3, isMaximizing: false, is_white: is_white, timer: timer);
            board.UndoMove(move);

            if (score > best_score)
            {
                best_score = score;
                best_move = move;
            }
            if (timer.MillisecondsElapsedThisTurn >= 1950)
                break;
        }
        return best_move;
    }

    /*
    ** if no time or is unplayable, or at end of tree return
    ** if our turn, we look for highest value, if opponet turn, they try to find lowest value
    ** 
    */
    int Minimax(Board board, int depth, bool isMaximizing, bool is_white, Timer timer)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw() || timer.MillisecondsElapsedThisTurn >= 1950)
        {
            return rating(is_white, board);
        }
        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0)
        {
            return rating(is_white, board); // stalemate or checkmate
        }
        int bestEval = isMaximizing ? int.MinValue : int.MaxValue;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = Minimax(board, depth - 1, !isMaximizing, is_white, timer);
            board.UndoMove(move);

            if (isMaximizing)
            {
                bestEval = Math.Max(bestEval, eval);
            }
            else
            {
                bestEval = Math.Min(bestEval, eval);
            }
            if (timer.MillisecondsElapsedThisTurn >= 1950)
                break;
        }
        return bestEval;
    }


    /*
    ** essentially, we take the best move we have currently found,
    ** and we check that the next opponent move will not absolutely
    ** annihilate us (as in we are shooting ourself in foot)
    ** returns true if actually we have bad move, and false if its ok
    */
    bool foot_is_shot(Board board, int score, Move best_move)
    {
        board.MakeMove(best_move);
        Move[] all_moves = board.GetLegalMoves();
        bool bad = false;
        foreach (Move move in all_moves)
        {
            if (move_is_checkmate(board, move) || move_is_draw(board, move))
            {
                bad = true;
                break;
            }
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            if (score - capturedPieceValue < 0)
            {
                bad = true;
                break;
            }
        }
        board.UndoMove(best_move);
        //if bad is true we have to add bad_move to a list of illegal moves
        return bad;
    }

    Move return_best_pos(Board board, Move[] moves, out int bestCaptureValue)
    {
        // Compute from scratch each call
        bestCaptureValue = int.MinValue;
        Move best_move = Move.NullMove;
        Random rng = new();
        // But for simplicity, initialize to a random move in case no captures found.
        if (moves.Length > 0)
            best_move = moves[rng.Next(moves.Length)];
        foreach (var move in moves)
        {
            if (move_is_checkmate(board, move))
            {
                best_move = move;
                bestCaptureValue = int.MaxValue; // highest priority
                break;
            }
            Piece cap = board.GetPiece(move.TargetSquare);
            int val = pieceValues[(int)cap.PieceType];
            if (val > bestCaptureValue)
            {
                bestCaptureValue = val;
                best_move = move;
            }
        }
        // If no captures found, bestCaptureValue may remain int.MinValue; you can normalize that to 0:
        if (bestCaptureValue < 0)
            bestCaptureValue = 0;
        return best_move;
    }

    public Move Think(Board board, Timer timer)
    {
        return min_max_handle(board, timer);
    }
}