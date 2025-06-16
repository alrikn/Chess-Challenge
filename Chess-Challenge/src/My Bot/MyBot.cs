using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values index: 1=pawn ... 6=king; index 0 unused.
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsRepeatedPosition();
        board.UndoMove(move);
        return isDraw;
    }

    private class MoveNode
    {
        public Move Move;
        public List<MoveNode> Children = new();
        public int Score = 0; // for leaf evaluation or propagated alpha-beta result
        public MoveNode(Move move) => Move = move;
    }

    /*
    ** breaks when they are black
    */
    int EvaluateBoard(Board board)
    {
        int score = 0;
        var allPieceLists = board.GetAllPieceLists();
        for (int i = 0; i < allPieceLists.Length; i++)
        {
            var pieceList = allPieceLists[i];
            bool isWhite = i < 6;
            int pieceType = (i % 6) + 1; // 1=pawn ... 6=king
            int value = pieceValues[pieceType];
            score += value * pieceList.Count * (isWhite ? 1 : -1);
        }
        // small king-safety: penalize if side to move is in check
        if (board.IsInCheck())
        {
            // if white to move and in check, bad for white → negative; 
            // but EvaluateBoard returns positive = good for white.
            score += board.IsWhiteToMove ? -500 : 500;
        }
        return score;
    }

    // Build a full tree to given depth, storing evaluations at leaves
    void BuildTree(Board board, MoveNode node, int depth, Timer timer)
    {
        if (depth == 0)
        {
            node.Score = EvaluateBoard(board) * (board.IsWhiteToMove ? -1 : 1);
            return;
        }
        // Optional: check timer here to bail early; if time is up, you might assign a heuristic and return.
        foreach (Move move in board.GetLegalMoves())
        {
            if (timer.MillisecondsElapsedThisTurn > 5000)
                break;
            board.MakeMove(move);

            MoveNode child = new MoveNode(move);
            node.Children.Add(child);
            BuildTree(board, child, depth - 1, timer);

            board.UndoMove(move);
        }
        // If no children (no legal moves), you might set node.Score here:
        if (node.Children.Count == 0)
        {
            // game-over: checkmate or stalemate
            if (board.IsInCheckmate())
            {
                // losing position for side to move:
                node.Score = board.IsWhiteToMove ? int.MinValue/2 : int.MaxValue/2;
            }
            else
            {
                // stalemate or draw:
                node.Score = 0;
            }
        }
    }

    // Alpha-Beta over MoveNode tree
    int AlphaBeta(MoveNode node, int depth, int alpha, int beta, bool maximizing, Timer timer)
    {
        if (timer.MillisecondsElapsedThisTurn > 5000)
        {
            // Time’s up: return the current static score (either leaf-eval or previously propagated).
            return node.Score;
        }
        if (depth == 0 || node.Children.Count == 0)
        {
            return node.Score;
        }

        if (maximizing)
        {
            int value = int.MinValue;
            foreach (var child in node.Children)
            {
                int score = AlphaBeta(child, depth - 1, alpha, beta, false, timer);
                value = Math.Max(value, score);
                alpha = Math.Max(alpha, value);
                if (alpha >= beta)
                    break; // beta cutoff
            }
            node.Score = value;
            return value;
        }
        else
        {
            int value = int.MaxValue;
            foreach (var child in node.Children)
            {
                int score = AlphaBeta(child, depth - 1, alpha, beta, true, timer);
                value = Math.Min(value, score);
                beta = Math.Min(beta, value);
                if (beta <= alpha)
                    break; // alpha cutoff
            }
            node.Score = value;
            return value;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        // Choose search depth (plies)
        int depth = 5; // adjust per performance/time
        MoveNode root = new MoveNode(Move.NullMove);

        BuildTree(board, root, depth, timer);

        // If no legal moves at root, return NullMove
        if (root.Children.Count == 0)
            return Move.NullMove;

        // Run alpha-beta on the built tree
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;
        // Root is maximizing (we choose best)
        AlphaBeta(root, depth, alpha, beta, true, timer);

        // Pick the child with highest Score
        MoveNode bestChild = root.Children
            .OrderByDescending(n => n.Score)
            .First();

        return bestChild.Move;
    }
}