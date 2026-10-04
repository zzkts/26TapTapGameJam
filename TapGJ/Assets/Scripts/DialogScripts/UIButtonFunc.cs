using System;
using UnityEngine;
using UnityEngine.UI;

namespace DialogScripts
{
    public class UIButtonFunc: MonoBehaviour
    {
        public Toggle toggle;
        public void OnAutoButtonClick()
        {
            bool isOn = toggle.isOn;
            if (DataManager.IsFulled && isOn)
            {
                return;
            }
            if (isOn)
            {
                Debug.Log("OnAutoButtonClick");
                DialogView.StartAutoPlay();
            }
            else
            {
                DialogView.StopAutoPlay();
            }
        }
    }
}