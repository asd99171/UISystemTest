using UnityEngine;

namespace RPGSystem.UI
{
    /// <summary>
    /// 장비 패널 플레이스홀더.
    /// panelName을 "Equipment"로 설정.
    /// </summary>
    public class EquipmentPanelPlaceholder : UIPanel
    {
        protected override void OnOpened()
        {
            Debug.Log("[EquipmentPanel] Opened — displaying equipment slots");
        }

        protected override void OnClosed()
        {
            Debug.Log("[EquipmentPanel] Closed");
        }
    }
}
