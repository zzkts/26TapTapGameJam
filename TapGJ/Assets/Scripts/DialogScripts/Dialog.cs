using System;
using UnityEngine.SceneManagement;

namespace DialogScripts
{
    public class Dialog
    {
        public static void RunDialog(string file_name, Action callBack)
        {
            DataManager.LoadJson(file_name);
            DialogView.CallBack = callBack;
            SceneManager.LoadScene("BeginScene", LoadSceneMode.Additive);
        }
    }
}