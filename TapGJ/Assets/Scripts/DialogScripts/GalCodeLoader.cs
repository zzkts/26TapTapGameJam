using System.Linq;
using DialogScripts;
using Tool.ComponentTool;
using UnityEngine;

public static class GalCodeLoader
{
    /// <summary>
    /// 处理一个剧情代码行
    /// </summary>
    /// <param name="code"></param>
    public static void HandleCodeLine(string code)
    {
        if (!code.Contains(" "))
        {
            Debug.LogWarning($"The Code {code} can't be handled!");
        }
        string[] parts = code.Split(' ');
        string head = parts.First();
        string[] args = parts.Skip(1).ToArray();
        switch (head)
        {
            case "changeFace":
                break;
            case "speaker":
                ComponentTool.Change_Text(UIHook.Instance.Speaker, args[0]);
                break;
        }
    }
}
