using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LitJson;
using UnityEngine;

namespace DialogScripts
{
    public class DataManager
    {
        public static StoryObject CurrentDialogJson{ get; set; } = new();
        public static int MaxIndex => CurrentDialogJson.StoryText.Length - 1;
        public static int CurrentIndex { get; set; } = -1;
        public static bool IsFulled => CurrentIndex >= MaxIndex;
        public static string CurrentDialog => CurrentDialogJson.StoryText[CurrentIndex];
        [CanBeNull] public static string CurrentCode => CurrentDialogJson.Code[CurrentIndex.ToString()];
        
        public static void LoadJson(string name)
        {
            Debug.Log(Resources.Load<TextAsset>("Json/Dialog/" + name).text);
            // 更新当前Json
            var data = JsonMapper.ToObject(Resources.Load<TextAsset>("Json/Dialog/" + name).text);
            // 设置值
            CurrentDialogJson.StoryText = JsonMapper.ToObject<string[]>(data["StoryText"].ToJson());
            CurrentDialogJson.Code = JsonMapper.ToObject<Dictionary<string, string>>(data["Code"].ToJson());
            CurrentIndex = -1;
        }
    }

    public class StoryObject
    {
        public string[] StoryText { get; internal set; } = { "12", "23" };
        public Dictionary<string, string> Code { get; internal set; } = new();
    }
}