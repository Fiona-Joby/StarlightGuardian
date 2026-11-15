using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Displays the menu arrow while its button is hovered or selected.</summary>
public sealed class MenuButtonSelectionMarker : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private const float ArrowRectWidth = 200f;
    private const float ArrowRectHeight = 112.5f;
    private const float GapFromButton = 28f;

    private RectTransform buttonRect;
    private RectTransform arrowRect;
    private Image arrowImage;
    private bool pointerOver;
    private bool selected;
    private float additionalLeftOffset;

    public void Initialize(Sprite sprite, float additionalLeftOffset = 0f)
    {
        if (sprite == null) return;

        this.additionalLeftOffset = additionalLeftOffset;
        buttonRect = transform as RectTransform;
        if (buttonRect == null) return;

        if (arrowImage == null)
        {
            GameObject arrow = new GameObject("Selection Arrow", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image));
            arrow.transform.SetParent(transform, false);
            arrowRect = (RectTransform)arrow.transform;
            Canvas arrowCanvas = arrow.GetComponent<Canvas>();
            arrowCanvas.overrideSorting = true;
            arrowCanvas.sortingOrder = 100;
            arrowImage = arrow.GetComponent<Image>();
            arrowImage.raycastTarget = false;
            arrowImage.preserveAspect = true;
        }

        arrowImage.sprite = sprite;
        arrowImage.color = Color.white;
        arrowImage.enabled = true;
        LayoutArrow();
        RefreshVisibility();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        RefreshVisibility();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        RefreshVisibility();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        RefreshVisibility();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        RefreshVisibility();
    }

    private void OnDisable()
    {
        pointerOver = false;
        selected = false;
        RefreshVisibility();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (arrowRect != null) LayoutArrow();
    }

    private void LayoutArrow()
    {
        if (arrowRect == null || buttonRect == null) return;

        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = new Vector2(ArrowRectWidth, ArrowRectHeight);
        Vector3 buttonScale = buttonRect.localScale;
        arrowRect.localScale = new Vector3(
            buttonScale.x == 0f ? 1f : 1f / buttonScale.x,
            buttonScale.y == 0f ? 1f : 1f / buttonScale.y,
            buttonScale.z == 0f ? 1f : 1f / buttonScale.z);
        float offsetScale = buttonScale.x == 0f ? 1f : buttonScale.x;
        arrowRect.anchoredPosition = new Vector2(
            -buttonRect.rect.width * 0.5f - GapFromButton - additionalLeftOffset / offsetScale,
            0f);
        arrowRect.SetAsLastSibling();
    }

    private void RefreshVisibility()
    {
        if (arrowImage != null) arrowImage.enabled = pointerOver || selected;
    }
}
