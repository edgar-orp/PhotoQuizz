using UnityEngine;

namespace dev.xrlab.photoquizz.Core
{
    public class QuizAnswerData : MonoBehaviour
    {
        [Header("Respuesta")]
        public int questionIndex;  // Ej: 1, 2, 3...
        public int optionIndex;    // Ej: 0=A, 1=B, 2=C, 3=D
    }
}