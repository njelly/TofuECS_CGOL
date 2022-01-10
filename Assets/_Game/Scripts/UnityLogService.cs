using System;
using Tofunaut.TofuECS;
using UnityEngine;

namespace Tofunaut.TofuECS_COGL
{
    public class UnityLogService : ILogService
    {
        public void Info(string s)
        {
            Debug.Log(s);
        }

        public void Warn(string s)
        {
            Debug.LogWarning(s);
        }

        public void Error(string s)
        {
            Debug.LogError(s);
        }

        public void Exception(Exception e)
        {
            Debug.LogException(e);
        }
    }
}