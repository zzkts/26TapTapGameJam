using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DialogScripts
{
    public class DialogKeyboard: MonoBehaviour
    {
        private void Update()
        {
            // 如果左键按下
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // 预留状态判断
                if (false)
                {
                    return;   
                }
                // 如果剧情没有到最后，以及当前没有在播放剧情，以及没有点到按钮
                if (!DataManager.IsFulled && !DialogView.isRunning && 
                    !EventSystem.current.currentSelectedGameObject)
                {

                }
            }
        }
    }
}