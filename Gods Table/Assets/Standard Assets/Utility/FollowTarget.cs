using System;
using UnityEngine;


namespace UnityStandardAssets.Utility
{
    public class FollowTarget : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 7.5f, 0f);

        public bool freezeX;
        public bool freezeY;
        public bool freezeZ;

        private void LateUpdate()
        {
            Vector3 next = target.position + offset;
            if (freezeX) next.x = transform.position.x;
            if (freezeY) next.y = transform.position.y;
            if (freezeZ) next.z = transform.position.z;
            transform.position = next;
        }
    }
}
