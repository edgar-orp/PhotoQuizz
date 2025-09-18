using dev.xrlab.photoquizz.Audio;
using System.Collections;
using UnityEngine;

namespace dev.xrlab.photoquizz.Core
{
    public class ProfileManager : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private GameObject ResultsPanel;
        [SerializeField] private GameObject ResultsStandby;

        [Header("Profile Parents")]
        [SerializeField] private GameObject Perfil_Emocional;
        [SerializeField] private GameObject Perfil_Social;
        [SerializeField] private GameObject Perfil_Emprendedor;
        [SerializeField] private GameObject Perfil_Influenciable;

        [Header("Profile Steps (order matters)")]
        [SerializeField] private GameObject[] EmocionalSteps;     // Emocional_01, _02, _03
        [SerializeField] private GameObject[] SocialSteps;        // Social_01, _02, _03
        [SerializeField] private GameObject[] EmprendedorSteps;   // Emprendedor_01, _02, _03
        [SerializeField] private GameObject[] InfluenciableSteps; // Influenciable_01, _02, _03

        [SerializeField] private float ResultsDelaySeconds = 3f;

        private Coroutine delayRoutine;
        private ProfileId currentProfile = ProfileId.Undefined;
        private int currentStep = -1; // -1 = aún en Standby

        public void Begin(ProfileId perfil)
        {
            currentProfile = perfil;
            currentStep = -1;

            if (ResultsPanel) ResultsPanel.SetActive(true);
            HideAllProfilesAndSteps();

            if (ResultsStandby) ResultsStandby.SetActive(true);

            if (delayRoutine != null) StopCoroutine(delayRoutine);
            delayRoutine = StartCoroutine(ShowAfterDelay());
        }

        // Conecta este método al botón "Siguiente" de cada pantalla (_01 y _02),
        // y opcionalmente también al _03 si quieres repetir/terminar manualmente.
        public void OnNextStep()
        {
            if (currentProfile == ProfileId.Undefined) return;

            SoundManager.Instance.Play("ButtonNext");

            // primera vez: salir de Standby y mostrar paso 0
            if (currentStep < 0)
            {
                if (ResultsStandby) ResultsStandby.SetActive(false);
                ShowStep(0);
                return;
            }

            ShowStep(currentStep + 1);
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(ResultsDelaySeconds);
            OnNextStep(); // muestra el primer paso
            delayRoutine = null;
        }

        private void ShowStep(int stepIndex)
        {
            var steps = GetStepsArray(currentProfile);
            var parent = GetParent(currentProfile);

            if (parent) parent.SetActive(true);
            if (steps == null || steps.Length == 0) return;

            // clamp
            if (stepIndex < 0) stepIndex = 0;
            if (stepIndex >= steps.Length) stepIndex = steps.Length - 1;

            // activar sólo el paso actual
            for (int i = 0; i < steps.Length; i++)
                if (steps[i]) steps[i].SetActive(i == stepIndex);

            currentStep = stepIndex;
        }

        private void HideAllProfilesAndSteps()
        {
            if (Perfil_Emocional) Perfil_Emocional.SetActive(false);
            if (Perfil_Social) Perfil_Social.SetActive(false);
            if (Perfil_Emprendedor) Perfil_Emprendedor.SetActive(false);
            if (Perfil_Influenciable) Perfil_Influenciable.SetActive(false);

            HideArray(EmocionalSteps);
            HideArray(SocialSteps);
            HideArray(EmprendedorSteps);
            HideArray(InfluenciableSteps);

            if (ResultsStandby) ResultsStandby.SetActive(false);
        }

        private static void HideArray(GameObject[] arr)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i]) arr[i].SetActive(false);
        }

        private GameObject[] GetStepsArray(ProfileId perfil)
        {
            switch (perfil)
            {
                case ProfileId.Emocional: return EmocionalSteps;
                case ProfileId.Social: return SocialSteps;
                case ProfileId.Emprendedor: return EmprendedorSteps;
                case ProfileId.Influenciable: return InfluenciableSteps;
                default: return null;
            }
        }

        private GameObject GetParent(ProfileId perfil)
        {
            switch (perfil)
            {
                case ProfileId.Emocional: return Perfil_Emocional;
                case ProfileId.Social: return Perfil_Social;
                case ProfileId.Emprendedor: return Perfil_Emprendedor;
                case ProfileId.Influenciable: return Perfil_Influenciable;
                default: return null;
            }
        }

        public void ResetResults()
        {
            if (delayRoutine != null) { StopCoroutine(delayRoutine); delayRoutine = null; }
            currentProfile = ProfileId.Undefined;
            currentStep = -1;

            // Apaga todo
            if (ResultsStandby) ResultsStandby.SetActive(false);
            HideArray(EmocionalSteps);
            HideArray(SocialSteps);
            HideArray(EmprendedorSteps);
            HideArray(InfluenciableSteps);

            if (Perfil_Emocional) Perfil_Emocional.SetActive(false);
            if (Perfil_Social) Perfil_Social.SetActive(false);
            if (Perfil_Emprendedor) Perfil_Emprendedor.SetActive(false);
            if (Perfil_Influenciable) Perfil_Influenciable.SetActive(false);

            if (ResultsPanel) ResultsPanel.SetActive(false);
        }

    }
}
