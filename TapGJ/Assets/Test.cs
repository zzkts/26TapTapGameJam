using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button btn;
    public bool isOpen = false;
    void Start()
    {
        btn.onClick.AddListener(() =>
        {
            isOpen = !isOpen;
            if (isOpen)
            {
                UIManager.Instance.HidePanel<BeginPanel>();
            }
            else
            {
                UIManager.Instance.ShowPanel<BeginPanel>();
            }
        });
    }
}
