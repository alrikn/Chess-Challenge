using ChessChallenge.API;
using System;

public class MyBot : IChessBot
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

    int number_of_evals = 0;

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
        if (board.HasKingsideCastleRight(is_white) || board.HasQueensideCastleRight(is_white))
        {
            result += 20; //to make sure that the king isn't running around
        }

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
        int time_limit = calculate_best_time(timer);

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
        Console.WriteLine($"depth reached: {depth - 1}; num of updates: {num_of_update}; score = {best_score}; time_limit = {time_limit}, number_of_eval = {number_of_evals}");
        return best_move;
    }


    /*
    **timer.MillisecondsElapsedThisTurn;
    **timer.MillisecondsRemaining;
    **each player has 60 seconds long. should return somehwere between 4 and 2 depending on how much time is left.
    **2 only is under 7 seconds
    */
    int calculate_best_time(Timer timer)
    {
        int total = timer.GameStartTimeMilliseconds;
        int time_left = timer.MillisecondsRemaining;

        float percentage = (float)time_left / total;
        if (percentage <= 0.1) //smaller than 10 %
        {
            Console.WriteLine("panic mode");
            return timer.MillisecondsRemaining / 30;
        }
        return time_left / 80; //grandmaster are never longer than 80 moves, and stupider, you are, shorter the game
    }


    long Quiescence(Board board, bool is_white, bool isMaximizing, long alpha, long beta)
    {
        long standPat = rating(is_white, board);
        number_of_evals++;

        if (isMaximizing)
        {
            if (standPat >= beta)
                return beta;
            if (standPat > alpha)
                alpha = standPat;
        }
        else
        {
            if (standPat <= alpha)
                return alpha;
            if (standPat < beta)
                beta = standPat;
        }

        Move[] moves = board.GetLegalMoves(true); // captures only
        foreach (var move in moves)
        {
            board.MakeMove(move);
            long score = Quiescence(board, is_white, !isMaximizing, alpha, beta);
            board.UndoMove(move);

            if (isMaximizing)
            {
                if (score > alpha)
                    alpha = score;
                if (alpha >= beta)
                    break;
            }
            else
            {
                if (score < beta)
                    beta = score;
                if (beta <= alpha)
                    break;
            }
        }

        return isMaximizing ? alpha : beta;
    }


    /*
    ** if no time or is unplayable, or at end of tree return
    ** if our turn, we look for highest value, if opponet turn, they try to find lowest value
    */
    long Minimax(Board board, int depth, bool isMaximizing, bool is_white, Timer timer, long alpha, long beta, int time_limit)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw() || timer.MillisecondsElapsedThisTurn >= time_limit)
        {
            if (board.IsInCheckmate() || board.IsDraw())
                return rating(is_white, board);
            return Quiescence(board, is_white, isMaximizing, alpha, beta);
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
