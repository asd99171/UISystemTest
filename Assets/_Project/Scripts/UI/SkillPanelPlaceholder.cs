using UnityEngine;

namespace RPGSystem.UI
{
    /// <summary>
    /// 스킬 패널 플레이스홀더.
    /// panelName을 "Skill"로 설정.
    /// </summary>
    public class SkillPanelPlaceholder : UIPanel
    {
        protected override void OnOpened()
        {
            Debug.Log("[SkillPanel] Opened — displaying skill list");
        }

        protected override void OnClosed()
        {
            Debug.Log("[SkillPanel] Closed");
        }
    }
}
