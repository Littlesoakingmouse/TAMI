using UnityEngine;

namespace Tami
{
    /// <summary>Hoạt ảnh nhẹ cho nhân viên quầy: đầu nhún như đang thở, vẫy tay khi chào hàng.</summary>
    public class StaffIdle : MonoBehaviour
    {
        Transform head, wavingArm;
        Vector3 headBase;
        float t, waveUntil;

        void Awake()
        {
            head = transform.Find("head");
            wavingArm = transform.Find("arm_right");
            if (head != null) headBase = head.localPosition;
            t = Random.value * 10f;
        }

        /// <summary>Vẫy tay trong vài giây (khi đang mời khách).</summary>
        public void Wave(float seconds) => waveUntil = Time.time + seconds;

        void Update()
        {
            t += Time.deltaTime;
            bool waving = Time.time < waveUntil;
            if (head != null) head.localPosition = headBase + new Vector3(0f, Mathf.Sin(t * (waving ? 9f : 2.6f)) * (waving ? 0.03f : 0.015f), 0f);
            if (wavingArm != null) wavingArm.localRotation = Quaternion.Euler(0f, 0f, waving ? 70f + Mathf.Sin(t * 12f) * 25f : 0f);
        }
    }
}
