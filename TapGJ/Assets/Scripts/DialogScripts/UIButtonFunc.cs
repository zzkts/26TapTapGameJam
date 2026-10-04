using System;
using UnityEngine;
using UnityEngine.UI;

namespace DialogScripts
{
    public class UIButtonFunc: MonoBehaviour
    {
        public void OnAutoButtonClick(bool isOn)
        {
            Debug.Log(isOn);
            // if (DataManager.IsFulled && isOn)
            // {
            //     return;
            // }
            // if (isOn)
            // {
            //     Debug.Log("OnAutoButtonClick");
            //     DialogView.StartAutoPlay();
            // }
            // else
            // {
            //     DialogView.StopAutoPlay();
            // }
            // UIHook.Instance.AutoToggle.isOn = !isOn;
        }
    }
}