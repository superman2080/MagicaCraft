using UnityEngine;
using UnityEngine.UI;

public abstract class SubUIManager : MonoBehaviour
{
    public abstract int SortingOrder { get; }

    public Canvas Canvas { get; private set; }
    public bool IsVisible => gameObject.activeSelf;

    protected virtual void Awake()
    {
        Canvas = GetComponent<Canvas>();
        Canvas.sortingOrder = SortingOrder;
    }

    protected virtual void Start()
    {
        UIManager.Instance.Register(this);
    }

    public virtual void Show() => gameObject.SetActive(true);
    public virtual void Hide() => gameObject.SetActive(false);
}
