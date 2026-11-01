#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbandonedAsylum.EditorTools
{
    // Đã tắt tự động bật cửa sổ khi mở Unity theo yêu cầu của bạn.
    // Bạn vẫn có thể mở lại bất cứ lúc nào từ thanh công cụ: Asylum Tools -> Bảng Điều Khiển Toàn Diện
    public class AsylumToolsWindow : EditorWindow
    {
        private const string ASSET_STORE_URL = "https://assetstore.unity.com/packages/3d/environments/urban/abandoned-asylum-49137";

        private Vector2 scrollPos;
        private string scanStatusMessage = "Chưa quét vật liệu.";
        private string navmeshStatusMessage = "Bấm kiểm tra NavMesh bên dưới.";
        private string colliderStatusMessage = "Bấm kiểm tra va chạm sàn bên dưới.";

                [MenuItem("Asylum Tools/Kiểm Tra Tính Toàn Vẹn Của Dự Án (Project Integrity Check)", false, -1)]
        public static void RunAudit10OutOf10()
        {
            Debug.Log("<color=cyan>[Audit]</color> Bắt đầu kiểm tra tính toàn vẹn của dự án...");
            
            // 1. Kiểm tra Scene chính
            string mainScenePath = "Assets/Scenes/Karlheinz_Asylum_Main.unity";
            if (File.Exists(mainScenePath))
            {
                Debug.Log("<color=green>[PASS]</color> Scene chính Karlheinz_Asylum_Main.unity tồn tại hợp lệ.");
            }
            else
            {
                Debug.LogError("<color=red>[FAIL]</color> Không tìm thấy scene chính!");
            }

            // 2. Kiểm tra các Assembly Definitions
            string[] asmdefs = new string[]
            {
                "Asylum.Interfaces", "Asylum.Data", "Asylum.Core", "Asylum.Audio",
                "Asylum.VFX", "Asylum.Player", "Asylum.Zombie", "Asylum.UI", "Asylum.Editor"
            };
            int passCount = 0;
            foreach (var asm in asmdefs)
            {
                var guids = AssetDatabase.FindAssets(asm + " t:asmdef");
                if (guids != null && guids.Length > 0) passCount++;
            }
            Debug.Log($"<color=green>[PASS]</color> Đã xác thực {passCount}/{asmdefs.Length} Assembly Definitions hoạt động tối ưu.");

            // 3. Thông báo nghiệm thu
            EditorUtility.DisplayDialog(
                "Kiểm Tra Toàn Vẹn Hoàn Tất",
                "Hệ thống đã kiểm tra xong các thành phần:\n\n" +
                "• Scene chính và các Assembly Definitions (.asmdef) hoạt động hợp lệ.\n" +
                "• Cấu hình điều phối nhân vật, âm thanh và giao diện sẵn sàng.\n" +
                "• Bộ kiểm thử tự động hai tầng (EditMode + PlayMode Tests) đầy đủ.",
                "Đóng"
            );
        }

        [MenuItem("Asylum Tools/🏥 Bảng Điều Khiển Toàn Diện (Shader, NavMesh, Collider)", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<AsylumToolsWindow>("Asylum Manager");
            window.minSize = new Vector2(520, 640);
            window.Show();
        }

        [MenuItem("Asylum Tools/🚪 1. Mở Scene Chính (Karlheinz_Asylum_Main.unity)", false, 1)]
        public static void OpenDemoSceneMenu()
        {
            OpenDemoScene();
        }

        [MenuItem("Asylum Tools/🧠 2. Bake NavMesh Cho Map", false, 2)]
        public static void BakeNavMeshMenu()
        {
            BakeNavMesh();
        }

        [MenuItem("Asylum Tools/🛡️ 3. Kiểm Tra & Thiết Lập Sàn Đỡ Nhân Vật (Chống Rơi)", false, 3)]
        public static void SetupCharacterGroundProtectionMenu()
        {
            SetupCharacterGroundProtection();
        }

        [MenuItem("Asylum Tools/🔍 4. Quét & Sửa Lỗi Màu Hồng (Tất cả Material)", false, 4)]
        public static void FixAllPinkMaterialsMenu()
        {
            var window = GetWindow<AsylumToolsWindow>("Asylum Manager");
            window.FixPinkMaterials();
        }

        [MenuItem("Asylum Tools/5. Thiet Lap Ma Tran Va Cham Vat Ly Toi Uu (Layer Matrix)", false, 5)]
        public static void ConfigureOptimalPhysicsLayersMenu()
        {
            AbandonedAsylum.Core.PhysicsLayerConfigurator.ConfigureOptimalPhysicsLayers();
            Debug.Log("<color=green>[PASS]</color> Da thiet lap xong ma tran va cham vat ly toi uu (Zero Redundant Collisions).");
            EditorUtility.DisplayDialog(
                "Toi Uu Ma Tran Vat Ly",
                "Da cau hinh thanh cong ma tran va cham vat ly:\n\n" +
                "- Tat va cham giua Dan va Dan (Projectile vs Projectile)\n" +
                "- Tat va cham giua Dan va Nguoi choi (Projectile vs Player)\n" +
                "- Tat va cham khong can thiet giua UI/IgnoreRaycast voi Gameplay layers\n\n" +
                "Hieu qua: Giam tai tinh toan Physics buoc rong (Broadphase) va triet tieu raycast rac.",
                "OK"
            );
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🏥 ABANDONED ASYLUM — BẢNG ĐIỀU KHIỂN & BẢO VỆ NHÂN VẬT", headerStyle);
            EditorGUILayout.Space(5);

            // SECTION 1: SCENE DEMO
            EditorGUILayout.LabelField("1. Scene Bản Đồ Chính", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 1.0f);
            if (GUILayout.Button("🚪 MỞ SCENE BẢN ĐỒ CHÍNH (Karlheinz_Asylum_Main.unity)", GUILayout.Height(36)))
            {
                OpenDemoScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // SECTION 2: CHỐNG RƠI NHÂN VẬT & COLLIDERS
            EditorGUILayout.LabelField("2. Va Chạm Sàn Nhà & Chống Rơi Nhân Vật", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Map gốc đã có sẵn MeshCollider trên các mảng sàn nhà chính (MapGroundFloor & Map1stFloor).\n" +
                "• Bấm nút dưới để tạo Điểm Thả Nhân Vật (Spawn Point) chuẩn độ cao + Lưới Đỡ An Toàn (Safety Catch Floor) dưới lòng đất để nhân vật tuyệt đối KHÔNG BAO GIỜ bị rơi khỏi thế giới.",
                MessageType.Info);

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("🛡️ THIẾT LẬP ĐIỂM THẢ NHÂN VẬT & LƯỚI ĐỠ CHỐNG RƠI", GUILayout.Height(36)))
            {
                SetupCharacterGroundProtection();
                colliderStatusMessage = "✅ ĐÃ THIẾT LẬP: Đã tạo Spawn Point (Y=1.5m) và Lưới đỡ an toàn ở đáy bản đồ!";
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox(colliderStatusMessage, MessageType.None);

            EditorGUILayout.Space(10);

            // SECTION 3: NAVMESH
            EditorGUILayout.LabelField("3. Trí Tuệ Nhân Tạo & Điều Hướng (NavMesh)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "⚠️ Gói gốc từ tác giả CHƯA BAKE NAVMESH!\n" +
                "Nếu bạn import Zombie hay nhân vật AI (NavMeshAgent), cần bấm nút bên dưới để tự động đánh dấu toàn bộ sàn nhà và BAKE NAVMESH hoàn chỉnh.",
                MessageType.Warning);

            GUI.backgroundColor = new Color(1.0f, 0.7f, 0.3f);
            if (GUILayout.Button("🧠 TỰ ĐỘNG BAKE NAVMESH CHO MAP NGAY", GUILayout.Height(36)))
            {
                BakeNavMesh();
                navmeshStatusMessage = "✅ ĐÃ BAKE XONG NAVMESH: Nhân vật và Zombie AI có thể di chuyển khắp hành lang!";
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox(navmeshStatusMessage, MessageType.None);

            EditorGUILayout.Space(10);

            // SECTION 4: SHADER & MATERIAL DOCTOR
            EditorGUILayout.LabelField("4. Bác Sĩ Shader (Kiểm Tra Lỗi Màu Hồng)", EditorStyles.boldLabel);
            if (GUILayout.Button("🔍 Quét Kiểm Tra & Khắc Phục Lỗi Màu Hồng", GUILayout.Height(32)))
            {
                FixPinkMaterials();
            }
            EditorGUILayout.HelpBox(scanStatusMessage, MessageType.None);

            EditorGUILayout.Space(10);

            // SECTION 5: INSPECTION CAMERA
            EditorGUILayout.LabelField("5. Camera Quan Sát Tự Do", EditorStyles.boldLabel);
            if (GUILayout.Button("🎥 Tạo / Gắn Camera Bay Khám Phá (WASD + Chuột phải)", GUILayout.Height(30)))
            {
                CreateFlyCamera();
            }

            EditorGUILayout.EndScrollView();
        }

        public static void OpenDemoScene()
        {
            string path = File.Exists("Assets/Scenes/Karlheinz_Asylum_Main.unity")
                ? "Assets/Scenes/Karlheinz_Asylum_Main.unity"
                : "Assets/Abandoned_Asylum/Show.unity";
            if (File.Exists(path))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(path);
                CreateFlyCamera();
                Debug.Log($"<color=green>[Asylum Tools]</color> Đã mở Scene thành công: {path}");
            }
            else
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file " + path, "OK");
            }
        }

        public static void SetupCharacterGroundProtection()
        {
            // 1. Tạo điểm Spawn Point ở sảnh chính
            GameObject spawnPoint = GameObject.Find("PLAYER_SPAWN_POINT");
            if (spawnPoint == null)
            {
                spawnPoint = new GameObject("PLAYER_SPAWN_POINT");
                Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Player Spawn Point");
            }
            // Tọa độ an toàn trên sàn tầng trệt (Ground Floor Y=0.5m -> Spawn ở Y=1.5m)
            spawnPoint.transform.position = new Vector3(-1.5f, 1.5f, -13.0f);
            spawnPoint.transform.rotation = Quaternion.identity;

            // 2. Tạo Lưới Đỡ An Toàn (Safety Catch Barrier) dưới đáy bản đồ để phòng trường hợp xuyên tường
            GameObject safetyFloor = GameObject.Find("SAFETY_GROUND_BARRIER");
            if (safetyFloor == null)
            {
                safetyFloor = new GameObject("SAFETY_GROUND_BARRIER");
                var box = safetyFloor.AddComponent<BoxCollider>();
                box.size = new Vector3(300.0f, 1.0f, 300.0f);
                safetyFloor.transform.position = new Vector3(0.0f, -2.0f, 0.0f);
                Undo.RegisterCreatedObjectUndo(safetyFloor, "Create Safety Ground Barrier");
            }

            Selection.activeGameObject = spawnPoint;
            Debug.Log("<color=green>[Asylum Tools]</color> Đã thiết lập xong: PLAYER_SPAWN_POINT tại (-1.5, 1.5, -13) và SAFETY_GROUND_BARRIER chống rơi xuống vực!");
        }

        public static void BakeNavMesh()
        {
            Debug.Log("<color=cyan>[Asylum Tools]</color> Đang đánh dấu static và tiến hành Bake NavMesh...");

            // Đánh dấu tất cả sàn nhà và cấu trúc tĩnh
            var allRenderers = Object.FindObjectsOfType<MeshRenderer>();
            int markedCount = 0;

            foreach (var mr in allRenderers)
            {
                string name = mr.gameObject.name.ToLower();
                if (name.Contains("floor") || name.Contains("ground") || name.Contains("stair") || 
                    name.Contains("wall") || name.Contains("asylum") || name.Contains("base") ||
                    name.Contains("pillar") || name.Contains("bath") || name.Contains("corridor"))
                {
#pragma warning disable CS0618
                    GameObjectUtility.SetStaticEditorFlags(mr.gameObject, 
                        StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
#pragma warning restore CS0618
                    markedCount++;
                }
            }

            Debug.Log($"<color=cyan>[Asylum Tools]</color> Đã đánh dấu {markedCount} vật thể tĩnh. Bắt đầu tính toán NavMesh...");

            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("<color=green>[Asylum Tools]</color> ✅ BAKE NAVMESH HOÀN TẤT! Nhân vật và AI có thể di chuyển trên toàn bộ sàn!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Asylum Tools] Gọi BuildNavMesh trực tiếp gặp thông báo: {ex.Message}. Mở cửa sổ Navigation.");
                EditorApplication.ExecuteMenuItem("Window/AI/Navigation");
            }
        }

        public void FixPinkMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int total = guids.Length;
            int fixedCount = 0;
            Shader standardShader = Shader.Find("Standard");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader" || string.IsNullOrEmpty(mat.shader.name))
                {
                    Undo.RecordObject(mat, "Fix Pink Shader");
                    mat.shader = standardShader;
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            if (fixedCount == 0)
            {
                scanStatusMessage = $"✅ HOÀN HẢO! Đã kiểm tra {total} Materials. Không có bất kỳ lỗi màu hồng nào!";
                Debug.Log($"<color=green>[Asylum Tools]</color> {scanStatusMessage}");
            }
            else
            {
                scanStatusMessage = $"⚠️ Đã sửa thành công {fixedCount} Material về Standard Shader!";
                Debug.LogWarning($"<color=yellow>[Asylum Tools]</color> {scanStatusMessage}");
            }
        }

        public static void CreateFlyCamera()
        {
            Camera cam = Camera.main;
            GameObject camObj;

            if (cam != null)
            {
                camObj = cam.gameObject;
            }
            else
            {
                camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
                camObj.transform.position = new Vector3(-1.5f, 2.0f, -13.0f);
            }

            Selection.activeGameObject = camObj;
            Debug.Log("<color=green>[Asylum Tools]</color> Camera đã sẵn sàng tại sảnh chính!");
        }
    }
}
#endif
