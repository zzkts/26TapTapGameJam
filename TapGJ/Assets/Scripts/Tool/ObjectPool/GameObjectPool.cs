using UnityEngine;
using System.Collections.Generic;

public class GameObjectPool
{
    private Transform parent;
    private GameObject prefab;

    private int maxCapacity = 50;
    private Stack<GameObject> pool = new Stack<GameObject>();

    public GameObjectPool(GameObject prefab, int perCreate = 0, int maxCapacity = 50)
        : this(prefab, new GameObject($"objectpool_{prefab.name}").transform, perCreate, maxCapacity) { }
    
    public GameObjectPool(GameObject prefab, Transform parent, int perCreate = 0, int maxCapacity = 50)
    {
        this.prefab = prefab;
        this.maxCapacity = maxCapacity;

        for (int i = 0; i < perCreate; i++) Create();
    }

    private GameObject Create()
    {
        GameObject go = GameObject.Instantiate(prefab, parent);
        Hide(go);
        return go;
    }

    #region Core
    public GameObject Get()
    {
        GameObject go = pool.Count == 0 ? Create() : pool.Pop();
        Show(go);
        return go;
    }

    public void Recycle(GameObject go)
    {
        if(pool.Count >= maxCapacity)
        {
            GameObject.Destroy(go);
            return;
        }
        Hide(go);
        pool.Push(go);
    }

    #endregion

    private void Hide(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(parent);
    }
    private void Show(GameObject go)
    {
        go.SetActive(true);
        go.transform.SetParent(null);
    }
}