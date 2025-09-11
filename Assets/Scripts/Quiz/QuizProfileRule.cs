using System;

namespace dev.xrlab.photoquizz.Core
{
    [Serializable]
    public struct QuizProfileRule
    {
        public int q1;      // 0=a, 1=b, 2=c, 3=d
        public int q2;
        public int q3;
        public ProfileId profile;
    }
}