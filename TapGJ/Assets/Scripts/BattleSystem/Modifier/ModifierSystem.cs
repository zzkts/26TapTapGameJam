using UnityEngine;
using System.Collections.Generic;

public class ModifierSystem
{
    private static ModifierSystem instance;
    public static ModifierSystem Instance
    {
        get
        {
            if (instance == null)
                instance = new ModifierSystem();
            return instance;
        }
    }

    private ModifierSystem() { }

    public WeaponData ApplyAllModify(WeaponData src, List<Modifier> modifiers)
    {
        if (src == null)
        {
            return null;
        }

        WeaponData result = Object.Instantiate(src);
        if (modifiers == null)
        {
            return result;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            Modifier modifier = modifiers[i];
            if (modifier == null)
            {
                continue;
            }

            switch (modifier.ModifierType)
            {
                case E_ModifierType.Atk:
                    result.damage += modifier.ModifierValue;
                    break;
                case E_ModifierType.FireElement:
                    // 第一版暂不处理元素和投射物替换。
                    break;
            }
        }

        return result;
    }
}
