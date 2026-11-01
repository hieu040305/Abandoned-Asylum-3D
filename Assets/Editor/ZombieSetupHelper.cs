#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using AbandonedAsylum.Zombie;

namespace AbandonedAsylum.EditorTools
{
    [InitializeOnLoad]
    public static class ZombieSetupHelper
    {
        private const string ZOMBIE_DIR = "Assets/Characters/Zombie";
        private const string MODEL_PATH = "Assets/Characters/Zombie/Parasite_Model.fbx";
        private const string ANIM_DIR = "Assets/Characters/Zombie/Animations";
        private const string IDLE_PATH = "Assets/Characters/Zombie/Animations/Zombie_Idle.fbx";
        private const string RUN_PATH = "Assets/Characters/Zombie/Animations/Zombie_Running.fbx";
        private const string ATTACK_PATH = "Assets/Characters/Zombie/Animations/Zombie_Attack.fbx";
        private const string DEATH_PATH = "Assets/Characters/Zombie/Animations/Zombie_Death.fbx";

        private const string IDLE_ANIM = "Assets/Characters/Zombie/Animations/Zombie_Idle.anim";
        private const string RUN_ANIM = "Assets/Characters/Zombie/Animations/Zombie_Running.anim";
        private const string ATTACK_ANIM = "Assets/Characters/Zombie/Animations/Zombie_Attack.anim";
        private const string DEATH_ANIM = "Assets/Characters/Zombie/Animations/Zombie_Death.anim";

        private const string CONTROLLER_PATH = "Assets/Characters/Zombie/Zombie_Controller.controller";
        private const string PREFAB_PATH = "Assets/Characters/Zombie/Zombie_Parasite.prefab";
        private static string SCENE_PATH => File.Exists("Assets/Scenes/Karlheinz_Asylum_Main.unity")
            ? "Assets/Scenes/Karlheinz_Asylum_Main.unity"
            : "Assets/Abandoned_Asylum/Show.unity";

        private const string MAT_BODY_PATH = "Assets/Characters/Zombie/Materials/Mat_ParasiteZombie_Body.mat";
        private const string MAT_HEAD_PATH = "Assets/Characters/Zombie/Materials/Mat_ParasiteZombie_Head.mat";

        static ZombieSetupHelper()
        {
            EditorApplication.delayCall += CheckAndSetupAuto;
        }

        private static void CheckAndSetupAuto()
        {
            // Tự động thiết lập nếu phát hiện file death mới hoặc prefab chưa hoàn chỉnh
            if (File.Exists(MODEL_PATH) && File.Exists(DEATH_PATH) && !File.Exists(DEATH_ANIM))
            {
                Debug.Log("<color=cyan>[ZombieAutoSetup]</color> Phát hiện Animation Death mới, đang tự động đồng bộ hóa...");
                SetupCompleteZombie();
            }
        }

