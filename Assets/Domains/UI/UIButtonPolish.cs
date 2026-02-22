using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButtonPolish : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector3 originalScale;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.9f;
    [SerializeField] private float duration = 0.2f;

    private Button button;

    private void Awake()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        transform.DOScale(originalScale * hoverScale, duration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, duration).SetEase(Ease.InQuad);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        transform.DOPunchScale(Vector3.one * -0.1f, 0.2f);
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
}
