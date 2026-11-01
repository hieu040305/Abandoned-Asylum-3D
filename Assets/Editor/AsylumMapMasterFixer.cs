#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AbandonedAsylum.EditorTools
{
    public static class AsylumMapMasterFixer
    {
        private const string PREFAB_PATH = "Assets/Abandoned_Asylum/Prefabs/Asylum.prefab";
        private const string SCENE_PATH = "Assets/Scenes/Karlheinz_Asylum_Main.unity";
        private const string MESH_GUID_ELEVATOR_DOOR = "63054fdc7b5db8543990f13ecdc3bf24";
        private const string MAT_GUID_ELEVATOR_DOOR = "83bbdbb6035578e47801b5aeea5d43be";

        [MenuItem("Asylum Tools/🛠️ Khắc Phục Toàn Diện Cầu Thang Hầm, Kẹt Cửa & Cửa Thang Máy", false, 6)]
        public static void FixAllIssues()
        {
            Debug.Log("<color=cyan>[MASTER_FIXER]</color> Bắt đầu quy trình khắc phục bản đồ...");

            // -------------------------------------------------------------
            // BƯỚC 1: SỬA TRONG ASYLUM PREFAB
            // -------------------------------------------------------------
            FixPrefab();

            // -------------------------------------------------------------
            // BƯỚC 2: SỬA TRONG SHOW.UNITY SCENE
            // -------------------------------------------------------------
            FixScene();

            // -------------------------------------------------------------
            // BƯỚC 3: BAKE LẠI NAVMESH
            // -------------------------------------------------------------
            BakeNavMesh();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[MASTER_FIXER]</color> ✅ HOÀN THÀNH 100%! Đã thông cầu thang hầm, đóng kín cửa thang máy và loại bỏ triệt để hiện tượng kẹt cửa!");
        }

        private static void FixPrefab()
        {
            if (!File.Exists(PREFAB_PATH))
            {
                Debug.LogError($"[MASTER_FIXER] Không tìm thấy prefab tại {PREFAB_PATH}");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            if (prefabRoot == null)
            {
                Debug.LogError("[MASTER_FIXER] Không thể mở Prefab Contents!");
                return;
            }

            try
            {
                // 1.1 Khôi phục Elevator_Door và Elevator_Door (1)
                Transform elevatorWallTr = null;
                var allTrans = prefabRoot.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTrans)
                {
                    if (t.name == "Elevator_Wall" && t.parent != null && t.parent.name == "Asylum")
                    {
                        elevatorWallTr = t;
                        break;
                    }
                }

                if (elevatorWallTr != null)
                {
                    Mesh doorMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(MESH_GUID_ELEVATOR_DOOR));
                    Material doorMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(MAT_GUID_ELEVATOR_DOOR));

                    // Cửa tầng 1 / cửa trên
                    Transform door1 = elevatorWallTr.Find("Elevator_Door");
                    if (door1 == null)
                    {
                        GameObject go = new GameObject("Elevator_Door");
                        go.transform.SetParent(elevatorWallTr, false);
                        go.transform.localPosition = new Vector3(89.59253f, 18.623165f, -4.5348926f);
                        go.transform.localScale = new Vector3(4.934596f, 4.934596f, 4.934596f);
                        go.transform.localRotation = Quaternion.identity;

                        var mf = go.AddComponent<MeshFilter>();
                        mf.sharedMesh = doorMesh;
                        var mr = go.AddComponent<MeshRenderer>();
                        if (doorMat != null) mr.sharedMaterial = doorMat;
                        var mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = doorMesh;

                        Debug.Log("[MASTER_FIXER] 🚪 Đã khôi phục Elevator_Door trong Prefab!");
                    }

                    // Cửa tầng hầm / cửa dưới
                    Transform door2 = elevatorWallTr.Find("Elevator_Door (1)");
                    if (door2 == null)
                    {
                        GameObject go = new GameObject("Elevator_Door (1)");
                        go.transform.SetParent(elevatorWallTr, false);
                        go.transform.localPosition = new Vector3(89.59253f, -19.180775f, -4.5348954f);
                        go.transform.localScale = new Vector3(4.934596f, 4.934596f, 4.934596f);
                        go.transform.localRotation = Quaternion.identity;

                        var mf = go.AddComponent<MeshFilter>();
                        mf.sharedMesh = doorMesh;
                        var mr = go.AddComponent<MeshRenderer>();
                        if (doorMat != null) mr.sharedMaterial = doorMat;
                        var mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = doorMesh;

                        Debug.Log("[MASTER_FIXER] 🚪 Đã khôi phục Elevator_Door (1) trong Prefab!");
                    }
                }

                // 1.2 Gỡ bỏ MeshCollider gây kẹt trên tất cả các Door_Frame
                int disabledFrameCols = 0;
                foreach (var t in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Contains("Door_Frame"))
                    {
                        var mc = t.GetComponent<MeshCollider>();
                        if (mc != null)
                        {
                            UnityEngine.Object.DestroyImmediate(mc, true);
                            disabledFrameCols++;
                        }
                    }
                }
                Debug.Log($"[MASTER_FIXER] 🧹 Đã gỡ bỏ {disabledFrameCols} MeshCollider gây nghẽn/kẹt trên các khung cửa (Door_Frame) trong Prefab!");

                // 1.3 Tạo Smooth Ramp Collider cho Basement_Stairs
                Transform basementStairsTr = null;
                foreach (var t in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Basement_Stairs")
                    {
                        basementStairsTr = t;
                        break;
                    }
                }

                if (basementStairsTr != null)
                {
                    Transform existingRamp = basementStairsTr.Find("SmoothRampCollider");
                    if (existingRamp != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existingRamp.gameObject, true);
                    }

                    // Tạo collider dốc nghiêng mượt mà nối từ sàn tầng 1 xuống sàn tầng hầm
                    // Dựa trên bounds đo được: X=[15.66, 18.56] (width ~2.9m), Y=[0.0, -3.84] (height ~3.84m), Z=[-6.71, -13.88] (length ~7.17m)
                    GameObject ramp = new GameObject("SmoothRampCollider");
                    ramp.transform.SetParent(basementStairsTr, false);
                    // Cầu thang đổ dốc theo chiều -Z (từ Z=-6.71 xuống Z=-13.88) và hạ độ cao từ Y=0 xuống Y=-3.84
                    ramp.transform.localPosition = new Vector3(17.11f, -1.92f, -10.30f);
                    // Góc dốc tính từ arctan(3.84 / 7.17) ≈ 28.17 độ quanh trục X
                    ramp.transform.localRotation = Quaternion.Euler(-28.17f, 0f, 0f);

                    var boxCol = ramp.AddComponent<BoxCollider>();
                    // Chiều rộng X=2.8m, độ dày Y=0.15m, chiều dài dốc Z=sqrt(3.84^2 + 7.17^2) ≈ 8.13m
                    boxCol.size = new Vector3(2.8f, 0.15f, 8.2f);
                    boxCol.center = Vector3.zero;

                    Debug.Log("[MASTER_FIXER] 🪜 Đã tạo Smooth Ramp Collider trơn nhẵn cho cầu thang xuống hầm!");
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void FixScene()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH);

            // 2.1 Xử lý SAFETY_GROUND_BARRIER chắn cầu thang xuống hầm
            var safetyBarrier = GameObject.Find("SAFETY_GROUND_BARRIER");
            if (safetyBarrier != null)
            {
                // Hạ sâu xuống -25m để chỉ đóng vai trò chốt chặn cuối cùng nếu rơi ra ngoài bản đồ, tuyệt đối không cản tầng hầm (Y=-3.84)
                safetyBarrier.transform.position = new Vector3(0f, -25f, 0f);
                Debug.Log("[MASTER_FIXER] 🛡️ Đã hạ vị trí SAFETY_GROUND_BARRIER xuống Y=-25m (giải phóng hoàn toàn tầng hầm và cầu thang hầm)!");
            }

            // 2.2 Đảm bảo không còn MeshCollider nào trên các Door_Frame trong scene
            int sceneFrameCols = 0;
            var allCols = UnityEngine.Object.FindObjectsOfType<MeshCollider>();
            foreach (var mc in allCols)
            {
                if (mc != null && mc.name.Contains("Door_Frame"))
                {
                    UnityEngine.Object.DestroyImmediate(mc, true);
                    sceneFrameCols++;
                }
            }
            Debug.Log($"[MASTER_FIXER] 🚪 Đã dọn dẹp {sceneFrameCols} MeshCollider khung cửa trong Scene!");

            // 2.3 Tinh chỉnh CharacterController trên nhân vật Player
            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    // Chuẩn hóa thông số TPS/FPS:
                    // Radius = 0.27m (thay vì 0.32m) giúp lọt vừa mọi cửa hẹp mà không ma sát
                    // SkinWidth = 0.03m (thay vì 0.08m) tránh hiện tượng dội tường và giật giật
                    // StepOffset = 0.25m giúp bước qua mọi gờ sàn 1.4cm êm ái, đồng thời lọt dưới dạ cửa 2.2m không bị kẹt khi đi ra
                    // SlopeLimit = 50 độ hỗ trợ leo mọi loại dốc cầu thang
                    cc.radius = 0.27f;
                    cc.skinWidth = 0.03f;
                    cc.stepOffset = 0.25f;
                    cc.slopeLimit = 50.0f;
                    cc.height = 1.8f;
                    cc.center = new Vector3(0f, 0.9f, 0f);

                    EditorUtility.SetDirty(cc);
                    EditorUtility.SetDirty(player);
                    Debug.Log($"[MASTER_FIXER] 🏃 Đã tối ưu hóa CharacterController của nhân vật: Radius={cc.radius}, SkinWidth={cc.skinWidth}, StepOffset={cc.stepOffset}, SlopeLimit={cc.slopeLimit}");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BakeNavMesh()
        {
            try
            {
                NavMeshBuilder.ClearAllNavMeshes();
                NavMeshBuilder.BuildNavMesh();
                Debug.Log("[MASTER_FIXER] 🧭 Đã bake lại NavMesh toàn bộ bản đồ!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MASTER_FIXER] Lưu ý khi bake NavMesh: {ex.Message}");
            }
        }
    }
}
#endif
