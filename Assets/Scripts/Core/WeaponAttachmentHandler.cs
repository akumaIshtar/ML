using UnityEngine;
namespace Core
{
    public class WeaponAttachmentHandler:MonoBehaviour
    {
        public Transform weaponJoint;

        public void AttachWeapon(GameObject weaponInstance)
        {
            if (weaponJoint == null) return;
            weaponInstance.transform.SetParent(weaponJoint);
            weaponInstance.transform.localRotation = Quaternion.identity;
            weaponInstance.transform.localPosition = Vector3.zero;
        }
    }
}