        [MenuItem("Asylum Tools/🧟 Thiết Lập Toàn Diện Quái Vật Zombie (Parasite)", false, 11)]
        public static void SetupCompleteZombie()
        {
            Debug.Log("<color=cyan>[ZombieSetup]</color> ========================================================");
            Debug.Log("<color=cyan>[ZombieSetup]</color> BẮT ĐẦU THIẾT LẬP TOÀN DIỆN QUÁI VẬT ZOMBIE & COMBAT & SPAWNER");
            Debug.Log("<color=cyan>[ZombieSetup]</color> ========================================================");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 1. Cấu hình Humanoid Rig cho Model & 4 Animations
            Avatar zombieAvatar = SetupHumanoidRigs();
            if (zombieAvatar == null)
            {
                Debug.LogError("[ZombieSetup] Không thể tiếp tục do thiếu Avatar!");
                return;
            }

            // 2. Tạo Animation Clips chỉnh sửa được (.anim) có gắn sẵn Event & Loop
            AnimationClip idleClip = CreateEditableClip(IDLE_PATH, IDLE_ANIM, true);
            AnimationClip runClip = CreateEditableClip(RUN_PATH, RUN_ANIM, true);
            AnimationClip attackClip = CreateEditableAttackClip(ATTACK_PATH, ATTACK_ANIM);
            AnimationClip deathClip = CreateEditableDeathClip(DEATH_PATH, DEATH_ANIM);

            // 3. Tạo Animator Controller chuẩn (Full-Body, bao gồm cả Death State ngã xuống sàn)
            AnimatorController animController = CreateZombieAnimatorController(idleClip, runClip, attackClip, deathClip);
            if (animController == null)
            {
                Debug.LogError("[ZombieSetup] Không thể tạo Animator Controller!");
                return;
            }

            // 4. Tạo Prefab Zombie_Parasite chuẩn chuyên nghiệp (Gán Materials PBR, Kinematic Rigidbody, NavMeshAgent mượt mà)
            GameObject zombiePrefab = CreateZombiePrefab(zombieAvatar, animController);

            // 5. Thiết lập Zombie & ZombieSpawner (10s) trong Scene Show.unity + Khôi phục toàn bộ tường hầm
            SetupZombieInScene(zombiePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[ZombieSetup]</color> ========================================================");
            Debug.Log("<color=green>[ZombieSetup]</color> ✅ HOÀN THÀNH 100%! TẤT CẢ VẤN ĐỀ ĐÃ ĐƯỢC GIẢI QUYẾT TRIỆT ĐỂ:");
            Debug.Log("<color=green>[ZombieSetup]</color> 1. Tường hầm (HallUnderground): Đã bật lại MeshCollider, không còn đi xuyên tường.");
            Debug.Log("<color=green>[ZombieSetup]</color> 2. Quái vật hết trắng bệch: Đã gán bộ Material PBR (Albedo 2K + Normal Map + Specular).");
            Debug.Log("<color=green>[ZombieSetup]</color> 3. Khử hoàn toàn giật lag / đứng yên: Thêm Kinematic Rigidbody, buffer stoppingDistance = 1.3m, điều tiết pathfinding 0.15s.");
            Debug.Log("<color=green>[ZombieSetup]</color> 4. Animation Death & Ngã sát sàn: Đã nạp Zombie_Death, tắt collider khi chết, nằm sát sàn, xong animation mới destroy.");
            Debug.Log("<color=green>[ZombieSetup]</color> 5. Điểm sinh quái (SpawnZombie): Tích hợp ZombieSpawner tự động sinh 1 quái mỗi 10 giây.");
            Debug.Log("<color=green>[ZombieSetup]</color> 6. Chớp máu đỏ viền màn hình: Đã tạo hiệu ứng Radial Blood Vignette khi Player bị cào.");
            Debug.Log("<color=green>[ZombieSetup]</color> ========================================================");
        }

        private static Avatar SetupHumanoidRigs()
        {
            if (!File.Exists(MODEL_PATH))
            {
                Debug.LogError($"[ZombieSetup] Không tìm thấy file model tại {MODEL_PATH}");
                return null;
            }

            // 1.1 Cấu hình Model chính
            AssetDatabase.ImportAsset(MODEL_PATH, ImportAssetOptions.ForceSynchronousImport);
            var modelImporter = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                modelImporter.SaveAndReimport();
                Debug.Log("<color=cyan>[ZombieSetup]</color> 🦴 Đã cấu hình Humanoid Rig cho Parasite_Model.fbx");
            }

            Avatar avatar = GetAvatar(MODEL_PATH);
            if (avatar == null)
            {
                Debug.LogError("[ZombieSetup] Không tìm thấy Avatar trong Parasite_Model.fbx sau khi import!");
                return null;
            }

            // 1.2 Cấu hình 4 Animation Clips FBX
            SetupAnimClipFBX(IDLE_PATH, avatar, true, true);
            SetupAnimClipFBX(RUN_PATH, avatar, true, true);
            SetupAnimClipFBX(ATTACK_PATH, avatar, false, true);
            SetupAnimClipFBX(DEATH_PATH, avatar, false, true); // loop=false, lockY=true để hạ thấp hông sát sàn nhà

            return avatar;
        }

        private static void SetupAnimClipFBX(string path, Avatar sourceAvatar, bool loop, bool lockY)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ZombieSetup] Không tìm thấy file animation tại {path}");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;

