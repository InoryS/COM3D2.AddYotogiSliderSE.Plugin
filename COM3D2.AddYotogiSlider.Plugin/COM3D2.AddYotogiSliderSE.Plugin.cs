
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using UnityObsoleteGui;
using PV = UnityObsoleteGui.PixelValuesCM3D2;
using Yotogis;

using COM3D2.AddYotogiSliderSE.Plugin.Extensions;

namespace COM3D2.AddYotogiSliderSE.Plugin
{
    public static class VERSION
    {
        public const string NUMBER = "1.0.0.10";

#if DEBUG
        public const string RELEASE_TYPE = "debug";
#else
        public const string RELEASE_TYPE = "release";
#endif

        public const string VARIAN = "standard";
    }


    [BepInPlugin(AddYotogiSliderSE.Uuid, AddYotogiSliderSE.PluginName, AddYotogiSliderSE.Version)]
    [BepInDependency("org.bepinex.plugins.unityinjectorloader", BepInDependency.DependencyFlags.SoftDependency)]
    public class AddYotogiSliderSE : BaseUnityPlugin
    {
#region Constants

        public const string Uuid = "COM3D2.AddYotogiSliderSE2";
        public const string PluginName = "AddYotogiSliderSE2";

        public const string Version = VERSION.NUMBER;

        private readonly float TimePerUpdateSpeed = 0.33f;
        private readonly float WaitBoneLoad = 1.00f;
        private const string LogLabel = AddYotogiSliderSE.PluginName + " : ";

        public enum TunLevel
        {
            None = -1,
            Friction = 0,
            Petting = 1,
            Nip = 2,
        }

        public enum KupaLevel
        {
            None = -1,
            Sex = 0,
            Vibe = 1,
        }

        #endregion

        #region Variables

        //private string _toggleKey = "f5";
        private ConfigEntry<KeyboardShortcut> ToggleKey;

        private int sceneLevel;
        private bool visible = false;
        private bool bNormalYotogiScene = false;
        private bool bCompatibilityYotogiScene = false;
        private bool bInitCompleted = false;
        private bool bLoadBoneAnimetion = false;
        private bool bSyncMotionSpeed = false;
        private bool bCursorOnWindow = false;
        private float fPassedTimeOnLevel = 0f;
        private bool canStart { get { return bInitCompleted && bLoadBoneAnimetion; } }
        private bool kagScriptCallbacksOverride = false;

        private string[] sKey = { "WIN", "STATUS", "AHE", "BOTE", "FACEBLEND", "FACEANIME" };
        private string[] sliderName = { "興奮", "精神", "官能", "理性", "感度", "速度" };
        private string[] sliderNameAutoAHE = { "瞳Y" };
        private string[] sliderNameAutoTUN = { "乳首肥大度", "乳首萎え", "乳首勃起", "乳首たれ" };
        private string[] sliderNameAutoBOTE = { "腹" };
        private string[] sliderNameAutoKUPA = { "前", "後", "拡張度", "陰唇", "膣", "尿道", "すじ", "クリ" };
        private List<string> sStageNames = new List<string>();
        private Dictionary<string, PlayAnime> pa = new Dictionary<string, PlayAnime>();

        private Window window;
        private Rect winRatioRect = new Rect(0.75f, 0.25f, 0.20f, 0.65f);
        private Rect winAnimeRect;
        private float[] fWinAnimeFrom;
        private float[] fWinAnimeTo;

        private Dictionary<string, YotogiPanel> panel = new Dictionary<string, YotogiPanel>();
        private Dictionary<string, YotogiSlider> slider = new Dictionary<string, YotogiSlider>();
        private Dictionary<string, YotogiButtonGrid> grid = new Dictionary<string, YotogiButtonGrid>();
        private Dictionary<string, YotogiToggle> toggle = new Dictionary<string, YotogiToggle>();
        private Dictionary<string, YotogiLineSelect> lSelect = new Dictionary<string, YotogiLineSelect>();

        private int iLastExcite = 0;
        private int iOrgasmCount = 0;
        //private int iLastSliderFrustration = 0;
        //private float fLastSliderSensitivity = 0f;
        private float fPassedTimeOnCommand = -1f;

        private string lastSkillName;
        private CommonCommandData currentCommandData;

        //AutoAHE
        private bool bOrgasmAvailable = false;                                                     //BodyShapeKeyチェック
        private float fEyePosToSliderMul = 5000f;
        private float fOrgasmsPerAheLevel = 3f;
        private int idxAheOrgasm
        {
            get { return (int)Math.Min(Math.Max(Math.Floor((iOrgasmCount - 1) / fOrgasmsPerAheLevel), 0), 2); }
        }
        private int[] iAheExcite = new int[] { 267, 233, 200 };                               //適用の興奮閾値
        private float fAheDefEye = 0f;
        private float fAheLastEye = 0f;
        private float fAheEyeDecrement = 0.20f / 60f;                                               //放置時の瞳降下
        private float[] fAheNormalEyeMax = new float[] { 40f, 45f, 50f };                             //通常時の瞳の最大値
        private float[] fAheOrgasmEyeMax = new float[] { 50f, 60f, 70f };                             //絶頂時の瞳の最大値
        private float[] fAheOrgasmEyeMin = new float[] { 30f, 35f, 40f };                             //絶頂時の瞳の最小値
        private float[] fAheOrgasmSpeed = new float[] { 90f, 80f, 70f };                             //絶頂時のモーション速度
        private float[] fAheOrgasmConvulsion = new float[] { 60f, 80f, 100f };                            //絶頂時の痙攣度
        private string[] sAheOrgasmFace = new string[] { "エロ放心", "エロ好感３", "通常射精後１" }; //絶頂時のFace
        private string[] sAheOrgasmFaceBlend = new string[] { "頬１涙１", "頬２涙２", "頬３涙３よだれ" }; //絶頂時のFaceBlend
        private int iAheOrgasmChain = 0;

        // AutoTUN
        private bool bBokkiChikubiAvailable = false;
        private int[] iTunValue = { 3, 5, 100 }; // 乳首勃起増加量
        private float fChikubiScale = 25f;
        private float fChikubiNae = 0f;
        private float fChikubiBokki = 0f;
        private float fChikubiTare = 0f;
        private float iDefChikubiNae;
        private float iDefChikubiTare;

        //AutoBOTE
        private int iDefHara;             //腹の初期値
        private int iCurrentHara;         //腹の現在値
        private int iHaraIncrement = 10;  //一回の腹の増加値
        private int iBoteHaraMax = 100; //腹の最大値
        private int iBoteCount = 0;   //中出し回数

        //AutoKUPA
        private bool bKupaAvailable = false;             //BodyShapeKeyチェック
        private bool bKupaFuck = false;             //挿入しているかどうか
        private float fKupaLevel = 70f;         //拡張bodyに対して何％小さく挙動するか
        private float fLabiaKupa = 0f;
        private float fVaginaKupa = 0f;
        private float fNyodoKupa = 0f;
        private float fSuji = 0f;
        private int iKupaDef = 0;
        private int iKupaStart = 0;
        private int iKupaIncrementPerOrgasm = 0;           //絶頂回数当たりの通常時局部開き値の増加値
        private int iKupaNormalMax = 0;           //通常時の局部開き最大値
        private int iKupaMin
        {
            get
            {
                return (int)Mathf.Max(iKupaDef,
                        Mathf.Min(iKupaStart + iKupaIncrementPerOrgasm * iOrgasmCount, iKupaNormalMax));
            }
        }
        private int[] iKupaValue = { 100, 50 }; //最大の局部開き値
        private int iKupaWaitingValue = 5;           //待機モーションでの局部開き値幅
        private float fPassedTimeOnAutoKupaWaiting = 0;

        //AnalKUPA
        private bool bAnalKupaAvailable = false;   //BodyShapeKeyチェック
        private bool bAnalKupaFuck = false;   //挿入しているかどうか
        private int iAnalKupaDef = 0;
        private int iAnalKupaStart = 0;
        private int iAnalKupaIncrementPerOrgasm = 0;       //絶頂回数当たりの通常時アナル開き値の増加値
        private int iAnalKupaNormalMax = 0;       //通常時のアナル開き最大値
        private int iAnalKupaMin
        {
            get
            {
                return (int)Mathf.Max(iAnalKupaDef,
                        Mathf.Min(iAnalKupaStart + iAnalKupaIncrementPerOrgasm * iOrgasmCount, iAnalKupaNormalMax));
            }
        }
        private int[] iAnalKupaValue = { 100, 50 }; //最大のアナル開き値
        private int iAnalKupaWaitingValue = 5;           //待機モーションでのアナル開き値幅
        private float fPassedTimeOnAutoAnalKupaWaiting = 0;

        private bool bLabiaKupaAvailable = false;
        private bool bVaginaKupaAvailable = false;
        private bool bNyodoKupaAvailable = false;
        private bool bSujiAvailable = false;
        private bool bClitorisAvailable = false;
        //private int iLabiaKupaMin = 0;
        //private int iVaginaKupaMin = 0;
        //private int iNyodoKupaMin = 0;
        //private int iSujiMin = 0;
        private int iClitorisMin = 0;

        //FaceNames
        private string[] sFaceNames =
        {
        "エロ通常１", "エロ通常２", "エロ通常３", "エロ羞恥１", "エロ羞恥２", "エロ羞恥３",
        "エロ興奮０", "エロ興奮１", "エロ興奮２", "エロ興奮３", "エロ緊張",   "エロ期待",
        "エロ好感１", "エロ好感２", "エロ好感３", "エロ我慢１", "エロ我慢２", "エロ我慢３",
        "エロ嫌悪１", "エロ怯え",   "エロ痛み１", "エロ痛み２", "エロ痛み３", "エロメソ泣き",
        "エロ絶頂",  "エロ痛み我慢", "エロ痛み我慢２","エロ痛み我慢３", "エロ放心", "発情",
        "通常射精後１", "通常射精後２", "興奮射精後１", "興奮射精後２", "絶頂射精後１", "絶頂射精後２",
        "エロ舐め愛情", "エロ舐め愛情２", "エロ舐め快楽", "エロ舐め快楽２", "エロ舐め嫌悪", "エロ舐め通常",
        "閉じ舐め愛情", "閉じ舐め快楽", "閉じ舐め快楽２", "閉じ舐め嫌悪", "閉じ舐め通常", "接吻",
        "エロフェラ愛情", "エロフェラ快楽", "エロフェラ嫌悪", "エロフェラ通常", "エロ舌責", "エロ舌責快楽",
        "閉じフェラ愛情", "閉じフェラ快楽", "閉じフェラ嫌悪", "閉じフェラ通常", "閉じ目",   "目口閉じ",
        "通常", "怒り", "笑顔", "微笑み", "悲しみ２", "泣き",
        "きょとん", "ジト目","あーん", "ためいき", "ドヤ顔", "にっこり",
        "びっくり", "ぷんすか", "まぶたギュ", "むー", "引きつり笑顔", "疑問",
        "苦笑い", "困った", "思案伏せ目", "少し怒り", "誘惑",  "拗ね",
        "優しさ","居眠り安眠","目を見開いて","痛みで目を見開いて", "余韻弱","目口閉じ",
        "口開け","恥ずかしい","照れ", "照れ叫び","ウインク照れ", "にっこり照れ",
        "ダンス目つむり","ダンスあくび","ダンスびっくり","ダンス微笑み","ダンス目あけ","ダンス目とじ",
        "ダンスウインク", "ダンスキス", "ダンスジト目","ダンス困り顔", "ダンス真剣","ダンス憂い",
        "ダンス誘惑", "頬０涙０", "頬０涙１", "頬０涙２", "頬０涙３", "頬１涙０",
        "頬１涙１",   "頬１涙２", "頬１涙３", "頬２涙０", "頬２涙１", "頬２涙２",
        "頬２涙３",   "頬３涙１", "頬３涙０", "頬３涙２", "頬３涙３", "追加よだれ",
        "頬０涙０よだれ", "頬０涙１よだれ", "頬０涙２よだれ", "頬０涙３よだれ", "頬１涙０よだれ", "頬１涙１よだれ",
        "頬１涙２よだれ", "頬１涙３よだれ", "頬２涙０よだれ", "頬２涙１よだれ", "頬２涙２よだれ", "頬２涙３よだれ",
        "頬３涙０よだれ", "頬３涙１よだれ", "頬３涙２よだれ", "頬３涙３よだれ" , "エラー", "デフォ",
        };
        private string[] sFaceBlendCheek = new string[] { "頬０", "頬１", "頬２", "頬３" };
        private string[] sFaceBlendTear = new string[] { "涙０", "涙１", "涙２", "涙３" };


        // ゲーム内部変数への参照
        private Maid maid;
        //private FieldInfo maidStatusInfo;
        private FieldInfo maidFoceKuchipakuSelfUpdateTime;

        private YotogiPlayManager yotogiPlayManager;
        private YotogiOldPlayManager yotogiOldPlayManager;
        private WfScreenChildren playManagerAsWfScreenChildren;

        private YorogiParamBasicBarDelegator yotogiParamBasicBarDelegator;
        private Action<Skill.Data.Command.Data> orgOnClickCommand;
        private Action<Skill.Old.Data.Command.Data> orgOnClickCommandOld;

        private KagScript kagScript;
        private Func<KagTagSupport, bool> orgTagFace;
        private Func<KagTagSupport, bool> orgTagFaceBlend;

        private Animation anm_BO_body001;
        private Animation[] anm_BO_mbody;

        private bool isInOutAnimationActive = false;
#endregion

#region Nested classes

        private static class CompatibilityModeStage
        {
            private static readonly string[] plugins = { "vp001", "legacy", "pp001", "pp002", "pp003", "vrcom", "karaoke001" };

            private static readonly string[][] stages = new string[][] {
                new string[] { "LockerRoom", "SMRoom2" },
                new string[] { "Salon", "Salon_Day", "MyBedRoom", "MyBedRoom_Night", "PlayRoom", "Pool", "SMRoom", "Bathroom",
                    "PlayRoom2", "Salon_Garden", "LargeBathRoom", "MaidRoom", "OiranRoom", "Penthouse", "Town", "Salon_Entrance",
                    "Shitsumu", "Shitsumu_Night", "Shitsumu_ChairRot", "Shitsumu_ChairRot_Night", "DressRoom_NoMirror", "Syosai",
                    "Syosai_Night", "Kitchen", "Kitchen_Night", "Bar" },
                new string[] { "MyBedRoom_NightOff", "Toilet", "Train", "Oheya" },
                new string[] { "ClassRoom_Play", "HoneymoonRoom", "ClassRoom", "OutletPark" },
                new string[] { "Sea", "Sea_Night", "Yashiki_Day", "Yashiki", "Yashiki_pillow", "BigSight", "BigSight_Night",
                    "PrivateRoom", "PrivateRoom_Night" },
                new string[] { "Sea_VR", "Sea_VR_Night", "Villa", "Villa_Night", "Villa_BedRoom", "Villa_BedRoom_Night", "Villa_Farm",
                    "Villa_Farm_Night", "Rotenburo", "Rotenburo_Night" },
                new string[] { "KaraokeRoom" }
            };

            public static List<string> GetStageList()
            {
                List<string> stageList = new List<string>();
                for (int i = 0; i < plugins.Length; i++)
                {
                    if (PluginData.IsEnabled(plugins[i])) stageList.AddRange(stages[i]);
                }

                return stageList;
            }
        }

        // Skill.Data.Command.DataとSkill.Old.Data.Command.Dataを同じように扱うためのクラス
        private class CommonCommandData
        {
            public class Basic
            {
                private static readonly Dictionary<YotogiOld.SkillCommandType, Yotogi.SkillCommandType> dicSkillCommandType =
                    new Dictionary<YotogiOld.SkillCommandType, Yotogi.SkillCommandType>()
                    {
                        { YotogiOld.SkillCommandType.単発, Yotogi.SkillCommandType.単発 },
                        { YotogiOld.SkillCommandType.単発_挿入, Yotogi.SkillCommandType.単発_挿入 },
                        { YotogiOld.SkillCommandType.挿入, Yotogi.SkillCommandType.挿入 },
                        { YotogiOld.SkillCommandType.止める, Yotogi.SkillCommandType.止める },
                        { YotogiOld.SkillCommandType.絶頂, Yotogi.SkillCommandType.絶頂 },
                        { YotogiOld.SkillCommandType.継続, Yotogi.SkillCommandType.継続 }
                    };

                public string name { get; private set; }
                public string group_name { get; private set; }
                public Yotogi.SkillCommandType command_type { get; private set; }

                public Basic(Skill.Data.Command.Data.Basic basic)
                {
                    SetBasic(basic);
                }

                public Basic(Skill.Old.Data.Command.Data.Basic basic)
                {
                    SetBasic(basic);
                }

                public void SetBasic(Skill.Data.Command.Data.Basic basic)
                {
                    this.name = basic.name;
                    this.group_name = basic.group_name;
                    this.command_type = basic.command_type;
                }

                public void SetBasic(Skill.Old.Data.Command.Data.Basic basic)
                {
                    this.name = basic.name;
                    this.group_name = basic.group_name;
                    this.command_type = dicSkillCommandType[basic.command_type];
                }
            }

            public class Status
            {
                public int frustration { get; private set; }

                public Status(Skill.Data.Command.Data.Status status)
                {
                }

                public Status(Skill.Old.Data.Command.Data.Status status)
                {
                    SetStatus(status);
                }

                public void SetStatus(Skill.Data.Command.Data.Status status)
                {
                }

                public void SetStatus(Skill.Old.Data.Command.Data.Status status)
                {
                    this.frustration = status.frustration;
                }
            }

            private readonly Basic basic_;
            private readonly Status status_;

            public Basic basic { get { return basic_; } }
            public Status status { get { return status_; } }

            public string skillName { get; private set; }

            public CommonCommandData(Skill.Data.Command.Data data)
            {
                this.basic_ = new Basic(data.basic);
                this.status_ = new Status(data.status);
                this.skillName = data.basic.skill.name;
            }

            public CommonCommandData(Skill.Old.Data.Command.Data data)
            {
                this.basic_ = new Basic(data.basic);
                this.status_ = new Status(data.status);

                Skill.Old.Data skillOldData = Skill.Old.Get(data.basic.skill_id);
                this.skillName = (skillOldData != null) ? skillOldData.name : string.Empty;
            }

            public void SetData(Skill.Data.Command.Data data)
            {
                this.basic.SetBasic(data.basic);
                this.status.SetStatus(data.status);
                this.skillName = data.basic.skill.name;
            }

