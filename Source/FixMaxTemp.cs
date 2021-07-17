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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class FixMaxTemps : MonoBehaviour
    {
        //public PartModule RFEngineConfig = null;
        //public FieldInfo[] RFEConfigs = null;
        
        public void Start()
        {
            Debug.Log("FixMaxTemps: Fixing Temps");
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("REENTRY_EFFECTS"))
            {
                if(node.HasValue("name") && node.GetValue("name") == "Default" && node.HasValue("ridiculousMaxTemp"))
                {
                    double maxTemp;
                    double scale = 0.5f;
                    bool bDebugLog = false;
                    if (node.HasValue("bFMTDebugLog"))
                        bool.TryParse(node.GetValue("bFMTDebugLog"), out bDebugLog);
                    if (node.HasValue ("maxTempScale"))
                        double.TryParse(node.GetValue("maxTempScale"), out scale);
                    if(scale > 0 && double.TryParse(node.GetValue("ridiculousMaxTemp"), out maxTemp))
                    {
                        Debug.Log("Using ridiculousMaxTemp = " + maxTemp.ToString() + " / maxTempScale =" + scale.ToString());
                        
                        if ((object)PartLoader.LoadedPartsList != null)
                        {
                            //StringBuilder fixMaxTempLogs = new StringBuilder();
                            //foreach (AvailablePart part in PartLoader.LoadedPartsList)
                            Debug.Log("Loaded Parts List Count = " + PartLoader.LoadedPartsList.Count.ToString());

                            for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++)
                            {
                                AvailablePart part = PartLoader.LoadedPartsList[i];
                                try
                                {
                                    if ((object)part.partPrefab != null && !(part.partPrefab.FindModuleImplementing<ModuleHeatShield>() || part.partPrefab.FindModuleImplementing<ModuleAblator>()))
                                    {
                                        ModuleAeroReentry _ModuleAeroReentry = part.partPrefab.FindModuleImplementing<ModuleAeroReentry>();
                                        if (_ModuleAeroReentry != null)
                                        {
                                            if (_ModuleAeroReentry.leaveTemp)
                                            {
                                                Debug.Log("[DRE] skipping part " + part.name + " (leaveTemp = True)");
                                                continue;
                                            }
                                        }
                                        if (part.name == "flag")
                                        {
                                            Debug.Log("[DRE] Ignoring part 'flag'");
                                            continue;
                                        }
                                        double oldTemp = part.partPrefab.maxTemp;

                                        if (part.partPrefab.maxTemp > maxTemp)
                                        {
                                            part.partPrefab.maxTemp = Math.Min(part.partPrefab.maxTemp * scale, maxTemp);
                                            part.partPrefab.skinMaxTemp = Math.Min(part.partPrefab.skinMaxTemp * scale, maxTemp);

                                            if (bDebugLog)
                                               Debug.Log("[DRE] rebalancing OP maxTemp/skinMaxTemp for part " + part.name);

                                            double curScale = part.partPrefab.maxTemp / oldTemp;

                                            //foreach (PartModule module in part.partPrefab.Modules)
                                            for (int j = 0; j < part.partPrefab.Modules.Count; j++)
                                            {
                                                PartModule module = part.partPrefab.Modules[j];
                                                if (module is ModuleEngines)
                                                {
                                                    ((ModuleEngines)module).heatProduction *= (float)curScale;
                                                    if (bDebugLog)
                                                       Debug.Log("Adjusted heat production of engine module " + module.name);
                                                }
                                            }
                                        }

                                        if (part.partPrefab.skinMaxTemp > maxTemp)
                                        {
                                            part.partPrefab.skinMaxTemp = Math.Min(part.partPrefab.skinMaxTemp * scale, maxTemp);
                                            if (bDebugLog)
                                               Debug.Log("[DRE] rebalancing OP skinMaxTemp for part " + part.name);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                   Debug.Log("Error processing part maxTemp " + part.name + "\n" + e.Message);
                                }
                                try
                                {
                                    if ((object)part.partPrefab != null && (object)part.partPrefab.Modules != null)
                                    {
                                        bool add = true;
                                        for (int k = 0; k < part.partPrefab.Modules.Count; k++)
                                        {
                                            if (part.partPrefab.Modules[k] is ModuleAeroReentry)
                                            {
                                                Debug.Log(part.name + " already has ModuleAeroReentry. Not adding.");
                                                add = false;
                                                continue;
                                            }
                                            if (part.name == "flag")
                                            {
                                                Debug.Log("Not adding ModuleAeroReentry to part 'flag'");
                                                add = false;
                                                continue;
                                            }
                                        }
                                        if (add)
                                        {
                                            if (bDebugLog)
                                               Debug.Log("Adding ModuleAeroReentry to part " + part.name);
											part.partPrefab.AddModule("ModuleAeroReentry", true).OnStart(PartModule.StartState.None);
                                        }
                                    }
                                    else
                                       Debug.Log("Error adding ModuleAeroReentry to " + part.name + "(either partPrefab or partPrefab.Modules was null)");
                                }
                                catch (Exception e)
                                {
                                   Debug.Log("Error adding ModuleAeroReentry to " + part.name + "\n" +e.Message);
                                }
                            }
                            //if (bDebugLog)
                            //    Debug.Log(fixMaxTempLogs);
                            Debug.Log("FixMaxTemps finished walking through part list.");
                        }
                    }
                }
            }
        }

        void LogBuild(StringBuilder log, string logstring)
        {
            log.Append("[DeadlyReentry.FixMaxTemps] " + logstring + "\n");
        }
        /*
        static void print(string msg)
        {
            MonoBehaviour.print("[DeadlyReentry.FixMaxTemps] " + msg);
        }
        */
    }
}