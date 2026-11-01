#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Cinemachine;
using AbandonedAsylum.Player;

namespace AbandonedAsylum.Editor
{
    public static class CinemachineSetupHelper
    {
        [MenuItem("Asylum Tools/🎥 Setup Cinemachine Camera System", false, 5)]
        public static void SetupCinemachineInCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                string fallbackPath = File.Exists("Assets/Scenes/Karlheinz_Asylum_Main.unity")
                    ? "Assets/Scenes/Karlheinz_Asylum_Main.unity"
                    : "Assets/Abandoned_Asylum/Show.unity";
                scene = EditorSceneManager.OpenScene(fallbackPath);
            }

            GameObject playerObj = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[CinemachineSetup] Không tìm thấy Player GameObject trong Scene!");
                return;
            }

            Camera mainCam = Camera.main;
            GameObject camObj;
            if (mainCam != null)
            {
                camObj = mainCam.gameObject;
            }
            else
            {
                camObj = GameObject.Find("Main Camera");
                if (camObj == null)
                {
                    camObj = new GameObject("Main Camera");
                    mainCam = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                    camObj.tag = "MainCamera";
                }
            }

            var brain = camObj.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = camObj.AddComponent<CinemachineBrain>();
            }
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.2f);

            var camCtrl = camObj.GetComponent<PlayerCameraController>();
            if (camCtrl == null)
            {
                camCtrl = camObj.AddComponent<PlayerCameraController>();
            }
            camCtrl.target = playerObj.transform;
            camCtrl.SetupCinemachine();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=green>[CinemachineSetup] ✅ Đã cấu hình và lưu hệ thống Cinemachine (Brain, VCams, FollowTarget) thành công vào Scene!</color>");
        }
    }
}
#endif
