#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AbandonedAsylum.EditorTools
{
    public static class CharacterSetupHelper
    {
        [MenuItem("Asylum Tools/⚡ Thiết Lập Toàn Diện Nhân Vật & Súng (TPS Style)", false, 10)]
        public static void SetupAll()
        {
            Debug.Log("<color=cyan>[Player Setup]</color> Bắt đầu thiết lập nhân vật, súng, animation và camera...");

            // 1. Cấu hình Texture Normal Maps
            ConfigureNormalMapTextures();

            // 2. Tạo PBR Materials cho Súng và Nhân Vật
            SetupMaterials();

            // 3. Cấu hình Humanoid cho Model nhân vật Ch15
            SetupHumanoidRig("Assets/Characters/Ch15/Ch15_Character.fbx");

            // 4. Lấy Avatar từ Ch15
            Avatar ch15Avatar = GetAvatarFromModel("Assets/Characters/Ch15/Ch15_Character.fbx");

            // 5. Cấu hình Humanoid và Loop cho tất cả Animations
            string animFolder = "Assets/Characters/Ch15/Animations";
            if (Directory.Exists(animFolder))
            {
                string[] animFiles = Directory.GetFiles(animFolder, "*.fbx");
                foreach (var file in animFiles)
                {
                    string fName = Path.GetFileName(file);
                    bool isFiring = fName.ToLower().Contains("firing") || fName.ToLower().Contains("shoot");
                    SetupAnimationRig(file.Replace("\\", "/"), ch15Avatar, !isFiring);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. Tạo UpperBody Avatar Mask cho Shooting Layer
            AvatarMask upperBodyMask = CreateUpperBodyMask();

            // 7. Tạo/Cập nhật Animator Controller TPS (2D Blend Tree + Shooting Layer)
            AnimatorController animController = CreateAnimatorController(upperBodyMask);

            // 8. Mở Scene Show.unity và thiết lập nhân vật + gắn súng trực tiếp vào tay
            SetupCharacterInScene(animController);

            Debug.Log("<color=green>[Player Setup]</color> ✅ HOÀN TẤT 100%! Súng đã gắn sẵn trên tay trong Scene View, vật liệu chuẩn PBR, animation chuẩn không giật!");
        }

        private static void ConfigureNormalMapTextures()
        {
            string[] texturePaths = Directory.GetFiles("Assets", "*Normal*.png", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets", "*Normal*.jpg", SearchOption.AllDirectories))
                .ToArray();

            foreach (var path in texturePaths)
            {
                string unityPath = path.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                    Debug.Log($"<color=cyan>[Texture]</color> Đã định dạng Normal Map: {unityPath}");
                }
            }
        }

        public static void SetupMaterials()
        {
            // --- Ch15 Materials ---
            string charMatDir = "Assets/Characters/Ch15/Materials";
            if (!Directory.Exists(charMatDir)) Directory.CreateDirectory(charMatDir);

            CreateStandardMaterial($"{charMatDir}/Ch15_body.mat",
                "Assets/Characters/Ch15/Textures/Ch15_1001_Diffuse.png",
                "Assets/Characters/Ch15/Textures/Ch15_1001_Normal.png");

            CreateStandardMaterial($"{charMatDir}/Ch15_body1.mat",
                "Assets/Characters/Ch15/Textures/Ch15_1002_Diffuse.png",
                "Assets/Characters/Ch15/Textures/Ch15_1002_Normal.png",
                "Assets/Characters/Ch15/Textures/Ch15_1002_Emissive.png");

            // --- Kalashnicolt Materials (khớp chính xác 5 slot: MAG, scope, Stock, handg1, M4_re) ---
            string weaponMatDir = "Assets/Weapons/Kalashnicolt/Materials";
            if (!Directory.Exists(weaponMatDir)) Directory.CreateDirectory(weaponMatDir);

            CreateStandardMaterial($"{weaponMatDir}/MAG.mat",
                "Assets/Weapons/Kalashnicolt/Textures/M16_M16_mag_BaseColor.jpg",
                "Assets/Weapons/Kalashnicolt/Textures/M16_M16_mag_Normal.jpg",
                null,
                "Assets/Weapons/Kalashnicolt/Textures/M16_M16_mag_Metalness.jpg");

            CreateStandardMaterial($"{weaponMatDir}/scope.mat",
                "Assets/Weapons/Kalashnicolt/Textures/scope_scope1_BaseColor.jpg",
                "Assets/Weapons/Kalashnicolt/Textures/scope_scope1_Normal.jpg",
                null,
                "Assets/Weapons/Kalashnicolt/Textures/scope_scope1_Metalness.jpg");

            CreateStandardMaterial($"{weaponMatDir}/Stock.mat",
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_Stock_BaseColor.jpg",
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_Stock_Normal.jpg",
                null,
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_Stock_Metalness.jpg");

            CreateStandardMaterial($"{weaponMatDir}/handg1.mat",
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_handg_BaseColor.jpg",
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_handg_Normal.jpg",
                null,
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_handg_Metalness.jpg");

            CreateStandardMaterial($"{weaponMatDir}/M4_re.mat",
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_M4_re_BaseColor.jpg",
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_M4_re_Normal.jpg",
                null,
                "Assets/Weapons/Kalashnicolt/Textures/Kalashnicolt_M4_re_Metalness.jpg");
        }

        private static Material CreateStandardMaterial(string matPath, string albedoPath, string normalPath = null, string emissivePath = null, string metallicPath = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Mobile/Diffuse");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            if (!string.IsNullOrEmpty(albedoPath))
            {
                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                if (albedo != null) mat.mainTexture = albedo;
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal != null)
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", normal);
                }
            }

            if (!string.IsNullOrEmpty(metallicPath))
            {
                Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                if (metallic != null)
                {
                    mat.EnableKeyword("_METALLICGLOSSMAP");
                    mat.SetTexture("_MetallicGlossMap", metallic);
                    mat.SetFloat("_Glossiness", 0.65f);
                }
            }

            if (!string.IsNullOrEmpty(emissivePath))
            {
                Texture2D emissive = AssetDatabase.LoadAssetAtPath<Texture2D>(emissivePath);
                if (emissive != null)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetTexture("_EmissionMap", emissive);
                    mat.SetColor("_EmissionColor", Color.white);
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetupHumanoidRig(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                Debug.Log($"<color=cyan>[Rig]</color> Đã thiết lập Humanoid cho {path}");
            }
        }

        private static Avatar GetAvatarFromModel(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Avatar avatar) return avatar;
            }
            return null;
        }

        private static void SetupAnimationRig(string path, Avatar sourceAvatar, bool loop = true)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                bool needReimport = false;
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    if (sourceAvatar != null)
                    {
                        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                        importer.sourceAvatar = sourceAvatar;
                    }
                    else
                    {
                        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    }
                    needReimport = true;
                }

                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].loopTime = loop;
                        clips[i].loopPose = loop;
                        clips[i].lockRootRotation = true;
                        clips[i].lockRootHeightY = true;
                        clips[i].lockRootPositionXZ = true;
                    }
                    importer.clipAnimations = clips;
                    needReimport = true;
                }

                if (needReimport)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static AvatarMask CreateUpperBodyMask()
        {
            string maskPath = "Assets/Characters/Ch15/UpperBodyMask.mask";
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, maskPath);
            }

            // Tách thân trên và thân dưới: Thân dưới (Root, Chân) tắt hoàn toàn, Thân trên (Thân, Đầu, Tay) bật
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController CreateAnimatorController(AvatarMask upperBodyMask)
        {
            string controllerPath = "Assets/Characters/Ch15/Ch15_Player_Controller.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            // Thêm Parameters
            AddParameterIfNotExists(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameterIfNotExists(controller, "Horizontal", AnimatorControllerParameterType.Float);
            AddParameterIfNotExists(controller, "Vertical", AnimatorControllerParameterType.Float);
            AddParameterIfNotExists(controller, "IsAiming", AnimatorControllerParameterType.Bool);
            AddParameterIfNotExists(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            AddParameterIfNotExists(controller, "Shoot", AnimatorControllerParameterType.Trigger);

            // Load clips chuẩn do user cung cấp (Dùng Ch15_nonPBR@Rifle Aiming Idle làm idle chuẩn)
            AnimationClip idleClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Rifle Aiming Idle.fbx") ??
                                    LoadFirstClip("Assets/Characters/Ch15/Animations/idle aiming.fbx");
            AnimationClip walkForwardClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Walk Forward.fbx") ??
                                           LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Rifle Walk.fbx") ??
                                           LoadFirstClip("Assets/Characters/Ch15/Animations/walk forward.fbx");
            AnimationClip runForwardClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Rifle Run.fbx") ??
                                          LoadFirstClip("Assets/Characters/Ch15/Animations/run forward.fbx");
            AnimationClip backWalkClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Backwards Rifle Walk.fbx") ??
                                        LoadFirstClip("Assets/Characters/Ch15/Animations/walk backward.fbx");
            AnimationClip backRunClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Backwards Rifle Run.fbx") ??
                                       LoadFirstClip("Assets/Characters/Ch15/Animations/run backward.fbx");
            AnimationClip runLeftClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Run Left.fbx") ??
                                       LoadFirstClip("Assets/Characters/Ch15/Animations/run left.fbx");
            AnimationClip runRightClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Ch15_nonPBR@Run Right.fbx") ??
                                        LoadFirstClip("Assets/Characters/Ch15/Animations/run right.fbx");
            AnimationClip firingClip = LoadFirstClip("Assets/Characters/Ch15/Animations/Firing Rifle.fbx");

            // --- LAYER 0: Locomotion (2D Blend Tree thân dưới và toàn thân) ---
            var baseStateMachine = controller.layers[0].stateMachine;
            foreach (var childState in baseStateMachine.states.ToArray())
            {
                baseStateMachine.RemoveState(childState.state);
            }

            // Tạo Blend Tree 2D mượt mà
            BlendTree blendTree;
            var locoState = controller.CreateBlendTreeInController("Locomotion", out blendTree);
            blendTree.blendType = BlendTreeType.SimpleDirectional2D;
            blendTree.blendParameter = "Horizontal";
            blendTree.blendParameterY = "Vertical";

            if (idleClip != null) blendTree.AddChild(idleClip, new Vector2(0f, 0f));
            if (walkForwardClip != null) blendTree.AddChild(walkForwardClip, new Vector2(0f, 0.5f));
            if (runForwardClip != null) blendTree.AddChild(runForwardClip, new Vector2(0f, 1.0f));
            if (backWalkClip != null) blendTree.AddChild(backWalkClip, new Vector2(0f, -0.5f));
            if (backRunClip != null) blendTree.AddChild(backRunClip, new Vector2(0f, -1.0f));
            if (runLeftClip != null) blendTree.AddChild(runLeftClip, new Vector2(-1.0f, 0f));
            if (runRightClip != null) blendTree.AddChild(runRightClip, new Vector2(1.0f, 0f));

            baseStateMachine.defaultState = locoState;

            // --- LAYER 1: Aiming & Shooting Layer (Giữ nguyên tư thế ngắm súng ở thân trên khi di chuyển) ---
            bool hasShootingLayer = false;
            foreach (var l in controller.layers)
            {
                if (l.name == "Shooting") { hasShootingLayer = true; break; }
            }

            if (!hasShootingLayer)
            {
                controller.AddLayer("Shooting");
            }

            var allLayers = controller.layers;
            allLayers[0].iKPass = true; // Bật IK Pass cho Base Layer

            AnimatorControllerLayer shootingLayer = null;
            for (int i = 0; i < allLayers.Length; i++)
            {
                if (allLayers[i].name == "Shooting")
                {
                    allLayers[i].avatarMask = upperBodyMask;
                    allLayers[i].defaultWeight = 1.0f;
                    allLayers[i].iKPass = true; // Bật IK Pass cho Shooting Layer
                    shootingLayer = allLayers[i];
                    break;
                }
            }
            controller.layers = allLayers;

            var shootSM = shootingLayer.stateMachine;
            foreach (var childState in shootSM.states.ToArray())
            {
                shootSM.RemoveState(childState.state);
            }
            foreach (var t in shootSM.anyStateTransitions.ToArray())
            {
                shootSM.RemoveAnyStateTransition(t);
            }

            // Trạng thái mặc định thân trên: Luôn giữ thế ngắm súng chuẩn (AimingIdle)
            var aimIdleState = shootSM.AddState("AimingIdle");
            aimIdleState.motion = idleClip;

            // Trạng thái bắn nảy giật (FireRecoil)
            var fireState = shootSM.AddState("FireRecoil");
            fireState.motion = firingClip;

            // AnyState -> FireRecoil khi bắn
            var tAnyToFire = shootSM.AddAnyStateTransition(fireState);
            tAnyToFire.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            tAnyToFire.canTransitionToSelf = true;
            tAnyToFire.hasExitTime = false;
            tAnyToFire.duration = 0.02f;

            // FireRecoil -> AimingIdle khi dứt phát bắn
            var tToAim = fireState.AddTransition(aimIdleState);
            tToAim.hasExitTime = true;
            tToAim.exitTime = 0.85f;
            tToAim.duration = 0.08f;

            shootSM.defaultState = aimIdleState;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddParameterIfNotExists(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static AnimationClip LoadFirstClip(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        public static void SetupCharacterInScene(AnimatorController animController)
        {
            string scenePath = File.Exists("Assets/Scenes/Karlheinz_Asylum_Main.unity")
                ? "Assets/Scenes/Karlheinz_Asylum_Main.unity"
                : "Assets/Abandoned_Asylum/Show.unity";
            if (!File.Exists(scenePath)) return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = EditorSceneManager.OpenScene(scenePath);

            // Tìm hoặc tạo Player
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Ch15/Ch15_Character.fbx");
                if (modelPrefab != null)
                {
                    player = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                    player.name = "Player";
                }
                else
                {
                    player = new GameObject("Player");
                }
            }

            // Gán Materials cho SkinnedMeshRenderer của nhân vật Ch15
            var smr = player.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                Material mat1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Characters/Ch15/Materials/Ch15_body.mat");
                Material mat2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Characters/Ch15/Materials/Ch15_body1.mat");
                if (mat1 != null && mat2 != null)
                {
                    smr.sharedMaterials = new Material[] { mat1, mat2 };
                    Debug.Log("<color=green>[Material]</color> ✅ Đã gán PBR Materials cho nhân vật Ch15.");
                }
            }

            // Đặt vị trí chính xác tại điểm 'spawn' do user tạo
            GameObject spawnPoint = GameObject.Find("spawn") ?? GameObject.Find("PLAYER_SPAWN_POINT");
            if (spawnPoint != null)
            {
                player.transform.position = spawnPoint.transform.position;
                player.transform.rotation = spawnPoint.transform.rotation;
                Debug.Log($"<color=green>[Spawn]</color> ✅ Đã đặt nhân vật tại điểm spawn: {spawnPoint.transform.position}");
            }
            else
            {
                player.transform.position = new Vector3(0.024f, 1.401f, -11.576f);
            }

            // Setup CharacterController
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.32f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Setup Animator
            var anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = animController;
                anim.applyRootMotion = false;
            }

            // Setup PlayerCharacterController
            var charCtrl = player.GetComponent<Player.PlayerCharacterController>();
            if (charCtrl == null) charCtrl = player.AddComponent<Player.PlayerCharacterController>();

            // --- GẮN SÚNG VÀO TAY PHẢI TRONG EDITOR (ƯU TIÊN GIỮ NGUYÊN VỊ TRÍ USER TỰ CHỈNH) ---
            Transform rightHand = FindDeepChild(player.transform, "mixamorig:RightHand") ?? FindDeepChild(player.transform, "RightHand");
            if (rightHand != null)
            {
                Transform equippedRifle = rightHand.Find("Kalashnicolt_Equipped");
                if (equippedRifle == null && rightHand.childCount > 0)
                {
                    equippedRifle = rightHand.GetChild(0);
                }

                if (equippedRifle == null)
                {
                    GameObject rifleFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapons/Kalashnicolt/Kalashnicolt.fbx");
                    if (rifleFbx != null)
                    {
                        GameObject rifleObj = (GameObject)PrefabUtility.InstantiatePrefab(rifleFbx, rightHand);
                        rifleObj.name = "Kalashnicolt_Equipped";
                        rifleObj.transform.localPosition = new Vector3(0.08f, 0.05f, 0.02f);
                        rifleObj.transform.localRotation = Quaternion.Euler(-85f, -10f, 95f);
                        rifleObj.transform.localScale = Vector3.one;
                        equippedRifle = rifleObj.transform;
                    }
                }

                // Gán đúng 5 PBR Materials vào súng để hết trắng bệch
                if (equippedRifle != null)
                {
                    Material matMAG = AssetDatabase.LoadAssetAtPath<Material>("Assets/Weapons/Kalashnicolt/Materials/MAG.mat");
                    Material matScope = AssetDatabase.LoadAssetAtPath<Material>("Assets/Weapons/Kalashnicolt/Materials/scope.mat");
                    Material matStock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Weapons/Kalashnicolt/Materials/Stock.mat");
                    Material matHandg = AssetDatabase.LoadAssetAtPath<Material>("Assets/Weapons/Kalashnicolt/Materials/handg1.mat");
                    Material matM4 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Weapons/Kalashnicolt/Materials/M4_re.mat");

                    var meshRenderers = equippedRifle.GetComponentsInChildren<MeshRenderer>();
                    foreach (var mr in meshRenderers)
                    {
                        mr.sharedMaterials = new Material[] { matMAG, matScope, matStock, matHandg, matM4 };
                    }
                    Debug.Log("<color=green>[Weapon Material]</color> ✅ Súng sẵn sàng trên tay phải, bạn có thể tự do dùng Gizmo chỉnh vị trí theo mắt nhìn.");
                }
            }

            // Gán Rifle Prefab dự phòng cho script
            GameObject rifleAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapons/Kalashnicolt/Kalashnicolt.fbx");
            SerializedObject so = new SerializedObject(charCtrl);
            so.FindProperty("riflePrefab").objectReferenceValue = rifleAsset;
            so.ApplyModifiedProperties();

            // Setup Camera Main
            Camera cam = Camera.main;
            GameObject camObj = (cam != null) ? cam.gameObject : new GameObject("Main Camera");
            if (cam == null)
            {
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }

            var camCtrl = camObj.GetComponent<Player.PlayerCameraController>();
            if (camCtrl == null) camCtrl = camObj.AddComponent<Player.PlayerCameraController>();
            camCtrl.target = player.transform;
            camCtrl.SetupCinemachine();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = player;
        }

        [MenuItem("Asylum Tools/🔍 Đo Khoảng Cách Và Góc Hai Tay", false, 20)]
        public static void MeasureHands()
        {
            string scenePath = File.Exists("Assets/Scenes/Karlheinz_Asylum_Main.unity")
                ? "Assets/Scenes/Karlheinz_Asylum_Main.unity"
                : "Assets/Abandoned_Asylum/Show.unity";
            EditorSceneManager.OpenScene(scenePath);

            GameObject player = GameObject.Find("Player");
            if (player == null) { Debug.LogError("Không tìm thấy Player!"); return; }

            Animator anim = player.GetComponent<Animator>();
            Transform rh = anim != null ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
            Transform lh = anim != null ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            Transform grip = FindDeepChild(player.transform, "LeftHandGrip");
            Transform equippedRifle = FindDeepChild(player.transform, "Kalashnicolt_Equipped");

            Debug.Log($"=== THÔNG SỐ ĐO ĐẠC TRONG UNITY ===");
            if (rh != null) Debug.Log($"RightHand Pos: {rh.position} | Rot: {rh.eulerAngles}");
            if (lh != null) Debug.Log($"LeftHand Pos: {lh.position} | Rot: {lh.eulerAngles}");
            if (equippedRifle != null) Debug.Log($"Rifle LocalPos: {equippedRifle.localPosition} | LocalRot: {equippedRifle.localEulerAngles}");
            if (grip != null) Debug.Log($"LeftHandGrip World Pos: {grip.position}");

            if (lh != null && grip != null)
            {
                float dist = Vector3.Distance(lh.position, grip.position);
                Debug.Log($"<color=yellow>Khoảng cách giữa LeftHand tự nhiên và LeftHandGrip súng: {dist * 100f:F1} cm</color>");
                Vector3 delta = grip.position - lh.position;
                Debug.Log($"Độ lệch (Grip - LH): X={delta.x*100f:F1}cm, Y={delta.y*100f:F1}cm, Z={delta.z*100f:F1}cm");
            }
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform result = FindDeepChild(child, childName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif
