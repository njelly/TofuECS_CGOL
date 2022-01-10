using Tofunaut.TofuECS;

namespace Tofunaut.TofuECS_COGL.ECS
{
    public unsafe class BoardSystem : ISystem
    {
        public void Initialize(Simulation s)
        {
            if (!s.DB.GetSingleton(out COGLConfig config))
            {
                s.Log.Error("no COGLConfig data registered in the DB");
                return;
            }

            s.ModifySingletonComponent((ref Board board) =>
            {
                board.Size = config.BoardSize;
                for (var i = 0; i < config.BoardSize * config.BoardSize; i++)
                    board.State[i] = false;
            });
        }

        public void Process(Simulation s)
        {
            s.ModifySingletonComponent((ref Board board) =>
            {
                var offset = board.Size * board.Size;
                var toFlip = stackalloc int[offset];
                var numToFlip = 0;
                for (var x = 0; x < board.Size; x++)
                {
                    for (var y = 0; y < board.Size; y++)
                    {
                        var index = x + y * board.Size;
                        var numAlive = 0;

                        // top left
                        if (board.State[(index + board.Size - 1 + offset) % offset])
                            numAlive++;

                        // top middle
                        if (board.State[(index + board.Size + offset) % offset])
                            numAlive++;

                        // top right
                        if (board.State[(index + board.Size + 1 + offset) % offset])
                            numAlive++;

                        // middle left
                        if (board.State[(index - 1 + offset) % offset])
                            numAlive++;

                        // middle right
                        if (board.State[(index + 1 + offset) % offset])
                            numAlive++;

                        // bottom left
                        if (board.State[(index - board.Size - 1 + offset) % offset])
                            numAlive++;

                        // bottom middle
                        if (board.State[(index - board.Size + offset) % offset])
                            numAlive++;

                        // bottom right
                        if (board.State[(index - board.Size + 1 + offset) % offset])
                            numAlive++;

                        var isAlive = board.State[index];
                        var doFlip = false;
                        if (isAlive)
                            doFlip = numAlive is < 2 or > 3;
                        else
                            doFlip = numAlive == 3;

                        if (doFlip)
                            toFlip[++numToFlip] = index;
                    }
                }

                var boardStateChangedEvent = new BoardStateChangedEvent
                {
                    XCoords = new int[numToFlip],
                    YCoords = new int[numToFlip],
                    States = new bool[numToFlip],
                };

                for (var i = 0; i < numToFlip; i++)
                {
                    board.State[toFlip[i]] = !board.State[toFlip[i]];
                    boardStateChangedEvent.XCoords[i] = toFlip[i] % board.Size;
                    boardStateChangedEvent.YCoords[i] = toFlip[i] / board.Size;
                    boardStateChangedEvent.States[i] = board.State[toFlip[i]];
                }
                
                s.QueueExternalEvent(boardStateChangedEvent);
            });
        }
    }

    public struct BoardStateChangedEvent
    {
        public int[] XCoords;
        public int[] YCoords;
        public bool[] States;
    }
}