using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace DialogScripts
{
    public class UIHook: MonoSingleton<UIHook>
    {
        public GameObject Speaker;
        public GameObject DialogText;
        public GameObject BasePanel;
        public GameObject AutoToggle;
    }

    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = GameObject.Find("Canvas").GetComponent<T>();
                }
                return _instance;
            }
        }
    }
    
    
    
}