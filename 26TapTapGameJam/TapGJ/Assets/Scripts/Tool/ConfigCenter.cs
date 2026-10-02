using cfg;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

public class ConfigCenter : Singleton<ConfigCenter>
{
    //存了所有表
    public static Tables Tables { get; private set; }
    protected override void OnInit()
    {
        Tables = new Tables(ConfigLoader);
        //获取某个配置表
        //Tables.Tb"配置表的名字".Get(id);
    }
    //加载配置表
    private JArray ConfigLoader(string jsonFileName)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("DataCfg/" + jsonFileName);
        return JsonConvert.DeserializeObject<JArray>(textAsset.text);
    }
}