            public void SetData(Skill.Old.Data.Command.Data data)
            {
                this.basic.SetBasic(data.basic);
                this.status.SetStatus(data.status);

                Skill.Old.Data skillOldData = Skill.Old.Get(data.basic.skill_id);
                this.skillName = (skillOldData != null) ? skillOldData.name : string.Empty;
            }
        }

        private class YorogiParamBasicBarDelegator
        {
            private YotogiParamBasicBar param_basic_bar_;
            private YotogiOldParamBasicBar old_param_basic_bar_;

            public bool IsReady
            {
                get { return param_basic_bar_ != null || old_param_basic_bar_ != null; }
            }

            public delegate void SetCurrentStatusDelegate(int cur_num, bool is_anime);

            public SetCurrentStatusDelegate SetCurrentExcite;
            public SetCurrentStatusDelegate SetCurrentMind;
            public SetCurrentStatusDelegate SetCurrentReason;
            public SetCurrentStatusDelegate SetCurrentSensual;

            public void SetParamBasicBar(YotogiParamBasicBar paramBasicBar)
            {
                Clear();
                if (paramBasicBar == null) return;
                this.param_basic_bar_ = paramBasicBar;
                this.SetCurrentExcite = new SetCurrentStatusDelegate(this.param_basic_bar_.SetCurrentExcite);
                this.SetCurrentMind = new SetCurrentStatusDelegate(this.param_basic_bar_.SetCurrentMind);
                this.SetCurrentSensual = new SetCurrentStatusDelegate(this.param_basic_bar_.SetCurrentSensual);
            }

            public void SetParamBasicBar(YotogiOldParamBasicBar paramBasicBar)
            {
                Clear();
                if (paramBasicBar == null) return;
                this.old_param_basic_bar_ = paramBasicBar;
                this.SetCurrentExcite = new SetCurrentStatusDelegate(this.old_param_basic_bar_.SetCurrentExcite);
                this.SetCurrentMind = new SetCurrentStatusDelegate(this.old_param_basic_bar_.SetCurrentMind);
                this.SetCurrentReason = new SetCurrentStatusDelegate(this.old_param_basic_bar_.SetCurrentReason);
            }

            private void Clear()
            {
                this.param_basic_bar_ = null;
                this.old_param_basic_bar_ = null;
                this.SetCurrentExcite = null;
                this.SetCurrentMind = null;
                this.SetCurrentSensual = null;
                this.SetCurrentReason = null;
            }
        }


        private class YotogiPanel : Container
        {
            public enum HeaderUI
            {
                None,
                Slider,
                Face
            }

            private Rect padding { get { return PV.PropRect(paddingPx); } }
            private int paddingPx = 4;
            private GUIStyle labelStyle = new GUIStyle("label");
            private GUIStyle toggleStyle = new GUIStyle("toggle");
            private GUIStyle buttonStyle = new GUIStyle("button");
            private string headerHeightPV = "C1";
            private string headerFontSizePV = "C1";
            private HeaderUI headerUI;
            private bool childrenVisible = false;

            private event EventHandler<ToggleEventArgs> OnEnableChanged;

            public string Title;
            public string HeaderUILabelText;
            public bool Enabled = false;
            public bool HeaderUIToggle = false;

            public YotogiPanel(string name, string title) : this(name, title, HeaderUI.None) { }
            public YotogiPanel(string name, string title, HeaderUI type) : this(name, title, type, null) { }
            public YotogiPanel(string name, string title, EventHandler<ToggleEventArgs> onEnableChanged) : this(name, title, HeaderUI.None, onEnableChanged) { }
            public YotogiPanel(string name, string title, HeaderUI type, EventHandler<ToggleEventArgs> onEnableChanged)
            : base(name, new Rect(Window.AutoLayout, Window.AutoLayout, Window.AutoLayout, 0))
            {
                this.Title = title;
                this.headerUI = type;
                this.OnEnableChanged += onEnableChanged;
                Resize();
            }

            public override void Draw(Rect outRect)
            {
                Rect groupRect = PV.InsideRect(outRect, padding);

                labelStyle = "box";
                GUI.Label(outRect, "", labelStyle);
                GUI.BeginGroup(groupRect);
                {
                    int headerHeight = PV.Line(headerHeightPV);
                    int headerFontSize = PV.Font(headerFontSizePV);

                    Rect cur = new Rect(0f, 0f, padding.width, headerHeight);

                    cur.width = groupRect.width * 0.325f;
                    buttonStyle.fontSize = headerFontSize;
                    resizeOnChangeChildrenVisible(GUI.Toggle(cur, childrenVisible, Title, buttonStyle));
                    cur.x += cur.width;

                    cur.width = groupRect.width * 0.300f;
                    cur.y -= PV.PropPx(2);
                    toggleStyle.fontSize = headerFontSize;
                    toggleStyle.alignment = TextAnchor.MiddleLeft;
                    toggleStyle.normal.textColor = toggleColor(Enabled);
                    toggleStyle.hover.textColor = toggleColor(Enabled);
                    onEnableChange(GUI.Toggle(cur, Enabled, toggleText(Enabled), toggleStyle));
                    cur.y += PV.PropPx(2);
                    cur.x += cur.width;

                    labelStyle = "label";
                    labelStyle.fontSize = headerFontSize;
                    switch (headerUI)
                    {
                        case HeaderUI.Slider:
                            {
                                cur.width = groupRect.width * 0.375f;
                                labelStyle.alignment = TextAnchor.MiddleRight;
                                GUI.Label(cur, "Pin", labelStyle);
                            }
                            break;

                        case HeaderUI.Face:
                            {
                                cur.width = groupRect.width * 0.375f;
                                labelStyle = "box";
                                labelStyle.fontSize = headerFontSize;
                                labelStyle.alignment = TextAnchor.MiddleRight;
                                GUI.Label(cur, HeaderUILabelText, labelStyle);
                            }
                            break;

                        default: break;
                    }

                    cur.x = 0;
                    cur.y += cur.height + +PV.PropPx(3);
                    cur.width = groupRect.width;

                    foreach (Element child in children)
                    {
                        if (!(child.Visible)) continue;

                        cur.height = child.Height;
                        child.Draw(cur);
                        cur.y += cur.height + PV.PropPx(3);
                    }
                }
                GUI.EndGroup();
            }

            public override void Resize() { Resize(false); }
            public override void Resize(bool broadCast)
            {
                float height = PV.Line(headerHeightPV) + PV.PropPx(3);

                foreach (Element child in children) if (child.Visible) height += child.Height + PV.PropPx(3);
                rect.height = height + (int)padding.height * 2;

                if (!broadCast) notifyParent(true, false);
            }

            //----

            private void resizeOnChangeChildrenVisible(bool b)
            {
                if (b != childrenVisible)
                {
                    foreach (Element child in children) child.Visible = b;
                    childrenVisible = b;
                }
            }

            private void onEnableChange(bool newValue)
            {
                if (this.Enabled != newValue)
                {
                    this.Enabled = newValue;
                    if (this.OnEnableChanged != null)
                    {
                        OnEnableChanged(this, new ToggleEventArgs(this.Title, newValue));
                    }
                }
            }

            private Color toggleColor(bool b) { return b ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0.2f, 0.2f, 1f); }
            private string toggleText(bool b) { return b ? "Enabled" : "Disabled"; }
        }

        private class YotogiSlider : Element
        {
            private HSlider slider;
            private string lineHeightPV = "C1";
            private string fontSizePV = "C1";
            private GUIStyle labelStyle = new GUIStyle("label");
            private string labelText = "";
            private bool pinEnabled = false;

            public float Value { get { return slider.Value; } set { if (!Pin) slider.Value = value; } }
            public float Default;
            public bool Pin;

            public YotogiSlider(string name, float min, float max, float def, EventHandler<SliderEventArgs> onChange, string label, bool pinEnabled)
            : base(name, new Rect(Window.AutoLayout, Window.AutoLayout, Window.AutoLayout, 0))
            {
                this.slider = new HSlider(name + ":slider", rect, min, max, def, onChange);
                this.Default = def;
                this.labelText = label;
                this.pinEnabled = pinEnabled;
                Resize();
            }

            public override void Draw(Rect outRect)
            {
                Rect cur = outRect;
                labelStyle = "label";

                cur.width = outRect.width * 0.3625f;
                labelStyle.fontSize = PV.Font(fontSizePV);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(cur, labelText, labelStyle);
                cur.x += cur.width;

                cur.width = outRect.width * 0.1575f;
                labelStyle = "box";
                labelStyle.fontSize = PV.Font(fontSizePV);
                labelStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(cur, slider.Value.ToString("F0"), labelStyle);
                cur.x += cur.width + outRect.width * 0.005f;

                cur.width = outRect.width * 0.400f;
                cur.y += PV.PropPx(4);
                slider.Draw(cur);
                cur.y -= PV.PropPx(4);
                cur.x += cur.width;

                if (pinEnabled)
                {
                    cur.width = outRect.width * 0.075f;
                    cur.y -= PV.PropPx(2);
                    Pin = GUI.Toggle(cur, Pin, "");
                }
            }

            public override void Resize() { Resize(false); }
            public override void Resize(bool broadCast) { rect.height = PV.Line(lineHeightPV); }
        }

        private class YotogiToggle : Element
        {
            private Toggle toggle;
            private GUIStyle labelStyle = new GUIStyle("label");
            private string lineHeightPV = "C1";
            private string fontSizePV = "C1";

            public bool Value
            {
                get { return toggle.Value; }
                set { toggle.Value = value; }
            }
            public string LabelText;

            public YotogiToggle(string name, bool def, string text, EventHandler<ToggleEventArgs> onChange)
            : base(name, new Rect(Window.AutoLayout, Window.AutoLayout, Window.AutoLayout, 0))
            {
                this.toggle = new Toggle(name + ":toggle", rect, def, text, onChange);
                this.LabelText = text;
                Resize();
            }

            public override void Draw(Rect outRect)
            {
                Rect cur = outRect;

                cur.width = outRect.width * 0.5f;
                labelStyle.fontSize = PV.Font(fontSizePV);
                labelStyle.alignment = TextAnchor.MiddleLeft;
                GUI.Label(cur, LabelText, labelStyle);
                cur.x += cur.width;

                cur.width = outRect.width * 0.5f;
                toggle.Style.fontSize = PV.Font(fontSizePV);
                toggle.Style.alignment = TextAnchor.MiddleLeft;
                toggle.Style.normal.textColor = toggleColor(toggle.Value);
                toggle.Style.hover.textColor = toggleColor(toggle.Value);
                toggle.Content.text = toggleText(toggle.Value);
                cur.y -= PV.PropPx(2);
                toggle.Draw(cur);
            }

            public override void Resize() { Resize(false); }
            public override void Resize(bool broadCast) { rect.height = PV.Line(lineHeightPV); }

            private Color toggleColor(bool b) { return b ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0.2f, 0.2f, 1f); }
            private string toggleText(bool b) { return b ? "Enabled" : "Disabled"; }
        }

        private class YotogiButtonGrid : Element
        {
            private string[] buttonNames;
            private GUIStyle toggleStyle = new GUIStyle("toggle");
            private GUIStyle buttonStyle = new GUIStyle("button");
            private string lineHeightPV = "C1";
            private string fontSizePV = "C1";
            private int viewRow = 6;
            private int columns = 2;
            private int spacerPx = 5;
            private int rowPerSpacer = 3;
            private int colPerSpacer = -1;
            private bool tabEnabled = false;
            private int tabSelected = -1;
            private Vector2 scrollViewVector = Vector2.zero;
            private SelectButton[] selectButton;

            public bool GirdToggle = false;
            public string GirdLabelText = "";

            public event EventHandler<ButtonEventArgs> OnClick;

            readonly string[] cheekBlendOptions = { "頬０", "頬１", "頬２", "頬３" };
            readonly string[] tearBlendOptions = { "涙０", "涙１", "涙２", "涙３" };

            public YotogiButtonGrid(string name, string[] buttonNames, EventHandler<ButtonEventArgs> _onClick, int row, bool tabEnabled)
            : base(name, new Rect(Window.AutoLayout, Window.AutoLayout, Window.AutoLayout, 0))
            {
                this.buttonNames = buttonNames;
                this.OnClick += _onClick;
                this.viewRow = row;
                this.tabEnabled = tabEnabled;


                if (tabEnabled)
                {
                    selectButton = new SelectButton[2]
                        { new SelectButton("SelectButton:Cheek", rect, cheekBlendOptions, this.OnSelectButtonFaceBlend),
                          new SelectButton("SelectButton:Tear",  rect, tearBlendOptions, this.OnSelectButtonFaceBlend)};
                    onChangeTab(0);
                }

                Resize();
            }

            public override void Draw(Rect outRect)
            {
                int spacer = PV.PropPx(spacerPx);
                int btnNum = buttonNames.Length;
                int tabLine = PV.Line(lineHeightPV) + PV.PropPx(3);
                int rowNum = (int)Math.Ceiling((double)btnNum / columns);

                GUI.BeginGroup(outRect);
                {
                    Rect cur = new Rect(0, 0, outRect.width, PV.Line(lineHeightPV));

                    if (tabEnabled)
                    {
                        cur.width = outRect.width * 0.3f;
                        toggleStyle.fontSize = PV.Font(fontSizePV);
                        toggleStyle.alignment = TextAnchor.MiddleLeft;
                        toggleStyle.normal.textColor = toggleColor(GirdToggle);
                        toggleStyle.hover.textColor = toggleColor(GirdToggle);
                        onClickDroolToggle(GUI.Toggle(cur, GirdToggle, "よだれ", toggleStyle));
                        cur.x += cur.width;

                        cur.width = outRect.width * 0.7f;
                        onChangeTab(GUI.Toolbar(cur, tabSelected, new string[2] { "頬・涙・涎", "全種Face" }, buttonStyle));

                        cur.x = 0f;
                        cur.y += cur.height + PV.PropPx(3);
                        cur.width = outRect.width;
                    }

                    if (!tabEnabled || tabSelected == 1)
                    {
                        Rect scrlRect = new Rect(cur.x, cur.y, cur.width, outRect.height - (tabEnabled ? tabLine : 0));
                        Rect contentRect = new Rect(0f, 0f, outRect.width - PV.Sys_("HScrollBar.Width") - spacer,
                                                    PV.Line(lineHeightPV) * rowNum + spacer * (int)(rowNum / rowPerSpacer));

                        scrollViewVector = GUI.BeginScrollView(scrlRect, scrollViewVector, contentRect, false, true);
                        {
                            Rect scrlCur = new Rect(0, 0, contentRect.width / columns, PV.Line(lineHeightPV));
                            int row = 1, col = 1;

                            foreach (string buttonName in buttonNames)
                            { 
                                onClick(GUI.Button(scrlCur, buttonName), buttonName);

                                if (columns > 0 && col == columns)
                                {
                                    scrlCur.x = 0;
                                    scrlCur.y += scrlCur.height;
                                    if (rowPerSpacer > 0 && row % rowPerSpacer == 0) scrlCur.y += spacer;
                                    row++;
                                    col = 1;
                                }
                                else
                                {
                                    scrlCur.x += scrlCur.width;
                                    if (colPerSpacer > 0 && col % colPerSpacer == 0) scrlCur.x += spacer;
                                    col++;
                                }
                            }
                        }
                        GUI.EndScrollView();

                    }
                    else if (tabSelected == 0)
                    {
                        selectButton[0].Draw(cur);
                        cur.y += cur.height;
                        selectButton[1].Draw(cur);
                    }

                }
                GUI.EndGroup();
            }

            public override void Resize() { Resize(false); }
            public override void Resize(bool broadCast)
            {
                int spacer = PV.PropPx(spacerPx);
                int tabLine = PV.Line(lineHeightPV) + PV.PropPx(3);

                if (!tabEnabled) rect.height = PV.Line(lineHeightPV) * viewRow + spacer * (int)(viewRow / rowPerSpacer);
                else if (tabSelected == 0) rect.height = tabLine + PV.Line(lineHeightPV) * 2;
                else if (tabSelected == 1) rect.height = tabLine + PV.Line(lineHeightPV) * viewRow + spacer * (int)(viewRow / rowPerSpacer);

                if (!broadCast) notifyParent(true, false);
            }

            string SelectedFaceBlend
            {
                get
                {
                    var cheekName = cheekBlendOptions[selectButton[0].SelectedIndex];
                    var tearName = tearBlendOptions[selectButton[1].SelectedIndex];

                    var faceName = $"{cheekName}{tearName}";
                    if (GirdToggle) faceName += "よだれ";
                    return faceName;
                }
            }

            public void OnSelectButtonFaceBlend(object sb, SelectEventArgs args)
            {
                if (((YotogiPanel)Parent).Enabled)
                {
                    LogDebug($"FaceBlend: {this.SelectedFaceBlend}");
                    OnClick(this, new ButtonEventArgs(this.name, this.SelectedFaceBlend));
                }
            }

            private void onClickDroolToggle(bool b)
            {
                if (b != GirdToggle)
                {
                    GirdToggle = b;
                    LogDebug($"FaceBlend: {this.SelectedFaceBlend}");
                    OnClick(this, new ButtonEventArgs(this.name, this.SelectedFaceBlend));
                }
            }

            private void onChangeTab(int i)
            {
                if (i != tabSelected)
                {
                    tabSelected = i;
                    Resize();
                }
            }

            private void onClick(bool click, string s)
            {
                if (click)
                {
                    LogDebug($"FaceAnime: {s}");
                    OnClick(this, new ButtonEventArgs(this.name, s));
                }
            }

            private Color toggleColor(bool b) { return b ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0.2f, 0.2f, 1f); }
        }

        private class YotogiLineSelect : Element
        {
            private string[] names;
            private int currentIndex = 0;
            private GUIStyle labelStyle = new GUIStyle("label");
            private GUIStyle buttonStyle = new GUIStyle("button");
            private string heightPV = "C1";
            private string fontSizePV = "C1";

            public int CurrentIndex { get { return currentIndex; } }
            public string CurrentName { get { return names[currentIndex]; } }

            public event EventHandler<ButtonEventArgs> OnClick;

            public YotogiLineSelect(string name, string _label, string[] _names, int def, EventHandler<ButtonEventArgs> _onClick)
            : base(name, new Rect(Window.AutoLayout, Window.AutoLayout, Window.AutoLayout, 0))
            {
                this.names = new string[_names.Length];
                Array.Copy(_names, this.names, _names.Length);
                this.currentIndex = def;
                this.OnClick += _onClick;

                Resize();
            }

            public override void Draw(Rect outRect)
            {
                Rect cur = outRect;
                int fontSize = PV.Font(fontSizePV);

                /*cur.width = outRect.width * 0.3f;
                labelStyle           = "label";
                labelStyle.fontSize  = PV.Font(fontSizePV);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(cur, label, labelStyle);
                cur.x += cur.width;*/

                cur.width = outRect.width * 0.125f;
                buttonStyle.fontSize = PV.Font(fontSizePV);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                onClick(GUI.Button(cur, "<"), -1);
                cur.x += cur.width + outRect.width * 0.025f;

                cur.width = outRect.width * 0.7f;
                labelStyle = "box";
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(cur, names[currentIndex], labelStyle);
                cur.x += cur.width + outRect.width * 0.025f;

                cur.width = outRect.width * 0.125f;
                buttonStyle.fontSize = PV.Font(fontSizePV);
                buttonStyle.alignment = TextAnchor.MiddleCenter;
                onClick(GUI.Button(cur, ">"), 1);

            }

            public override void Resize(bool bc)
            {
                this.rect.height = PV.Line(heightPV);
                if (!bc) notifyParent(true, false);
            }

            private void onClick(bool click, int di)
            {
                if (click)
                {
                    if ((di < 0 && currentIndex > 0) || (di > 0 && currentIndex < names.Length - 1))
                    {
                        currentIndex += di;
                        OnClick(this, new ButtonEventArgs(this.name, names[currentIndex]));
                    }
                }
            }
        }

        private class PlayAnime
        {
            public enum Formula
            {
                Linear,
                Quadratic,
                Convulsion
            }

            private float[] value;
            private float[] vFrom;
            private float[] vTo;
            private Formula type;
            private int num;
            private bool play = false;
            private float passedTime = 0f;
            private float startTime = 0f;
            private float finishTime = 0f;
            //private float[] actionTime;
            public float progress { get { return (passedTime - startTime) / (finishTime - startTime); } }

            private Action<float> setValue0 = null;
            private Action<float[]> setValue = null;

            public string Name;
            public string Key { get { return (Name.Split('.'))[0]; } }
            public bool NowPlaying { get { return play && (passedTime < finishTime); } }
            public bool SetterExist { get { return (num == 1) ? !IsNull(setValue0) : !IsNull(setValue); } }


            public PlayAnime(string name, int n, float st, float ft) : this(name, n, st, ft, Formula.Linear) { }
            public PlayAnime(string name, int n, float st, float ft, Formula t)
            {
                Name = name;
                num = n;
                value = new float[n];
                vFrom = new float[n];
                vTo = new float[n];
                startTime = st;
                finishTime = ft;
                type = t;
            }

            public bool IsKye(string s) { return s == Key; }
            public bool Contains(string s) { return Name.Contains(s); }

            public void SetFrom(float vform) { vFrom[0] = vform; }
            public void SetTo(float vto) { vTo[0] = vto; }
            public void SetSetter(Action<float> func) { setValue0 = func; }
            public void Set(float vform, float vto) { SetFrom(vform); SetTo(vto); }

            public void SetFrom(float[] vform) { if (vform.Length == num) Array.Copy(vform, vFrom, num); }
            public void SetTo(float[] vto) { if (vto.Length == num) Array.Copy(vto, vTo, num); }
            public void SetSetter(Action<float[]> func) { setValue = func; }
            public void Set(float[] vform, float[] vto) { SetFrom(vform); SetTo(vto); }

            public void Play()
            {
                if (SetterExist)
                {
                    passedTime = 0f;
                    play = true;
                }
            }
            public void Play(float vform, float vto) { Set(vform, vto); Play(); }
            public void Play(float[] vform, float[] vto) { Set(vform, vto); Play(); }

            public void Stop() { play = false; }

            public void Update()
            {
                if (play)
                {
                    bool change = false;

                    for (int i = 0; i < num; i++)
                    {
                        if (vFrom[i] == vTo[i]) continue;

                        if (passedTime >= finishTime)
                        {
                            Stop();
                        }
                        else if (passedTime >= startTime)
                        {
                            switch (type)
                            {
                                case Formula.Linear:
                                    {
                                        value[i] = vFrom[i] + (vTo[i] - vFrom[i]) * progress;
                                        change = true;
                                    }
                                    break;

                                case Formula.Quadratic:
                                    {
                                        value[i] = vFrom[i] + (vTo[i] - vFrom[i]) * Mathf.Pow(progress, 2);
                                        change = true;
                                    }
                                    break;

                                case Formula.Convulsion:
                                    {
                                        float t = Mathf.Pow(progress + 0.05f * UnityEngine.Random.value, 2f) * 2f * Mathf.PI * 6f;

                                        value[i] = (vTo[i] - vFrom[i])
                                                * Mathf.Clamp(Mathf.Clamp(Mathf.Pow((Mathf.Cos(t - Mathf.PI / 2f) + 1f) / 2f, 3f) * Mathf.Pow(1f - progress, 2f) * 4f, 0f, 1f)
                                                                + Mathf.Sin(t * 3f) * 0.1f * Mathf.Pow(1f - progress, 3f), 0f, 1f);

                                        if (progress < 0.03f) value[i] *= Mathf.Pow(1f - (0.03f - progress) * 33f, 2f);
                                        change = true;

                                    }
                                    break;

                                default: break;
                            }

                            //LogError("PlayAnime["+Name+"].Update : {0}", value[i]);
                        }
                    }

                    if (change)
                    {
                        if (num == 1) setValue0(value[0]);
                        else setValue(value);
                    }
                }

                passedTime += Time.deltaTime;
            }
        }

