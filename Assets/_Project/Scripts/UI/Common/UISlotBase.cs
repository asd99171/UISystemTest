using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace RPGSystem.UI
{
    /// <summary>
    /// UI 슬롯 베이스 클래스.
    /// 모든 슬롯 UI(인벤토리/장비/스킬)가 이 클래스를 상속한다.
    ///
    /// 기능:
    /// - 아이콘 + 수량 텍스트 표시
    /// - 선택 상태 하이라이트
    /// - 클릭(좌/우) + 더블클릭 감지 (IPointerClickHandler)
    ///
    /// [GameObject] 슬롯 Prefab에 부착. Image(아이콘), TMP_Text(수량), Image(선택 프레임) 자식 필요.
    /// </summary>
    public abstract class UISlotBase : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("슬롯 UI 요소")]
        [Tooltip("아이템/스킬 아이콘 이미지")]
        [SerializeField] protected Image iconImage;

        [Tooltip("수량 텍스트 (스택 수, 레벨 등)")]
        [SerializeField] protected TMP_Text quantityText;

        [Tooltip("선택 상태 하이라이트 프레임")]
        [SerializeField] protected GameObject selectionFrame;

        [Tooltip("빈 슬롯 배경 이미지 (아이콘 숨길 때 표시)")]
        [SerializeField] protected Image emptyBackground;

        /// <summary>이 슬롯의 인덱스</summary>
        public int SlotIndex { get; private set; }

        /// <summary>현재 선택 상태</summary>
        public bool IsSelected { get; private set; }

        /// <summary>슬롯이 비어있는지</summary>
        public bool IsEmpty { get; protected set; } = true;

        /// <summary>더블클릭 감지 간격</summary>
        private const float DOUBLE_CLICK_TIME = 0.3f;
        private float _lastClickTime;

        // ──────────────────────────────────────
        // 초기화
        // ──────────────────────────────────────

        /// <summary>슬롯 인덱스 설정 (Controller가 초기화 시 호출)</summary>
        public void Initialize(int index)
        {
            SlotIndex = index;
            SetSelected(false);
            Clear();
        }

        // ──────────────────────────────────────
        // 표시 업데이트
        // ──────────────────────────────────────

        /// <summary>아이콘 설정</summary>
        protected void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;

            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.color = Color.white;
                iconImage.enabled = true;
                if (emptyBackground != null)
                    emptyBackground.enabled = false;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = Color.clear;
                if (emptyBackground != null)
                    emptyBackground.enabled = true;
            }
        }

        /// <summary>수량 텍스트 설정</summary>
        protected void SetQuantity(string text)
        {
            if (quantityText == null) return;
            quantityText.text = text;
            quantityText.enabled = !string.IsNullOrEmpty(text);
        }

        /// <summary>슬롯 비우기</summary>
        public virtual void Clear()
        {
            IsEmpty = true;
            SetIcon(null);
            SetQuantity(null);
        }

        // ──────────────────────────────────────
        // 선택 상태
        // ──────────────────────────────────────

        /// <summary>선택 상태 설정</summary>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selectionFrame != null)
                selectionFrame.SetActive(selected);
        }

        // ──────────────────────────────────────
        // 포인터 이벤트
        // ──────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float time = Time.unscaledTime;
                if (time - _lastClickTime < DOUBLE_CLICK_TIME)
                {
                    OnDoubleClick();
                    _lastClickTime = 0f;
                }
                else
                {
                    OnLeftClick();
                    _lastClickTime = time;
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit();
        }

        // ──────────────────────────────────────
        // 서브클래스 오버라이드용
        // ──────────────────────────────────────

        /// <summary>좌클릭 (선택)</summary>
        protected virtual void OnLeftClick() { }

        /// <summary>우클릭 (사용/장착/해제)</summary>
        protected virtual void OnRightClick() { }

        /// <summary>더블클릭 (사용/장착)</summary>
        protected virtual void OnDoubleClick() { }

        /// <summary>마우스 진입 (툴팁 표시)</summary>
        protected virtual void OnHoverEnter() { }

        /// <summary>마우스 이탈 (툴팁 숨김)</summary>
        protected virtual void OnHoverExit() { }
    }
}
