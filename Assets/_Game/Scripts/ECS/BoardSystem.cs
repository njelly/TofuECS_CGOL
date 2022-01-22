using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS_CGOL.ECS
{
    public class BoardSystem : ISystem, ISystemEventListener<SetBoardStateInput>
    {
        public static event EventHandler<BoardStateChangedEventArgs> StateChanged;

        private bool[] _stateCached;
        private int[] _flippedIndexesCached;

        public unsafe void Initialize(Simulation s)
        {
            var boardData = s.GetSingletonComponent<BoardData>();
            var buffer = s.AnonymousBuffer<bool>(s.GetSingletonComponent<BoardData>().BufferIndex);
            _stateCached = new bool[buffer.Size];
            _flippedIndexesCached = new int[buffer.Size];

            var r = s.GetSingletonComponentUnsafe<XorShiftRandom>();
            var i = 0;
            while (buffer.NextUnsafe(ref i, out var value))
                *value = r->NextInt16() > 0;
            
            buffer.GetState(_stateCached);

            StateChanged?.Invoke(this, new BoardStateChangedEventArgs
            {
                States = _stateCached,
                FlippedIndexes = _flippedIndexesCached,
                BoardSize = boardData.BoardSize,
                NumToFlip = buffer.Size,
            });
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
                
                // NOTE: 'offset' is used here to avoid negative values with the % operator

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

                if (doFlip)
                    _flippedIndexesCached[++numToFlip] = i;
            }

            for (var j = 0; j < numToFlip; j++)
                _stateCached[_flippedIndexesCached[j]] = !_stateCached[_flippedIndexesCached[j]];
            
            buffer.SetState(_stateCached);
                
            StateChanged?.Invoke(this, new BoardStateChangedEventArgs
            {
                BoardSize = boardData.BoardSize,
                FlippedIndexes = _flippedIndexesCached,
                NumToFlip = numToFlip,
                States = _stateCached,
            });
        }

        public unsafe void OnSystemEvent(Simulation s, in SetBoardStateInput eventData)
        {
            var boardData = s.GetSingletonComponent<BoardData>();
            var buffer = s.AnonymousBuffer<bool>(boardData.BufferIndex);
            var boardStateChangedEvent = new BoardStateChangedEventArgs
            {
                BoardSize = boardData.BoardSize,
                FlippedIndexes = new int[eventData.NewValues.Length],
                States = eventData.NewValues,
            };

            for (var i = 0; i < eventData.NewValues.Length; i++)
            {
                boardStateChangedEvent.FlippedIndexes[i] = i;
                *buffer.GetAtUnsafe(i) = eventData.NewValues[i];
            }
            
            StateChanged?.Invoke(this, boardStateChangedEvent);
        }
    }

    public class BoardStateChangedEventArgs : EventArgs
    {
        public int BoardSize;
        public int[] FlippedIndexes;
        public bool[] States;
        public int NumToFlip;
    }
}