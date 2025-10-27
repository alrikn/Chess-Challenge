# chess bot
---

I really wanted to try my hand at making a bot that could play chess, as i had been playing a lot of chess and wanted to see if i could translate my mediocre chess skills to a chess bot. I am using the framework made by Sebastian Lague that he used to make a challenge some time ago. I chose this framework because i wasn’t so much interested at making a chess architecture so much as just jumping straight into programming and being able to immediately see the results of playing against that chess bot. (i truly believe that being able to see the results of your efforts is what makes SW so fun).

---

## Current bot:

around 1000 elo, plays very aggressively, but easily tricked (only sees 3-4 moves ahead). As soon as you play a few games against it, you can beat it consistently (its very predictable).

---

## Bot versions:

- Version 0: random move chooser that would look at all the moves (so only 1 move in advance). if it could take a piece, it would. I was quite terrible, as you could bait it to take a pawn with the queen and immediately eat the queen.

- Version 0.5: i made it check if the move it chose could result in the opponent taking the piece. this was quite a clunky way of doing this, and couldn’t really be used for looking more than 2 moves in advance, so i abandoned this approach and later went to a classic minmax.

- Version 1: basic minmax with non-dynamic time handling (always looked 3 moves in advance and if it ran out of time, looks 2 moves in advance). this bot was quite bad, but it could consistently beat my version 0 bots so i considered it a win. the moves were score based (the only time that a score changed was when it lost a piece or won a piece). this also meant that it would only see captures, and if it could not see any captures or losses, it would play completely randomly. For some reason it really liked moving the king around.

- Version 2: same minmax logic, but i made quite a big update to how the scoring worked. mobility now became part of the score. (how many moves could it possibly see). this actually made it quite better but also very aggressive since it would now try to take moves that would give it a lot of freedom. this also meant that it would try to get the queen out as soon as possible (since the queen can move around a lot). i also made it try to push its pawns a lot more since before in the endgame it would essentially idle because it couldn’t look far enough in the future to see that if it pushed its pawn to the end it would get a queen and pretty much win. i also changed how it calculated at what depth it would stop. (before it was a set depth, 2 or 3). i had discovered a func concept, which was iterative deepening. Essentially it would check everything (by everything, i mean what minmax doesn’t prune) on depth 1, check if it had time left then go to depth 2 etc… this had a few advantages and disavantages meaning that it would dynamically handle depths in a way, but it would also redo a lot of work (because depth 3 also does the work of depth 2, depth 4 also does the work of depth 3 etc..). this did mean that it got a much more inefficent, but it was balanced by the new rating function and the fact that it used time a lot more fully.

- Version 3: i saw on a chess forum that the alpha beta pruning was actually a lot more efficient if you sorted the moves first, so i did that and it was indeed better i thinks thats all i did. looking at it now, i’m pretty sure i did it a bit wrong (only implemented it at root depth instead of at all depths), but it was consistently beating the previous version, so i did not care.

- Version 4: i implemented quiescence and a much better time management system, that made it much more efficient (used more time and thought more). I’m pretty sure that implementing quiescence actually made it a bit worse, but it was offset by the better time management, and so it still beat the previous version, but not as overwhelmingly as the other versions would beat their own previous versions in the past.

- Version 5: this version was very frustrating for me, as i at first wanted to try my hand at making a dictionary to rid myself of the inherent inefficiencies of iterative deepening. This made the bot much much worse. Essentially it actually took much more time to malloc an entry in the dictionary and look up one at every single move than actually just calculating the move with minmax. Even worse, when running at a depth of 4 or 5, the dictionary only ever matched 3000 or 4000 moves, (which is very little). Complete waste of time. in the end, i removed all of that, and just focused on bug i was noticing in the logs. Essentially, when it was winning it would indeed print out int.max, which is normal, but when losing it wouldn’t print int.min. i was worried that this was because it was failing to see it was losing, but it turned out to just be the way that i stored and updated the value that i logged so i fixed that and called it an update.