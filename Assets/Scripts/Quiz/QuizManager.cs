using System;
using System.Collections.Generic;
using UnityEngine;

namespace dev.xrlab.photoquizz.Core
{
    public class QuizManager : MonoBehaviour
    {
        [Header("Question Panels")]
        [SerializeField] private GameObject Question_01;
        [SerializeField] private GameObject Question_02;
        [SerializeField] private GameObject Question_03;

        private readonly Dictionary<int, int> answers = new Dictionary<int, int>(); // 1..3 -> 0..3

        public event Action<ProfileId> QuizCompleted;

        public void ShowQuestion(int index)
        {
            if (Question_01) Question_01.SetActive(index == 1);
            if (Question_02) Question_02.SetActive(index == 2);
            if (Question_03) Question_03.SetActive(index == 3);
        }

        public void OnAnswerClicked(QuizAnswerData answer)
        {
            answers[answer.questionIndex] = answer.optionIndex;

            int next = answer.questionIndex + 1;
            if (next <= 3) ShowQuestion(next);

            if (answer.questionIndex == 3)
            {
                var perfil = ComputeProfileFromClientRules();
                QuizCompleted?.Invoke(perfil);   // <-- notifica a AppManager
            }
        }


        public ProfileId ComputeProfileFromClientRules()
        {
            if (!answers.TryGetValue(1, out var a1)) return ProfileId.Undefined;
            if (!answers.TryGetValue(2, out var a2)) return ProfileId.Undefined;
            if (!answers.TryGetValue(3, out var a3)) return ProfileId.Undefined;

            return ProfileCalculator.ComputeProfile(a1, a2, a3);
        }

    }
}