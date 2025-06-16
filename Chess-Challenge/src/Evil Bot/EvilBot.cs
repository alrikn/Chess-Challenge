using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
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
            bool is_draw = board.IsRepeatedPosition();
            board.UndoMove(move);
            return is_draw;
        }

        Move return_nice_pos(Board board, Move[] all_moves, ref int score)
        {
            Random rng = new();
            Move move_result = all_moves[rng.Next(all_moves.Length)];

            foreach (Move move in all_moves)
            {
                if (move_is_checkmate(board, move))
                {
                    move_result = move;
                    break;
                }
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
                if (capturedPieceValue > score)
                {
                    move_result = move;
                    score = capturedPieceValue;
                }
            }
            return move_result;
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

            // If you want randomness among equals, you could shuffle or pick random start:
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


        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        public Move Think(Board board, Timer timer)
        {
            Move[] all_moves = board.GetLegalMoves();
            HashSet<Move> bad_moves = new HashSet<Move>();
            Random rng = new();
            int bestCap;
            Move move_result = all_moves[rng.Next(all_moves.Length)]; ;

            while (true)
            {
                Move[] legal_moves = all_moves.Where(m => !bad_moves.Contains(m)).ToArray();
                if (legal_moves.Length == 0)
                {
                    // fallback: no non-bad moves remain; pick any or resign
                    // for now pick a random from all_moves or the last known move
                    return move_result != Move.NullMove ? move_result : all_moves[rng.Next(all_moves.Length)];
                }
                move_result = return_best_pos(board, legal_moves, out bestCap);

                if (foot_is_shot(board, bestCap, move_result))
                {
                    bad_moves.Add(move_result);
                    continue; // retry with remaining moves
                }
                return move_result;
            }
        }
    }
}