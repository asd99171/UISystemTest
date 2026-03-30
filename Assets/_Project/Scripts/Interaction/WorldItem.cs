using UnityEngine;
using RPGSystem.Item.Data;

namespace RPGSystem.Interaction
{
    /// <summary>
    /// 월드에 배치된 아이템 오브젝트.
    /// 플레이어가 근처에서 상호작용하면 인벤토리에 추가된다.
    ///
    /// [GameObject] 아이템 3D 모델/스프라이트에 부착.
    /// [Required] Collider (Trigger) — 플레이어 감지용.
    /// [Inspector] itemData, amount 설정.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldItem : MonoBehaviour
    {
        [Header("아이템 설정")]
        [Tooltip("이 오브젝트가 나타내는 아이템")]
        [SerializeField] private ItemData itemData;

        [Tooltip("수량")]
        [SerializeField] private int amount = 1;

        [Header("시각 효과 (선택)")]
        [Tooltip("아이템이 위아래로 떠다니는 효과")]
        [SerializeField] private bool floatEffect = true;

        [Tooltip("떠다니는 높이")]
        [SerializeField] private float floatAmplitude = 0.2f;

        [Tooltip("떠다니는 속도")]
        [SerializeField] private float floatSpeed = 2f;

        [Tooltip("회전 속도 (도/초)")]
        [SerializeField] private float rotateSpeed = 90f;

        private Vector3 _startPosition;

        /// <summary>이 월드 아이템의 데이터</summary>
        public ItemData ItemData => itemData;

        /// <summary>수량</summary>
        public int Amount => amount;

        private void Start()
        {
            _startPosition = transform.position;

            // Collider가 Trigger인지 확인
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[WorldItem] '{gameObject.name}' Collider를 isTrigger로 설정하세요.");
            }
        }

        private void Update()
        {
            if (!floatEffect) return;

            // 위아래 떠다니기
            var pos = _startPosition;
            pos.y += Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = pos;

            // 회전
            if (rotateSpeed > 0)
                transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// 아이템을 줍기. 성공 시 오브젝트를 제거하고 true 반환.
        /// </summary>
        public bool Pickup()
        {
            var inventory = Inventory.InventoryManager.Instance;
            if (inventory == null)
            {
                Debug.LogWarning("[WorldItem] InventoryManager가 없습니다.");
                return false;
            }

            int added = inventory.AddItem(itemData, amount);
            if (added <= 0)
            {
                Debug.Log($"[WorldItem] 인벤토리가 가득 차서 '{itemData.itemName}'을 주울 수 없습니다.");
                return false;
            }

            Debug.Log($"[WorldItem] 획득: {itemData.itemName} x{added}");

            Core.EventBus.Publish(new Core.ItemPickedUpEvent
            {
                itemData = itemData,
                amount = added
            });

            Destroy(gameObject);
            return true;
        }
    }
}
