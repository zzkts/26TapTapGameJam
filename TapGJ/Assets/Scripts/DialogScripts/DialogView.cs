using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DialogScripts
{
    public static class DialogView
    {
        private static CancellationTokenSource _rollTextSource { get; set; } = new();
        private static CancellationTokenSource _autoRollSource { get; set; } = new();
        public static bool isRunning;
        public static bool isAutoPlay;
        private static StringBuilder _builder { get; set; } = new();
        public static Action CallBack { get; set; } = () =>
        {
            SceneManager.UnloadSceneAsync("BeginScene");
        };
        
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
            if (DataManager.CurrentDialogJson.Code.ContainsKey(DataManager.CurrentIndex.ToString()))
            {
                GalCodeLoader.HandleCodeLine(DataManager.CurrentCode);
            }
        }

        private static async UniTaskVoid AutoRollText(CancellationToken cancellationToken)
        {
            isAutoPlay = true;
            if (DataManager.CurrentIndex is -1)
            {
                await Task.Delay(1000, cancellationToken);
            }
            else
            {
                await DelayCurrentLine(cancellationToken, 2);
            }
            while (!DataManager.IsFulled)
            {
                DataManager.CurrentIndex += 1;
                StartRollText(DataManager.CurrentDialog);
                await DelayCurrentLine(cancellationToken);
            }
            isAutoPlay = false;
        }

        private static async UniTask DelayCurrentLine(CancellationToken cancellationToken, int update = 1)
        {
            int need_time = DataManager.CurrentDialog.Length * DialogSetting.PerCharDelay * 8 / update;
            await Task.Delay(need_time, cancellationToken);
        } 

        public static void StartAutoPlay()
        {
            AutoRollText(_autoRollSource.Token).Forget();
        }

        public static void StopAutoPlay()
        {
            Debug.Log("Cancel Auto Play");
            _autoRollSource.Cancel();
            _autoRollSource.Dispose();
            _autoRollSource = new CancellationTokenSource();
            isAutoPlay = false;
        }
    }
}