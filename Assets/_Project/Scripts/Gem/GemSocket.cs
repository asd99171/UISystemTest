using System;
using RPGSystem.Item.Data;

namespace RPGSystem.Gem
{
    /// <summary>
    /// 잼 소켓 하나를 표현하는 데이터.
    /// 스킬에 포함되어 잼 장착 상태를 추적한다.
    /// </summary>
    [Serializable]
    public class GemSocket
    {
        /// <summary>소켓 인덱스 (스킬 내 위치)</summary>
        public int index;

        /// <summary>소켓이 해금되었는지 (숙련도 레벨에 의해 결정)</summary>
        public bool isUnlocked;

        /// <summary>장착된 잼 데이터 (비어있으면 null)</summary>
        public GemData attachedGem;

        public GemSocket(int index)
        {
            this.index = index;
            isUnlocked = false;
            attachedGem = null;
        }

        /// <summary>소켓이 비어있는지 (해금 + 잼 미장착)</summary>
        public bool IsEmpty => isUnlocked && attachedGem == null;

        /// <summary>잼이 장착되어 있는지</summary>
        public bool HasGem => attachedGem != null;

        /// <summary>잼 장착</summary>
        public void AttachGem(GemData gem)
        {
            attachedGem = gem;
        }

        /// <summary>잼 해제. 해제된 잼 데이터 반환 (인벤토리 반환용)</summary>
        public GemData DetachGem()
        {
            var gem = attachedGem;
            attachedGem = null;
            return gem;
        }
    }
}
