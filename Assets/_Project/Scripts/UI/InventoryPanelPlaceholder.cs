using UnityEngine;

namespace RPGSystem.UI
{
    /// <summary>
    /// 인벤토리 패널 플레이스홀더.
    /// 실제 UI 구현 전에 UIManager + GameState 전환을 테스트하기 위한 더미 패널.
    /// Canvas 아래 Panel GameObject에 부착한다.
    ///
    /// [설정]
    /// 1. Canvas 아래에 Panel (Image + Text "Inventory") 생성
    /// 2. 이 스크립트 부착
    /// 3. panelName을 "Inventory"로 설정
    /// 4. GameObject를 비활성 상태로 시작
    /// 5. UIManager의 panelRegistry에 등록
    /// </summary>
    public class InventoryPanelPlaceholder : UIPanel
    {
        protected override void OnOpened()
        {
            Debug.Log("[InventoryPanel] Opened — displaying inventory grid");
        }

        protected override void OnClosed()
        {
            Debug.Log("[InventoryPanel] Closed");
        }
    }
}
