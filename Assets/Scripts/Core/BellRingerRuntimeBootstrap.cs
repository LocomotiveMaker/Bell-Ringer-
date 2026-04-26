using BellRinger.Debug;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Core
{
    public static class BellRingerRuntimeBootstrap
    {
        public const string RuntimeRootName = "BellRingerRuntime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            EnsureRuntimeHost();
        }

        public static GameObject EnsureRuntimeHost()
        {
            GameObject root = GameObject.Find(RuntimeRootName);

            if (root == null)
            {
                root = new GameObject(RuntimeRootName);
                Object.DontDestroyOnLoad(root);
            }

            EnsureComponent<HardwareBridge>(root);
            EnsureComponent<BellRingerRuntimeStatusWriter>(root);
            EnsureComponent<BellRingerDebugOverlay>(root);
            EnsureComponent<BellRingerAudioDemoBootstrapper>(root);
            return root;
        }

        private static T EnsureComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
    }
}
