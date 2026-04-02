using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WeaponCameraSync : MonoBehaviour
{
    public Camera mainCamera; // 拖入你的 Main Camera
    private Camera _weaponCamera;

    private void Awake()
    {
        _weaponCamera = GetComponent<Camera>();

        // 如果没有手动拖拽，自动去父节点寻找 Main Camera
        if (mainCamera == null && transform.parent != null)
        {
            mainCamera = transform.parent.GetComponent<Camera>();
        }
    }

    private void LateUpdate()
    {
        if (mainCamera != null && _weaponCamera != null)
        {
            // 完美同步开镜时的视野放大缩小
            _weaponCamera.fieldOfView = mainCamera.fieldOfView;
        }
    }
}