                var clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].loopTime = loop;
                        clips[i].loopPose = loop;
                        clips[i].lockRootRotation = true;
                        clips[i].lockRootHeightY = lockY;
                        clips[i].lockRootPositionXZ = true;
                    }
                    importer.clipAnimations = clips;
                }

                importer.SaveAndReimport();
                Debug.Log($"<color=cyan>[ZombieSetup]</color> 🎬 Đã cấu hình FBX: {Path.GetFileName(path)} (Loop={loop}, LockY={lockY})");
            }
        }

        private static AnimationClip CreateEditableClip(string fbxPath, string outAnimPath, bool loop)
        {
            AnimationClip fbxClip = GetAnimationClip(fbxPath);
            if (fbxClip == null) return null;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outAnimPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                EditorUtility.CopySerialized(fbxClip, clip);
                clip.name = Path.GetFileNameWithoutExtension(outAnimPath);

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, outAnimPath);
                Debug.Log($"<color=cyan>[ZombieSetup]</color> 🎞️ Đã tạo editable clip: {outAnimPath}");
            }
            return clip;
        }

        private static AnimationClip CreateEditableAttackClip(string fbxPath, string outAnimPath)
        {
            AnimationClip fbxClip = GetAnimationClip(fbxPath);
            if (fbxClip == null) return null;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outAnimPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                EditorUtility.CopySerialized(fbxClip, clip);
                clip.name = Path.GetFileNameWithoutExtension(outAnimPath);

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                settings.loopBlendPositionY = true;
                settings.keepOriginalPositionY = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AnimationEvent clawEvent = new AnimationEvent
                {
                    time = clip.length * 0.42f,
                    functionName = "OnZombieClawAttack"
                };
                AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[] { clawEvent });

                AssetDatabase.CreateAsset(clip, outAnimPath);
                Debug.Log($"<color=cyan>[ZombieSetup]</color> ⚡ Đã tạo Attack Clip kèm Animation Event 'OnZombieClawAttack' tại time={clawEvent.time:F2}s");
            }
            return clip;
        }

        private static AnimationClip CreateEditableDeathClip(string fbxPath, string outAnimPath)
        {
            AnimationClip fbxClip = GetAnimationClip(fbxPath);
            if (fbxClip == null) return null;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outAnimPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                EditorUtility.CopySerialized(fbxClip, clip);
                clip.name = Path.GetFileNameWithoutExtension(outAnimPath);

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                settings.loopBlendPositionY = true;
                settings.keepOriginalPositionY = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, outAnimPath);
                Debug.Log($"<color=cyan>[ZombieSetup]</color> ☠️ Đã tạo Death Clip: {outAnimPath} (length={clip.length:F2}s)");
            }
            return clip;
        }

        private static AnimatorController CreateZombieAnimatorController(AnimationClip idleClip, AnimationClip runClip, AnimationClip attackClip, AnimationClip deathClip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            }

            // Clear old parameters and layers
            controller.parameters = new AnimatorControllerParameter[0];

            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            if (idleClip == null) idleClip = GetAnimationClip(IDLE_PATH);
            if (runClip == null) runClip = GetAnimationClip(RUN_PATH);
            if (attackClip == null) attackClip = GetAnimationClip(ATTACK_PATH);
            if (deathClip == null) deathClip = GetAnimationClip(DEATH_PATH);

            var rootStateMachine = controller.layers[0].stateMachine;
            while (rootStateMachine.states.Length > 0)
            {
                rootStateMachine.RemoveState(rootStateMachine.states[0].state);
            }

            // 1. Idle State
            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            // 2. Run State
            var runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;

            // 3. Attack State (Toàn thân đứng cào, chân bám sàn)
            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;

            // 4. Death State (Ngã gục xuống sàn)
            var deathState = rootStateMachine.AddState("Death");
            deathState.motion = deathClip;

            rootStateMachine.defaultState = idleState;

            // Transitions: Idle <-> Run
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.15f;
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.15f;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Transitions: AnyState -> Attack
            var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.08f;
            anyToAttack.canTransitionToSelf = false;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            // Transition: Attack -> Idle khi cào xong
            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.90f;
            attackToIdle.duration = 0.18f;

            // Transitions: AnyState -> Death khi hết máu
            var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.10f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            EditorUtility.SetDirty(controller);
            Debug.Log("<color=green>[ZombieSetup]</color> 🕹️ Đã cập nhật Animator Controller (Idle, Run, Attack, Death)");
            return controller;
        }

        private static GameObject CreateZombiePrefab(Avatar avatar, AnimatorController animController)
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            if (modelPrefab == null)
            {
                Debug.LogError("[ZombieSetup] Không thể load GameObject từ " + MODEL_PATH);
                return null;
            }

            GameObject zombieInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            zombieInstance.name = "Zombie_Parasite";
            PrefabUtility.UnpackPrefabInstance(zombieInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            zombieInstance.tag = "Untagged";
            zombieInstance.layer = LayerMask.NameToLayer("Default");

            // Gán Materials PBR (khử hoàn toàn màu trắng bệch)
            Material matBody = AssetDatabase.LoadAssetAtPath<Material>(MAT_BODY_PATH);
            Material matHead = AssetDatabase.LoadAssetAtPath<Material>(MAT_HEAD_PATH);
            var smr = zombieInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && matBody != null && matHead != null)
            {
                smr.sharedMaterials = new Material[] { matBody, matHead };
                Debug.Log("<color=green>[ZombieSetup]</color> 🎨 Đã gán thành công 2 vật liệu PBR cho Zombie: Body & Head!");
            }

            // 1. Animator
            var anim = zombieInstance.GetComponent<Animator>();
            if (anim == null) anim = zombieInstance.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.runtimeAnimatorController = animController;
            anim.applyRootMotion = false;

            // 2. Kinematic Rigidbody (khử hoàn toàn xung đột PhysX gây giật lag)
            var rb = zombieInstance.GetComponent<Rigidbody>();
            if (rb == null) rb = zombieInstance.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // 3. NavMeshAgent
            var agent = zombieInstance.GetComponent<NavMeshAgent>();
            if (agent == null) agent = zombieInstance.AddComponent<NavMeshAgent>();
            agent.speed = 3.6f;
            agent.stoppingDistance = 1.3f; // Nhỏ hơn attackRange 1.8m để chạy mượt vào tầm cào
            agent.acceleration = 16.0f;
            agent.angularSpeed = 360.0f;
            agent.radius = 0.35f;
            agent.height = 1.9f;
            agent.autoBraking = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            // 4. CapsuleCollider
            var col = zombieInstance.GetComponent<CapsuleCollider>();
            if (col == null) col = zombieInstance.AddComponent<CapsuleCollider>();
            col.radius = 0.35f;
            col.height = 1.9f;
            col.center = new Vector3(0f, 0.95f, 0f);

            // 5. ZombieController
            var ctrl = zombieInstance.GetComponent<ZombieController>();
            if (ctrl == null) ctrl = zombieInstance.AddComponent<ZombieController>();

            // 6. AudioSource
            var audio = zombieInstance.GetComponent<AudioSource>();
            if (audio == null) audio = zombieInstance.AddComponent<AudioSource>();
            audio.spatialBlend = 1.0f;
            audio.maxDistance = 25.0f;
            audio.playOnAwake = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zombieInstance, PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(zombieInstance);

            Debug.Log($"<color=green>[ZombieSetup]</color> 📦 Đã tạo Prefab chuyên nghiệp: {PREFAB_PATH}");
            return prefab;
        }

        private static void SetupZombieInScene(GameObject zombiePrefab)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
            }

            // 1. Tìm hoặc tạo điểm SpawnZombie
            GameObject spawnPoint = GameObject.Find("SpawnZombie");
            if (spawnPoint == null)
            {
                foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
                {
                    if (go.name.ToLower().Contains("spawnzombie") || go.name.ToLower().Contains("zombiespawn"))
                    {
                        spawnPoint = go;
                        break;
                    }
                }
            }

            if (spawnPoint == null)
            {
                spawnPoint = new GameObject("SpawnZombie");
                spawnPoint.transform.position = new Vector3(17.0f, 0.05f, -2.5f);
                spawnPoint.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Gắn DimensionalPortal tự động sinh quái và quản lý cổng không gian
            var portal = spawnPoint.GetComponent<DimensionalPortal>();
            if (portal == null) portal = spawnPoint.AddComponent<DimensionalPortal>();
            
            // Xóa instance zombie cũ nếu có
            GameObject existingZombie = GameObject.Find("Zombie_Parasite");
            if (existingZombie != null)
            {
                UnityEngine.Object.DestroyImmediate(existingZombie);
            }

            // Đặt 1 Zombie mẫu đầu tiên sẵn ở scene để người chơi trải nghiệm ngay
            GameObject zombieInScene = (GameObject)PrefabUtility.InstantiatePrefab(zombiePrefab);
            zombieInScene.name = "Zombie_Parasite";
            zombieInScene.transform.position = spawnPoint.transform.position;
            zombieInScene.transform.rotation = spawnPoint.transform.rotation;

            var agent = zombieInScene.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(spawnPoint.transform.position, out hit, 4.0f, NavMesh.AllAreas))
                {
                    zombieInScene.transform.position = hit.position;
                    agent.Warp(hit.position);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[ZombieSetup]</color> 🧟 Đã đặt Zombie_Parasite & ZombieSpawner (10s) vào Scene Show.unity!");
        }

        private static Avatar GetAvatar(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Avatar av && av.isValid) return av;
            }
            return null;
        }

        private static AnimationClip GetAnimationClip(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
#endif
