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
    class ModuleAeroReentry : PartModule
    {
        [KSPField]
        public bool leaveTemp = false;

        [KSPField(isPersistant = false, guiActive = true, guiName = "maxOperationalTemp", guiUnits = " K", guiFormat = "F2", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double maxOperationalTemp = -1d;

        [KSPField(isPersistant = false, guiActive = true, guiName = "skinMaxOperationalTemp", guiUnits = " K", guiFormat = "F2", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double skinMaxOperationalTemp = -1d;

        public bool is_debugging;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Acceleration", guiUnits = " G", guiFormat = "F3", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double displayGForce;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Damage", guiUnits = "", guiFormat = "G", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public string displayDamage;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Cumulative G", guiUnits = "", guiFormat = "F0", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double gExperienced = 0;

        protected bool useLowerOperationalTemp = true;

        protected FlightIntegrator flightIntegrator;

        /*
        private ModularFlightIntegrator fi;
        public ModularFlightIntegrator FI
        {
            get
            {
                if ((object)fi == null)
                    fi = vessel.gameObject.GetComponent<ModularFlightIntegrator>();
                return fi;
            }
            set
            {
                fi = value;
            }
        }
        
        public ModularFlightIntegrator.PartThermalData ptd;
        */
        private double lastGForce = 0;
        protected double lastTemperature;

        [KSPField(isPersistant = true)]
        public bool dead;

        [KSPField]
        public float gTolerance = -1;

        [KSPField(isPersistant = true)]
        public float crashTolerance = 8;

        private bool is_on_fire = false;
        private bool is_gforce_fx_playing = false;

        private bool is_engine = false;
        private DateTime nextScream = DateTime.Now;

        protected double recordedHeatLoad = 0.0;
        protected double maximumRecordedHeat = 0.0;
        protected double heatFluxPerArea = 0.0;
        protected DamageCube damageCube;

        [KSPField(isPersistant = true)]
        protected float internalDamage;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Rec. Heat", guiUnits = "", guiFormat = "F4", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        protected string displayRecordedHeatLoad = "0 W";

        [KSPField(isPersistant = false, guiActive = false, guiName = "Max. Rec. Heat", guiUnits = "", guiFormat = "F4", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        protected string displayMaximumRecordedHeat = "0 W";

        [KSPField(isPersistant = false, guiActive = false, guiName = "Heat/cm2", guiUnits = "", guiFormat = "F4", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        protected string displayHeatFluxPerArea = "0 W/cm2";

        [KSPField(isPersistant = false, guiActive = false, guiName = "Exposed Area", guiUnits = "", guiFormat = "F2", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        protected string displayExposedAreas = "0";

        [KSPEvent(guiName = "Reset Heat Record", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 4f, groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public void ResetRecordedHeat()
        {
            displayRecordedHeatLoad = "0 W";
            displayMaximumRecordedHeat = "0 W";
            recordedHeatLoad = 0f;
            maximumRecordedHeat = 0f;
            if ((object)myWindow != null)
                myWindow.displayDirty = true;
        }

        [KSPEvent(guiName = "No Damage", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 4f)]
        public void RepairDamage()
        {
            // We pass this off to the damage cube now.
            this.damageCube.RepairCubeDamage(FlightGlobals.ActiveVessel.VesselValues.RepairSkill.value);
            this.RepairInternalDamage(FlightGlobals.ActiveVessel.VesselValues.RepairSkill.value);

            ProcessDamage();
            SetDamageLabel();
            if ((object)myWindow != null)
                myWindow.displayDirty = true;
        }

        public void RepairInternalDamage(int repairSkill)
        {
            //int requiredSkill;

            if(internalDamage > 0)
            {
                int requiredSkill = 0;

                if(internalDamage > 0.75)
                    requiredSkill = 5;
                else if(internalDamage > 0.375)
                    requiredSkill = 4;
                else if(internalDamage > 0.1875)
                    requiredSkill = 3;
                else if(internalDamage > 0.09375)
                    requiredSkill = 2;
                else if(internalDamage > 0)
                    requiredSkill = 1;

                if(FlightGlobals.ActiveVessel.VesselValues.RepairSkill.value >= requiredSkill)
                {
                    internalDamage = internalDamage - UnityEngine.Random.Range(0.0f, 0.1f);
                    if (internalDamage < 0)
                        internalDamage = 0;
                } 
                else
                    ScreenMessages.PostScreenMessage("<color=orange>[DeadlyReentry]: " + this.part.partInfo.title + " is too badly damaged internally for this Kerbal's skill level.</color>", 6f, ScreenMessageStyle.UPPER_LEFT);
            }
        }

        EventData<GameEvents.ExplosionReaction> ReentryReaction = GameEvents.onPartExplode;
        
        UIPartActionWindow _myWindow = null; 
        UIPartActionWindow myWindow 
        {
            get
            {
                if((object)_myWindow == null)
                {
                    UIPartActionWindow[] windows = FindObjectsOfType<UIPartActionWindow>();
                    for(int i = windows.Length - 1; i >= 0; --i)
                    {
                        if (windows[i].part == part)
                        {
                            _myWindow = windows[i];
                            break;
                        }
                    }
                }
                return _myWindow;
            }
        }
        
        static string FormatTime(double time)
        {
            int iTime = (int) time % 3600;
            int seconds = iTime % 60;
            int minutes = (iTime / 60) % 60;
            int hours = (iTime / 3600);
            return hours.ToString ("D2") 
                + ":" + minutes.ToString ("D2") + ":" + seconds.ToString ("D2");
        }

        static string FormatFlux(double flux, bool scale = false)
        {
            if (scale)
                flux *= TimeWarp.fixedDeltaTime;
            if (flux >= 1000000000.0)
                return (flux / 1000000000.0).ToString("F2") + " T";
            else if (flux >= 1000000.0)
                return (flux / 1000000.0).ToString("F2") + " G";
            else if (flux >= 1000.0)
                return (flux / 1000.0).ToString("F2") + " M";
            else if (flux >= 1.0)
                return (flux).ToString("F2") + " k";
            else
                return (flux * 1000.0).ToString("F2");
            
        }

        public static void PlaySound(FXGroup fx, float volume)
        {
            volume = Mathf.Clamp01(volume);
            if(fx.audio.isPlaying)
            {
                if(fx.audio.volume < volume)
                    fx.audio.volume = volume;
            } else {
                fx.audio.volume = volume;
                fx.audio.Play ();
            }
            //if(this.is_debugging)
            //    print (fx.audio.clip.name);
        }
        FXGroup _gForceFX = null;
        FXGroup gForceFX 
        {
            get
            {
                if((object)_gForceFX == null)
                {
                    _gForceFX = new FXGroup (part.name + "_Crushing");
                    _gForceFX.audio = gameObject.AddComponent<AudioSource>();
                    _gForceFX.audio.clip = GameDatabase.Instance.GetAudioClip("DeadlyReentry/Sounds/gforce_damage");
                    _gForceFX.audio.spatialBlend = 1f;
                    _gForceFX.audio.volume = GameSettings.SHIP_VOLUME;
                    _gForceFX.audio.Stop ();
                }
                return _gForceFX;
                
            }
        }
        
        FXGroup _ablationSmokeFX = null;
        FXGroup ablationSmokeFX 
        {
            get
            {
                if((object)_ablationSmokeFX == null)
                {
                    _ablationSmokeFX = new FXGroup (part.name + "_Smoking");
                    _ablationSmokeFX.fxEmittersNewSystem.Add (Emitter("fx_smokeTrail_medium").GetComponent<ParticleSystem>());
                }
                return _ablationSmokeFX;
            }
        }
        
        FXGroup _ablationFX = null;
        FXGroup ablationFX 
        {
            get
            {
                if((object)_ablationFX == null)
                {
                    _ablationFX = new FXGroup (part.name + "_Burning");
                    _ablationFX.fxEmittersNewSystem.Add (Emitter("fx_exhaustFlame_yellow").GetComponent<ParticleSystem>());
                    _ablationFX.fxEmittersNewSystem.Add(Emitter("fx_exhaustSparks_yellow").GetComponent<ParticleSystem>());
                    _ablationFX.audio = gameObject.AddComponent<AudioSource>();
                    _ablationFX.audio.clip = GameDatabase.Instance.GetAudioClip("DeadlyReentry/Sounds/fire_damage");
                    _ablationFX.audio.spatialBlend = 1f;
                    _ablationFX.audio.volume = GameSettings.SHIP_VOLUME;
                    _ablationFX.audio.Stop ();
                    
                }
                return _ablationFX;
            }
        }

        FXGroup _screamFX = null;
        FXGroup screamFX
        {
            get
            {
                if ((object)_screamFX == null)
                {
                    _screamFX = new FXGroup(part.name + "_Screaming");
                    _screamFX.audio = gameObject.AddComponent<AudioSource>();
                    _screamFX.audio.clip = GameDatabase.Instance.GetAudioClip("DeadlyReentry/Sounds/scream");
                    _screamFX.audio.spatialBlend = 1f;
                    _screamFX.audio.volume = GameSettings.SHIP_VOLUME;
                    _screamFX.audio.Stop();
                }
                return _screamFX;
            }
        }
        
        public override void OnAwake()
        {
            base.OnAwake();
            damageCube = new DamageCube();

            // are we an engine?
            is_engine = part.FindModuleImplementing<ModuleEngines>() != null;

            useLowerOperationalTemp = part.FindModuleImplementing<ModuleAblator>() == null;
        }

        void OnDestroy()
        {
            //FI = null;
            if((object)_ablationFX != null && (object)_ablationFX.audio != null)
                Destroy(_ablationFX.audio);
            if((object)_gForceFX != null && (object)_gForceFX.audio != null)
                Destroy(_gForceFX.audio);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            /*
            if (node.HasValue("maxOperationalTemp") && double.TryParse(node.GetValue("maxOperationalTemp"), out maxOperationalTemp))
            {
                Debug.Log("part " + part.name + ".maxOperationalTemp = " + maxOperationalTemp.ToString());
            }
            if (node.HasValue("skinMaxOperationalTemp") && double.TryParse(node.GetValue("skinMaxOperationalTemp"), out skinMaxOperationalTemp))
            {
                Debug.Log("part " + part.name + ".skinMaxOperationalTemp = " + skinMaxOperationalTemp.ToString());
            }
            */

            if (node.HasValue("damageCube"))
            {
                this.damageCube.LoadFromString(node.GetValue("damageCube"));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            string serializedDamageCube = this.damageCube.SaveToString();
            node.AddValue("damageCube", serializedDamageCube);
        }

        public virtual void FixedUpdate()
        {
            if (!FlightGlobals.ready)
                return;

            displayDamage = internalDamage.ToString();

            // sanity check for parts whose max temps might change
            if (part.maxTemp < maxOperationalTemp)
                maxOperationalTemp = useLowerOperationalTemp ? part.maxTemp * 0.85 : part.maxTemp;
            if (part.skinMaxTemp < skinMaxOperationalTemp)
                skinMaxOperationalTemp = useLowerOperationalTemp ? part.skinMaxTemp * 0.85 : part.skinMaxTemp;

            // sanity checking
            if (Double.IsNaN(part.temperature))
                part.temperature = 297.6;
            if (Double.IsNaN(part.skinTemperature))
                part.skinTemperature = Math.Min(this.skinMaxOperationalTemp, vessel.externalTemperature);
            
            CheckForFire();
            CheckGeeForces();
            if (is_debugging && vessel.mach > 1.0)
            {
                double thermalConvectionFlux = double.IsNaN(part.thermalConvectionFlux) ? 0d : part.thermalConvectionFlux;
                double thermalRadiationFlux = double.IsNaN(part.thermalRadiationFlux) ? 0d : part.thermalRadiationFlux;
                double totalThermalFlux = thermalConvectionFlux + thermalRadiationFlux;
                if (totalThermalFlux > 0)
                {
                    recordedHeatLoad += totalThermalFlux;
                    maximumRecordedHeat = Math.Max(maximumRecordedHeat, totalThermalFlux);
                }

                heatFluxPerArea = totalThermalFlux / part.skinExposedArea;
                if (Double.IsNaN(heatFluxPerArea))
                    heatFluxPerArea = 0;
                
                displayExposedAreas = part.skinExposedArea.ToString("F4");

                displayRecordedHeatLoad = FormatFlux(recordedHeatLoad, true) + "J";
                displayMaximumRecordedHeat = FormatFlux(maximumRecordedHeat) + "W";
                //displayMaximumRecordedHeat = part.skinExposedArea.ToString("F2") + " / " + part.radiativeArea.ToString("F2") + "  m2";

                double aeroThermalFlux = 0;

                if ((object)flightIntegrator != null)
                {
                    aeroThermalFlux = (Math.Pow(this.flightIntegrator.backgroundRadiationTempExposed, 4) * PhysicsGlobals.StefanBoltzmanConstant * PhysicsGlobals.RadiationFactor);
                }
                //displayHeatFluxPerArea = FormatFlux(heatFluxPerArea/10000) + "W/cm2";
                if (heatFluxPerArea > 0)
                    displayHeatFluxPerArea = (heatFluxPerArea).ToString("F4") + "(" + (this.part.vessel.convectiveCoefficient + aeroThermalFlux).ToString("F4") + ")";
            }
            if (TimeWarp.CurrentRate <= PhysicsGlobals.ThermalMaxIntegrationWarp)
                lastTemperature = part.temperature;
        }

        public virtual void Update()
        {
            if (is_debugging != PhysicsGlobals.ThermalDataDisplay)
            {
                is_debugging = PhysicsGlobals.ThermalDataDisplay;

                //Fields["FIELD-TO-DISPLAY"].guiActive = PhysicsGlobals.ThermalDataDisplay;
                Fields["displayRecordedHeatLoad"].guiActive = PhysicsGlobals.ThermalDataDisplay;
                Fields["displayMaximumRecordedHeat"].guiActive = PhysicsGlobals.ThermalDataDisplay;
                Fields["displayHeatFluxPerArea"].guiActive = PhysicsGlobals.ThermalDataDisplay;
                Events["ResetRecordedHeat"].guiActive = PhysicsGlobals.ThermalDataDisplay;
                Fields["displayDamage"].guiActive = PhysicsGlobals.ThermalDataDisplay;
                Fields["displayExposedAreas"].guiActive = PhysicsGlobals.ThermalDataDisplay;

                if ((object)myWindow != null)
                {
                    myWindow.displayDirty = true;
                }
            }
        }
        
        public void CheckGeeForces()
        {
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !CheatOptions.UnbreakableJoints)
            {
                if (dead || (object)vessel == null || TimeWarp.fixedDeltaTime > 0.5 || TimeWarp.fixedDeltaTime <= 0)
                    return; // don't check G-forces in warp
                
                double geeForce = vessel.geeForce_immediate;
                if (geeForce > 40 && geeForce > lastGForce)
                {
                    // G forces over 40 are probably a Kraken twitch unless they last multiple frames
                    displayGForce = displayGForce * (1 - TimeWarp.fixedDeltaTime) + (float)(lastGForce * TimeWarp.fixedDeltaTime);
                }
                else
                {
                    //keep a running average of G force over 1s, to further prevent absurd spikes (mostly decouplers & parachutes)
                    displayGForce = displayGForce * (1 - TimeWarp.fixedDeltaTime) + (float)(geeForce * TimeWarp.fixedDeltaTime);
                }
                if (displayGForce < ReentryPhysics.crewGMin)
                    gExperienced = 0;
                
                //double gTolerance;
                if (gTolerance < 0)
                {
                    if (is_engine && damageCube.averageDamage < 1)
                        gTolerance = (float)Math.Pow(UnityEngine.Random.Range(11.9f, 12.1f) * part.crashTolerance, 0.5);
                    else
                        gTolerance = (float)Math.Pow(UnityEngine.Random.Range(5.9f, 6.1f) * part.crashTolerance, 0.5);
                    
                    gTolerance *= ReentryPhysics.gToleranceMult;
                }
                if (gTolerance >= 0 && displayGForce > gTolerance)
                { // G tolerance is based roughly on crashTolerance
                    AddDamage(TimeWarp.fixedDeltaTime * (float)(displayGForce / gTolerance - 1), Vector3.zero);
                    if (!vessel.isEVA)
                    { // kerbal bones shouldn't sound like metal when they break.
                        gForceFX.audio.pitch = (float)(displayGForce / gTolerance);
                        PlaySound(gForceFX, damageCube.averageDamage * 0.3f + 0.7f);
                        is_gforce_fx_playing = true;
                    }
                }
                else if (is_gforce_fx_playing)
                {
                    double new_volume = (gForceFX.audio.volume *= 0.8f);
                    if (new_volume < 0.001f)
                    {
                        gForceFX.audio.Stop();
                        is_gforce_fx_playing = false;
                    }
                }
                if ((damageCube.averageDamage >= 1.0f || internalDamage >= 1f) && !dead)
                {
                    dead = true;
                    FlightLogger.fetch.LogEvent("[" + FormatTime(vessel.missionTime) + "] "
                                              + part.partInfo.title + " exceeded g-force tolerance.");
                    // TODO See if we still need this or similar code. Rewrite if needed. Remove if obsolete.
                    //if ( part is StrutConnector )
                    //{
                    //    ((StrutConnector)part).BreakJoint();
                    //}
                    
                    part.explode();
                    return;
                }

                float crewGKillChance = 1f-Mathf.Pow(1f-ReentryPhysics.crewGKillChance, TimeWarp.fixedDeltaTime / 0.02f);
                if (Math.Max(displayGForce, geeForce) >= ReentryPhysics.crewGMin)
                {
                    gExperienced += Math.Pow(Math.Min(Math.Abs(Math.Max(displayGForce, geeForce)), ReentryPhysics.crewGClamp), ReentryPhysics.crewGPower) * TimeWarp.fixedDeltaTime;
                    List<ProtoCrewMember> crew = part.protoModuleCrew; //vessel.GetVesselCrew();
                    if (gExperienced > ReentryPhysics.crewGWarn && crew.Count > 0)
                    {
                        if (DeadlyReentryScenario.displayCrewGForceWarning && gExperienced < ReentryPhysics.crewGLimit)
                            ScreenMessages.PostScreenMessage(ReentryPhysics.crewGWarningMsg);
                        else
                        {
                            // borrowed from TAC Life Support
                            if (UnityEngine.Random.Range(0f, 1f) < crewGKillChance)
                            {
                                int crewMemberIndex = UnityEngine.Random.Range(0, crew.Count - 1);
                                if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
                                {
                                    CameraManager.Instance.SetCameraFlight();
                                }
                                ProtoCrewMember member = crew[crewMemberIndex];
                                
                                ScreenMessages.PostScreenMessage(vessel.vesselName + ": Crewmember " + member.name + " died of G-force damage!", 30.0f, ScreenMessageStyle.UPPER_CENTER);
                                FlightLogger.fetch.LogEvent("[" + FormatTime(vessel.missionTime) + "] "
                                                          + member.name + " died of G-force damage.");
                                Debug.Log("*DRE* [" + Time.time + "]: " + vessel.vesselName + " - " + member.name + " died of G-force damage.");
                                
                                part.RemoveCrewmember(member);
                                member.Die();
                                if (vessel.isEVA)
                                    this.part.explode();
                            }
                        }
                    }
                }
                lastGForce = vessel.geeForce_immediate;
            }
        }
        
        public void AddDamage(float dmg, Vector3 dir)
        {
            if (dead || (object)part == null || (object)part.partInfo == null || (object)part.partInfo.partPrefab == null)
                return;
            //if(is_debugging)
            //    print (part.partInfo.title + ": +" + dmg + " damage");
            if (dir == Vector3.zero)
                damageCube.AddCubeDamageAll(dmg);
            else
                damageCube.AddCubeDamageFacing(dir, dmg);

            ProcessDamage();
            SetDamageLabel ();
        }

        public void AddInternalDamage(float dmg)
        {
            this.internalDamage = Mathf.Clamp01(this.internalDamage + (dmg / 100));
            ProcessDamage();
            SetDamageLabel();
        }
        
        public void ProcessDamage()
        {
            if (!vessel.isEVA)
            {
                damageCube.ProcessDamage();
                part.breakingForce = part.partInfo.partPrefab.breakingForce * (1 - damageCube.averageDamage);
                part.breakingTorque = part.partInfo.partPrefab.breakingTorque * (1 - damageCube.averageDamage);
                part.crashTolerance = part.partInfo.partPrefab.crashTolerance * (1 - 0.5f * damageCube.averageDamage);
            }
        }

        public void SetDamageLabel() 
        {
            if(!vessel.isEVA)
            {
                if((object)Events == null)
                    return;

                float maxDamage = Mathf.Max(internalDamage, damageCube.maxDamage);

                if(maxDamage > 0.5)
                    Events["RepairDamage"].guiName = "Repair Critical Damage";
                else if(maxDamage > 0.25)
                    Events["RepairDamage"].guiName = "Repair Heavy Damage";
                else if(maxDamage > 0.125)
                    Events["RepairDamage"].guiName = "Repair Moderate Damage";
                else if(maxDamage > 0)
                    Events["RepairDamage"].guiName = "Repair Light Damage";
                else
                    Events["RepairDamage"].guiName = "No Damage";
            }
        }

        public float RangePercent(float min, float max, float value)
        {
            return (value - min) / (max - min);
        }

        public double RangePercent(double min, double max, double value)
        {
            return (value - min) / (max - min);
        }

        public void CheckForFire()
        {
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !CheatOptions.IgnoreMaxTemperature)
            {
                if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.PRELAUNCH || FlightGlobals.ActiveVessel.missionTime > 2.0)
                {
                    if (dead)
                        return;

                    if (part.temperature > maxOperationalTemp)
                    {
                        // for scream / fear reaction ratio, use scalding water as upper value
                        float tempRatio = (float)RangePercent(maxOperationalTemp, 322.15, part.temperature);

                        if (part.mass > 1)
                            tempRatio /= part.mass;
                        
                        AddInternalDamage(TimeWarp.fixedDeltaTime * tempRatio);

                        if (vessel.isEVA && tempRatio >= 0.089 && nextScream <= DateTime.Now)
                        {
                            ReentryReaction.Fire(new GameEvents.ExplosionReaction(0, tempRatio));
                            PlaySound(screamFX, tempRatio);
                            nextScream = DateTime.Now.AddSeconds(15);
                        }
                    }

                    if (part.skinTemperature > skinMaxOperationalTemp && damageCube != null)
                    {
                        // Handle client-side fire stuff.
                        // OH GOD IT'S ON FIRE.
                        float tempRatio = (float)RangePercent(skinMaxOperationalTemp, part.skinMaxTemp, part.skinTemperature);
                        tempRatio *= (float)(part.skinTemperature / part.skinMaxTemp);
                        AddDamage(TimeWarp.fixedDeltaTime * tempRatio, part.partTransform.InverseTransformDirection(-this.vessel.upAxis));
                        float soundTempRatio = (float)(tempRatio);
                        PlaySound(ablationFX, soundTempRatio);

                        if (vessel.isEVA)
                            PlaySound(screamFX, 1f);

                        if (damageCube.averageDamage >= 1.0f)
                        { // has it burnt up completely?

                            List<ParticleSystem> fxs = ablationFX.fxEmittersNewSystem;
                            for (int i = fxs.Count - 1; i >= 0; --i)
                                GameObject.Destroy(fxs[i].gameObject);
                            fxs = ablationSmokeFX.fxEmittersNewSystem;
                            for (int i = fxs.Count - 1; i >= 0; --i)
                                GameObject.Destroy(fxs[i].gameObject);
                            fxs = screamFX.fxEmittersNewSystem;
                            for (int i = fxs.Count - 1; i >= 0; --i)
                                GameObject.Destroy(fxs[i].gameObject);
                            
                            if (false && !dead)
                            {
                                dead = true;
                                FlightLogger.fetch.LogEvent("[" + FormatTime(vessel.missionTime) + "] "
                                    + part.partInfo.title + " burned up from overheating.");
                                
                                // TODO See if we still need this or similar code. Rewrite if needed. Remove if obsolete.
                                //if ( part is StrutConnector )
                                //{
                                //    ((StrutConnector)part).BreakJoint();
                                //}
                                
                                part.explode();
                                return;
                            }
                        }
                        else
                        {
                            is_on_fire = true;
                            List<ParticleSystem> fxs = ablationFX.fxEmittersNewSystem;
                            ParticleSystem fx;
                            for (int i = fxs.Count - 1; i >= 0; --i)
                            {
                                fx = fxs[i];
                                fx.gameObject.SetActive(true);
                                fx.gameObject.transform.LookAt(part.transform.position + vessel.srf_velocity);
                                fx.gameObject.transform.Rotate(90, 0, 0);
                            }
                            fxs = ablationSmokeFX.fxEmittersNewSystem;
                            for (int i = fxs.Count - 1; i >= 0; --i)
                            {
                                fx = fxs[i];
                                fx.gameObject.SetActive(vessel.atmDensity > 0.02);
                                fx.gameObject.transform.LookAt(part.transform.position + vessel.srf_velocity);
                                fx.gameObject.transform.Rotate(90, 0, 0);
                            }
                            float distance = Vector3.Distance(this.part.partTransform.position, FlightGlobals.ActiveVessel.vesselTransform.position);
                            ReentryReaction.Fire(new GameEvents.ExplosionReaction(distance, tempRatio));
                        }
                    }
                    else if (is_on_fire)
                    { // not on fire.
                        is_on_fire = false;

                        List<ParticleSystem> fxs = ablationFX.fxEmittersNewSystem;
                        for (int i = fxs.Count - 1; i >= 0; --i)
                            fxs[i].gameObject.SetActive(false);
                        fxs = ablationSmokeFX.fxEmittersNewSystem;
                        for (int i = fxs.Count - 1; i >= 0; --i)
                            fxs[i].gameObject.SetActive(false);
                    }
                    // Now: If a hole got burned in our hull... start letting the fire in!
                    if (part.machNumber >= 1)
                    {
                        float damage = damageCube.GetCubeDamageFacing(part.partTransform.InverseTransformDirection(-this.vessel.upAxis));
                        if (damage > 0f && vessel.externalTemperature > part.temperature)
                        {
                            double convectiveFluxLeak = part.thermalConvectionFlux * (1 - (part.temperature / vessel.externalTemperature)) * (double)damage;
                            part.AddThermalFlux(convectiveFluxLeak);
                        }
                    }
                    else
                    {
                        float damage = damageCube.averageDamage;
                        if (damage > 0f && part.ptd != null)
                        {
                            double convectiveFluxLeak = (double)damage * Math.Abs(part.ptd.convectionFlux) * (1 - (part.temperature / part.ptd.postShockExtTemp));
                            part.AddThermalFlux(convectiveFluxLeak);
                        }
                    }
                }
            }
        }
        
        public GameObject Emitter(string fxName)
        {
            GameObject fx = (GameObject)UnityEngine.Object.Instantiate (UnityEngine.Resources.Load ("Effects/" + fxName));
            
            fx.transform.parent = part.transform;
            fx.transform.localPosition = new Vector3 (0, 0, 0);
            fx.transform.localRotation = Quaternion.identity;
            fx.SetActive (false);
            return fx;      
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (maxOperationalTemp < 0d || maxOperationalTemp > part.maxTemp)
                maxOperationalTemp = useLowerOperationalTemp ? part.maxTemp * (is_engine ? 0.975 : 0.85) : part.maxTemp;
            if (skinMaxOperationalTemp < 0d || skinMaxOperationalTemp > part.skinMaxTemp)
                skinMaxOperationalTemp = useLowerOperationalTemp ? part.skinMaxTemp * (is_engine ? 0.975 : 0.85) : part.skinMaxTemp;

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // disable any menu items that might cause trouble on an EVA Kerbal
            if (vessel.isEVA)
                Events["RepairDamage"].guiActive = false;
            else
            {
                ProcessDamage();
                SetDamageLabel();
            }
            flightIntegrator = vessel.gameObject.GetComponent<FlightIntegrator>();
        }

        static void print(string msg)
        {
            MonoBehaviour.print("[DeadlyReentry.ModuleAeroReentry] " + msg);
        }

        protected class DamageCube
        {
            public float[] damage = new float[6];
            public float totalDamage = 0f;
            public float averageDamage = 0f;
            public float maxDamage = 0f;

            public void AddCubeDamageFacing(Vector3 dir, float damageTaken)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 faceDirection = DragCubeList.GetFaceDirection((DragCube.DragFace)i);
                    float dotProduct = Vector3.Dot(dir, faceDirection);
                    if (dotProduct > 0f)
                    {
                        this.damage[i] = Mathf.Min(this.damage[i] + (damageTaken * dotProduct), 1f);
                    }
                }
            }

            // This is for g-force related damage
            public void AddCubeDamageAll(float damageTaken)
            {
                for (int i = 0; i < 6; i++)
                {
                    this.damage[i] = Mathf.Min(this.damage[i] + damageTaken, 1f);
                }
            }

            public void ProcessDamage()
            {
                totalDamage = damage.Sum();
                averageDamage = totalDamage / 6f;
                maxDamage = damage.Max();
            }

            public float GetCubeDamageFacing(Vector3 dir)
            {
                float damage = 0f;
                int faceCount = 0;
                for (int i = 0; i < 6; i++)
                {
                    Vector3 faceDirection = DragCubeList.GetFaceDirection((DragCube.DragFace)i);
                    float dotProduct = Vector3.Dot(dir, faceDirection);
                    if (dotProduct > 0f)
                    {
                        damage += this.damage[i] * dotProduct;
                        faceCount++;
                    }
                }
                return damage / faceCount; // average it
            }

            public float GetCubeDamageTotal()
            {
                float dmg = 0f;

                for (int i = 0; i < 6; i++)
                {
                    dmg += this.damage[i];
                }
                return dmg;
            }

            public void RepairCubeDamage(int possessedSkill)
            {
                int requiredSkill = 0;

                for (int i = 0; i < 6; i++)
                {
                    if (damage[i] > 0.75)
                        requiredSkill = 5;
                    else if (damage[i] > 0.375)
                        requiredSkill = 4;
                    else if (damage[i] > 0.1875)
                        requiredSkill = 3;
                    else if (damage[i] > 0.09375)
                        requiredSkill = 2;
                    else if (damage[i] > 0)
                        requiredSkill = 1;

                    if (possessedSkill >= requiredSkill)
                    {
                        // just fix it all...?
                        damage[i] = 0f;
                    }
                }
            }

            public bool LoadFromString(string damageCubeString)
            {
                string[] data = damageCubeString.Split(new char[] {
                    ',',
                    ' '
                }, StringSplitOptions.RemoveEmptyEntries);
                if (data.Length != 6)
                {
                    Debug.Log("ModuleAeroReentry.DamageCube.LoadFromString() - Unable to deserialize: wrong number of entries in damage cube");
                    return false;
                }
                for (int i = 0; i < 6; i++)
                {
                    float num = 0f;
                    if (!float.TryParse(data[i], out num))
                    {
                        Debug.Log("Unable to parse float element of damage cube.");
                        return false;
                    }
                    this.damage[i] = num;
                }
                return true;
            }

            public string SaveToString()
            {
                string text = string.Empty;
                for (int i = 0; i < 6; i++)
                {
                    if (text != string.Empty)
                    {
                        text += ", ";
                    }
                    text += this.damage[i].ToString();
                }
                return text;
            }
        }
    }

    class ModuleKerbalAeroReentry : ModuleAeroReentry, IAnalyticTemperatureModifier
    {
        [KSPField]
        double heatRejection = 0.1; // real suit could get rid of 297 watts but KSP thermal is weak and forgiving and we're not adding body heat

        [KSPField(isPersistant = false, guiActive = false, guiName = "Health", guiUnits = "%", guiFormat = "F0")]
        string injury = "";


        private double _toBeSkin;
        private double _toBeInternal;
        private double desiredSuitTemp = 310.15;

        [KSPField(isPersistant = true)]
        public bool needsSuitTempInit = true;

        [KSPField(isPersistant = true)]
        private string EVATestProperty = "default value";

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onCrewOnEva.Add(OnCrewOnEva);
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

            base.OnStart(state);

            if (this.needsSuitTempInit)
            {
                this.part.temperature = desiredSuitTemp;
                this.needsSuitTempInit = false;
            }
            //this.part.gaugeThresholdMult = this.gaugeThresholdMult;
            //this.part.edgeHighlightThresholdMult = this.edgeHighlightThresholdMult;
            Debug.Log("[ModuleKerbalAeroReentry.OnStart()] EVATestProperty = " + EVATestProperty);
        }

        public void OnVesselGoOffRails(Vessel v)
        {
            if ((object)v != null)
            {
                //this.part.gaugeThresholdMult = this.gaugeThresholdMult;
                //this.part.edgeHighlightThresholdMult = this.edgeHighlightThresholdMult;
            }
        }

        public override void FixedUpdate()
        {
            // give Kerbals minimal heat rejection ability
            double tempDelta = part.temperature - desiredSuitTemp;

            if (tempDelta > 0)
            {
                // TODO maybe put in safeguards against excessively high cooling rates but the default is low enough to be fine
                part.AddThermalFlux(-heatRejection); // double heat added/removed until this is fixed in next KSP update
                part.AddSkinThermalFlux(heatRejection);
            }

            base.FixedUpdate();

            if (tempDelta <= 0 && this.internalDamage > 0)
            {
                this.internalDamage = Mathf.Clamp01(this.internalDamage - (0.00001f * TimeWarp.fixedDeltaTime)); // Kerbals will heal internal damage as long as they are not overheated.
            }

            if (this.internalDamage > 0)
            {
                Fields["injury"].guiActive = true;
                this.injury = ((1 - this.internalDamage) * 100).ToString();
            }
            else
                Fields["injury"].guiActive = false;

            if (EVATestProperty != "default value")
            {
                Debug.Log("EVATestProperty = " + EVATestProperty);
                EVATestProperty = "default value";
            }
        }

        // setting values in this event is pointless as they are reset to their default values in OnStart()
        public void OnCrewOnEva(GameEvents.FromToAction<Part, Part> eventData)
        {
            this.EVATestProperty = "value changed";
            Debug.Log("[ModuleKerbalAeroReentry.OnCrewOnEva()] EVATestProperty = " + EVATestProperty);
    }

        #region IAnalyticTemperatureModifier
        // Analytic Interface - only purpose is to pin Kerbal internal to a reasonable value
        public void SetAnalyticTemperature(FlightIntegrator fi, double analyticTemp, double toBeInternal, double toBeSkin)
        {
            _toBeSkin = toBeSkin;
            _toBeInternal = toBeInternal;
        }

        public double GetSkinTemperature(out bool lerp)
        {
            lerp = true;
            return _toBeSkin;
        }

        public double GetInternalTemperature(out bool lerp)
        {
            lerp = true;
            if (vessel.isEVA)
                return lastTemperature;
            else
                return _toBeInternal;
        }
        #endregion
    }

    class ModuleHeatShield : ModuleAblator
    {
        public PartResource _ablative = null; // pointer to the PartResource

        [KSPField()]
        protected double depletedMaxTemp = 1200.0;

        public new void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            
            base.Start();
            if((object)ablativeResource != null && ablativeResource != "")
            {
                if(part.Resources.Contains(ablativeResource))
                {
                    _ablative = part.Resources[ablativeResource];
                } else
                    print("ablative lookup failed!");
            }
            else
                print("Heat shield missing ablative! Probable cause: third party mod using outdated ModuleHeatShield configs.");
        }

        public new void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready)
                return;
            //if (ablativeResource == "")
                // silently fail.
            //    return;
            base.FixedUpdate ();
            // if less than one gram remaining
            if (_ablative.amount * _ablative.info.density <= 0.000001)
            {
                part.skinMaxTemp = Math.Min(part.skinMaxTemp, depletedMaxTemp);
                part.heatConductivity = part.partInfo.partPrefab.heatConductivity;
                // TODO Think about removing the next two lines; skin-skin and skin-internal depend on heatConductivity so this could be overkill
                //part.skinSkinConductionMult = Math.Min(part.skinSkinConductionMult, depletedConductivity);
                //part.skinInternalConductionMult = Math.Min(part.skinInternalConductionMult, depletedConductivity);
            }
        }
    }
    
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