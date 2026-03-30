using System.Collections.Generic;
using UnityEngine;

namespace RPGSystem.Interaction
{
    /// <summary>
    /// 플레이어에 부착하는 아이템 줍기 시스템.
    /// 범위 안의 WorldItem을 감지하고, 상호작용 키를 누르면 가장 가까운 아이템을 줍는다.
    ///
    /// [GameObject] 플레이어 오브젝트에 부착.
    /// [Required] 플레이어에 Collider + Rigidbody 필요 (OnTrigger 감지용).
    /// [Inspector] pickupKey, pickupRange 설정.
    /// </summary>
    public class ItemPickupSystem : MonoBehaviour
    {
        [Header("줍기 설정")]
        [Tooltip("상호작용 키")]
        [SerializeField] private KeyCode pickupKey = KeyCode.F;

        [Tooltip("줍기 범위 (SphereCollider 반지름과 맞추기)")]
        [SerializeField] private float pickupRange = 3f;

        [Header("UI 안내 (선택)")]
        [Tooltip("줍기 가능할 때 표시할 안내 UI")]
        [SerializeField] private GameObject pickupPromptUI;

        [Tooltip("안내 텍스트 (TMPro)")]
        [SerializeField] private TMPro.TMP_Text promptText;

        /// <summary>현재 범위 안에 있는 WorldItem 목록</summary>
        private readonly List<WorldItem> _nearbyItems = new List<WorldItem>();

        /// <summary>현재 가장 가까운 아이템</summary>
        public WorldItem ClosestItem { get; private set; }

        private void Start()
        {
            if (pickupPromptUI != null)
                pickupPromptUI.SetActive(false);

            // 자동으로 SphereCollider 트리거 추가 (없으면)
            var trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = pickupRange;
            }
        }

        private void Update()
        {
            // UI 모드에서는 줍기 비활성
            if (Core.GameManager.Instance != null && !Core.GameManager.Instance.IsInputAllowed)
                return;

            UpdateClosestItem();
            UpdatePromptUI();

            if (ClosestItem != null && Input.GetKeyDown(pickupKey))
            {
                ClosestItem.Pickup();
                _nearbyItems.Remove(ClosestItem);
                ClosestItem = null;
            }
        }

        private void UpdateClosestItem()
        {
            // 제거된 오브젝트 정리
            _nearbyItems.RemoveAll(item => item == null);

            if (_nearbyItems.Count == 0)
            {
                ClosestItem = null;
                return;
            }

            // 가장 가까운 아이템 찾기
            float minDist = float.MaxValue;
            WorldItem closest = null;
            var myPos = transform.position;

            foreach (var item in _nearbyItems)
            {
                float dist = Vector3.Distance(myPos, item.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = item;
                }
            }

            ClosestItem = closest;
        }

        private void UpdatePromptUI()
        {
            if (pickupPromptUI == null) return;

            bool show = ClosestItem != null;
            pickupPromptUI.SetActive(show);

            if (show && promptText != null && ClosestItem.ItemData != null)
            {
                promptText.text = $"[{pickupKey}] {ClosestItem.ItemData.itemName} 줍기";
            }
        }

        // ──────────────────────────────────────
        // Trigger 감지
        // ──────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            var worldItem = other.GetComponent<WorldItem>();
            if (worldItem != null && !_nearbyItems.Contains(worldItem))
            {
                _nearbyItems.Add(worldItem);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var worldItem = other.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                _nearbyItems.Remove(worldItem);
                if (ClosestItem == worldItem)
                    ClosestItem = null;
            }
        }

        // ──────────────────────────────────────
        // Gizmo (에디터에서 범위 시각화)
        // ──────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
}
