using UnityEngine;

namespace RPGSystem.UI
{
    /// <summary>
    /// UI 패널 베이스 클래스.
    /// 모든 UI 패널(인벤토리, 장비, 스킬 등)이 이 클래스를 상속한다.
    /// UIManager가 이 클래스를 통해 패널을 열고 닫는다.
    ///
    /// [사용법]
    /// 1. 이 클래스를 상속받은 패널 스크립트를 만든다.
    /// 2. Canvas 아래 패널 UI GameObject에 부착한다.
    /// 3. UIManager.panelRegistry에 등록한다.
    /// 4. 패널 GameObject는 비활성 상태로 시작한다 (SetActive(false)).
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("패널 설정")]
        [Tooltip("이 패널의 고유 이름 (UIManager에서 식별용)")]
        [SerializeField] private string panelName;

        /// <summary>패널 고유 이름</summary>
        public string PanelName => panelName;

        /// <summary>패널이 현재 열려있는지</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 패널 열기. UIManager가 호출한다.
        /// 서브클래스에서 override하여 데이터 갱신 등을 수행할 수 있다.
        /// </summary>
        public virtual void Open()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            OnOpened();
        }

        /// <summary>
        /// 패널 닫기. UIManager가 호출한다.
        /// </summary>
        public virtual void Close()
        {
            IsOpen = false;
            OnClosed();
            gameObject.SetActive(false);
        }

        /// <summary>패널이 열린 직후 호출 (서브클래스 override용)</summary>
        protected virtual void OnOpened() { }

        /// <summary>패널이 닫히기 직전 호출 (서브클래스 override용)</summary>
        protected virtual void OnClosed() { }
    }
}
