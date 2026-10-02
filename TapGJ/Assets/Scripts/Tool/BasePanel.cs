using UnityEngine;
using UnityEngine.Events;

/// <summary>可选的场景面板基类，提供一次初始化和淡入淡出。</summary>
public abstract class BasePanel : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private const float AlphaSpeed = 10f;
    public bool isShow;
    private bool initialized;
    private UnityAction hideCallBack;

    protected virtual void Awake()
    {
        EnsureCanvasGroup();
        // 场景里默认开启的面板保持可见。
        isShow = true;
    }

    protected virtual void Start() => EnsureInitialized();

    /// <summary>注册按钮事件等，基类保证每个实例只调用一次。</summary>
    public abstract void Init();

    public virtual void ShowMe()
    {
        EnsureCanvasGroup();
        EnsureInitialized();
        // 取消尚未结束的隐藏，避免重新打开后又被旧回调关闭。
        hideCallBack = null;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        isShow = true;
    }

    public virtual void HideMe(UnityAction callBack)
    {
        EnsureCanvasGroup();
        isShow = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        hideCallBack = callBack;
        if (!isActiveAndEnabled || canvasGroup.alpha <= 0f)
            CompleteHide();
    }

    protected virtual void Update()
    {
        if (canvasGroup == null) return;
        float targetAlpha = isShow ? 1f : 0f;
        // 暂停菜单在 Time.timeScale = 0 时也能完成显示和隐藏。
        // 淡入淡出完成后不再写入 alpha，避免静止 UI 持续标记重绘。
        if (canvasGroup.alpha != targetAlpha)
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha,
                AlphaSpeed * Time.unscaledDeltaTime);
        if (!isShow && canvasGroup.alpha <= 0f && hideCallBack != null)
            CompleteHide();
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        Init();
    }

    private void CompleteHide()
    {
        UnityAction callback = hideCallBack;
        hideCallBack = null;
        callback?.Invoke();
    }
}
