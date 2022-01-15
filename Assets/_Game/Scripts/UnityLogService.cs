using System;
using Tofunaut.TofuECS;

namespace Tofunaut.TofuECS_CGOL
{
    public class UnityLogService : ILogService
    {
        public void Debug(string s) => UnityEngine.Debug.Log(s);
        public void Info(string s) => UnityEngine.Debug.Log(s);
        public void Warn(string s) => UnityEngine.Debug.LogWarning(s);
        public void Error(string s) => UnityEngine.Debug.LogError(s);
        public void Exception(Exception e) => UnityEngine.Debug.LogException(e);
    }
}