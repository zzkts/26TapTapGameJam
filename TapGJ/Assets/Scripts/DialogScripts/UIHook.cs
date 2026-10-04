using Unity.VisualScripting;
using UnityEngine;

namespace DialogScripts
{
    public class UIHook: MonoSingleton<UIHook>
    {
        public GameObject Speaker;
        public GameObject DialogText;
        public GameObject BasePanel;
    }

    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.Find("Canvas").GetComponent<T>();
                }
                return _instance;
            }
        }
    }
    
    
}