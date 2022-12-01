﻿using Modding;
using SFCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MonoMod.RuntimeDetour;
using TestOfTeamwork.Consts;
using TestOfTeamwork.MonoBehaviours;
using UnityEngine;
using UObject = UnityEngine.Object;
using SFCore.Generics;
using Logger = Modding.Logger;

namespace TestOfTeamwork;

public class TestOfTeamwork : FullSettingsMod<TotSaveSettings, TotGlobalSettings>
{
    internal static TestOfTeamwork Instance;

    public LanguageStrings LangStrings { get; private set; }
    public TextureStrings SpriteDict { get; private set; }
    public AudioStrings AudioDict { get; private set; }
    public SceneChanger SceneChanger { get; private set; }

#if DEBUG_CHARMS
        // DEBUG
        public CharmHelper charmHelper { get; private set; }
#endif

    public static AudioClip GetAudio(string name) => Instance.AudioDict.Get(name);

    public static Sprite GetSprite(string name) => Instance.SpriteDict.Get(name);

    public override string GetVersion() => SFCore.Utils.Util.GetVersion(Assembly.GetExecutingAssembly());

    public override List<ValueTuple<string, string>> GetPreloadNames()
    {
        return new List<ValueTuple<string, string>>
        {
            new ValueTuple<string, string>("White_Palace_18", "White Palace Fly"),
            new ValueTuple<string, string>("White_Palace_18", "saw_collection/wp_saw"),
            new ValueTuple<string, string>("White_Palace_18", "saw_collection/wp_saw (2)"),
            new ValueTuple<string, string>("White_Palace_18", "Soul Totem white_Infinte"),
            new ValueTuple<string, string>("White_Palace_18", "Area Title Controller"),
            new ValueTuple<string, string>("White_Palace_18", "glow response lore 1/Glow Response Object (11)"),
            new ValueTuple<string, string>("White_Palace_18", "_SceneManager"),
            new ValueTuple<string, string>("White_Palace_18", "Inspect Region"),
            new ValueTuple<string, string>("White_Palace_18", "_Managers/PlayMaker Unity 2D"),
            new ValueTuple<string, string>("White_Palace_18", "Music Region (1)"),
            new ValueTuple<string, string>("White_Palace_17", "WP Lever"),
            new ValueTuple<string, string>("White_Palace_17", "White_ Spikes"),
            new ValueTuple<string, string>("White_Palace_17", "Cave Spikes Invis"),
            new ValueTuple<string, string>("White_Palace_09", "Quake Floor"),
            new ValueTuple<string, string>("Grimm_Divine", "Charm Holder"),
            new ValueTuple<string, string>("White_Palace_03_hub", "WhiteBench"),
            new ValueTuple<string, string>("Crossroads_07", "Breakable Wall_Silhouette"),
            new ValueTuple<string, string>("Deepnest_East_Hornet_boss", "Hornet Outskirts Battle Encounter"),
            new ValueTuple<string, string>("Deepnest_East_Hornet_boss", "Hornet Boss 2"),
            new ValueTuple<string, string>("White_Palace_03_hub", "door1"),
            new ValueTuple<string, string>("White_Palace_03_hub", "Dream Entry")
        };
    }

    public TestOfTeamwork() : base("Test of Teamwork")
    {
        Instance = this;

        LangStrings = new LanguageStrings();
        SpriteDict = new TextureStrings();

        AchievementHelper.AddAchievement(AchievementStrings.DefeatedWeaverPrincessKey, GetSprite(TextureStrings.AchievementWeaverPrincessKey), LanguageStrings.AchievementDefeatedWeaverPrincessTitleKey, LanguageStrings.AchievementDefeatedWeaverPrincessTextKey, true);

        InitInventory();

#if DEBUG_CHARMS
            charmHelper = new CharmHelper();
            charmHelper.customCharms = 4;
            charmHelper.customSprites = new Sprite[] { GetSprite(TextureStrings.YKey), GetSprite(TextureStrings.EKey), GetSprite(TextureStrings.EKey), GetSprite(TextureStrings.TKey) };
#endif

        InitCallbacks();
    }

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        Log("Initializing");
        Instance = this;

