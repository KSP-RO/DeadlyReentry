using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace DeadlyReentry
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class ReentryPhysics : MonoBehaviour
    {
        private static AerodynamicsFX _afx;
        
        public static AerodynamicsFX afx {
            get
            {
                if ((object)_afx == null)
                {
                    GameObject fx = GameObject.Find ("FXLogic");
                    if ((object)fx != null)
                {
                        _afx = fx.GetComponent<AerodynamicsFX> ();
                    }
                }
                return _afx;
            }
        }
        
        public static Vector3 frameVelocity;
        
        public static GUIStyle warningMessageStyle = new GUIStyle();
        public static FontStyle fontStyle = new FontStyle();
        
        public static ScreenMessage crewGWarningMsg;

        public static float gToleranceMult = 6.0f;
        public static float crewGClamp = 30;
        public static float crewGPower = 4;
        public static float crewGMin = 5;
        public static float crewGWarn = 300000;
        public static float crewGLimit = 600000;
        public static float crewGKillChance = 0.75f;
        public static int maxHeatScale = 2;


        public static bool debugging = false;

        public void Start()
        {
            enabled = true; // 0.24 compatibility
			crewGWarningMsg = new ScreenMessage("<color=#ff0000>Reaching Crew G limit!</color>", 1f, ScreenMessageStyle.UPPER_CENTER);
            Debug.Log("[DRE] - ReentryPhysics.Start(): LoadSettings(), Difficulty: " + DeadlyReentryScenario.DifficultyName);
            LoadSettings(); // Moved loading of REENTRY_EFFECTS into a generic loader which uses new difficulty settings
            //warningMessageStyle.font = GUI.skin.font;
            //warningMessageStyle.fontSize = 32;
            //warningMessageStyle.
            //warningMessageStyle.fontStyle = GUI.skin.label.fontStyle;
            //crewGWarningMsg.guiStyleOverride = warningMessageStyle;
        }

        public static void LoadSettings()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes ("REENTRY_EFFECTS"))
            {
                if (node.HasValue("name") && node.GetValue("name") == DeadlyReentryScenario.DifficultyName)
                {
                    if (node.HasValue("gToleranceMult"))
                        float.TryParse(node.GetValue("gToleranceMult"), out gToleranceMult);                    
                    if (node.HasValue("crewGClamp"))
                        float.TryParse(node.GetValue("crewGClamp"), out crewGClamp);
                    if (node.HasValue("crewGPower"))
                        float.TryParse(node.GetValue("crewGPower"), out crewGPower);
                    if (node.HasValue("crewGMin"))
                        float.TryParse(node.GetValue("crewGMin"), out crewGMin);
                    if (node.HasValue("crewGWarn"))
                        float.TryParse(node.GetValue("crewGWarn"), out crewGWarn);
                    if (node.HasValue("crewGLimit"))
                        float.TryParse(node.GetValue("crewGLimit"), out crewGLimit);
                    if (node.HasValue("crewGKillChance"))
                        float.TryParse(node.GetValue("crewGKillChance"), out crewGKillChance);
                    if (node.HasValue("maxHeatScale"))
                        int.TryParse(node.GetValue("maxHeatScale"), out maxHeatScale);
                    
                    break;
                }
            }
        }
        
        public static void SaveSettings()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes ("REENTRY_EFFECTS"))
            {
                if (node.HasValue("name") && node.GetValue("name") == DeadlyReentryScenario.DifficultyName)
                {
                    if (node.HasValue("gToleranceMult"))
                        node.SetValue("gToleranceMult", gToleranceMult.ToString());                  
                    if (node.HasValue("crewGClamp"))
                        node.SetValue("crewGClamp", crewGClamp.ToString());
                    if (node.HasValue("crewGPower"))
                        node.SetValue("crewGPower", crewGPower.ToString());
                    if (node.HasValue("crewGMin"))
                        node.SetValue("crewGMin", crewGMin.ToString());
                    if (node.HasValue("crewGWarn"))
                        node.SetValue("crewGWarn", crewGWarn.ToString());
                    if (node.HasValue("crewGLimit"))
                        node.SetValue("crewGLimit", crewGLimit.ToString());
                    if (node.HasValue("crewGKillChance"))
                        node.SetValue("crewGKillChance", crewGKillChance.ToString());
                    
                    if(node.HasValue("debugging"))
                        node.SetValue("debugging", debugging.ToString());
                    break;
                }
            }
            SaveCustomSettings();
        }
        public static void SaveCustomSettings()
        {
            string[] difficultyNames = {"Default"};
            float ftmp;
            double dtmp;
            
            ConfigNode savenode = new ConfigNode();
            foreach(string difficulty in difficultyNames)
            {
                foreach (ConfigNode settingNode in GameDatabase.Instance.GetConfigNodes ("REENTRY_EFFECTS"))
                {
                    if (settingNode.HasValue("name") && settingNode.GetValue("name") == difficulty)
                    {
                        // This is :Final because it represents player choices and must not be overridden by other mods.
                        ConfigNode node = new ConfigNode("@REENTRY_EFFECTS[" + difficulty + "]:Final");
                        
                        if (settingNode.HasValue("gToleranceMult"))
                        {
                            float.TryParse(settingNode.GetValue("gToleranceMult"), out ftmp);
                            node.AddValue ("@gToleranceMult", ftmp);
                        }

                        if (settingNode.HasValue("crewGKillChance"))
                        {
                            float.TryParse(settingNode.GetValue("crewGKillChance"), out ftmp);
                            node.AddValue ("@crewGKillChance", ftmp);
                        }                        
                        
                        if (settingNode.HasValue("crewGClamp"))
                        {
                            double.TryParse(settingNode.GetValue("crewGClamp"), out dtmp);
                            node.AddValue ("@crewGClamp", dtmp);
                        }

                        if (settingNode.HasValue("crewGPower"))
                        {
                            double.TryParse(settingNode.GetValue("crewGPower"), out dtmp);
                            node.AddValue ("@crewGPower", dtmp);
                        }

                        if (settingNode.HasValue("crewGMin"))
                        {
                            double.TryParse(settingNode.GetValue("crewGMin"), out dtmp);
                            node.AddValue ("@crewGMin", dtmp);
                        }

                        if (settingNode.HasValue("crewGWarn"))
                        {
                            double.TryParse(settingNode.GetValue("crewGWarn"), out dtmp);
                            node.AddValue ("@crewGWarn", dtmp);
                        }

                        if (settingNode.HasValue("crewGLimit"))
                        {
                            double.TryParse(settingNode.GetValue("crewGLimit"), out dtmp);
                            node.AddValue ("@crewGLimit", dtmp);
                        }
                        
                        savenode.AddNode (node);
                        break;
                    }
                }
            }
            savenode.Save (KSPUtil.ApplicationRootPath.Replace ("\\", "/") + "GameData/DeadlyReentry/custom.cfg");
        }
    }
}