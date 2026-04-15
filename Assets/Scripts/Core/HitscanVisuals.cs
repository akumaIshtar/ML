using UnityEngine;

namespace Core
{
    public class HitscanVisuals : MonoBehaviour
    {
        [Header("Tracer (弹道/残影) 设置")]
        [Tooltip("用于 LineRenderer 的高亮/发光材质。如果不填，会默认使用黄色无光照材质。")]
        public Material tracerMaterial;
        public float tracerWidth = 0.005f;
        public float tracerDuration = 0.05f;

        [Header("Decal (弹孔) 设置")]
        [Tooltip("弹孔材质（贴图）。如果不填，会使用黑色方块代替。")]
        public Material decalMaterial;
        public float decalSize = 0.1f;
        public float decalLifetime = 5f;

        public void PlayHitscanVisuals(Vector3 barrelPos, Vector3 hitPos, Vector3 hitNormal, bool didHit)
        {
            SpawnTracer(barrelPos, hitPos);

            if (didHit)
            {
                SpawnDecal(hitPos, hitNormal);
            }
        }

        private void SpawnTracer(Vector3 start, Vector3 end)
        {
            GameObject tracerObj = new GameObject("Tracer");
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();

            if (tracerMaterial != null)
            {
                lr.material = tracerMaterial;
            }
            else
            {
                // 默认程序化黄色射线材料
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = new Color(1f, 0.8f, 0.2f, 1f);
                lr.endColor = new Color(1f, 0.8f, 0.2f, 0f);
            }

            lr.startWidth = tracerWidth;
            lr.endWidth = tracerWidth;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            // 程序化淡出与自毁可以考虑配合协程或者直接 Destroy
            Destroy(tracerObj, tracerDuration);
        }

        private void SpawnDecal(Vector3 pos, Vector3 normal)
        {
            GameObject decalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decalObj.name = "BulletDecal";

            // 移除碰撞体
            Destroy(decalObj.GetComponent<Collider>());

            // 将弹孔放置在命中点，并稍微偏移避免与墙面 Z-fighting
            decalObj.transform.position = pos + normal * 0.005f;
            // 使 Quad 贴合墙面法线
            decalObj.transform.rotation = Quaternion.LookRotation(normal);
            decalObj.transform.localScale = new Vector3(decalSize, decalSize, decalSize);

            MeshRenderer mr = decalObj.GetComponent<MeshRenderer>();
            if (decalMaterial != null)
            {
                mr.material = decalMaterial;
            }
            else
            {
                // 默认制作一个黑色的弹孔
                mr.material = new Material(Shader.Find("Standard"));
                mr.material.color = Color.black;
            }

            Destroy(decalObj, decalLifetime);
        }
    }
}
