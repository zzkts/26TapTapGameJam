using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>显示、隐藏当前场景中已有的 UI，不加载或销毁 Prefab。</summary>
public sealed class UIManager : Singleton<UIManager>
{

    private readonly Dictionary<Type, BasePanel> panels = new Dictionary<Type, BasePanel>();
    private readonly Dictionary<string, GameObject> namedPanels = new Dictionary<string, GameObject>();
    private readonly Dictionary<GameObject, PanelEntry> managedObjects = new Dictionary<GameObject, PanelEntry>();
    private int sceneHandle = -1;

    // Singleton<T> 的 new() 约束要求 public 无参构造函数，正常使用请访问 Instance。
    public UIManager() { }

    private sealed class PanelEntry
    {
        public readonly BasePanel Panel;
        public readonly UnityAction Disable;

        public PanelEntry(GameObject target)
        {
            Panel = target.GetComponent<BasePanel>();
            // 每个面板只创建一次回调，反复淡出时不再产生闭包。
            Disable = () => { if (target != null) target.SetActive(false); };
        }
    }

    public T ShowPanel<T>() where T : BasePanel
    {
        T panel = GetPanel<T>();
        SetVisible(panel != null ? panel.gameObject : null, true, false);
        return panel;
    }

    public GameObject ShowPanel(string panelName) => ShowPanel(GetPanelObject(panelName));

    public GameObject ShowPanel(GameObject panel)
    {
        SetVisible(panel, true, false);
        return panel;
    }

    public void HidePanel<T>(bool isFade = true) where T : BasePanel
    {
        T panel = GetPanel<T>();
        if (panel != null) SetVisible(panel.gameObject, false, isFade);
    }

    public void HidePanel(string panelName, bool isFade = false)
    {
        HidePanel(GetPanelObject(panelName), isFade);
    }

    public void HidePanel(GameObject panel, bool isFade = false)
    {
        SetVisible(panel, false, isFade);
    }

    /// <summary>查找当前场景中的面板，包括未激活的面板；同类型有多个时请传 GameObject。</summary>
    public T GetPanel<T>() where T : BasePanel
    {
        UpdateScene();
        Type type = typeof(T);
        if (panels.TryGetValue(type, out BasePanel cached) && cached != null)
            return cached as T;

        foreach (T panel in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (panel.gameObject.scene.handle != sceneHandle) continue;
            panels[type] = panel;
            GetEntry(panel.gameObject);
            return panel;
        }
        return null;
    }

    /// <summary>按 Hierarchy 名字查找，包括未激活的对象。名字应在当前场景中唯一。</summary>
    public GameObject GetPanelObject(string panelName)
    {
        UpdateScene();
        if (string.IsNullOrWhiteSpace(panelName)) return null;
        if (namedPanels.TryGetValue(panelName, out GameObject cached) && cached != null)
            return cached;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != panelName) continue;
            namedPanels[panelName] = child.gameObject;
            GetEntry(child.gameObject);
            return child.gameObject;
        }
        return null;
    }

    /// <summary>关闭已管理的场景 UI，保留对象；下次调用会重新查找。</summary>
    public void Clear()
    {
        UpdateScene();
        // OnDisable 可能调用管理器，用快照避免遍历期间集合改变。
        var snapshot = new List<GameObject>(managedObjects.Keys);
        foreach (GameObject panel in snapshot)
            if (panel != null) SetVisible(panel, false, false);
        panels.Clear();
        namedPanels.Clear();
        managedObjects.Clear();
    }

    private void SetVisible(GameObject panel, bool visible, bool isFade)
    {
        UpdateScene();
        if (panel == null)
        {
            Debug.LogWarning("UIManager：找不到场景 UI，请检查对象名字或是否已放入场景。");
            return;
        }
        PanelEntry entry = GetEntry(panel);
        BasePanel basePanel = entry.Panel;
        if (visible)
        {
            // 已经显示的面板不重复触发淡入，也不重复激活层级。
            if (panel.activeSelf && (basePanel == null || basePanel.isShow)) return;
            panel.SetActive(true);
            basePanel?.ShowMe();
        }
        else if (basePanel != null && isFade && basePanel.isActiveAndEnabled)
        {
            basePanel.HideMe(entry.Disable);
        }
        else
        {
            if (basePanel != null) basePanel.isShow = false;
            if (panel.activeSelf) panel.SetActive(false);
        }
    }

    private PanelEntry GetEntry(GameObject panel)
    {
        if (!managedObjects.TryGetValue(panel, out PanelEntry entry))
        {
            entry = new PanelEntry(panel);
            managedObjects.Add(panel, entry);
        }
        return entry;
    }

    private void UpdateScene()
    {
        int current = SceneManager.GetActiveScene().handle;
        if (current == sceneHandle) return;
        panels.Clear();
        namedPanels.Clear();
        managedObjects.Clear();
        sceneHandle = current;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        // 禁用 Domain Reload 时清掉上次运行的引用，不主动创建单例。
        if (!IsCreated) return;
        Instance.panels.Clear();
        Instance.namedPanels.Clear();
        Instance.managedObjects.Clear();
        Instance.sceneHandle = -1;
    }
}