#endregion

#region MonoBehaviour methods
        
        internal static AddYotogiSliderSE Instance
        {
            get;
            private set;
        }

        public void Awake()
        {
            if(Instance != null)
            {
                throw new Exception("Already initialized");
            }

            Instance = this;

            try
            {
                DontDestroyOnLoad(this);

                pa["WIN.Load"] = new PlayAnime("WIN.Load", 2, 0.00f, 0.25f, PlayAnime.Formula.Quadratic);
                pa["AHE.継続.0"] = new PlayAnime("AHE.継続.0", 1, 0.00f, 0.75f);
                pa["AHE.絶頂.0"] = new PlayAnime("AHE.絶頂.0", 2, 6.00f, 9.00f);
                pa["AHE.痙攣.0"] = new PlayAnime("AHE.痙攣.0", 1, 0.00f, 9.00f, PlayAnime.Formula.Convulsion);
                pa["AHE.痙攣.1"] = new PlayAnime("AHE.痙攣.1", 1, 0.00f, 10.00f, PlayAnime.Formula.Convulsion);
                pa["AHE.痙攣.2"] = new PlayAnime("AHE.痙攣.2", 1, 0.00f, 11.00f, PlayAnime.Formula.Convulsion);
                pa["BOTE.絶頂"] = new PlayAnime("BOTE.絶頂", 1, 0.00f, 6.00f);
                pa["BOTE.止める"] = new PlayAnime("BOTE.止める", 1, 0.00f, 4.00f);
                pa["BOTE.流れ出る"] = new PlayAnime("BOTE.流れ出る", 1, 0.00f, 20.00f);
                pa["KUPA.挿入.0"] = new PlayAnime("KUPA.挿入.0", 1, 0.50f, 1.50f);
                pa["KUPA.挿入.1"] = new PlayAnime("KUPA.挿入.1", 1, 1.50f, 2.50f);
                pa["KUPA.止める"] = new PlayAnime("KUPA.止める", 1, 0.00f, 2.00f);
                pa["AKPA.挿入.0"] = new PlayAnime("AKPA.挿入.0", 1, 0.50f, 1.50f);
                pa["AKPA.挿入.1"] = new PlayAnime("AKPA.挿入.1", 1, 1.50f, 2.50f);
                pa["AKPA.止める"] = new PlayAnime("AKPA.止める", 1, 0.00f, 2.00f);
                pa["KUPACL.剥く.0"] = new PlayAnime("KUPACL.剥く.0", 1, 0.00f, 0.30f);
                pa["KUPACL.剥く.1"] = new PlayAnime("KUPACL.剥く.1", 1, 0.20f, 0.60f);
                pa["KUPACL.被る"] = new PlayAnime("KUPACL.被る", 1, 0.00f, 0.40f);

                LogDebug(string.Format("Starting {0} v{1}", AddYotogiSliderSE.PluginName, AddYotogiSliderSE.Version));
            }
            catch (Exception e)
            {
                LogError(e);
                Destroy(this);
            }

            var harmony = new Harmony(Uuid);
            harmony.PatchAll(typeof(AddYotogiSliderSE));
            InOutAnimationHook.Init();

            OldConfigCheck();

            ToggleKey = Config.Bind("General", "Toggle shortcut", new KeyboardShortcut(KeyCode.F5));
        }

        public void Start()
        {
            LogInfo("Plugin started");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WfScreenManager), "RunScreen")]
        public static void YotogiManager_RunScreen(string screen_name, object __instance)
        {
            if (__instance is YotogiManager || __instance is YotogiOldManager)
            {
                LogDebug($"YotogiManager_RunScreen({screen_name})");
                Instance.OnRunScreen(screen_name);

            }
        }

        void OnRunScreen(string screen_name)
        {
            try
            {
                switch (screen_name)
                {
                    case "Play":
                        {
                            Instance.initOnStartSkill();
                        }
                        break;
                    case "Null":
                        {
                            Instance.preFinalize();
                        }
                        break;

                }

            }
            catch(Exception e)
            {
                Logger.LogError(e);
            }
        }


        //Overwrite for Level that is loaded.
        //Level 14 is for Yotogi in Normal Mode
        //Level 63 is for Yotogi in Compatibility Mode
        public void OnLevelWasLoaded(int level)
        {
            fPassedTimeOnLevel = 0f;

            LogInfo("Current Level Loaded is " + level.ToString());

            bNormalYotogiScene = (level == 14);
            bCompatibilityYotogiScene = (level == 63);

            sceneLevel = level;

            SybarisCheck();
            CheckIsInOutAnimationActive();
        }

        public void Update()
        {
            // We only want to check inout once per frame
            // so we check it here and set cached value since the original check
            // uses reflection and may be expensive
            CheckIsInOutAnimationActive();

#if DEBUG
            if (Input.GetKeyDown(_toggleKey))
            {
                LogDebug("Loading Inits v" + AddYotogiSliderSE.Version);
                LogDebug("Scene Level " + sceneLevel.ToString());
                LogDebug("canStart " + canStart.ToString());
                LogDebug("bInitCompleted " + bInitCompleted.ToString());
                LogDebug("bLoadBoneAnimetion " + bLoadBoneAnimetion.ToString());

                showMaidStatus();

                LogDebug($"bNormalYotogiScene: {bNormalYotogiScene}");
                LogDebug($"bCompatibilityYotogiScene: {bCompatibilityYotogiScene}");
                LogDebug($"canStart: {canStart}");
                LogDebug($"visible: {visible}");
                LogDebug($"NowPlaying: {pa["WIN.Load"].NowPlaying}");
                LogDebug($"maid: {maid}");
            }

#endif
            fPassedTimeOnLevel += Time.deltaTime;

            if (bInitCompleted)
            {
                if (bNormalYotogiScene || bCompatibilityYotogiScene)
                {
                    if (canStart)
                    {
                        if (ToggleKey.Value.IsDown())
                        {
                            winAnimeRect = window.Rectangle;
                            visible = !visible;
                            playAnimeOnInputKeyDown();
                        }

                        if (fPassedTimeOnCommand >= 0f) fPassedTimeOnCommand += Time.deltaTime;

                        updateAnimeOnUpdate();
                    }
                }
                else
                {
                    finalize();
                }
                
            }
        }

        //Initialising GUI
        public void OnGUI()
        {
            if ((bNormalYotogiScene || bCompatibilityYotogiScene) && canStart)
            {
                updateAnimeOnGUI();

                if (visible && !pa["WIN.Load"].NowPlaying)
                {
                    updateCameraControl();
                    window.Draw();
                }
            }
        }

#endregion

#region Callbacks

        public void OnYotogiPlayManagerOnClickCommand(Skill.Data.Command.Data command_data)
        {
            iLastExcite = maid.status.currentExcite;
            //fLastSliderSensitivity = slider["Sensitivity"].Value;
            //iLastSliderFrustration = getSliderFrustration();
            fPassedTimeOnCommand = 0f;

            //if (panel["Status"].Enabled) updateMaidFrustration(iLastSliderFrustration);
            initAnimeOnCommand();

            orgOnClickCommand(command_data);

            if (currentCommandData == null)
            {
                currentCommandData = new CommonCommandData(command_data);
            }
            else
            {
                currentCommandData.SetData(command_data);
            }

            afterYotogiPlayManagerOnClickCommand(currentCommandData);
        }

        public void OnYotogiOldPlayManagerOnClickCommand(Skill.Old.Data.Command.Data command_data)
        {
            iLastExcite = maid.status.currentExcite;
            //fLastSliderSensitivity = slider["Sensitivity"].Value;
            //iLastSliderFrustration = getSliderFrustration();
            fPassedTimeOnCommand = 0f;

            //if (panel["Status"].Enabled) updateMaidFrustration(iLastSliderFrustration);
            initAnimeOnCommand();

            orgOnClickCommandOld(command_data);

            if (currentCommandData == null)
            {
                currentCommandData = new CommonCommandData(command_data);
            }
            else
            {
                currentCommandData.SetData(command_data);
            }

            afterYotogiPlayManagerOnClickCommand(currentCommandData);
        }

        public bool OnYotogiKagManagerTagFace(KagTagSupport tag_data)
        {
            if (panel["FaceAnime"].Enabled || pa["AHE.絶頂.0"].NowPlaying)
            {
                return false;
            }
            else
            {
                panel["FaceAnime"].HeaderUILabelText = tag_data.GetTagProperty("name").AsString();
                return orgTagFace(tag_data);
            }
        }

        public bool OnYotogiKagManagerTagFaceBlend(KagTagSupport tag_data)
        {
            if (panel["FaceBlend"].Enabled || pa["AHE.絶頂.0"].NowPlaying)
            {
                return false;
            }
            else
            {
                panel["FaceBlend"].HeaderUILabelText = tag_data.GetTagProperty("name").AsString();
                return orgTagFaceBlend(tag_data);
            }
        }

        public void OnChangeSliderExcite(object ys, SliderEventArgs args)
        {
            if (panel["Status"].Enabled) updateMaidExcite((int)args.Value);
        }

        public void OnChangeSliderMind(object ys, SliderEventArgs args)
        {
            if (panel["Status"].Enabled) updateMaidMind((int)args.Value);
        }

        public void OnChangeSliderReason(object ys, SliderEventArgs args)
        {
            if (panel["Status"].Enabled) updateMaidReason((int)args.Value);
        }

        public void OnChangeSliderSensual(object ys, SliderEventArgs args)
        {
            if (panel["Status"].Enabled) updateMaidSensual((int)args.Value);
        }