        SceneChanger = new SceneChanger(preloadedObjects);
        AudioDict = new AudioStrings(SceneChanger);

        //GameManager.instance.StartCoroutine(Register2BossModCore());
        //Platform.Current.EncryptedSharedData.SetInt(AchievementStrings.DefeatedWeaverPrincessKey, 0); // DEBUG

        Log("Initialized");
    }

    private void InitCallbacks()
    {
        // Hooks
        ModHooks.GetPlayerBoolHook += OnGetPlayerBoolHook;
        ModHooks.SetPlayerBoolHook += OnSetPlayerBoolHook;
        ModHooks.GetPlayerIntHook += OnGetPlayerIntHook;
        ModHooks.SetPlayerIntHook += OnSetPlayerIntHook;
        ModHooks.ApplicationQuitHook += SaveTotGlobalSettings;
        ModHooks.LanguageGetHook += OnLanguageGetHook;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void InitInventory()
    {
        ItemHelper.AddNormalItem(GetSprite(TextureStrings.InvHornetKey), nameof(SaveSettings.SFGrenadeTestOfTeamworkHornetCompanion), LanguageStrings.HornetInvNameKey, LanguageStrings.HornetInvDescKey);
    }

    private void OnSceneChanged(UnityEngine.SceneManagement.Scene from, UnityEngine.SceneManagement.Scene to)
    {
        string scene = to.name;

        try
        {
            if (HeroController.instance.gameObject.GetComponent<SceneExpander>() != null)
            {
                UObject.Destroy(HeroController.instance.gameObject.GetComponent<SceneExpander>());
            }
        }
        catch
        { }

        if (scene == TransitionGateNames.Rt)
        {
            // Black Egg Entrance, needs change to make Hornet give the Item
            SceneChanger.CR_Change_Room_temple(to);
        }
        else if (scene == TransitionGateNames.Wp06)
        {
            // Path of Pain Entrance, needs change to make "Test of Teamwork" accessible
            SceneChanger.CR_Change_White_Palace_06(to);
        }
        else if (scene == TransitionGateNames.Tot01)
        {
            GameManager.instance.RefreshTilemapInfo(scene);
        }
        else if (scene == TransitionGateNames.Tot02)
        {
            GameManager.instance.RefreshTilemapInfo(scene);
        }
        else if (scene == TransitionGateNames.Tot03)
        {
            GameManager.instance.RefreshTilemapInfo(scene);
        }
        else if (scene == TransitionGateNames.TotEndless)
        {
            SceneChanger.CR_Change_ToTEndless(to);
            GameManager.instance.RefreshTilemapInfo(scene);
        }
        else if (scene == TransitionGateNames.TotDropdown)
        {
            GameManager.instance.RefreshTilemapInfo(scene);
        }
        else if (scene == TransitionGateNames.TotClaus)
        {
            GameManager.instance.RefreshTilemapInfo(scene);
        }
        else if (scene == TransitionGateNames.TotOrganRoom)
        {
            GameManager.instance.RefreshTilemapInfo(scene);
        }
    }

    private void SaveTotGlobalSettings()
    {
        SaveGlobalSettings();
    }

    #region Get/Set Hooks

    private string OnLanguageGetHook(string key, string sheet, string orig)
    {
#if DEBUG_CHARMS
            // There probably is a better way to do this, but for now take this
#region Custom Charms
            if (key.StartsWith("CHARM_NAME_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return "CHARM NAME";
                }
            }
            if (key.StartsWith("CHARM_DESC_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return "CHARM DESC";
                }
            }
#endregion
#endif
        if (LangStrings.ContainsKey(key, sheet))
        {
            return LangStrings.Get(key, sheet);
        }
        return orig;
    }

    private bool OnGetPlayerBoolHook(string target, bool orig)
    {
#if DEBUG_CHARMS
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.gotCustomCharms[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.newCustomCharms[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.equippedCustomCharms[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
#endif
        var tmpField = ReflectionHelper.GetFieldInfo(typeof(TotSaveSettings), target);
        if (tmpField != null)
        {
            return (bool) tmpField.GetValue(SaveSettings);
        }
        if (target == "alwaysFalse")
        {
            return false;
        }
        return orig;
    }

    private bool OnSetPlayerBoolHook(string target, bool orig)
    {
#if DEBUG_CHARMS
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    Settings.gotCustomCharms[charmHelper.charmIDs.IndexOf(charmNum)] = val;
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    Settings.newCustomCharms[charmHelper.charmIDs.IndexOf(charmNum)] = val;
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    Settings.equippedCustomCharms[charmHelper.charmIDs.IndexOf(charmNum)] = val;
                }
            }
#endif
        var tmpField = ReflectionHelper.GetFieldInfo(typeof(TotSaveSettings), target);
        if (tmpField != null)
        {
            tmpField.SetValue(SaveSettings, orig);
        }
        return orig;
    }

    private int OnGetPlayerIntHook(string target, int orig)
    {
#if DEBUG_CHARMS
            if (target.StartsWith("charmCost_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmHelper.charmIDs.Contains(charmNum))
                {
                    return Settings.customCharmCosts[charmHelper.charmIDs.IndexOf(charmNum)];
                }
            }
#endif
        var tmpField = ReflectionHelper.GetFieldInfo(typeof(TotSaveSettings), target);
        if (tmpField != null)
        {
            return (int) tmpField.GetValue(SaveSettings);
        }
        return orig;
    }

    private int OnSetPlayerIntHook(string target, int orig)
    {
        var tmpField = ReflectionHelper.GetFieldInfo(typeof(TotSaveSettings), target);
        if (tmpField != null)
        {
            tmpField.SetValue(SaveSettings, orig);
        }
        return orig;
    }

    #endregion Get/Set Hooks

    enum Commands
    {
        NumBosses,
        StatueName,
        StatueDescription,
        CustomScene,
        ScenePrefabName,
        STATUE_GO
    }
    private bool _r2BmcTimeout;
    private bool _r2BmcSuccess;
    private static readonly string R2BmcBmc = "BossModCore";
    private static readonly string R2BmcCom = $"{R2BmcBmc} - ";
    private static readonly string R2BmcSetNum = $" - {Commands.NumBosses}";
    private static readonly string R2BmcSetStatName = $" - {Commands.StatueName} - ";
    private static readonly string R2BmcSetStatDesc = $" - {Commands.StatueDescription} - ";
    private static readonly string R2BmcSetCustomScene = $" - {Commands.CustomScene} - ";
    private static readonly string R2BmcSetCustomSceneName = $" - {Commands.ScenePrefabName} - ";
    private static readonly string R2BmcSetStatGo = $" - {Commands.STATUE_GO} - ";

    private IEnumerator Register2BossModCore()
    {
        PlayerData pd = PlayerData.instance;
        _r2BmcTimeout = false;

        GameManager.instance.StartCoroutine(RegisterTimeout());

        while (!_r2BmcTimeout)
        {
            _r2BmcSuccess = pd.GetBool(R2BmcBmc);
            if (_r2BmcSuccess)
            {
                _r2BmcTimeout = true;
            }
            yield return null;
        }

        if (!_r2BmcSuccess)
        {
            Log(R2BmcBmc + " not found!");
            yield break;
        }
        Log(R2BmcBmc + " is able to be registered to!");
        yield return null;

        pd.SetInt(R2BmcCom + GetType().Name + R2BmcSetNum, 1);
        pd.SetString(R2BmcCom + GetType().Name + R2BmcSetStatName + "0", "Boss Statue Name");
        pd.SetString(R2BmcCom + GetType().Name + R2BmcSetStatDesc + "0", "Boss Statue Description");
        pd.SetBool(R2BmcCom + GetType().Name + R2BmcSetCustomScene + "0", false);
        pd.SetString(R2BmcCom + GetType().Name + R2BmcSetCustomSceneName + "0", "GG_Hornet_2");
        pd.SetVariable(R2BmcCom + GetType().Name + R2BmcSetStatGo + "0", new GameObject("StatePrefabGO"));
    }

    private IEnumerator RegisterTimeout()
    {
        yield return new WaitForSecondsRealtime(3.0f);
        _r2BmcTimeout = true;
        _r2BmcSuccess = false;
    }
}