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
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }
        Move return_nice_pos(Board board, Move[] all_moves, ref int score)
        {
            Random rng = new();
            Move move_result = all_moves[rng.Next(all_moves.Length)];
    
            foreach (Move move in all_moves)
            {
                if (MoveIsCheckmate(board, move))
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
                if (MoveIsCheckmate(board, move))
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
    
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        public Move Think(Board board, Timer timer)
        {
            Move[] all_moves = board.GetLegalMoves();
            List<Move> bad_moves = new List<Move>();
            Random rng = new();
            Move move_result = all_moves[rng.Next(all_moves.Length)];
            int score = 0;
    
            start:
            //score = 0;
            /*if i put score = 0 here, my bot suddenly becomes much worse.
            why?*/
            // remove bad moves
            Move[] legal_moves = all_moves.Where(m => !bad_moves.Contains(m)).ToArray();
    
            // If all moves are bad, return a default one
            if (legal_moves.Length == 0)
            {
                return move_result;
            }
    
            move_result = return_nice_pos(board, legal_moves, ref score);
    
            if (foot_is_shot(board, score, move_result))
            {
                bad_moves.Add(move_result);
                goto start;
            }
            //Console.WriteLine("MyBot plays: " + move_result);
            return move_result;
        }
    }
}