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
        protected static ScreenMessage crewGWarningMsg = new ScreenMessage("<color=#ff0000>Reaching Crew G limit!</color>", 1f, ScreenMessageStyle.UPPER_CENTER);
        
        [KSPField]
        public bool leaveTemp = false;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Operational Temp", guiUnits = " K", guiFormat = "F2", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double maxOperationalTemp = -1d;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Max Operational Skin Temp", guiUnits = " K", guiFormat = "F2", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double skinMaxOperationalTemp = -1d;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Acceleration", guiUnits = " G", guiFormat = "F3", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double displayGForce;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Damage", guiUnits = "", guiFormat = "G", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public string displayDamage;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Cumulative G", guiUnits = "", guiFormat = "F0", groupDisplayName = "Deadly Reentry Debug", groupName = "DeadlyReentryDebug")]
        public double gExperienced = 0;

        [KSPField]
        public bool useLowerOperationalTemp = true;

        private double lastGForce = 0;
        protected double lastTemperature;

        [KSPField(isPersistant = true)]
        public bool dead;

        [KSPField]
        public float gTolerance = -1;

        private bool is_on_fire = false;
        private bool is_gforce_fx_playing = false;

        private bool is_engine = false;
        private double nextScreamUT = -1d;

        protected DamageCube damageCube;

        [KSPField(isPersistant = true)]
        protected float internalDamage;

        protected bool is_debugging = false;

        [KSPEvent(guiName = "No Damage", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 4f)]
        public void RepairDamage()
        {
            // We pass this off to the damage cube now.
            this.damageCube.RepairCubeDamage(FlightGlobals.ActiveVessel.VesselValues.RepairSkill.value);
            this.RepairInternalDamage(FlightGlobals.ActiveVessel.VesselValues.RepairSkill.value);

            ProcessDamage();
            SetDamageLabel();
            if (myWindow != null)
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

        UIPartActionWindow _myWindow = null; 
        UIPartActionWindow myWindow 
        {
            get
            {
                if(_myWindow == null)
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
        }
        FXGroup _gForceFX = null;
        FXGroup gForceFX 
        {
            get
            {
                if(_gForceFX == null)
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
                if(_ablationSmokeFX == null)
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
                if(_ablationFX == null)
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
                if (_screamFX == null)
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

            useLowerOperationalTemp &= part.FindModuleImplementing<ModuleAblator>() == null;
        }

        void OnDestroy()
        {
            if (_ablationFX != null)
            {
                if(_ablationFX.audio != null)
                GameObject.Destroy(_ablationFX.audio);
                foreach (var ps in _ablationFX.fxEmittersNewSystem)
                    if (ps.gameObject != null)
                        GameObject.Destroy(ps.gameObject);
            }
            if(_ablationSmokeFX != null)
            {
                if (_ablationSmokeFX.audio != null)
                    GameObject.Destroy(_ablationSmokeFX.audio);
                foreach (var ps in _ablationSmokeFX.fxEmittersNewSystem)
                    if (ps.gameObject != null)
                        GameObject.Destroy(ps.gameObject);
            }
            if (_gForceFX != null)
            {
                if (_gForceFX.audio != null)
                    GameObject.Destroy(_gForceFX.audio);
                foreach (var ps in _gForceFX.fxEmittersNewSystem)
                    if (ps.gameObject != null)
                        GameObject.Destroy(ps.gameObject);
            }
            if (_screamFX != null)
            {
                if (_screamFX.audio != null)
                    GameObject.Destroy(_screamFX.audio);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasValue("damageCube"))
            {
                this.damageCube.LoadFromString(node.GetValue("damageCube"));
            }
        }

        protected double OperationalTempOffset(double temp)
        {
            double offsetMaxTemp = Math.Max(0d, temp - ReentryPhysics.minTempForCalcOperationalTemp);
            return Math.Max(ReentryPhysics.minOperationalTempOffset, Math.Min(ReentryPhysics.minTempForCalcOperationalTemp, 
                offsetMaxTemp * (is_engine ? 0.025 : HighLogic.CurrentGame.Parameters.CustomParams<DeadlyReentrySettings>().PartOperationalTempThreshold)));
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
                maxOperationalTemp = useLowerOperationalTemp ? part.maxTemp - OperationalTempOffset(part.maxTemp) : part.maxTemp;
            if (part.skinMaxTemp < skinMaxOperationalTemp)
                skinMaxOperationalTemp = useLowerOperationalTemp ? part.skinMaxTemp - OperationalTempOffset(part.skinMaxTemp) : part.skinMaxTemp;

            // sanity checking
            if (Double.IsNaN(part.temperature))
                part.temperature = 297.6;
            if (Double.IsNaN(part.skinTemperature))
                part.skinTemperature = Math.Min(this.skinMaxOperationalTemp, vessel.externalTemperature);
            
            CheckForFire();
            CheckGeeForces();
            
            if (TimeWarp.CurrentRate <= PhysicsGlobals.ThermalMaxIntegrationWarp)
                lastTemperature = part.temperature;
        }

        public virtual void Update()
        {
            if (is_debugging != PhysicsGlobals.ThermalDataDisplay)
            {
                is_debugging = PhysicsGlobals.ThermalDataDisplay;

                Fields[nameof(skinMaxOperationalTemp)].guiActive = PhysicsGlobals.ThermalDataDisplay;
                Fields[nameof(maxOperationalTemp)].guiActive = PhysicsGlobals.ThermalDataDisplay;

                if (myWindow != null)
                {
                    myWindow.displayDirty = true;
                }
            }
        }
        
        public void CheckGeeForces()
        {
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !CheatOptions.UnbreakableJoints)
            {
                if (dead || vessel == null || TimeWarp.fixedDeltaTime > 0.5 || TimeWarp.fixedDeltaTime <= 0)
                    return; // don't check G-forces in warp
                
                double geeForce = vessel.geeForce;
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
                    gTolerance = (float)part.gTolerance * HighLogic.CurrentGame.Parameters.CustomParams<DeadlyReentrySettings>().PartGToleranceThreshold * UnityEngine.Random.Range(0.95f, 1.05f);
                }
                if (gTolerance >= 0 && displayGForce > gTolerance)
                {
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
                    GameEvents.onOverG.Fire(new EventReport(FlightEvents.OVERG, part, part.partInfo.title, "", 0, part.vessel.geeForce.ToString("F0") + " / " + part.gTolerance.ToString("F0") + " G", part.explosionPotential));
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
                        if (HighLogic.CurrentGame.Parameters.CustomParams<DeadlyReentrySettings>().DisplayCrewGForceWarning)
                        {
                            ScreenMessages.PostScreenMessage(crewGWarningMsg);
                        }
                        if (gExperienced > ReentryPhysics.crewGLimit && HighLogic.CurrentGame.Parameters.CustomParams<DeadlyReentrySettings>().CrewDieFromG)
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
                                FlightLogger.fetch.LogEvent("[" + KSPUtil.PrintTimeStamp(vessel.missionTime, false, false) + "] " + member.name + " died of G-force damage.");
                                Debug.Log("*DRE* [" + Time.time + "]: " + vessel.vesselName + " - " + member.name + " died of G-force damage.");

                                part.RemoveCrewmember(member);
                                member.Die();
                                if (vessel.isEVA)
                                    this.part.explode();
                            }
                        }
                    }
                }
                lastGForce = geeForce;
            }
        }
        
        public void AddDamage(float dmg, Vector3 dir)
        {
            if (dead || part == null || part.partInfo == null || part.partInfo.partPrefab == null)
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
                if(Events == null)
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
                    
                    double UT = Planetarium.GetUniversalTime();

                    if (part.temperature > maxOperationalTemp)
                    {
                        // for scream / fear reaction ratio, use scalding water as upper value
                        float tempRatio = (float)RangePercent(maxOperationalTemp, part.maxTemp, part.temperature);

                        if (part.mass > 1)
                            tempRatio /= part.mass;
                        
                        AddInternalDamage(TimeWarp.fixedDeltaTime * tempRatio);

                        if (vessel.isEVA && tempRatio >= 0.089 && nextScreamUT <= UT && screamFX != null)
                        {
                            // Only FlightCameraFX and the Kerbals listen to this, so it's probably safe
                            GameEvents.onPartExplode.Fire(new GameEvents.ExplosionReaction(0, tempRatio));
                            PlaySound(screamFX, tempRatio);
                            nextScreamUT = UT + 15d;
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

                        if (vessel.isEVA && nextScreamUT <= UT)
                        {
                            PlaySound(screamFX, 1f);
                            nextScreamUT = UT + 15d;
                        }

                        if (damageCube.averageDamage >= 1.0f)
                        { // has it burnt up completely?

                            if (!dead)
                            {
                                dead = true;
                                GameEvents.onOverheat.Fire(new EventReport(FlightEvents.OVERHEAT, part, part.partInfo.title, "", 0, "skin took too much damage from overheating", part.explosionPotential));

                                part.explode();
                                return;
                            }
                        }
                        else
                        {
                            is_on_fire = true;
                            ParticleSystem fx;
                            List<ParticleSystem> fxs = ablationFX.fxEmittersNewSystem;
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
                            // Only FlightCameraFX and the Kerbals listen to this, so it's probably safe
                            GameEvents.onPartExplode.Fire(new GameEvents.ExplosionReaction(distance, tempRatio));
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
                    double machLerp = Math.Pow(UtilMath.Clamp01((part.machNumber - PhysicsGlobals.NewtonianMachTempLerpStartMach) / (PhysicsGlobals.NewtonianMachTempLerpEndMach - PhysicsGlobals.NewtonianMachTempLerpStartMach)), PhysicsGlobals.NewtonianMachTempLerpExponent);
                    double damage = UtilMath.Lerp(damageCube.averageDamage, damageCube.GetCubeDamageFacing(part.partTransform.InverseTransformDirection(-this.vessel.upAxis)), machLerp);
                    if (damage > 0d && part.ptd.postShockExtTemp > part.temperature)
                    {
                        double convectiveFluxLeak = part.ptd.finalCoeff * (part.ptd.postShockExtTemp - part.temperature ) * damage;
                        if (convectiveFluxLeak > 0d)
                            part.AddThermalFlux(convectiveFluxLeak);
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
                maxOperationalTemp = useLowerOperationalTemp ? part.maxTemp - OperationalTempOffset(part.maxTemp) : part.maxTemp;
            if (skinMaxOperationalTemp < 0d || skinMaxOperationalTemp > part.skinMaxTemp)
                skinMaxOperationalTemp = useLowerOperationalTemp ? part.skinMaxTemp - OperationalTempOffset(part.skinMaxTemp) : part.skinMaxTemp;

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

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            if (this.needsSuitTempInit)
            {
                this.part.temperature = desiredSuitTemp;
                this.needsSuitTempInit = false;
            }
            //this.part.gaugeThresholdMult = this.gaugeThresholdMult;
            //this.part.edgeHighlightThresholdMult = this.edgeHighlightThresholdMult;
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
            if (!string.IsNullOrEmpty(ablativeResource))
            {
                if (part.Resources.Contains(ablativeResource))
                {
                    _ablative = part.Resources[ablativeResource];
                }
                else
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
            }
        }
    }
}