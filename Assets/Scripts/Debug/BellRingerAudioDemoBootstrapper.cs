using BellRinger.Audio;
using BellRinger.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BellRinger.Debug
{
    public sealed class BellRingerAudioDemoBootstrapper : MonoBehaviour
    {
        private const string DemoRootName = "BellRingerAudioDemo";
        private const string DemoClipPath = "BellRingerDemo/default-bell";

        private bool _setupComplete;

        private void Start()
        {
            TrySetupDemoScene();
        }

        private void TrySetupDemoScene()
        {
            if (_setupComplete || !Application.isPlaying)
            {
                return;
            }

            _setupComplete = true;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "SampleScene")
            {
                return;
            }

            if (GameObject.Find(DemoRootName) != null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindFirstObjectByType<Camera>();
            }

            if (mainCamera == null)
            {
                UnityEngine.Debug.LogWarning("[BellRingerAudioDemoBootstrapper] No camera found for demo setup.");
                return;
            }

            if (mainCamera.GetComponent<BellRingerSimpleMoveLookController>() == null)
            {
                mainCamera.gameObject.AddComponent<BellRingerSimpleMoveLookController>();
            }

            if (mainCamera.GetComponent<BellRingerSpatialAudioLedController>() == null)
            {
                mainCamera.gameObject.AddComponent<BellRingerSpatialAudioLedController>();
            }

            mainCamera.transform.position = new Vector3(0f, 1.6f, -4f);
            mainCamera.transform.rotation = Quaternion.identity;

            if (Object.FindFirstObjectByType<BellRingerSpatialSoundTarget>() != null)
            {
                return;
            }

            GameObject demoRoot = new GameObject(DemoRootName);
            CreateFloor(demoRoot.transform);
            CreateDemoBell(demoRoot.transform);
        }

        private static void CreateFloor(Transform parent)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "BellRingerDemoFloor";
            floor.transform.SetParent(parent);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
        }

        private static void CreateDemoBell(Transform parent)
        {
            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bell.name = "BellRingerDemoBell";
            bell.transform.SetParent(parent);
            bell.transform.position = new Vector3(0f, 1.4f, 4f);
            bell.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

            AudioSource source = bell.AddComponent<AudioSource>();
            source.clip = Resources.Load<AudioClip>(DemoClipPath);
            source.volume = 1f;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 8f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.loop = true;
            source.playOnAwake = true;

            bell.AddComponent<BellRingerSpatialSoundTarget>();

            if (source.clip == null)
            {
                UnityEngine.Debug.LogWarning("[BellRingerAudioDemoBootstrapper] Demo bell clip was not found in Resources/BellRingerDemo/default-bell.");
            }
        }
    }
}