//        public void OnChangeSliderSensitivity(object ys, SliderEventArgs args)
//        {
//            ;
//        }

        public void OnChangeSliderMotionSpeed(object ys, SliderEventArgs args)
        {
            if (panel["Status"].Enabled) updateMotionSpeed(args.Value);
        }

        public void OnChangeSliderEyeY(object ys, SliderEventArgs args)
        {
            updateMaidEyePosY(args.Value);
        }

        public void OnChangeSliderChikubiScale(object ys, SliderEventArgs args)
        {
            float value = slider["ChikubiBokki"].Value * slider["ChikubiScale"].Value / 100;
            updateShapeKeyChikubiBokkiValue(value);
            setExIni("AutoTUN", "ChikubiScale", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderChikubiNae(object ys, SliderEventArgs args)
        {
            updateChikubiNaeValue(args.Value);
            setExIni("AutoTUN", "ChikubiNae", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderChikubiBokki(object ys, SliderEventArgs args)
        {
            float value = args.Value * slider["ChikubiScale"].Value / 100;
            updateShapeKeyChikubiBokkiValue(value);
        }

        public void OnChangeSliderChikubiTare(object ys, SliderEventArgs args)
        {
            updateShapeKeyChikubiTareValue(args.Value);
            setExIni("AutoTUN", "ChikubiTare", args.Value);
            SaveConfig();
        }

        public void OnChangeToggleSlowCreampie(object tgl, ToggleEventArgs args)
        {
            setExIni("AutoBOTE", "SlowCreampie", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderHara(object ys, SliderEventArgs args)
        {
            updateMaidHaraValue(args.Value);
        }

        public void OnChangeSliderKupa(object ys, SliderEventArgs args)
        {
            updateShapeKeyKupaValue(args.Value);
        }

        public void OnChangeSliderAnalKupa(object ys, SliderEventArgs args)
        {
            updateShapeKeyAnalKupaValue(args.Value);
        }

        public void OnChangeSliderKupaLevel(object ys, SliderEventArgs args)
        {
            setExIni("AutoKUPA", "KupaLevel", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderLabiaKupa(object ys, SliderEventArgs args)
        {
            updateShapeKeyLabiaKupaValue(args.Value);
            setExIni("AutoKUPA", "LabiaKupa", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderVaginaKupa(object ys, SliderEventArgs args)
        {
            updateShapeKeyVaginaKupaValue(args.Value);
            setExIni("AutoKUPA", "VaginaKupa", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderNyodoKupa(object ys, SliderEventArgs args)
        {
            updateShapeKeyNyodoKupaValue(args.Value);
            setExIni("AutoKUPA", "NyodoKupa", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderSuji(object ys, SliderEventArgs args)
        {
            updateShapeKeySujiValue(args.Value);
            setExIni("AutoKUPA", "Suji", args.Value);
            SaveConfig();
        }

        public void OnChangeSliderClitoris(object ys, SliderEventArgs args)
        {
            updateShapeKeyClitorisValue(args.Value);
        }

        public void OnChangeToggleLipsync(object tgl, ToggleEventArgs args)
        {
            updateMaidFoceKuchipakuSelfUpdateTime(args.Value);
        }

        public void OnChangeToggleConvulsion(object tgl, ToggleEventArgs args)
        {
            setExIni("AutoAHE", "ConvulsionEnabled", args.Value);
            SaveConfig();
        }

        public void OnChangeEnabledAutoAHE(object panel, ToggleEventArgs args)
        {
            setExIni("AutoAHE", "Enabled", args.Value);
            SaveConfig();
        }

        public void OnChangeEnabledAutoTUN(object panel, ToggleEventArgs args)
        {
            setExIni("AutoTUN", "Enabled", args.Value);
            SaveConfig();
        }

        public void OnChangeEnabledAutoBOTE(object panel, ToggleEventArgs args)
        {
            setExIni("AutoBOTE", "Enabled", args.Value);
            SaveConfig();
        }

        public void OnChangeEnabledAutoKUPA(object panel, ToggleEventArgs args)
        {
            setExIni("AutoKUPA", "Enabled", args.Value);
            SaveConfig();
        }

        public void OnClickButtonFaceAnime(object ygb, ButtonEventArgs args)
        {
            if (panel["FaceAnime"].Enabled)
            {
                maid.FaceAnime(args.ButtonName, 1f, 0);
                panel["FaceAnime"].HeaderUILabelText = args.ButtonName;
            }
        }

        public void OnClickButtonFaceBlend(object ysg, ButtonEventArgs args)
        {
            if (panel["FaceBlend"].Enabled)
            {
                maid.FaceBlend(args.ButtonName);
                panel["FaceBlend"].HeaderUILabelText = args.ButtonName;
            }
        }

        public void OnClickButtonStageSelect(object ysg, ButtonEventArgs args)
        {
            GameMain.Instance.BgMgr.ChangeBg(args.ButtonName);
        }

#endregion

#region Private methods
        private bool initPlugin()
        {
            this.maid = GameMain.Instance.CharacterMgr.GetMaid(0);

            this.maidFoceKuchipakuSelfUpdateTime = getFieldInfo<Maid>("m_bFoceKuchipakuSelfUpdateTime");
            if (IsNull(this.maidFoceKuchipakuSelfUpdateTime)) return false;

            if (this.yotogiParamBasicBarDelegator == null) this.yotogiParamBasicBarDelegator = new YorogiParamBasicBarDelegator();
            if (bNormalYotogiScene)
            {
                this.yotogiParamBasicBarDelegator.SetParamBasicBar(getInstance<YotogiParamBasicBar>());
            }
            else if (bCompatibilityYotogiScene)
            {
                this.yotogiParamBasicBarDelegator.SetParamBasicBar(getInstance<YotogiOldParamBasicBar>());
            }
            if (!this.yotogiParamBasicBarDelegator.IsReady) return false;

            // 夜伽コマンドフック Loading Kiss Object Instance so we can access them
            {
                if (bNormalYotogiScene)
                {
                    this.yotogiPlayManager = getInstance<YotogiPlayManager>();
                    if (!this.yotogiPlayManager) return false;

                    this.playManagerAsWfScreenChildren = this.yotogiPlayManager;

                    YotogiCommandFactory cf = getFieldValue<YotogiPlayManager, YotogiCommandFactory>(this.yotogiPlayManager, "command_factory_");
                    if (IsNull(cf)) return false;

                    try
                    {
                        cf.SetCommandCallback(new YotogiCommandFactory.CommandCallback(this.OnYotogiPlayManagerOnClickCommand));
                    }
                    catch (Exception ex) { LogError("SetCommandCallback() : {0}", ex); return false; }

                    this.orgOnClickCommand = getMethodDelegate<YotogiPlayManager, Action<Skill.Data.Command.Data>>(this.yotogiPlayManager, "OnClickCommand");
                    if (IsNull(this.orgOnClickCommand)) return false;
                }
                else
                {
                    this.yotogiOldPlayManager = getInstance<YotogiOldPlayManager>();
                    if (!this.yotogiOldPlayManager) return false;

                    this.playManagerAsWfScreenChildren = this.yotogiOldPlayManager;

                    YotogiOldCommandFactory cf = getFieldValue<YotogiOldPlayManager, YotogiOldCommandFactory>(this.yotogiOldPlayManager, "command_factory_");
                    if (IsNull(cf)) return false;

                    try
                    {
                        cf.SetCommandCallback(new YotogiOldCommandFactory.CommandCallback(this.OnYotogiOldPlayManagerOnClickCommand));
                    }
                    catch (Exception ex) { LogError("SetCommandCallback() : {0}", ex); return false; }

                    this.orgOnClickCommandOld = getMethodDelegate<YotogiOldPlayManager, Action<Skill.Old.Data.Command.Data>>(this.yotogiOldPlayManager, "OnClickCommand");
                    if (IsNull(this.orgOnClickCommandOld)) return false;
                }
            }

            // Face・FaceBlendフック
            {
                YotogiKagManager ykm = GameMain.Instance.ScriptMgr.yotogi_kag;
                if (IsNull(ykm)) return false;

                this.kagScript = getFieldValue<YotogiKagManager, KagScript>(ykm, "kag_");
                if (IsNull(this.kagScript)) return false;

                try
                {
                    this.kagScript.RemoveTagCallBack("face");
                    this.kagScript.AddTagCallBack("face", new KagScript.KagTagCallBack(this.OnYotogiKagManagerTagFace));
                    this.kagScript.RemoveTagCallBack("faceblend");
                    this.kagScript.AddTagCallBack("faceblend", new KagScript.KagTagCallBack(this.OnYotogiKagManagerTagFaceBlend));
                    kagScriptCallbacksOverride = true;
                }
                catch (Exception ex) { LogError("kagScriptCallBack() : {0}", ex); return false; }

                this.orgTagFace = getMethodDelegate<YotogiKagManager, Func<KagTagSupport, bool>>(ykm, "TagFace");
                this.orgTagFaceBlend = getMethodDelegate<YotogiKagManager, Func<KagTagSupport, bool>>(ykm, "TagFaceBlend");
                if (IsNull(this.orgTagFace)) return false;
            }

            // ステージリスト取得
            if (sStageNames.Count() == 0)
            {
                List<string> stagesNormal = new List<string>();
                PhotoBGData.Create();
                Dictionary<string, List<KeyValuePair<string, object>>> dictionary = new Dictionary<string, List<KeyValuePair<string, object>>>();
                foreach (KeyValuePair<string, List<PhotoBGData>> current in PhotoBGData.category_list)
                {
                    for (int i = 0; i < current.Value.Count; i++)
                    {
                        stagesNormal.Add(current.Value[i].create_prefab_name);
                    }
                }
                List<string> stagesOld = CompatibilityModeStage.GetStageList();
                sStageNames = stagesNormal.Union(stagesOld).ToList();
            }

            // PlayAnime
            {
                foreach (KeyValuePair<string, PlayAnime> o in pa)
                {
                    PlayAnime p = o.Value;
                    if (!p.SetterExist)
                    {
                        if (p.Contains("WIN")) p.SetSetter(updateWindowAnime);
                        if (p.Contains("BOTE")) p.SetSetter(updateMaidHaraValue);
                        if (p.Contains("KUPA")) p.SetSetter(updateShapeKeyKupaValue);
                        if (p.Contains("AKPA")) p.SetSetter(updateShapeKeyAnalKupaValue);
                        if (p.Contains("KUPACL")) p.SetSetter(updateShapeKeyClitorisValue);
                        if (p.Contains("AHE")) p.SetSetter(updateOrgasmConvulsion);

                        if (p.Contains("AHE.継続")) p.SetSetter(updateMaidEyePosY);
                        if (p.Contains("AHE.絶頂")) p.SetSetter(updateAheOrgasm);

                    }
                }
                fAheDefEye = maid.body0.trsEyeL.localPosition.y * fEyePosToSliderMul;
                iDefHara = maid.GetProp("Hara").value;
                iBoteCount = 0;
                iOrgasmCount = 0;
                iAheOrgasmChain = 0;
            }

            // BodyShapeKeyCheck
            bKupaAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("kupa");
            bOrgasmAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("orgasm");
            bAnalKupaAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("analkupa");
            bLabiaKupaAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("labiakupa");
            bVaginaKupaAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("vaginakupa");
            bNyodoKupaAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("nyodokupa");
            bSujiAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("suji");
            bClitorisAvailable = maid.body0.GetGoSlot(0).morph.hash.ContainsKey("clitoris");

            // BokkiChikubi ShapeKeyCheck
            bBokkiChikubiAvailable = this.isExistVertexMorph(maid.body0, "chikubi_bokki");

            // Window
            {
                window = new Window(winRatioRect, AddYotogiSliderSE.Version, "Yotogi Slider");

                float excite = (float)maid.status.currentExcite;
                float mind = (float)maid.status.currentMind;
                float reason = (float)maid.status.currentReason;
                float sensual = (float)maid.status.currentSensual;

                string sStagePrefab = "";
                if (bNormalYotogiScene)
                {
                    int i = (YotogiStageSelectManager.SelectedStageRefDayTime) ? 0 : 1;
                    sStagePrefab = YotogiStageSelectManager.SelectedStage.prefabName[i];
                }
                else if (bCompatibilityYotogiScene)
                {
                    sStagePrefab = YotogiOldStageSelectManager.StagePrefab;
                }
                int stageIndex = sStageNames.FindIndex(s => String.Equals(s, sStagePrefab, StringComparison.OrdinalIgnoreCase));

                slider["Excite"] = new YotogiSlider("Slider:Excite", -100f, 300f, excite, this.OnChangeSliderExcite, sliderName[0], true);
                slider["Mind"] = new YotogiSlider("Slider:Mind", 0f, mind, mind, this.OnChangeSliderMind, sliderName[1], true);
                if (bNormalYotogiScene)
                {
                    slider["Sensual"] = new YotogiSlider("Slider:Sensual", 0f, 300f, sensual, this.OnChangeSliderSensual, sliderName[2], true);
                }
                else if (bCompatibilityYotogiScene)
                {
                    slider["Reason"] = new YotogiSlider("Slider:Reason", 0f, reason, reason, this.OnChangeSliderReason, sliderName[3], true);
                    //slider["Sensitivity"] = new YotogiSlider("Slider:Sensitivity", -100f, 200f, sensitivity, this.OnChangeSliderSensitivity, sliderName[4], true);
                }
                slider["MotionSpeed"] = new YotogiSlider("Slider:MotionSpeed", 0f, 500f, 100f, this.OnChangeSliderMotionSpeed, sliderName[5], true);
                slider["EyeY"] = new YotogiSlider("Slider:EyeY", fAheDefEye, 100f, fAheDefEye, this.OnChangeSliderEyeY, sliderNameAutoAHE[0], false);

                slider["ChikubiScale"] = new YotogiSlider("Slider:ChikubiScale", 1f, 100f, fChikubiScale, this.OnChangeSliderChikubiScale, sliderNameAutoTUN[0], true);
                slider["ChikubiNae"] = new YotogiSlider("Slider:ChikubiNae", -15f, 150f, fChikubiNae, this.OnChangeSliderChikubiNae, sliderNameAutoTUN[1], true);
                slider["ChikubiBokki"] = new YotogiSlider("Slider:ChikubiBokki", -15f, 150f, fChikubiBokki, this.OnChangeSliderChikubiBokki, sliderNameAutoTUN[2], true);
                slider["ChikubiTare"] = new YotogiSlider("Slider:ChikubiTare", 0f, 150f, fChikubiTare, this.OnChangeSliderChikubiTare, sliderNameAutoTUN[3], true);

                toggle["SlowCreampie"] = new YotogiToggle("Toggle:SlowCreamPie", false, " Slow creampie", this.OnChangeToggleSlowCreampie);
                slider["Hara"] = new YotogiSlider("Slider:Hara", 0f, 150f, (float)iDefHara, this.OnChangeSliderHara, sliderNameAutoBOTE[0], false);

                slider["Kupa"] = new YotogiSlider("Slider:Kupa", 0f, 150f, 0f, this.OnChangeSliderKupa, sliderNameAutoKUPA[0], false);
                slider["AnalKupa"] = new YotogiSlider("Slider:AnalKupa", 0f, 150f, 0f, this.OnChangeSliderAnalKupa, sliderNameAutoKUPA[1], false);
                slider["KupaLevel"] = new YotogiSlider("Slider:KupaLevel", 0f, 100f, fKupaLevel, this.OnChangeSliderKupaLevel, sliderNameAutoKUPA[2], true);
                slider["LabiaKupa"] = new YotogiSlider("Slider:LabiaKupa", 0f, 150f, fLabiaKupa, this.OnChangeSliderLabiaKupa, sliderNameAutoKUPA[3], true);
                slider["VaginaKupa"] = new YotogiSlider("Slider:VaginaKupa", 0f, 150f, fVaginaKupa, this.OnChangeSliderVaginaKupa, sliderNameAutoKUPA[4], true);
                slider["NyodoKupa"] = new YotogiSlider("Slider:NyodoKupa", 0f, 150f, fNyodoKupa, this.OnChangeSliderNyodoKupa, sliderNameAutoKUPA[5], true);
                slider["Suji"] = new YotogiSlider("Slider:Suji", 0f, 150f, fSuji, this.OnChangeSliderSuji, sliderNameAutoKUPA[6], true);
                slider["Clitoris"] = new YotogiSlider("Slider:Clitoris", 0f, 150f, 0f, this.OnChangeSliderClitoris, sliderNameAutoKUPA[7], false);

                toggle["Lipsync"] = new YotogiToggle("Toggle:Lipsync", false, " Lipsync cancelling", this.OnChangeToggleLipsync);
                toggle["Convulsion"] = new YotogiToggle("Toggle:Convulsion", false, " Orgasm convulsion", this.OnChangeToggleConvulsion);

                grid["FaceAnime"] = new YotogiButtonGrid("GridButton:FaceAnime", sFaceNames, this.OnClickButtonFaceAnime, 6, false);
                grid["FaceBlend"] = new YotogiButtonGrid("GridButton:FaceBlend", sFaceNames, this.OnClickButtonFaceBlend, 6, true);

                lSelect["StageSelect"] = new YotogiLineSelect("LineSelect:StageSelect", "Stage : ", sStageNames.ToArray(), stageIndex, this.OnClickButtonStageSelect);

                slider["EyeY"].Visible = false;

                slider["ChikubiScale"].Visible = false;
                slider["ChikubiNae"].Visible = false;
                slider["ChikubiBokki"].Visible = false;
                slider["ChikubiTare"].Visible = false;

                toggle["SlowCreampie"].Visible = false;
                slider["Hara"].Visible = false;

                slider["Kupa"].Visible = false;
                slider["AnalKupa"].Visible = false;
                slider["KupaLevel"].Visible = false;
                slider["LabiaKupa"].Visible = false;
                slider["VaginaKupa"].Visible = false;
                slider["NyodoKupa"].Visible = false;
                slider["Suji"].Visible = false;
                slider["Clitoris"].Visible = false;

                toggle["Convulsion"].Visible = false;
                toggle["Lipsync"].Visible = false;
                grid["FaceAnime"].Visible = false;
                grid["FaceBlend"].Visible = false;

                if (stageIndex >= 0)
                {
                    window.AddChild(lSelect["StageSelect"]);
                    window.AddHorizontalSpacer();
                }

                panel["Status"] = window.AddChild<YotogiPanel>(new YotogiPanel("Panel:Status", "Status", YotogiPanel.HeaderUI.Slider));
                panel["Status"].AddChild(slider["Excite"]);
                panel["Status"].AddChild(slider["Mind"]);
                if (bNormalYotogiScene)
                {
                    panel["Status"].AddChild(slider["Sensual"]);
                }
                else if (bCompatibilityYotogiScene)
                {
                    panel["Status"].AddChild(slider["Reason"]);
                    //panel["Status"].AddChild(slider["Sensitivity"]);
                }
                panel["Status"].AddChild(slider["MotionSpeed"]);
                window.AddHorizontalSpacer();

                panel["AutoAHE"] = window.AddChild<YotogiPanel>(new YotogiPanel("Panel:AutoAHE", "AutoAHE", OnChangeEnabledAutoAHE));
                if (bOrgasmAvailable)
                {
                    panel["AutoAHE"].AddChild(toggle["Convulsion"]);
                }
                panel["AutoAHE"].AddChild(slider["EyeY"]);
                window.AddHorizontalSpacer();

                panel["AutoTUN"] = new YotogiPanel("Panel:AutoTUN", "AutoTUN", OnChangeEnabledAutoTUN);
                if (bBokkiChikubiAvailable)
                {
                    panel["AutoTUN"] = window.AddChild(panel["AutoTUN"]);
                    panel["AutoTUN"].AddChild(slider["ChikubiScale"]);
                    panel["AutoTUN"].AddChild(slider["ChikubiNae"]);
                    panel["AutoTUN"].AddChild(slider["ChikubiBokki"]);
                    panel["AutoTUN"].AddChild(slider["ChikubiTare"]);
                    window.AddHorizontalSpacer();
                }

                panel["AutoBOTE"] = window.AddChild<YotogiPanel>(new YotogiPanel("Panel:AutoBOTE", "AutoBOTE", OnChangeEnabledAutoBOTE));
                panel["AutoBOTE"].AddChild(toggle["SlowCreampie"]);
                panel["AutoBOTE"].AddChild(slider["Hara"]);
                window.AddHorizontalSpacer();

                panel["AutoKUPA"] = new YotogiPanel("Panel:AutoKUPA", "AutoKUPA", OnChangeEnabledAutoKUPA);
                if (bKupaAvailable || bAnalKupaAvailable)
                {
                    panel["AutoKUPA"] = window.AddChild(panel["AutoKUPA"]);
                    if (bKupaAvailable) panel["AutoKUPA"].AddChild(slider["Kupa"]);
                    if (bAnalKupaAvailable) panel["AutoKUPA"].AddChild(slider["AnalKupa"]);
                    if (bKupaAvailable || bAnalKupaAvailable) panel["AutoKUPA"].AddChild(slider["KupaLevel"]);
                    if (bLabiaKupaAvailable) panel["AutoKUPA"].AddChild(slider["LabiaKupa"]);
                    if (bVaginaKupaAvailable) panel["AutoKUPA"].AddChild(slider["VaginaKupa"]);
                    if (bNyodoKupaAvailable) panel["AutoKUPA"].AddChild(slider["NyodoKupa"]);
                    if (bSujiAvailable) panel["AutoKUPA"].AddChild(slider["Suji"]);
                    if (bClitorisAvailable) panel["AutoKUPA"].AddChild(slider["Clitoris"]);
                    window.AddHorizontalSpacer();
                }

                panel["FaceAnime"] = window.AddChild<YotogiPanel>(new YotogiPanel("Panel:FaceAnime", "FaceAnime", YotogiPanel.HeaderUI.Face));
                panel["FaceAnime"].AddChild(toggle["Lipsync"]);
                panel["FaceAnime"].AddChild(grid["FaceAnime"]);
                window.AddHorizontalSpacer();

                panel["FaceBlend"] = window.AddChild<YotogiPanel>(new YotogiPanel("Panel:FaceBlend", "FaceBlend", YotogiPanel.HeaderUI.Face));
                panel["FaceBlend"].AddChild(grid["FaceBlend"]);
            }

            // Preferences
            {
                clearExIniComments(); // ReloadConfigでコメントが追加されるので先にクリア
                ReloadConfig();

                panel["Status"].Enabled = parseExIni("Status", "EnableOnLoad", false);
                if (panel["Status"].Enabled)
                {
                    slider["Excite"].Value = Math.Max(Math.Min(parseExIni("Status", "ExciteValue", 0f), 300f), -100f);
                    slider["Excite"].Pin = parseExIni("Status", "ExcitePin", false);
                    slider["Mind"].Pin = parseExIni("Status", "MindPin", false);
                    if (bNormalYotogiScene)
                    {
                        if (slider["Sensual"].Value < 300f)
                        {
                            slider["Sensual"].Value = Math.Max(Math.Min(parseExIni("Status", "SensualValue", slider["Sensual"].Value), 300f), 0f);
                        }
                        slider["Sensual"].Pin = parseExIni("Status", "SensualPin", false);
                    }
                    else if (bCompatibilityYotogiScene)
                    {
                        slider["Reason"].Pin = parseExIni("Status", "ReasonPin", false);
                        //slider["Sensitivity"].Value = Math.Max(Math.Min(parseExIni("Status", "SensitivityValue", slider["Sensitivity"].Value), 200f), -100f);
                        //slider["Sensitivity"].Pin = parseExIni("Status", "SensitivityPin", false);
                    }
                    slider["MotionSpeed"].Pin = parseExIni("Status", "MotionSpeedPin", false);
                }

                panel["AutoAHE"].Enabled = parseExIni("AutoAHE", "Enabled", false);
                toggle["Convulsion"].Value = parseExIni("AutoAHE", "ConvulsionEnabled", false);
                fOrgasmsPerAheLevel = parseExIni("AutoAHE", "OrgasmsPerLevel", fOrgasmsPerAheLevel);
                fAheEyeDecrement = parseExIni("AutoAHE", "EyeDecrement", fAheEyeDecrement);
                for (int i = 0; i < 3; i++)
                {
                    iAheExcite[i] = parseExIni("AutoAHE", "ExciteThreshold_" + i, iAheExcite[i]);
                    fAheNormalEyeMax[i] = parseExIni("AutoAHE", "NormalEyeMax_" + i, fAheNormalEyeMax[i]);
                    fAheOrgasmEyeMax[i] = parseExIni("AutoAHE", "OrgasmEyeMax_" + i, fAheOrgasmEyeMax[i]);
                    fAheOrgasmEyeMin[i] = parseExIni("AutoAHE", "OrgasmEyeMin_" + i, fAheOrgasmEyeMin[i]);
                    fAheOrgasmSpeed[i] = parseExIni("AutoAHE", "OrgasmMotionSpeed_" + i, fAheOrgasmSpeed[i]);
                    fAheOrgasmConvulsion[i] = parseExIni("AutoAHE", "OrgasmConvulsion_" + i, fAheOrgasmConvulsion[i]);
                    sAheOrgasmFace[i] = parseExIni("AutoAHE", "OrgasmFace_" + i, sAheOrgasmFace[i]);
                    sAheOrgasmFaceBlend[i] = parseExIni("AutoAHE", "OrgasmFaceBlend_" + i, sAheOrgasmFaceBlend[i]);
                }

                if (bBokkiChikubiAvailable)
                {
                    panel["AutoTUN"].Enabled = parseExIni("AutoTUN", "Enabled", false);
                }
                else
                {
                    panel["AutoTUN"].Enabled = false;
                }
                slider["ChikubiScale"].Value = parseExIni("AutoTUN", "ChikubiScale", fChikubiScale);
                slider["ChikubiNae"].Value = parseExIni("AutoTUN", "ChikubiNae", fChikubiNae);
                slider["ChikubiBokki"].Value = slider["ChikubiNae"].Value;
                slider["ChikubiTare"].Value = parseExIni("AutoTUN", "ChikubiTare", fChikubiTare);
                iDefChikubiNae = slider["ChikubiNae"].Value;
                iDefChikubiTare = slider["ChikubiTare"].Value;

                panel["AutoBOTE"].Enabled = parseExIni("AutoBOTE", "Enabled", false);
                toggle["SlowCreampie"].Value = parseExIni("AutoBOTE", "SlowCreampie", toggle["SlowCreampie"].Value);
                iHaraIncrement = parseExIni("AutoBOTE", "Increment", iHaraIncrement);
                iBoteHaraMax = parseExIni("AutoBOTE", "Max", iBoteHaraMax);

                if (bKupaAvailable || bAnalKupaAvailable)
                {
                    panel["AutoKUPA"].Enabled = parseExIni("AutoKUPA", "Enabled", true);
                }
                else
                {
                    panel["AutoKUPA"].Enabled = false;
                }
                slider["KupaLevel"].Value = parseExIni("AutoKUPA", "KupaLevel", fKupaLevel);
                slider["LabiaKupa"].Value = parseExIni("AutoKUPA", "LabiaKupa", fLabiaKupa);
                slider["VaginaKupa"].Value = parseExIni("AutoKUPA", "VaginaKupa", fVaginaKupa);
                slider["NyodoKupa"].Value = parseExIni("AutoKUPA", "NyodoKupa", fNyodoKupa);
                slider["Suji"].Value = parseExIni("AutoKUPA", "Suji", fSuji);

                iKupaStart = parseExIni("AutoKUPA", "Start", iKupaStart);
                iKupaIncrementPerOrgasm = parseExIni("AutoKUPA", "IncrementPerOrgasm", iKupaIncrementPerOrgasm);
                iKupaNormalMax = parseExIni("AutoKUPA", "NormalMax", iKupaNormalMax);
                iKupaWaitingValue = parseExIni("AutoKUPA", "WaitingValue", iKupaWaitingValue);
                for (int i = 0; i < 2; i++)
                {
                    iKupaValue[i] = parseExIni("AutoKUPA", "Value_" + i, iKupaValue[i]);
                }

                iAnalKupaStart = parseExIni("AutoKUPA_Anal", "Start", iAnalKupaStart);
                iAnalKupaIncrementPerOrgasm = parseExIni("AutoKUPA_Anal", "IncrementPerOrgasm", iAnalKupaIncrementPerOrgasm);
                iAnalKupaNormalMax = parseExIni("AutoKUPA_Anal", "NormalMax", iAnalKupaNormalMax);
                iAnalKupaWaitingValue = parseExIni("AutoKUPA_Anal", "WaitingValue", iAnalKupaWaitingValue);
                for (int i = 0; i < 2; i++)
                {
                    iAnalKupaValue[i] = parseExIni("AutoKUPA_Anal", "Value_" + i, iAnalKupaValue[i]);
                }

                panel["FaceAnime"].Enabled = parseExIni("FaceAnime", "EnableOnLoad", false);
                if (panel["FaceAnime"].Enabled)
                {
                    //toggle["Lipsync"].Value = parseExIni("FaceAnime", "LipsyncCancelling", false);
                    //updateMaidFoceKuchipakuSelfUpdateTime(toggle["Lipsync"].Value);

                    string faceName = parseExIni("FaceAnime", "FaceName", string.Empty);
                    if (sFaceNames.Contains(faceName))
                    {
                        panel["FaceAnime"].HeaderUILabelText = faceName;
                    }
                }

                panel["FaceBlend"].Enabled = parseExIni("FaceBlend", "EnableOnLoad", false);
                if (panel["FaceBlend"].Enabled)
                {
                    string faceName = parseExIni("FaceBlend", "FaceName", string.Empty);
                    if (sFaceNames.Contains(faceName))
                    {
                        panel["FaceBlend"].HeaderUILabelText = faceName;
                    }
                }
            }

            return true;
        }

        private string getCurrentSkillName()
        {
            try
            {
                if (bNormalYotogiScene)
                {
                    Yotogi.SkillDataPair sdp = getFieldValue<YotogiPlayManager, Yotogi.SkillDataPair>(this.yotogiPlayManager, "skill_pair_");
                    return sdp.base_data.name;
                }
                else if (bCompatibilityYotogiScene)
                {
                    YotogiOld.SkillDataPair sdp = getFieldValue<YotogiOldPlayManager, YotogiOld.SkillDataPair>(this.yotogiOldPlayManager, "skill_pair_");
                    return sdp.base_data.name;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                LogDebug("Failed to get current skill name");
                return null;
            }
        }

        private void initOnStartSkill()
        {
#if DEBUG
            LogDebug("Starting initialization");
#endif
            if (!bInitCompleted)
            {
                initPlugin();
                bInitCompleted = true;
            }

            bLoadBoneAnimetion = false;
            bSyncMotionSpeed = true;
            bKupaFuck = false;
            bAnalKupaFuck = false;
            iBoteCount = 0;
            iKupaDef = 0;
            iAnalKupaDef = 0;

            if (panel["AutoAHE"].Enabled)
            {
                slider["EyeY"].Value = maid.body0.trsEyeL.localPosition.y * fEyePosToSliderMul;
            }

            if (panel["AutoTUN"].Enabled)
            {
                if (slider["ChikubiBokki"].Value > 0)
                {
                    updateShapeKeyChikubiBokkiValue(slider["ChikubiBokki"].Value / 2f);
                }
            }

            if (panel["AutoBOTE"].Enabled)
            {
                updateMaidHaraValue(Mathf.Max(iCurrentHara, iDefHara));
            }
            else
            {
                updateMaidHaraValue(iDefHara);
            }

            string skillName = getCurrentSkillName();
            if (!String.IsNullOrEmpty(skillName))
            {
                LogDebug("Start Skill : {0}", skillName);
                KupaLevel kl = checkSkillKupaLevel(skillName);
                if (kl != KupaLevel.None) iKupaDef = (int)(iKupaValue[(int)kl] * slider["KupaLevel"].Value / 100f);
                kl = checkSkillAnalKupaLevel(skillName);
                if (kl != KupaLevel.None) iAnalKupaDef = (int)(iAnalKupaValue[(int)kl] * slider["KupaLevel"].Value / 100f);
            }

            if (panel["AutoKUPA"].Enabled)
            {
                if (bKupaAvailable) updateShapeKeyKupaValue(iKupaMin);
                if (bAnalKupaAvailable) updateShapeKeyAnalKupaValue(iAnalKupaMin);
                //				if (bLabiaKupaAvailable) updateShapeKeyLabiaKupaValue(iLabiaKupaMin);
                //                if (bVaginaKupaAvailable) updateShapeKeyVaginaKupaValue(iVaginaKupaMin);
                //                if (bNyodoKupaAvailable) updateShapeKeyNyodoKupaValue(iNyodoKupaMin);
                //                if (bSujiAvailable) updateShapeKeySujiValue(iSujiMin);
                if (bClitorisAvailable) updateShapeKeyClitorisValue(iClitorisMin);
            }
            else
            {
                if (bKupaAvailable) updateShapeKeyKupaValue(0f);
                if (bAnalKupaAvailable) updateShapeKeyAnalKupaValue(0f);
                //				if (bLabiaKupaAvailable) updateShapeKeyLabiaKupaValue(0f);
                //                if (bVaginaKupaAvailable) updateShapeKeyVaginaKupaValue(0f);
                //                if (bNyodoKupaAvailable) updateShapeKeyNyodoKupaValue(0f);
                //                if (bSujiAvailable) updateShapeKeySujiValue(0f);
                if (bClitorisAvailable) updateShapeKeyClitorisValue(0f);
            }
            if (bOrgasmAvailable) updateShapeKeyOrgasmValue(0f);

            foreach (KeyValuePair<string, PlayAnime> kvp in pa) if (kvp.Value.NowPlaying) kvp.Value.Stop();

            StartCoroutine(getBoneAnimetionCoroutine(WaitBoneLoad));

            bSyncMotionSpeed = true;
            StartCoroutine(syncMotionSpeedSliderCoroutine(TimePerUpdateSpeed));

            if (panel["Status"].Enabled && slider["Excite"].Pin)
            {
                // 夜伽開始時にiniの設定を反映するため
                updateMaidExcite((int)slider["Excite"].Value);
            }
            else
            {
                slider["Excite"].Value = (float)maid.status.currentExcite;
            }

            if (panel["Status"].Enabled && slider["Mind"].Pin)
            {
                updateMaidMind((int)slider["Mind"].Value);
            }
            else
            {
                slider["Mind"].Value = (float)maid.status.currentMind;
            }

            if (bNormalYotogiScene)
            {
                // 官能は「発情させる」を使用するとスキル変更後0になる
                if (panel["Status"].Enabled && slider["Sensual"].Pin)
                {
                    updateMaidSensual((int)slider["Sensual"].Value);
                }
                else
                {
                    slider["Sensual"].Value = (float)maid.status.currentSensual;
                }
            }

            if (panel["FaceAnime"].Enabled)
            {
                maid.FaceAnime(panel["FaceAnime"].HeaderUILabelText, 1f, 0);
            }

            if (panel["FaceBlend"].Enabled)
            {
                maid.FaceBlend(panel["FaceBlend"].HeaderUILabelText);
            }

//            if (lSelect["StageSelect"].CurrentName != YotogiStageSelectManager.StagePrefab)
//            {
//                GameMain.Instance.BgMgr.ChangeBg(lSelect["StageSelect"].CurrentName);
//            }
        }

        private void preFinalize()
        {
            if (!bInitCompleted) return;

            this.maid = GameMain.Instance.CharacterMgr.GetMaid(0);
            if (!this.maid) return;
            this.updateMaidEyePosY(fAheDefEye);

            this.maid.ResetProp("Hara", true);
            updateSlider("Slider:Hara", iDefHara);
            if (bBokkiChikubiAvailable)
            {
                this.updateShapeKeyChikubiBokkiValue(iDefChikubiNae);
                this.updateShapeKeyChikubiTareValue(iDefChikubiTare);
            }

            LogDebug("Pre-finalization complete.");
            finalize();
        }

        private void finalize()
        {
            try
            {
                if (maid && toggle["Lipsync"]!=null && toggle["Lipsync"].Value)
                {
                    maidFoceKuchipakuSelfUpdateTime.SetValue(maid, false);
                }

                visible = false;

                window = null;
                panel.Clear();
                slider.Clear();
                grid.Clear();
                toggle.Clear();
                lSelect.Clear();

                bInitCompleted = false;
                bLoadBoneAnimetion = false;
                bSyncMotionSpeed = false;
                fPassedTimeOnCommand = -1f;

                iLastExcite = 0;
                iOrgasmCount = 0;
                //iLastSliderFrustration = 0;
                //fLastSliderSensitivity = 0f;

                iDefHara = 0;
                iCurrentHara = 0;
                iBoteCount = 0;

                bKupaFuck = false;
                bAnalKupaFuck = false;

                maid = null;
                //maidStatusInfo = null;
                maidFoceKuchipakuSelfUpdateTime = null;
                yotogiPlayManager = null;
                yotogiOldPlayManager = null;
                playManagerAsWfScreenChildren = null;
                orgOnClickCommand = null;
                orgOnClickCommandOld = null;

                currentCommandData = null;
                lastSkillName = null;
                yotogiParamBasicBarDelegator = null;

                if (kagScriptCallbacksOverride && (bNormalYotogiScene || bCompatibilityYotogiScene))
                {
                    kagScript.RemoveTagCallBack("face");
                    kagScript.AddTagCallBack("face", new KagScript.KagTagCallBack(this.orgTagFace));
                    kagScript.RemoveTagCallBack("faceblend");
                    kagScript.AddTagCallBack("faceblend", new KagScript.KagTagCallBack(this.orgTagFaceBlend));
                    kagScriptCallbacksOverride = false;

                    kagScript = null;
                    orgTagFace = null;
                    orgTagFaceBlend = null;
                }

                LogInfo("Finalization complete.");
            }
            catch (Exception ex) { LogError("finalize() : {0}", ex); return; }

        }

        private void detectSkill()
        {
            if (currentCommandData == null) return;

            string currentSkillName = currentCommandData.skillName;
            if (lastSkillName == null || !currentSkillName.Equals(lastSkillName))
            {
                Debug.Log(LogLabel + "Yotogi changed: " + lastSkillName + " >> " + currentSkillName);
                lastSkillName = currentSkillName;
            }
            
        }

        private bool VertexMorph_FromProcItem(TBody body, string sTag, float f)
        {
            bool bFace = false;
            var i = 0;
            foreach (var slot in body.EnumerateGoSlot())
            {
                TMorph morph = slot.morph;
                if (morph != null)
                {
                    if (morph.Contains(sTag))
                    {
                        if (i == 1)
                        {
                            bFace = true;
                        }
                        int h = (int)slot.morph.hash[sTag];
                        slot.morph.SetBlendValues(h, f);
                        slot.morph.FixBlendValues();
                    }
                }

                i++;
            }
            return bFace;
        }

        private bool isExistVertexMorph(TBody body, string sTag)
        {

            foreach (var slot in body.EnumerateGoSlot())
            {
                TMorph morph = slot.morph;
                if (morph != null)
                {
                    if (morph.Contains(sTag))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void animateAutoTun()
        {
            if (!panel["AutoTUN"].Enabled) return;

            float from = Mathf.Max(iDefChikubiNae, slider["ChikubiBokki"].Value);
            int i = (int)checkCommandTunLevel(this.currentCommandData.basic);
            if (i < 0) return;

            float to = from + (iTunValue[i] / 100f) * slider["ChikubiScale"].Value / 100f;
            if (to >= 100f * slider["ChikubiScale"].Value / 100f)
            {
                slider["ChikubiBokki"].Value = 100f * slider["ChikubiScale"].Value / 100f;
            }
            else
            {
                slider["ChikubiBokki"].Value = to;
            }
        }

        private void syncSlidersOnClickCommand(CommonCommandData.Status cmStatus)
        {
            if (panel["Status"].Enabled && slider["Excite"].Pin) updateMaidExcite((int)slider["Excite"].Value);
            else slider["Excite"].Value = (float)maid.status.currentExcite;

            if (panel["Status"].Enabled && slider["Mind"].Pin) updateMaidMind((int)slider["Mind"].Value);
            else slider["Mind"].Value = (float)maid.status.currentMind;

            if (bNormalYotogiScene)
            {
                if (panel["Status"].Enabled && slider["Sensual"].Pin) updateMaidSensual((int)slider["Sensual"].Value);
                else slider["Sensual"].Value = (float)maid.status.currentSensual;
            }
            else if (bCompatibilityYotogiScene)
            {
                if (panel["Status"].Enabled && slider["Reason"].Pin) updateMaidReason((int)slider["Reason"].Value);
                else slider["Reason"].Value = (float)maid.status.currentReason;

                // コマンド実行によるfrustration変動でfrustrationは0-100内に補正される為、
                // Statusパネル有効時はコマンド以前のスライダー値より感度を計算して表示
                // メイドのfrustrationを実際に弄るのはコマンド直前のみ
                //            slider["Sensitivity"].Value = (float)(maid.Param.status.correction_data.excite
                //                + (panel["Status"].Enabled ? iLastSliderFrustration + cmStatus.frustration : maid.Param.status.frustration)
                //                + (maid.Param.status.cur_reason < 20 ? 20 : 0));
            }

            if (panel["Status"].Enabled && slider["MotionSpeed"].Pin) updateMotionSpeed(slider["MotionSpeed"].Value);
            else foreach (AnimationState stat in anm_BO_body001) if (stat.enabled) slider["MotionSpeed"].Value = stat.speed * 100f;

            slider["EyeY"].Value = maid.body0.trsEyeL.localPosition.y * fEyePosToSliderMul;
            if (!toggle["SlowCreampie"].Value)
            {
                slider["Hara"].Value = (float)maid.GetProp("Hara").value;
            }
        }

        private IEnumerator syncMotionSpeedSliderCoroutine(float waitTime)
        {
            while (bSyncMotionSpeed)
            {
                if (bLoadBoneAnimetion)
                {
                    if (panel["Status"].Enabled && slider["MotionSpeed"].Pin && !pa["AHE.絶頂.0"].NowPlaying)
                    {
                        updateMotionSpeed(slider["MotionSpeed"].Value);
                    }
                    else
                    {
                        foreach (AnimationState stat in anm_BO_body001)
                        {
                            if (stat.enabled)
                            {
                                slider["MotionSpeed"].Value = stat.speed * 100f;
                                //LogDebug("{0}:{1}:{2}", stat.name, stat.speed, stat.enabled);
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(waitTime);
            }
        }

        private void initAnimeOnCommand()
        {
            if (panel["AutoAHE"].Enabled)
            {
                fAheLastEye = maid.body0.trsEyeL.localPosition.y * fEyePosToSliderMul;

                for (int i = 0; i < 1; i++)
                {
                    if (pa["AHE.絶頂." + i].NowPlaying) pa["AHE.絶頂." + i].Stop();
                    if (pa["AHE.継続." + i].NowPlaying) pa["AHE.継続." + i].Stop();
                }

                for (int i = 0; i < 2; i++)
                {
                    if (pa["KUPA.挿入." + i].NowPlaying) updateShapeKeyKupaValue((int)(iKupaValue[i] * slider["KupaLevel"].Value / 100f));
                    pa["KUPA.挿入." + i].Stop();
                }

                for (int i = 0; i < 2; i++)
                {
                    if (pa["AKPA.挿入." + i].NowPlaying) updateShapeKeyAnalKupaValue((int)(iAnalKupaValue[i] * slider["KupaLevel"].Value / 100f));
                    pa["AKPA.挿入." + i].Stop();
                }
            }

            if (panel["AutoBOTE"].Enabled)
            {
                // アニメ再生中にコマンド実行で強制的に終端値に
                if (pa["BOTE.絶頂"].NowPlaying)
                {
                    if (toggle["SlowCreampie"].Value)
                    {
                        updateMaidHaraValue(Mathf.Min(iCurrentHara, iBoteHaraMax));
                    }
                    else
                    {
                        updateMaidHaraValue(Mathf.Min(iCurrentHara + iHaraIncrement, iBoteHaraMax));
                    }
                }
                if (pa["BOTE.止める"].NowPlaying || pa["BOTE.流れ出る"].NowPlaying)
                {
                    updateMaidHaraValue(iCurrentHara);
                }

                pa["BOTE.絶頂"].Stop();
                pa["BOTE.止める"].Stop();
                pa["BOTE.流れ出る"].Stop();
            }

            if (panel["AutoKUPA"].Enabled)
            {
                if (pa["KUPA.止める"].NowPlaying) updateShapeKeyKupaValue(iKupaMin);
                pa["KUPA.止める"].Stop();

                if (pa["AKPA.止める"].NowPlaying) updateShapeKeyAnalKupaValue(iAnalKupaMin);
                pa["AKPA.止める"].Stop();
            }

        }

        private void afterYotogiPlayManagerOnClickCommand(CommonCommandData command_data)
        {
            playAnimeOnCommand(command_data.basic);
            syncSlidersOnClickCommand(command_data.status);


            if (command_data.basic.command_type == Yotogi.SkillCommandType.絶頂)
            {
                if (!panel["FaceAnime"].Enabled && pa["AHE.絶頂.0"].NowPlaying)
                {
                    maid.FaceAnime(sAheOrgasmFace[idxAheOrgasm], 5f, 0);
                    panel["FaceAnime"].HeaderUILabelText = sAheOrgasmFace[idxAheOrgasm];
                }
            }
        }

        private void playAnimeOnCommand(CommonCommandData.Basic data)
        {
            LogDebug("Skill:{0} Command:{1} Type:{2}", data.group_name, data.name, data.command_type);

            if (panel["AutoAHE"].Enabled)
            {
                float excite = maid.status.currentExcite;
                int i = idxAheOrgasm;

                if (data.command_type == Yotogi.SkillCommandType.絶頂)
                {
                    if (iLastExcite >= iAheExcite[i])
                    {
                        pa["AHE.継続.0"].Play(fAheLastEye, fAheOrgasmEyeMax[i]);

                        float[] xFrom = { fAheOrgasmEyeMax[i], fAheOrgasmSpeed[i] };
                        float[] xTo = { fAheOrgasmEyeMin[i], 100f };

                        updateMotionSpeed(fAheOrgasmSpeed[i]);
                        pa["AHE.絶頂.0"].Play(xFrom, xTo);

                        if (toggle["Convulsion"].Value)
                        {
                            if (pa["AHE.痙攣." + i].NowPlaying) iAheOrgasmChain++;
                            pa["AHE.痙攣." + i].Play(0f, fAheOrgasmConvulsion[i]);
                        }

                        iOrgasmCount++;
                    }
                }
                else
                {
                    if (excite >= iAheExcite[i])
                    {
                        float to = fAheNormalEyeMax[i] * (excite - iAheExcite[i]) / (300f - iAheExcite[i]);
                        pa["AHE.継続.0"].Play(fAheLastEye, to);
                    }
                    else
                    {
                        if (bNormalYotogiScene)
                        {
                            pa["AHE.継続.0"].Play(fAheLastEye, fAheDefEye);
                        }
                        else
                        {
                            pa["AHE.継続.0"].Play(fAheLastEye, fAheLastEye - 0.1f);
                        }
                    }
                }
            }


            if (panel["AutoBOTE"].Enabled)
            {
                float from = (float)Mathf.Max(iCurrentHara, iDefHara);
                float to = iDefHara;

                if (data.command_type == Yotogi.SkillCommandType.絶頂)
                {
                    if (!data.group_name.Contains("オナホコキ") && (data.name.Contains("中出し") || data.name.Contains("注ぎ込む")))
                    {
                        iBoteCount++;
                        to = Mathf.Min(iCurrentHara + iHaraIncrement, iBoteHaraMax);
                        pa["BOTE.絶頂"].Play(from, to);
                    }
                    else if (data.name.Contains("外出し"))
                    {
                        if (toggle["SlowCreampie"].Value)
                        {
                            pa["BOTE.流れ出る"].Play(from, to);
                        }
                        else
                        {
                            pa["BOTE.止める"].Play(from, to);
                        }
                        iBoteCount = 0;
                    }
                }
                else if (data.command_type == Yotogi.SkillCommandType.止める)
                {
                    if (toggle["SlowCreampie"].Value)
                    {
                        pa["BOTE.流れ出る"].Play(from, to);
                    }
                    else
                    {
                        pa["BOTE.止める"].Play(from, to);
                    }
                    iBoteCount = 0;
                }

                if (from <= to)
                {
                    iCurrentHara = (int)to;
                }

                if (data.command_type != Yotogi.SkillCommandType.絶頂)
                {
                    if (data.command_type != Yotogi.SkillCommandType.止める)
                    {
                        // 挿入
                        if (pa["BOTE.止める"].NowPlaying || pa["BOTE.流れ出る"].NowPlaying)
                        {
                            pa["BOTE.流れ出る"].Stop();
                            pa["BOTE.止める"].Stop();
                        }
                        iBoteCount = 0;
                    }
                    else
                    {
                        // 未挿入
                        from = (float)Mathf.Max(iCurrentHara, iDefHara);
                        to = iDefHara;

                        if ((!pa["BOTE.止める"].NowPlaying && !pa["BOTE.流れ出る"].NowPlaying) && from > to)
                        {
                            if (toggle["SlowCreampie"].Value)
                            {
                                pa["BOTE.流れ出る"].Play(from, to);
                            }
                            else
                            {
                                pa["BOTE.止める"].Play(from, to);
                            }
                            iBoteCount = 0;
                        }
                    }
                }
            }

            if (panel["AutoKUPA"].Enabled)
            {
                float from = slider["Kupa"].Value;
                int i = (int)checkCommandKupaLevel(data);
                if (i >= 0)
                {
                    if (from < (int)(iKupaValue[i] * slider["KupaLevel"].Value / 100f))
                        pa["KUPA.挿入." + i].Play(from, (int)(iKupaValue[i] * slider["KupaLevel"].Value / 100f));
                    bKupaFuck = true;
                }
                else if (bKupaFuck && checkCommandKupaStop(data))
                {
                    pa["KUPA.止める"].Play(from, iKupaMin);
                    bKupaFuck = false;
                }

                from = slider["AnalKupa"].Value;
                i = (int)checkCommandAnalKupaLevel(data);
                if (i >= 0)
                {
                    if (from < (int)(iAnalKupaValue[i] * slider["KupaLevel"].Value / 100f))
                        pa["AKPA.挿入." + i].Play(from, (int)(iAnalKupaValue[i] * slider["KupaLevel"].Value / 100f));
                    bAnalKupaFuck = true;
                }
                else if (bAnalKupaFuck && checkCommandAnalKupaStop(data))
                {
                    pa["AKPA.止める"].Play(from, iAnalKupaMin);
                    bAnalKupaFuck = false;
                }

                if (panel["Status"].Enabled && bClitorisAvailable)
                {
                    // 興奮の度合いによって程度が変わる
                    float offset = 0f;
                    float clitorisLong = 30f;
                    if (slider["Excite"].Value < 300f * 0.4f)
                    {
                        offset = 0f;
                        clitorisLong = 30f;
                    }
                    else if (slider["Excite"].Value < 300f * 0.7f)
                    {
                        offset = 40f;
                        clitorisLong = 30f;
                    }
                    else if (slider["Excite"].Value < 300f * 1.0f)
                    {
                        offset = 70f;
                        clitorisLong = 40f;
                    }
                    else
                    {
                        offset = 100f;
                        clitorisLong = 50f;
                    }

                    // クリトリスを責める系
                    if (data.name.Contains("クリトリス") || data.name.Contains("オナニー") || data.group_name.Contains("バイブを舐めさせる") || data.group_name.Contains("オナニー")
                        // スマタ・こすりつけ
                        || (data.group_name.StartsWith("洗い") && (data.name.Contains("洗わせる") || data.name.Contains("たわし洗い"))))
                    {
                        if (!pa["KUPACL.剥く.1"].NowPlaying)
                        {
                            pa["KUPACL.剥く.1"].Play(0f + offset, clitorisLong + offset);
                        }
                    }
                    else
                    {
                        // 絶頂したら飛び出る
                        if (!pa["KUPACL.剥く.0"].NowPlaying && !pa["KUPACL.剥く.1"].NowPlaying
                           && (data.command_type == Yotogi.SkillCommandType.絶頂 || data.name.Contains("強く責める"))
                           && slider["Clitoris"].Value < (clitorisLong - 10f + offset))
                        {
                            pa["KUPACL.剥く.0"].Play(0f + offset, clitorisLong + offset);

                            // 抜いたら引っ込む
                        }
                        else if (!pa["KUPACL.被る"].NowPlaying && data.command_type == Yotogi.SkillCommandType.止める
                                && slider["Clitoris"].Value > (clitorisLong - 10f + offset))
                        {
                            pa["KUPACL.被る"].Play(clitorisLong + offset, 0f + offset);
                        }
                    }
                }

            }
        }


        private void playAnimeOnInputKeyDown()
        {
            if (visible)
            {
                fWinAnimeFrom = new float[2] { Screen.width, 0f };
                fWinAnimeTo = new float[2] { winAnimeRect.x, 1f };
            }
            else
            {
                fWinAnimeFrom = new float[2] { winAnimeRect.x, 1f };
                fWinAnimeTo = new float[2] { (winAnimeRect.x + winAnimeRect.width / 2 > Screen.width / 2f) ? Screen.width : -winAnimeRect.width, 0f };
            }
            pa["WIN.Load"].Play(fWinAnimeFrom, fWinAnimeTo);
        }

        private void updateAnimeOnUpdate()
        {
            if (panel["AutoAHE"].Enabled)
            {
                if (pa["AHE.継続.0"].NowPlaying) pa["AHE.継続.0"].Update();

                if (pa["AHE.絶頂.0"].NowPlaying)
                {
                    pa["AHE.絶頂.0"].Update();
                    maid.FaceBlend(sAheOrgasmFaceBlend[idxAheOrgasm]);
                    panel["FaceBlend"].HeaderUILabelText = sAheOrgasmFaceBlend[idxAheOrgasm];
                }

                for (int i = 0; i < 3; i++) if (pa["AHE.痙攣." + i].NowPlaying) pa["AHE.痙攣." + i].Update();


                // 放置中の瞳自然降下
                if (!pa["AHE.継続.0"].NowPlaying && !pa["AHE.絶頂.0"].NowPlaying)
                {
                    // アニメーション開始時に瞳の位置がリセットされるので、現在のy座標ではなくスライダーの値を使用する
                    float eyepos = slider["EyeY"].Value;
                    if (eyepos > fAheDefEye) updateMaidEyePosY(eyepos - fAheEyeDecrement * (int)(fPassedTimeOnCommand / 10));
                }
            }

            if (panel["AutoTUN"].Enabled)
            {
                // 服を着ていると初期状態で反映されない？
                this.updateShapeKeyChikubiBokkiValue(slider["ChikubiBokki"].Value);
                this.updateShapeKeyChikubiTareValue(slider["ChikubiTare"].Value);
                this.detectSkill();
                if (this.currentCommandData != null)
                {
                    animateAutoTun();
                }
            }

            if (panel["AutoBOTE"].Enabled)
            {
                if (pa["BOTE.絶頂"].NowPlaying) pa["BOTE.絶頂"].Update();
                if (pa["BOTE.止める"].NowPlaying) pa["BOTE.止める"].Update();
                if (pa["BOTE.流れ出る"].NowPlaying) pa["BOTE.流れ出る"].Update();

                if (!pa["BOTE.絶頂"].NowPlaying && (pa["BOTE.止める"].NowPlaying || pa["BOTE.流れ出る"].NowPlaying)) iCurrentHara = (int)slider["Hara"].Value;

                this.detectSkill();
                if (this.currentCommandData != null && this.currentCommandData.basic.command_type != Yotogi.SkillCommandType.絶頂)
                {
                    if (this.currentCommandData.basic.command_type == Yotogi.SkillCommandType.止める)
                    {
                        float from = (float)Mathf.Max(iCurrentHara, iDefHara);
                        float to = iDefHara;

                        if ((!pa["BOTE.止める"].NowPlaying && !pa["BOTE.流れ出る"].NowPlaying) && from > to)
                        {
                            if (toggle["SlowCreampie"].Value)
                            {
                                pa["BOTE.流れ出る"].Play(from, to);
                            }
                            else
                            {
                                pa["BOTE.止める"].Play(from, to);
                            }
                            iBoteCount = 0;
                        }
                    }
                }
            }

            if (panel["AutoKUPA"].Enabled && !isInOutAnimationActive)
            {
                bool updated = false;
                string[] names = {
                    "KUPA.挿入.0", "KUPA.挿入.1", "KUPA.止める",
                    "AKPA.挿入.0", "AKPA.挿入.1", "AKPA.止める",
                    "KUPACL.剥く.0", "KUPACL.剥く.1", "KUPACL.被る",
                };
                foreach (var name in names)
                {
                    if (pa[name].NowPlaying)
                    {
                        pa[name].Update();
                        updated = true;
                    }
                }
                if (bKupaAvailable && iKupaWaitingValue > 0)
                {
                    var current = slider["Kupa"].Value;
                    if (!updated && current > 0)
                    {
                        fPassedTimeOnAutoKupaWaiting += Time.deltaTime;
                        float f2rad = 180f * fPassedTimeOnAutoKupaWaiting * Mathf.Deg2Rad;
                        float freq = bSyncMotionSpeed ? (slider["MotionSpeed"].Value / 100f) : 1f;
                        float value = current + iKupaWaitingValue * (1f + Mathf.Sin(freq * f2rad)) / 2f;
                        maid.body0.VertexMorph_FromProcItem("kupa", value / 100f);
                    }
                    else
                    {
                        fPassedTimeOnAutoKupaWaiting = 0;
                    }
                }
                if (bAnalKupaAvailable && iAnalKupaWaitingValue > 0)
                {
                    var current = slider["AnalKupa"].Value;
                    if (!updated && current > 0)
                    {
                        fPassedTimeOnAutoAnalKupaWaiting += Time.deltaTime;
                        float f2rad = 180f * fPassedTimeOnAutoAnalKupaWaiting * Mathf.Deg2Rad;
                        float freq = bSyncMotionSpeed ? (100f / slider["MotionSpeed"].Value) : 1f;
                        float value = current + iAnalKupaWaitingValue * (1f + Mathf.Sin(freq * f2rad)) / 2f;
                        maid.body0.VertexMorph_FromProcItem("analkupa", value / 100f);
                    }
                    else
                    {
                        fPassedTimeOnAutoAnalKupaWaiting = 0;
                    }
                }
            }
        }

        private void updateAnimeOnGUI()
        {
            if (pa["WIN.Load"].NowPlaying)
            {
                pa["WIN.Load"].Update();
            }
        }

        private void dummyWin(int winID) { }

        private void updateSlider(string name, float value)
        {
            Container.Find<YotogiSlider>(window, name).Value = value;
        }

        private void updateWindowAnime(float[] x)
        {
            winAnimeRect.x = x[0];
            GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, x[1]);

            GUIStyle winStyle = new GUIStyle("box");
            winStyle.fontSize = PV.Font("C1");
            winStyle.alignment = TextAnchor.UpperRight;
            winAnimeRect = GUI.Window(0, winAnimeRect, dummyWin, AddYotogiSliderSE.Version, winStyle);
        }

        private void updateMaidExcite(int value)
        {
            maid.status.currentExcite = value;
            yotogiParamBasicBarDelegator.SetCurrentExcite(value, true);
            iLastExcite = maid.status.currentExcite;
        }

        private void updateMaidMind(int value)
        {
            maid.status.currentMind = value;
            yotogiParamBasicBarDelegator.SetCurrentMind(value, true);
        }

        private void updateMaidReason(int value)
        {
            maid.status.currentReason = value;
            yotogiParamBasicBarDelegator.SetCurrentReason(value, true);
        }

        private void updateMaidSensual(int value)
        {
            maid.status.currentSensual = value;
            yotogiParamBasicBarDelegator.SetCurrentSensual(value, true);
        }

//        private void updateMaidFrustration(int value)
//        {
//            param.Status tmp = (param.Status)maidStatusInfo.GetValue(maid.Param);
//            tmp.frustration = value;
//            maidStatusInfo.SetValue(maid.Param, tmp);
//        }

        private void updateMaidEyePosY(float value)
        {
            if (value < fAheDefEye) value = fAheDefEye;
            Vector3 vl = maid.body0.trsEyeL.localPosition;
            Vector3 vr = maid.body0.trsEyeR.localPosition;
            maid.body0.trsEyeL.localPosition = new Vector3(vl.x, value / fEyePosToSliderMul, vl.z);
            maid.body0.trsEyeR.localPosition = new Vector3(vr.x, -value / fEyePosToSliderMul, vr.z);

            updateSlider("Slider:EyeY", value);
        }

        private void updateChikubiNaeValue(float value)
        {
            float forceTareValue = 0f;
            if (value < 0)
            {
                forceTareValue = 0f - value;
            }

            try
            {
                if (forceTareValue > 0f)
                {
                    updateShapeKeyChikubiTareValue(forceTareValue);
                }
                if (slider["ChikubiBokki"].Value < value)
                {
                    updateShapeKeyChikubiBokkiValue(value);
                }

            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:ChikubiNae", value);
        }

        private void updateShapeKeyChikubiBokkiValue(float value)
        {
            float forceTareValue = 0f;
            if (value < 0)
            {
                forceTareValue = 0f - value;
            }

            try
            {
                if (forceTareValue > 0f)
                {
                    updateShapeKeyChikubiTareValue(forceTareValue);
                }
                VertexMorph_FromProcItem(this.maid.body0, "chikubi_bokki", (value > 0f) ? (value / 100f) : 0f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:ChikubiBokki", value);
        }

        private void updateShapeKeyChikubiTareValue(float value)
        {
            try
            {
                VertexMorph_FromProcItem(this.maid.body0, "chikubi_tare", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:ChikubiTare", value);
        }

        private void updateMaidHaraValue(float value)
        {
            try
            {
                maid.SetProp("Hara", (int)value, true);
                maid.body0.VertexMorph_FromProcItem("hara", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:Hara", value);
        }

        private void updateMaidFoceKuchipakuSelfUpdateTime(bool b)
        {
            maidFoceKuchipakuSelfUpdateTime.SetValue(maid, b);
        }


        private void updateShapeKeyKupaValue(float value)
        {
            try
            {
                if (!isInOutAnimationActive) maid.body0.VertexMorph_FromProcItem("kupa", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:Kupa", value);
        }

        private void updateShapeKeyAnalKupaValue(float value)
        {
            try
            {
                if (!isInOutAnimationActive) maid.body0.VertexMorph_FromProcItem("analkupa", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:AnalKupa", value);
        }

        private void updateShapeKeyKupaLevelValue(float value)
        {
            updateSlider("Slider:KupaLevel", value);
        }

        private void updateShapeKeyLabiaKupaValue(float value)
        {
            try
            {
                if (!isInOutAnimationActive) maid.body0.VertexMorph_FromProcItem("labiakupa", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:LabiaKupa", value);
        }

        private void updateShapeKeyVaginaKupaValue(float value)
        {
            try
            {
                if (!isInOutAnimationActive) maid.body0.VertexMorph_FromProcItem("vaginakupa", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:VaginaKupa", value);
        }

        private void updateShapeKeyNyodoKupaValue(float value)
        {
            try
            {
                if (!isInOutAnimationActive) maid.body0.VertexMorph_FromProcItem("nyodokupa", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:NyodoKupa", value);
        }

        private void updateShapeKeySujiValue(float value)
        {
            try
            {
                if (!isInOutAnimationActive) maid.body0.VertexMorph_FromProcItem("suji", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:Suji", value);
        }

        private void updateShapeKeyClitorisValue(float value)
        {
            try
            {
                if (!isInOutAnimationActive) maid.body0.VertexMorph_FromProcItem("clitoris", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            updateSlider("Slider:Clitoris", value);
        }

        private void updateShapeKeyOrgasmValue(float value)
        {
            try
            {
                maid.body0.VertexMorph_FromProcItem("orgasm", value / 100f);
            }
            catch { /*LogError(ex);*/ }

            //LogWarning(value);
        }

        private void updateOrgasmConvulsion(float value)
        {
            //goBip01LThigh.transform.localRotation *= Quaternion.Euler(0f, 10f*value, 0f);
            if (bOrgasmAvailable)
            {
                updateShapeKeyOrgasmValue(value);
            }
        }

        private void updateMotionSpeed(float value)
        {
            foreach (AnimationState stat in anm_BO_body001) if (stat.enabled) stat.speed = value / 100f;
            foreach (Animation anm in anm_BO_mbody)
            {
                foreach (AnimationState stat in anm) if (stat.enabled) stat.speed = value / 100f;
            }
        }


        private void updateCameraControl()
        {
            Vector2 cursor = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool b = window.Rectangle.Contains(cursor);
            if (b != bCursorOnWindow)
            {
                GameMain.Instance.MainCamera.SetControl(!b);
                UICamera.InputEnable = !b;
                bCursorOnWindow = b;
            }
        }

        private void updateAheOrgasm(float[] x)
        {
            updateMaidEyePosY(x[0]);
            updateMotionSpeed(x[1]);
        }

//        private int getMaidFrustration()
//        {
//            param.Status tmp = (param.Status)maidStatusInfo.GetValue(maid.Param);
//            return tmp.frustration;
//            return 10;
//        }

//        private int getSliderFrustration()
//        {
//            return (int)(slider["Sensitivity"].Value - maid.Param.status.correction_data.excite + (maid.Param.status.cur_reason < 20 ? 20 : 0));
//            return 10;
//        }

        private IEnumerator getBoneAnimetionCoroutine(float waitTime)
        {
#if DEBUG
            LogDebug("getBoneAnimetionCoroutine - start");
#endif
            yield return new WaitForSeconds(waitTime);

#if DEBUG
            LogDebug("getBoneAnimetionCoroutine - waiting for maid");
#endif
            if (!maid) yield break;

            this.anm_BO_body001 = maid.body0.GetAnimation();

            List<GameObject> go_BO_mbody = new List<GameObject>();

            int i = 0;
            while (true)
            {
                GameObject go = GameObject.Find("Man[" + i + "]/Offset/_BO_mbody");
                if (!go) break;

                go_BO_mbody.Add(go);
                i++;
            }

            this.anm_BO_mbody = new Animation[i];
            for (int j = 0; j < i; j++) anm_BO_mbody[j] = go_BO_mbody[j].GetComponent<Animation>();

            bLoadBoneAnimetion = true;
#if DEBUG
            LogDebug($"getBoneAnimetionCoroutine - got animation {i}");
#endif
        }

        private TunLevel checkCommandTunLevel(CommonCommandData.Basic cmd)
        {
            // 止める
            if (cmd.name.Contains("止める")
               || cmd.name.Contains("止めさせる")
               || cmd.name.Contains("乳首を舐めさせる")
               || cmd.name.Contains("四つん這い尻舐め")
               || cmd.name.Contains("自分の胸を揉んで頂きながら")
               )
            {
                return TunLevel.None;
            }

            // 摘み上げ
            if (cmd.name.Contains("乳首でイカせる")
               || cmd.name.Contains("乳首を捻り上げる")
               || cmd.name.Contains("乳首を摘まみ上げる")) return TunLevel.Nip;

            if (cmd.group_name.Contains("パイズリ")
               || cmd.group_name.Contains("MP全身洗い"))
            {

                if (cmd.command_type == Yotogi.SkillCommandType.絶頂)
                {
                    return TunLevel.Petting;
                }
                if (cmd.name.Contains("乳首を摘")
                   || cmd.name.Contains("強引に胸を犯す")
                   ) return TunLevel.Petting;

                // 摩擦で刺激が入る系
                return TunLevel.Friction;
            }

            if (cmd.group_name.Contains("首絞め"))
            {
                if (cmd.command_type == Yotogi.SkillCommandType.絶頂)
                {
                    return TunLevel.Nip;
                }
                if (cmd.name.Contains("胸を叩く")) return TunLevel.Petting;
            }

            if (cmd.name.Contains("首絞め")
               || cmd.name.Contains("口を塞")
               || cmd.name.Contains("胸をムチで叩く")
               || cmd.name.Contains("胸にロウを垂らす")
               || cmd.name.Contains("胸を一本ムチで叩く")
               || cmd.name.Contains("胸に高温ロウを垂らす")
               || cmd.name.Contains("胸を揉ませながら")
               || cmd.name.Contains("胸を揉ませて頂く")
               || cmd.name.Contains("胸を叩く")
               || cmd.name.Contains("胸洗いをさせる")
               || cmd.name.Contains("両胸を揉む")

               ) return TunLevel.Petting;

            if (cmd.name.Contains("胸を揉")
               ) return TunLevel.Friction;

            return TunLevel.None;
        }

        private KupaLevel checkSkillKupaLevel(string skillName)
        {
            if (skillName.StartsWith("バイブ責めアナルセックス")) return KupaLevel.Vibe;
            if (skillName.Contains("露出プレイ")) return KupaLevel.Vibe;
            if (skillName.Contains("犬プレイ") && !skillName.Contains("セックス")) return KupaLevel.Vibe;
            return KupaLevel.None;
        }

        private KupaLevel checkSkillAnalKupaLevel(string skillName)
        {
            if (skillName.StartsWith("アナルバイブ責めセックス")) return KupaLevel.Vibe;
            return KupaLevel.None;
        }

        private KupaLevel checkCommandKupaLevel(CommonCommandData.Basic cmd)
        {
            if (cmd.command_type == Yotogi.SkillCommandType.挿入)
            {
                if (cmd.group_name.Contains("素股")) return KupaLevel.None;
                if (!cmd.group_name.Contains("アナル"))
                {
                    string[] t0 = { "セックス", "太バイブ", "正常位", "後背位", "騎乗位", "立位", "側位", "座位", "駅弁", "寝バック" };
                    if (t0.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Sex;

                    string[] t1 = { "愛撫", "オナニー", "バイブ", "シックスナイン",
                                    "ポーズ維持プレイ", "磔プレイ" };
                    if (t1.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Vibe;
                }
                else
                {
                    if (cmd.group_name.Contains("アナルバイブ責めセックス")) return KupaLevel.Sex;
                }
            }
            else if (cmd.group_name.Contains("三角木馬"))
            {
                if (cmd.name.Contains("肩を押す")) return KupaLevel.Vibe;
            }
            else if (cmd.group_name.Contains("まんぐり"))
            {
                if (cmd.name.Contains("愛撫") || cmd.name.Contains("クンニ")) return KupaLevel.Vibe;
            }
            if (!cmd.group_name.Contains("アナル"))
            {
                if (cmd.name.Contains("指を増やして")) return KupaLevel.Sex;
                if (cmd.group_name.Contains("バイブ") || cmd.group_name.Contains("オナニー"))
                {
                    // 口責めなどから直接「イカせる」を選択した場合
                    if (cmd.name == "イカせる") return KupaLevel.Vibe;
                }
            }
            return KupaLevel.None;
        }

        private KupaLevel checkCommandAnalKupaLevel(CommonCommandData.Basic cmd)
        {
            if (cmd.group_name.StartsWith("アナルバイブ責めセックス")) return KupaLevel.None;
            if (cmd.command_type == Yotogi.SkillCommandType.挿入)
            {
                string[] t0 = { "アナルセックス", "アナル正常位", "アナル後背位", "アナル騎乗位",
                    "2穴", "4P", "アナル処女喪失", "アナル処女再喪失", "アナル寝バック", "アナル駅弁", "アナル座位"};
                if (t0.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Sex;

                string[] t1 = { "アナルバイブ", "アナルオナニー" };
                if (t1.Any(t => cmd.group_name.Contains(t))) return KupaLevel.Vibe;
            }
            if (cmd.group_name.Contains("アナルバイブ"))
            {
                if (cmd.name == "イカせる") return KupaLevel.Vibe;
            }
            return KupaLevel.None;
        }

        private bool checkCommandKupaStop(CommonCommandData.Basic cmd)
        {
            if (cmd.group_name == "まんぐり返しアナルセックス")
            {
                if (cmd.name.Contains("責める")) return true;
            }
            // if (cmd.name.Contains("絶頂焦らし")) return true; // TODO: アニメーションタイミング変更
            return checkCommandAnyKupaStop(cmd);
        }

        private bool checkCommandAnalKupaStop(CommonCommandData.Basic cmd)
        {
            if (cmd.command_type == Yotogi.SkillCommandType.絶頂)
            {
                if (cmd.group_name.Contains("オナニー")) return true;
            }
            return checkCommandAnyKupaStop(cmd);
        }

        private bool checkCommandAnyKupaStop(CommonCommandData.Basic cmd)
        {
            if (cmd.command_type == Yotogi.SkillCommandType.止める) return true;
            if (cmd.command_type == Yotogi.SkillCommandType.絶頂)
            {
                if (cmd.group_name.Contains("愛撫")) return true;
                if (cmd.group_name.Contains("まんぐり")) return true; // TODO: アニメーションタイミング変更
                if (cmd.group_name.Contains("シックスナイン")) return true;
                if (cmd.name.Contains("外出し")) return true;
            }
            else
            {
                if (cmd.name.Contains("頭を撫でる")) return true;
                if (cmd.name.Contains("口を責める")) return true;
                if (cmd.name.Contains("クリトリスを責めさせる")) return true;
                if (cmd.name.Contains("バイブを舐めさせる")) return true;
                if (cmd.name.Contains("擦りつける")) return true;
                if (cmd.name.Contains("放尿させる")) return true;
            }
            return false;
        }

        private void clearExIniComments()
        {
        }

        readonly Dictionary<string, object> ExIni = new Dictionary<string, object>();
        private ConfigEntry<T> getIniEntry<T>(string section, string key, T def)
        {
            var iniKey = $"{section}.{key}";
            if (!ExIni.ContainsKey(iniKey))
            {
                ExIni[iniKey] = Config.Bind<T>(section, key, def);
            }

            return ExIni[iniKey] as ConfigEntry<T>;
        }

        private T parseExIni<T>(string section, string key, T def)
        {
            var entry = getIniEntry(section, key, def);
            var res = entry.Value;
            LogDebug("Ini: [{0}] {1}='{2}'", section, key, res);
            return res;
        }

        private void setExIni<T>(string section, string key, T value)
        {
            var entry = getIniEntry(section, key, value);
            entry.Value = value;
        }

        private void SaveConfig()
        {

        }

        private void ReloadConfig()
        {

        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void showMaidStatus()
        {
            if (maid)
            {
                LogDebug("Maid Excite {0}", maid.status.currentExcite);
                LogDebug("Maid Mind {0}", maid.status.currentMind);
                LogDebug("Maid Reason {0}", maid.status.currentReason);
                LogDebug("Maid Sensual {0}", maid.status.currentSensual);
                LogDebug("Maid Eye Y {0}", maid.body0.trsEyeL.localPosition.y * fEyePosToSliderMul);
            }
        }

        private static ManualLogSource Log => Instance.Logger;

        [System.Diagnostics.Conditional("DEBUG")]
        private static void LogDebug(string msg, params object[] args)
        {
            Log.LogDebug(string.Format(msg, args));
        }

        private static void LogWarning(string msg, params object[] args)
        {
            Log.LogWarning(string.Format(msg, args));
        }

        private static void LogError(string msg, params object[] args)
        {
            Log.LogError(string.Format(msg, args));
        }

        private static void LogError(object ex)
        {
            LogError("{0}", ex);
        }

        private static void LogInfo(string msg, params object[] args)
        {
            Log.LogInfo(string.Format(msg, args));
        }

#endregion

#region Utility methods

        internal static string GetFullPath(GameObject go)
        {
            string s = go.name;
            if (go.transform.parent != null) s = GetFullPath(go.transform.parent.gameObject) + "/" + s;

            return s;
        }

        internal static void WriteComponent(GameObject go)
        {
            Component[] compos = go.GetComponents<Component>();
            foreach (Component c in compos) { LogDebug("{0}:{1}", go.name, c.GetType().Name); }
        }

        internal static void WriteTrans(string s)
        {
            GameObject go = GameObject.Find(s);
            if (IsNull(go, s + " not found.")) return;

            WriteTrans(go.transform, 0, null);
        }
        internal static void WriteTrans(Transform t) { WriteTrans(t, 0, null); }
        internal static void WriteTrans(Transform t, int level, StreamWriter writer)
        {
            if (level == 0) writer = new StreamWriter(@".\" + t.name + @".txt", false);
            if (writer == null) return;

            string s = "";
            for (int i = 0; i < level; i++) s += "    ";
            writer.WriteLine(s + level + "," + t.name);
            foreach (Transform tc in t)
            {
                WriteTrans(tc, level + 1, writer);
            }

            if (level == 0) writer.Close();
        }

        internal static bool IsNull<T>(T t) where T : class
        {
            return (t == null) ? true : false;
        }

        internal static bool IsNull<T>(T t, string s) where T : class
        {
            if (t == null)
            {
                LogError(s);
                return true;
            }
            else return false;
        }

        internal static bool IsActive(GameObject go)
        {
            return go ? go.activeInHierarchy : false;
        }

        internal static T getInstance<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectOfType(typeof(T)) as T;
        }

        internal static TResult getMethodDelegate<T, TResult>(T inst, string name) where T : class where TResult : class
        {
            return Delegate.CreateDelegate(typeof(TResult), inst, name) as TResult;
        }

        internal static FieldInfo getFieldInfo<T>(string name)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            return typeof(T).GetField(name, bf);
        }

        internal static TResult getFieldValue<T, TResult>(T inst, string name)
        {
            if (inst == null) return default(TResult);

            FieldInfo field = getFieldInfo<T>(name);
            if (field == null) return default(TResult);

            return (TResult)field.GetValue(inst);
        }

        #endregion

        #region Compatibility methods

        internal new ManualLogSource Logger => base.Logger;

        private void OldConfigCheck()
        {
            var oldConfigPath = Path.Combine(Paths.GameRootPath, "Sybaris\\UnityInjector\\Config\\AddYotogiSliderSE.ini");
            if (File.Exists(oldConfigPath))
            {
                Logger.LogWarning($"Detected old sybaris configuration at [{oldConfigPath}]. Make sure to migrate your configuration to the new configuration path. See plugin README.md for details.");
            }
        }

        private void SybarisCheck()
        {
            var injector = GameObject.Find("UnityInjector");
            var component = injector?.GetComponent("AddYotogiSliderSE");

            if (component != null)
            {
                var assemblyLocation = component.GetType().Assembly.Location;
                Logger.LogWarning($"Detected old sybaris plugin at [{assemblyLocation}]. Disabling this plugin but you are advised to uninstall this properly. See plugin README.md for details.");
                GameObject.Destroy(component);
            }
        }

        private void CheckIsInOutAnimationActive()
        {
            var enabled = InOutAnimationHook.IsPluginEnabled && InOutAnimationHook.IsMorpherEnabled;
            if (isInOutAnimationActive != enabled)
            {
                isInOutAnimationActive = enabled;
                if (enabled)
                {
                    LogWarning("InOutAnimation morpher is enabled. AutoKUPA will be disabled and KUPA sliders will have no effect.");
                }
                else
                {
                    LogWarning("InOutAnimation morpher disabled.");
                }
            }
        }

#endregion
    }

}

namespace UnityObsoleteGui
{

    public abstract class Element : IComparable<Element>
    {
        protected readonly int id;

        protected string name;
        protected Rect rect;
        protected bool visible;

        public string Name { get { return name; } }

        public virtual Rect Rectangle { get { return rect; } }
        public virtual float Left { get { return rect.x; } }
        public virtual float Top { get { return rect.y; } }
        public virtual float Width { get { return rect.width; } }
        public virtual float Height { get { return rect.height; } }
        public virtual bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                if (Parent != null) notifyParent(false, true);
            }
        }

        public Container Parent = null;
        public event EventHandler<ElementEventArgs> NotifyParent = delegate { };


        public Element() { }
        public Element(string name, Rect rect)
        {
            this.id = this.GetHashCode();
            this.name = name;
            this.rect = rect;
            this.visible = true;
        }

        public virtual void Draw() { Draw(this.rect); }
        public virtual void Draw(Rect outRect) { }
        public virtual void Resize() { Resize(false); }
        public virtual void Resize(bool broadCast) { if (!broadCast) notifyParent(true, false); }

        public virtual int CompareTo(Element e) { return this.name.CompareTo(e.Name); }

        protected virtual void notifyParent(bool sizeChanged, bool visibleChanged)
        {
            NotifyParent(this, new ElementEventArgs(name, sizeChanged, visibleChanged));
        }
    }


    public abstract class Container : Element, IEnumerable<Element>
    {
        public static Element Find(Container parent, string s) { return Container.Find<Element>(parent, s); }
        public static T Find<T>(Container parent, string s) where T : Element
        {
            if (parent == null) return null;

            foreach (Element e in parent)
            {
                if (e is T && e.Name == s) return e as T;
                if (e is Container)
                {
                    T e2 = Find<T>(e as Container, s);
                    if (e2 != null) return e2 as T;
                }
            }

            return null;
        }

        //----

        protected List<Element> children = new List<Element>();

        public int ChildCount { get { return children.Count; } }


        public Container(string name, Rect rect) : base(name, rect) { }

        public Element this[string s]
        {
            get { return GetChild<Element>(s); }
            set { if (value is Element) AddChild(value); }
        }

        public Element AddChild(Element child) { return AddChild<Element>(child); }
        public T AddChild<T>(T child) where T : Element
        {
            if (child != null && !children.Contains(child))
            {
                child.Parent = this;
                child.NotifyParent += this.onChildChenged;
                children.Add(child);
                Resize();

                return child;
            }

            return null;
        }

        public Element GetChild(string s) { return GetChild<Element>(s); }
        public T GetChild<T>() where T : Element { return GetChild<T>(""); }
        public T GetChild<T>(string s) where T : Element
        {
            return children.FirstOrDefault(e => e is T && (s == "" ? true : e.Name == s)) as T;
        }

        public void RemoveChild(string s)
        {
            Element child = children.FirstOrDefault(e => e.Name == s);
            if (child != null)
            {
                child.Parent = null;
                child.NotifyParent -= this.onChildChenged;
                children.Remove(child);
                Resize();
            }
        }

        public void RemoveChildren()
        {
            foreach (Element child in children)
            {
                child.Parent = null;
                child.NotifyParent -= this.onChildChenged;
            }
            children.Clear();
            Resize();
        }

        public virtual void onChildChenged(object sender, EventArgs e) { Resize(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
        public IEnumerator<Element> GetEnumerator() { return children.GetEnumerator(); }

    }


    public class Window : Container
    {

#region Constants
        public const float AutoLayout = -1f;

        [Flags]
        public enum Scroll
        {
            None = 0x00,
            HScroll = 0x01,
            VScroll = 0x02
        }

#endregion



#region Nested classes

        private class HorizontalSpacer : Element
        {
            public HorizontalSpacer(float height)
            : base("Spacer:", new Rect(Window.AutoLayout, Window.AutoLayout, Window.AutoLayout, height))
            {
                this.name += this.id;
            }
        }

#endregion



#region Variables

        private Rect sizeRatio;
        private Rect baseRect;
        private Rect titleRect;
        private Rect contentRect;
        private Vector2 autoSize = Vector2.zero;
        private Vector2 hScrollViewPos = Vector2.zero;
        private Vector2 vScrollViewPos = Vector2.zero;
        private Vector2 lastScreenSize;
        private int colums = 1;

        public GUIStyle WindowStyle = new GUIStyle("window");
        public GUIStyle LabelStyle = new GUIStyle("label");
        public string HeaderText;
        public int HeaderFontSize;
        public string TitleText;
        public float TitleHeight;
        public int TitleFontSize;
        public Scroll scroll = Scroll.None;

#endregion



#region Methods

        public Window(Rect ratio, string header, string title) : this(title, ratio, header, title, null) { }
        public Window(string name, Rect ratio, string header, string title) : this(name, ratio, header, title, null) { }
        public Window(string name, Rect ratio, string header, string title, List<Element> children) : base(name, PV.PropScreenMH(ratio))
        {
            this.sizeRatio = ratio;
            this.HeaderText = header;
            this.TitleText = title;
            this.TitleHeight = PV.Line("C1");

            if (children != null && children.Count > 0)
            {
                this.children = new List<Element>(children);
                foreach (Element child in children)
                {
                    child.Parent = this;
                    child.NotifyParent += this.onChildChenged;

                }
                Resize();
            }

            lastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        public override void Draw(Rect outRect)
        {
            if (propScreen())
            {
                resizeAllChildren(this);
                Resize();
                outRect = rect;
            }

            WindowStyle.fontSize = PV.Font("C2");
            WindowStyle.alignment = TextAnchor.UpperRight;

            rect = GUI.Window(id, outRect, drawWindow, HeaderText, WindowStyle);
        }

        public override void Resize()
        {
            calcAutoSize();
        }

        public Element AddHorizontalSpacer() { return AddHorizontalSpacer((float)PV.Margin); }
        public Element AddHorizontalSpacer(float height) { return AddChild(new HorizontalSpacer(height)); }

        //----

        private void drawWindow(int id)
        {
            TitleHeight = PV.Line("C1");
            TitleFontSize = PV.Font("C2");

            LabelStyle.fontSize = TitleFontSize;
            LabelStyle.alignment = TextAnchor.UpperLeft;
            GUI.Label(titleRect, TitleText, LabelStyle);

            GUI.BeginGroup(contentRect);
            {
                Rect cur = new Rect(0f, 0f, 0f, 0f);

                foreach (Element child in children)
                {
                    if (!child.Visible) continue;

                    if (child.Left >= 0 || child.Top >= 0)
                    {
                        Rect tmp = new Rect((child.Left >= 0) ? child.Left : cur.x,
                                              (child.Top >= 0) ? child.Top : cur.y,
                                              (child.Width > 0) ? child.Width : autoSize.x,
                                              (child.Height > 0) ? child.Height : autoSize.y);

                        child.Draw(tmp);
                    }
                    else
                    {
                        cur.width = (child.Width > 0) ? child.Width : autoSize.x;
                        cur.height = (child.Height > 0) ? child.Height : autoSize.y;
                        child.Draw(cur);
                        cur.y += cur.height;
                    }
                }
            }
            GUI.EndGroup();

            GUI.DragWindow();
        }

        private bool propScreen()
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (lastScreenSize != screenSize)
            {
                rect = PV.PropScreenMH(rect.x, rect.y, sizeRatio.width, sizeRatio.height, lastScreenSize);
                lastScreenSize = screenSize;
                calcRectSize();
                return true;
            }
            return false;
        }

        private void calcRectSize()
        {
            baseRect = PV.InsideRect(rect);
            titleRect = new Rect(PV.Margin, 0, baseRect.width, TitleHeight);
            contentRect = new Rect(baseRect.x, baseRect.y + titleRect.height, baseRect.width, baseRect.height - titleRect.height);
        }

        public void calcAutoSize()
        {
            Vector2 used = Vector2.zero;
            Vector2 count = Vector2.zero;

            foreach (Element child in children)
            {
                if (!child.Visible) continue;

                if (!(child.Left > 0 || child.Top > 0) && child.Width > 0) used.x += child.Width;
                else count.x += 1;

                if (!(child.Left > 0 || child.Top > 0) && child.Height > 0) used.y += child.Height;
                else count.y += 1;
            }

            {
                bool rectChanged = false;

                if ((scroll & Window.Scroll.HScroll) == 0x00)
                {
                    if (contentRect.width < used.x || (contentRect.width > used.x && count.x == 0))
                    {
                        rect.width = used.x + PV.Margin * 2;
                        rectChanged = true;
                    }
                }

                if ((scroll & Window.Scroll.VScroll) == 0x00)
                {
                    if (contentRect.height < used.y || (contentRect.height > used.y && count.y == 0))
                    {
                        rect.height = used.y + titleRect.height + PV.Margin * 3;
                        rectChanged = true;
                    }
                }

                if (rectChanged) calcRectSize();
            }

            autoSize.x = (count.x > 0) ? (contentRect.width - used.x) / colums : contentRect.width;
            autoSize.y = (count.y > 0) ? (contentRect.height - used.y) / (float)Math.Ceiling(count.y / colums) : contentRect.height;
        }

        private void resizeAllChildren(Container parent)
        {
            if (parent == null) return;

            foreach (Element child in parent)
            {
                if (child is Container) resizeAllChildren(child as Container);
                else child.Resize(true);
            }
        }

#endregion

    }


    public class HSlider : Element
    {
        public GUIStyle Style = new GUIStyle("horizontalSlider");
        public GUIStyle ThumbStyle = new GUIStyle("horizontalSliderThumb");
        public float Value;
        public float Min;
        public float Max;

        public event EventHandler<SliderEventArgs> OnChange;

        public HSlider(string name, Rect rect, float min, float max, float def, EventHandler<SliderEventArgs> _OnChange) : base(name, rect)
        {
            this.Value = def;
            this.Min = min;
            this.Max = max;
            this.OnChange += _OnChange;
        }

        public override void Draw(Rect outRect)
        {
            onChange(GUI.HorizontalSlider(outRect, Value, Min, Max, Style, ThumbStyle));
        }

        private void onChange(float newValue)
        {
            if (newValue != Value)
            {
                OnChange(this, new SliderEventArgs(name, newValue));
                Value = newValue;
            }
        }
    }

    public class Toggle : Element
    {
        private bool val;

        public GUIStyle Style = new GUIStyle("toggle");
        public GUIContent Content;
        public bool Value { get { return val; } set { val = value; } }
        public string Text { get { return Content.text; } set { Content.text = value; } }

        public event EventHandler<ToggleEventArgs> OnChange;

        public Toggle(string name, Rect rect, EventHandler<ToggleEventArgs> _OnChange) : this(name, rect, false, "", _OnChange) { }
        public Toggle(string name, Rect rect, bool def, EventHandler<ToggleEventArgs> _OnChange) : this(name, rect, def, "", _OnChange) { }
        public Toggle(string name, Rect rect, string text, EventHandler<ToggleEventArgs> _OnChange) : this(name, rect, false, text, _OnChange) { }
        public Toggle(string name, Rect rect, bool def, string text, EventHandler<ToggleEventArgs> _OnChange) : base(name, rect)
        {
            this.val = def;
            this.Content = new GUIContent(text);
            this.OnChange += _OnChange;
        }

        public override void Draw(Rect outRect)
        {
            onChange(GUI.Toggle(outRect, Value, Content, Style));
        }

        private void onChange(bool newValue)
        {
            if (newValue != val) OnChange(this, new ToggleEventArgs(name, newValue));
            val = newValue;
        }
    }

    public class SelectButton : Element
    {
        private string[] buttonNames;
        private int selected = 0;

        public int SelectedIndex { get { return selected; } }
        public string Value { get { return buttonNames[selected]; } }

        public event EventHandler<SelectEventArgs> OnSelect;

        public SelectButton(string name, Rect rect, string[] buttonNames, EventHandler<SelectEventArgs> _onSelect) : base(name, rect)
        {
            this.buttonNames = buttonNames.Clone() as string[];
            this.OnSelect += _onSelect;
        }

        public override void Draw(Rect outRect)
        {
            onSelect(GUI.Toolbar(outRect, selected, buttonNames));
        }

        private void onSelect(int newSelected)
        {
            if (selected != newSelected)
            {
                selected = newSelected;
                OnSelect(this, new SelectEventArgs(name, newSelected, buttonNames[newSelected]));
            }
        }
    }


    public class ElementEventArgs : EventArgs
    {
        public string Name;
        public bool SizeChanged;
        public bool VisibleChanged;

        public ElementEventArgs(string name, bool sizeChanged, bool visibleChanged)
        {
            this.Name = name;
            this.SizeChanged = sizeChanged;
            this.VisibleChanged = visibleChanged;
        }
    }

    public class SliderEventArgs : EventArgs
    {
        public string Name;
        public float Value;

        public SliderEventArgs(string name, float value)
        {
            this.Name = name;
            this.Value = value;
        }
    }

    public class ButtonEventArgs : EventArgs
    {
        public string Name;
        public string ButtonName;

        public ButtonEventArgs(string name, string buttonName)
        {
            this.Name = name;
            this.ButtonName = buttonName;
        }
    }

    public class ToggleEventArgs : EventArgs
    {
        public string Name;
        public bool Value;

        public ToggleEventArgs(string name, bool b)
        {
            this.Name = name;
            this.Value = b;
        }
    }

    public class SelectEventArgs : EventArgs
    {
        public string Name;
        public int Index;
        public string ButtonName;

        public SelectEventArgs(string name, int idx, string buttonName)
        {
            this.Name = name;
            this.Index = idx;
            this.ButtonName = buttonName;
        }
    }


    public static class PixelValuesCM3D2
    {

#region Variables

        private static int margin = 10;
        private static Dictionary<string, int> font = new Dictionary<string, int>();
        private static Dictionary<string, int> line = new Dictionary<string, int>();
        private static Dictionary<string, int> sys = new Dictionary<string, int>();

        public static float BaseWidth = 1280f;
        public static float PropRatio = 0.6f;
        public static int Margin { get { return PropPx(margin); } set { margin = value; } }

#endregion



#region Methods

        static PixelValuesCM3D2()
        {
            font["C1"] = 12;
            font["C2"] = 11;
            font["H1"] = 20;
            font["H2"] = 16;
            font["H3"] = 14;

            line["C1"] = 18;
            line["C2"] = 14;
            line["H1"] = 30;
            line["H2"] = 24;
            line["H3"] = 22;

            sys["Menu.Height"] = 45;
            sys["OkButton.Height"] = 95;

            sys["HScrollBar.Width"] = 15;
        }

        public static int Font(string key) { return PropPx(font[key]); }
        public static int Line(string key) { return PropPx(line[key]); }
        public static int Sys(string key) { return PropPx(sys[key]); }

        public static int Font_(string key) { return font[key]; }
        public static int Line_(string key) { return line[key]; }
        public static int Sys_(string key) { return sys[key]; }

        public static Rect PropScreen(Rect ratio)
        {
            return new Rect((Screen.width - Margin * 2) * ratio.x + Margin
                           , (Screen.height - Margin * 2) * ratio.y + Margin
                           , (Screen.width - Margin * 2) * ratio.width
                           , (Screen.height - Margin * 2) * ratio.height);
        }

        public static Rect PropScreenMH(Rect ratio)
        {
            Rect r = PropScreen(ratio);
            r.y += Sys("Menu.Height");
            r.height -= (Sys("Menu.Height") + Sys("OkButton.Height"));

            return r;
        }

        public static Rect PropScreenMH(float left, float top, float width, float height, Vector2 last)
        {
            Rect r = PropScreen(new Rect((float)(left / (last.x - Margin * 2)), (float)(top / (last.y - Margin * 2)), width, height));
            r.height -= (Sys("Menu.Height") + Sys("OkButton.Height"));

            return r;
        }

        public static Rect InsideRect(Rect rect)
        {
            return new Rect(Margin, Margin, rect.width - Margin * 2, rect.height - Margin * 2);
        }

        public static Rect InsideRect(Rect rect, int height)
        {
            return new Rect(Margin, Margin, rect.width - Margin * 2, height);
        }

        public static Rect InsideRect(Rect rect, Rect padding)
        {
            return new Rect(rect.x + padding.x, rect.y + padding.x, rect.width - padding.width * 2, rect.height - padding.height * 2);
        }

        public static int PropPx(int px)
        {
            return (int)(px * (1f + (Screen.width / BaseWidth - 1f) * PropRatio));
        }

        public static Rect PropRect(int px)
        {
            return new Rect(PropPx(px), PropPx(px), PropPx(px), PropPx(px));
        }
#endregion

    }

}