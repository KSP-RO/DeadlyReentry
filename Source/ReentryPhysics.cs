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
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ReentryPhysics : MonoBehaviour
    {
        public static float crewGClamp = 30;
        public static float crewGPower = 4;
        public static float crewGMin = 5;
        public static float crewGWarn = 300000;
        public static float crewGLimit = 600000;
        public static float crewGKillChance = 0.75f;
        public static double minTempForCalcOperationalTemp = 400d;
        public static double minOperationalTempOffset = 10d;

        public void Start()
        {
            enabled = true; // 0.24 compatibility
            Debug.Log("[DRE] - ReentryPhysics.Start(): LoadSettings()");
            LoadSettings();
        }

        public static void LoadSettings()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("REENTRY_EFFECTS"))
            {
                if (node.HasValue("name") && node.GetValue("name") == "default")
                {
                    node.TryGetValue("crewGClamp", ref crewGClamp);
                    node.TryGetValue("crewGPower", ref crewGPower);
                    node.TryGetValue("crewGMin", ref crewGMin);
                    node.TryGetValue("crewGWarn", ref crewGWarn);
                    node.TryGetValue("crewGLimit", ref crewGLimit);
                    node.TryGetValue("crewGKillChance", ref crewGKillChance);
                    node.TryGetValue("minTempForCalcOperationalTemp", ref minTempForCalcOperationalTemp);
                    node.TryGetValue("minOperationalTempOffset", ref minOperationalTempOffset);

                    break;
                }
            }
        }
    }
}