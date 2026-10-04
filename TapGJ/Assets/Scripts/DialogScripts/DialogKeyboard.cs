using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DialogScripts
{
    public class DialogKeyboard: MonoBehaviour
    {
        private void Start()
        {
            DataManager.LoadJson("begin_story");
        }

        private void Update()
        {
            // 如果左键按下
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (DataManager.IsFulled)
                {
                    DialogView.CallBack();
                }
                // TODO 最好留一下判断
                // 如果剧情没有到最后，以及当前没有在播放剧情，以及没有点到按钮,以及不在自动
                if (!DataManager.IsFulled && !DialogView.isRunning && 
                    !DialogView.isAutoPlay &&
                    !EventSystem.current.currentSelectedGameObject)
                {
                    DialogView.RunDialogLine();
                }
            }
        }
    }
}