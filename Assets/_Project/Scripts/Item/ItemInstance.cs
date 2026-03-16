using System;
using RPGSystem.Item.Data;

namespace RPGSystem.Item
{
    /// <summary>
    /// 런타임 아이템 인스턴스.
    /// ScriptableObject(ItemData)는 에셋이므로 불변이고,
    /// 이 클래스가 실제 게임 내 아이템 하나를 표현한다.
    /// 수량, 고유ID 등 가변 상태를 가진다.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        /// <summary>런타임 고유 ID (저장/불러오기, 참조 추적용)</summary>
        public string uid;

        /// <summary>원본 아이템 데이터 (ScriptableObject 참조)</summary>
        public ItemData data;

        /// <summary>현재 수량 (스택 불가 아이템은 항상 1)</summary>
        public int amount;

        public ItemInstance(ItemData data, int amount = 1)
        {
            this.uid = Guid.NewGuid().ToString();
            this.data = data;
            this.amount = data.isStackable ? amount : 1;
        }

        /// <summary>스택에 추가 가능한 수량 반환</summary>
        public int StackableAmount => data.maxStack - amount;

        /// <summary>스택이 꽉 찼는지</summary>
        public bool IsStackFull => amount >= data.maxStack;

        /// <summary>ItemInstance 복제 (수량 분리 시 사용)</summary>
        public ItemInstance Clone(int newAmount)
        {
            return new ItemInstance(data, newAmount);
        }
    }
}
