namespace Tofunaut.TofuECS_COGL.ECS
{
    public unsafe struct Board
    {
        public fixed bool State[COGLConfig.MaxBoardSize * COGLConfig.MaxBoardSize];
        public int Size;
    }
}