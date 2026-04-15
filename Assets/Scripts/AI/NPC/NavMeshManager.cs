using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

namespace AI.NPC
{
    /// <summary>
    /// 导航管理器：负责宏观地形变动时的异步导航网格更新。
    /// 遵循单例模式，支持多 NavMeshSurface 的并发/顺序更新。
    /// </summary>
    public class NavMeshManager : MonoBehaviour
    {
        public static NavMeshManager Instance { get; private set; }

        [Header("配置")]
        [Tooltip("是否在启动时自动寻找场景中所有的 NavMeshSurface")]
        public bool autoScanOnAwake = true;

        [Tooltip("需要管理的导航表面列表")]
        public List<NavMeshSurface> surfaces = new List<NavMeshSurface>();

        private bool _isUpdating = false;

        private void Awake()
        {
            // 单例初始化
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); // 根据项目需求决定是否跨场景
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (autoScanOnAwake)
            {
                ScanSurfaces();
            }
        }

        /// <summary>
        /// 扫描并注册场景中所有的 NavMeshSurface
        /// </summary>
        public void ScanSurfaces()
        {
            NavMeshSurface[] foundSurfaces = FindObjectsOfType<NavMeshSurface>();
            foreach (var s in foundSurfaces)
            {
                if (!surfaces.Contains(s))
                {
                    surfaces.Add(s);
                }
            }
            Debug.Log($"[NavMeshManager] 已自动扫描并注册 {surfaces.Count} 个导航表面。");
        }

        /// <summary>
        /// 请求异步重烘焙所有注册的表面
        /// </summary>
        public void RequestAsyncUpdate()
        {
            if (_isUpdating)
            {
                Debug.LogWarning("[NavMeshManager] 正在进行更新，请勿重复触发。");
                return;
            }
            StartCoroutine(UpdateAllSurfacesRoutine());
        }

        private IEnumerator UpdateAllSurfacesRoutine()
        {
            _isUpdating = true;
            float startTime = Time.realtimeSinceStartup;

            foreach (var surface in surfaces)
            {
                if (surface == null || surface.navMeshData == null) continue;

                // 使用异步方式更新，避免主线程卡死
                // 注意：UpdateNavMesh 是针对已有数据的增量更新，效率极高
                var op = surface.UpdateNavMesh(surface.navMeshData);

                // 等待当前表面更新完成
                while (!op.isDone)
                {
                    yield return null;
                }
            }

            float duration = (Time.realtimeSinceStartup - startTime) * 1000f;
            Debug.Log($"[NavMeshManager] 所有导航表面异步更新完成，总耗时: {duration:F2} ms");
            _isUpdating = false;
        }
    }
}
