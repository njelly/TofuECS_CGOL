using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS_CGOL.ECS
{
    public class BoardSystem : ISystem, ISystemEventListener<SetBoardStateInput>
    {
        public static event EventHandler<BoardStateChangedEventArgs> StateChanged;

        private bool[] _boardStateCache;
        private int[] _toFlipCache;
        private int _width;
        private ComponentBuffer<bool> _bufferCached;
        private BoardStateChangedEventArgs _boardStateChangedEventArgsCached;

        public unsafe void Initialize(Simulation s)
        {
            _bufferCached = s.Buffer<bool>();
            var width = (int)Math.Round(Math.Sqrt(_bufferCached.Size));
            var boardStateChangedEvent = new BoardStateChangedEventArgs
            {
                BoardWidth = width,
                FlippedIndexes = new int[_bufferCached.Size],
                States = new bool[_bufferCached.Size],
            };

            var r = s.GetSingletonComponentUnsafe<XorShiftRandom>();
            for (var i = 0; i < _bufferCached.Size; i++)
            {
                var value =  r->NextInt32() > 0;
                boardStateChangedEvent.FlippedIndexes[i] = i;
                boardStateChangedEvent.States[i] = value;
                _bufferCached.Set(s.CreateEntity(), value);
            }

            _boardStateCache = new bool[_bufferCached.Size];
            _toFlipCache = new int[_bufferCached.Size];
            _width = (int)Math.Round(Math.Sqrt(_bufferCached.Size));

            _boardStateChangedEventArgsCached = new BoardStateChangedEventArgs
            {
                BoardWidth = _width,
                FlippedIndexes = _toFlipCache,
                States = _boardStateCache,
            };

            StateChanged?.Invoke(this, boardStateChangedEvent);
        }

        public unsafe void Process(Simulation s)
        {
            _bufferCached.GetState(_boardStateCache);
            _boardStateChangedEventArgsCached.NumToFlip = 0;
            for (var i = 0; i < _boardStateCache.Length; i++)
            {
                var numAlive = 0;
                
                // NOTE: 'offset' is used here to avoid negative values with the % operator

                // top left
                if (_boardStateCache[(i + _width - 1 + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;
                    
                // top center
                if (_boardStateCache[(i + _width + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;

                // top right
                if (_boardStateCache[(i + _width + 1 + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;

                // middle left
                if (_boardStateCache[(i - 1 + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;

                // middle right
                if (_boardStateCache[(i + 1 + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;

                // bottom left
                if (_boardStateCache[(i - _width - 1 + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;

                // bottom center
                if (_boardStateCache[(i - _width + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;

                // bottom right
                if (_boardStateCache[(i - _width + 1 + _boardStateCache.Length) % _boardStateCache.Length])
                    numAlive++;

                bool doFlip;
                if (_boardStateCache[i])
                    doFlip = numAlive is < 2 or > 3;
                else
                    doFlip = numAlive is 3;

                if (doFlip)
                    _toFlipCache[++_boardStateChangedEventArgsCached.NumToFlip] = i;
            }
                
            for (var j = 0; j < _boardStateChangedEventArgsCached.NumToFlip; j++)
            {
                var cellValue = _bufferCached.GetAtUnsafe(_toFlipCache[j]);
                *cellValue = !*cellValue;
            }
                
            StateChanged?.Invoke(this, _boardStateChangedEventArgsCached);
        }

        public unsafe void OnSystemEvent(Simulation s, in SetBoardStateInput eventData)
        {
            var boardStateChangedEvent = new BoardStateChangedEventArgs
            {
                BoardWidth = (int)Math.Round(Math.Sqrt(s.Buffer<bool>().Size)),
                FlippedIndexes = new int[eventData.NewValues.Length],
                States = eventData.NewValues,
            };

            var buffer = s.Buffer<bool>();
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
        public int BoardWidth;
        public int[] FlippedIndexes;
        public bool[] States;
        public int NumToFlip;
    }
}