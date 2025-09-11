namespace dev.xrlab.photoquizz.Core
{
    public static class ProfileCalculator
    {
        public static ProfileId ComputeProfile(int q1, int q2, int q3)
        {
            var rules = QuizProfileRules_Preset_Klarvillion.Rules;
            for (int i = 0; i < rules.Length; i++)
            {
                var r = rules[i];
                if (r.q1 == q1 && r.q2 == q2 && r.q3 == q3)
                    return r.profile;
            }
            return ProfileId.Undefined;
        }
    }
}