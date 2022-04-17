using System;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS_CGOL.ECS
{
    public class BoardSystem : ISystem, ISystemEventListener<SetBoardStateInput>, ISystemEventListener<SetStaticProbabilityInput>
    {
        public static event Action<BoardStateChangedEventData> StateChanged;

        private bool[] _stateCached;
        private int[] _flippedIndexesCached;

        public void Initialize(Simulation s)
        {
            var boardData = s.GetSingletonComponent<BoardData>();
            var buffer = s.AnonymousBuffer<bool>(boardData.BufferIndex);
            _stateCached = new bool[buffer.Size];
            _flippedIndexesCached = new int[buffer.Size];
            
            buffer.GetState(_stateCached);
        }

        public unsafe void Process(Simulation s)
        {
            var boardData = s.GetSingletonComponent<BoardData>();
            var buffer = s.AnonymousBuffer<bool>(boardData.BufferIndex);
            var bufferSize = buffer.Size;
            buffer.GetState(_stateCached);
            var numToFlip = 0;
            for(var i = 0; i < _stateCached.Length; i++)
            {
                var numAlive = 0;
                
                // NOTE: 'bufferSize' is used here to avoid negative values with the % operator

                // top left
                if (_stateCached[(i + boardData.BoardSize - 1 + bufferSize) % bufferSize])
                    numAlive++;
                    
                // top center
                if (_stateCached[(i + boardData.BoardSize + bufferSize) % bufferSize])
                    numAlive++;

                // top right
                if (_stateCached[(i + boardData.BoardSize + 1 + bufferSize) % bufferSize])
                    numAlive++;

                // middle left
                if (_stateCached[(i - 1 + bufferSize) % bufferSize])
                    numAlive++;

                // middle right
                if (_stateCached[(i + 1 + bufferSize) % bufferSize])
                    numAlive++;

                // bottom left
                if (_stateCached[(i - boardData.BoardSize - 1 + bufferSize) % bufferSize])
                    numAlive++;

                // bottom center
                if (_stateCached[(i - boardData.BoardSize + bufferSize) % bufferSize])
                    numAlive++;

                // bottom right
                if (_stateCached[(i - boardData.BoardSize + 1 + bufferSize) % bufferSize])
                    numAlive++;

                bool doFlip;
                if (_stateCached[i])
                    doFlip = numAlive is < 2 or > 3;
                else
                    doFlip = numAlive is 3;

                // random static
                //if (!doFlip)
                //{
                    var randomDecimal = s.GetSingletonComponentUnsafe<XorShiftRandom>()->NextDouble();
                    doFlip |= boardData.StaticProbability > randomDecimal;
                //}

                if (doFlip)
                    _flippedIndexesCached[numToFlip++] = i;
            }

            for (var i = 0; i < numToFlip; i++)
                _stateCached[_flippedIndexesCached[i]] = !_stateCached[_flippedIndexesCached[i]];
            
            buffer.SetState(_stateCached);
                
            StateChanged?.Invoke(new BoardStateChangedEventData
            {
                BoardSize = boardData.BoardSize,
                FlippedIndexes = _flippedIndexesCached,
                NumToFlip = numToFlip,
                States = _stateCached,
            });
        }

        public void OnSystemEvent(Simulation s, in SetBoardStateInput eventData)
        {
            var boardData = s.GetSingletonComponent<BoardData>();
            var buffer = s.AnonymousBuffer<bool>(boardData.BufferIndex);
            var boardStateChangedEvent = new BoardStateChangedEventData
            {
                BoardSize = boardData.BoardSize,
                FlippedIndexes = new int[eventData.NewValues.Length],
                States = eventData.NewValues,
                NumToFlip = eventData.NewValues.Length,
            };

            for (var i = 0; i < boardStateChangedEvent.FlippedIndexes.Length; i++)
                boardStateChangedEvent.FlippedIndexes[i] = i;
            
            buffer.SetState(eventData.NewValues);
            
            StateChanged?.Invoke(boardStateChangedEvent);
        }

        public unsafe void OnSystemEvent(Simulation s, in SetStaticProbabilityInput eventData)
        {
            s.GetSingletonComponentUnsafe<BoardData>()->StaticProbability = eventData.StaticProbability;
        }
    }

    public class BoardStateChangedEventData
    {
        public int BoardSize;
        public int[] FlippedIndexes;
        public bool[] States;
        public int NumToFlip;
    }
}