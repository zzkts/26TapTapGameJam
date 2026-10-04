using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace DialogScripts
{
    public static class DialogView
    {
        private static CancellationTokenSource _rollTextSource { get; set; } = new();
        private static CancellationTokenSource _autoRollSource { get; set; } = new();
        public static bool isRunning;
        public static bool isAutoPlay;
        private static StringBuilder _builder { get; set; } = new();
        public static Action CallBack { get; set; } = () => { };
        
        private static async UniTaskVoid RollText(string text, CancellationToken cancellationToken)
        {
            isRunning = true;
            _builder.Clear();
            TMP_Text textComponent = UIHook.Instance.DialogText.GetComponent<TMP_Text>();
            foreach (char c in text)
            {
                _builder.Append(c);
                textComponent.text = _builder.ToString();
                await Task.Delay(DialogSetting.PerCharDelay, cancellationToken);
            }
            isRunning = false;
        }

        /// <summary>
        /// 停止对话文本的滚动
        /// </summary>
        public static void StopRollText()
        {
            _rollTextSource.Cancel();
            _rollTextSource.Dispose();
            _rollTextSource = new CancellationTokenSource();
            isRunning = false;
        }

        /// <summary>
        /// 开启对话文本滚动
        /// </summary>
        /// <param name="text"></param>
        public static void StartRollText(string text)
        { 
            RollText(text, _rollTextSource.Token).Forget();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RunDialogLine()
        {
            DataManager.CurrentIndex += 1;
            DialogView.StartRollText(DataManager.CurrentDialog);
            if (DataManager.CurrentCode is not null)
            {
                GalCodeLoader.HandleCodeLine(DataManager.CurrentCode);
            }
        }

        private static async UniTaskVoid AutoRollText(CancellationToken cancellationToken)
        {
            isAutoPlay = true;
            while (!DataManager.IsFulled)
            {
                DataManager.CurrentIndex += 1;
                StartRollText(DataManager.CurrentDialog);
                int need_time = DataManager.CurrentDialog.Length * DialogSetting.PerCharDelay * 4;
                Debug.Log(need_time);
                await Task.Delay(need_time, cancellationToken);
            }
            isAutoPlay = false;
        }

        public static void StartAutoPlay()
        {
            AutoRollText(_autoRollSource.Token).Forget();
        }

        public static void StopAutoPlay()
        {
            _autoRollSource.Cancel();
            _autoRollSource.Dispose();
            _autoRollSource = new CancellationTokenSource();
            isAutoPlay = false;
        }
    }
}