using ChessChallenge.API;
using System.Collections.Generic;

using System;

public class V6_bot : IChessBot
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
        if (board.HasKingsideCastleRight(is_white) || board.HasQueensideCastleRight(is_white))
            result += 20; //to make sure that the king isn't running around

        if (board.IsInCheckmate()) //losing bad winning good
        {
            return is_player_turn ? int.MinValue : int.MaxValue; //Todo: add a depth variable to it for the min.value
            // if its your turn:(board.IsWhiteToMove == is_white) and the board is in checmate, that means that you are in checkmate, and so very bad
            //if its not your turn, it means the enemy is in checmate and so very good.
        }
        if (board.IsInCheck()) //checking opponent good being checked bad (usually)
        {
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

    void test_functions(Board board)
    {
        Move[] all_moves = board.GetLegalMoves(true); //only capture moves
        Move move = all_moves[0];
        Square king_square = board.GetKingSquare(true); //true == white
        bool same_move = move.Equals(all_moves[1]);
        PieceType piece = move.MovePieceType;
        if (piece == PieceType.Bishop)
            board.TrySkipTurn(); //Try skip the current turn. This will fail and return false if in check
        bool attack = board.SquareIsAttackedByOpponent(king_square); //could use this in the rating func to check stuff
        bool capture = move.IsCapture; // can be used to order moves
        board.TrySkipTurn(); //Try skip the current turn. This will fail and return false if in check
        board.UndoSkipTurn();
        bool bad_square = board.SquareIsAttackedByOpponent(move.TargetSquare);
        //the line above good be used to sort but also if we skip a turn,
        // to check if the place we are at is being protected (pawn chains and stuff)
    }

    /*
    ** defs:
    ** Move[] all_moves = board.GetLegalMoves(); // get all moves
    ** board.MakeMove(move); //advances board to move
    ** board.UndoMove(move); //resets board to the move that was (undoes board.MakeMove(move))
    ** timer.MillisecondsElapsedThisTurn //when this time_limit exit branch
    ** timer.MillisecondsRemaining //s
    ** TODO: order move 0 and/or find a way to avoid the foreach loop altogether
    ** TODO: ensure the bot always does at leat always a full depth 2 even if it runs out of time
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
        long best_score = int.MinValue; //this gets updated inside the loop
        long best_depth_score = best_score; //this gets updated when a depth has been fully done
        while (timer.MillisecondsElapsedThisTurn < time_limit)
        {
            //TODO: on each interative deeping, sort it so the move that turned out best last time is the first that this one takes
            //this is cus apparently its the move that is the most likely to be the best (so pruning should be better)
            //this can only really be done at root depth so i should do it right after board.GetLegalMoves();
            Move moveAtThisDepth = best_move;
            best_score = int.MinValue;
            num_of_update = 0;
            Move[] sub_moves = board.GetLegalMoves(); //do i even need to regenerate this each time?

            /*
            basic move ordering at root level:
            this ensures that our the first move will always be what we consider to be the best move,
            to make the pruning better and everything faster
            */
            long best_move_index = Array.IndexOf(sub_moves, best_move);
            sub_moves[best_move_index] = sub_moves[0];
            sub_moves[0] = best_move;


            foreach (Move move in sub_moves) //shouldn't we be able to do minmax immediately?
            {
                board.MakeMove(move);
                long score = Minimax(board, depth -1, false, is_white, timer, int.MinValue, int.MaxValue, time_limit);
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
                Console.WriteLine($"mate in {depth} found"); //this works great
                // but it might becom e dangerous if there is very little time
                return moveAtThisDepth;
            }
            if (timer.MillisecondsElapsedThisTurn >= time_limit)
                break;

            best_move = moveAtThisDepth;
            best_depth_score = best_score;
            depth++; // Try deeper
        }
        Console.WriteLine($"V6 - depth reached: {depth - 1}; num of updates: {num_of_update}; score = {best_depth_score}; time_limit = {time_limit}");
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
        return total / 80; //gm games are never longer than 80 moves, and the stupider you are, shorter the game
    }

    long Quiescence(Board board, bool is_white, bool isMaximizing, long alpha, long beta)
    {
        long standPat = rating(is_white, board);

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
        //TODO: if its in check, add 1 to depth
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
