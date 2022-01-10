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

            var boardEntity = s.CreateEntity();
            var boardBuffer = s.Buffer<Board>();
            var board = new Board
            {
                Size = config.BoardSize,
            };
            for (var i = 0; i < config.BoardSize * config.BoardSize; i++)
            {
                board.State[i] = false;
            }
            boardBuffer.Set(boardEntity, board);
        }

        public void Process(Simulation s)
        {
            var boardIterator = s.Buffer<Board>().GetIterator();
            
            // this is an easy pattern to use when we only care about a single instance of a component
            // TODO: should TofuECS just use an API for accessing singleton components?
            if (!boardIterator.Next())
                return;

            var current = boardIterator.Current;
            var offset = current.Size * current.Size;
            var toFlip = stackalloc int[offset];
            var numToFlip = 0;
            for (var x = 0; x < current.Size; x++)
            {
                for (var y = 0; y < current.Size; y++)
                {
                    var index = x + y * current.Size;
                    var numAlive = 0;
                    
                    // top left
                    if (current.State[(index + current.Size - 1 + offset) % offset])
                        numAlive++;
                    
                    // top middle
                    if (current.State[(index + current.Size + offset) % offset])
                        numAlive++;
                    
                    // top right
                    if (current.State[(index + current.Size + 1 + offset) % offset])
                        numAlive++;
                    
                    // middle left
                    if (current.State[(index - 1 + offset) % offset])
                        numAlive++;
                    
                    // middle right
                    if (current.State[(index + 1 + offset) % offset])
                        numAlive++;
                    
                    // bottom left
                    if (current.State[(index - current.Size - 1 + offset) % offset])
                        numAlive++;
                    
                    // bottom middle
                    if (current.State[(index - current.Size + offset) % offset])
                        numAlive++;
                    
                    // bottom right
                    if (current.State[(index - current.Size + 1 + offset) % offset])
                        numAlive++;
                    
                    var isAlive = current.State[index];
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
            
            boardIterator.ModifyCurrent((ref Board board) =>
            {
                for (var i = 0; i < numToFlip; i++)
                {
                    board.State[toFlip[i]] = !board.State[toFlip[i]];
                    boardStateChangedEvent.XCoords[i] = toFlip[i] % board.Size;
                    boardStateChangedEvent.YCoords[i] = toFlip[i] / board.Size;
                    boardStateChangedEvent.States[i] = board.State[toFlip[i]];
                }
            });
            
            s.QueueExternalEvent(boardStateChangedEvent);
        }
    }

    public struct BoardStateChangedEvent
    {
        public int[] XCoords;
        public int[] YCoords;
        public bool[] States;
    }
}