This update is by the KSP-RO team.
ialdabaoth (who is awesome) created Deadly Reentry 2, based on r4m0n's Deadly Reentry; this is a continuation. This continues Starwaster's continuation of NathanKell's work on Deadly Reentry continued.

License remains CC-BY-SA as modified by ialdabaoth.
Also included: Module Manager (by sarbian, swamp_ig, and ialdabaoth). See Module Manager thread for details and license and source: http://http://forum.kerbalspaceprogram.com/threads/55219
Module Manager is required for DREC to work.

Deadly Reentry 8.0 for KSP 1.10+

INSTALL INSTRUCTIONS:
1. If you currently have Deadly Reentry installed, go to KSP/GameData/DeadlyReentry and delete everything (files and folders) except custom.cfg. Also delete any old versions of ModuleManager (modulemanager.dll for example) in your KSP/GameData folder.
2. Extract this archive to your KSP/GameData folder.

USAGE INSTRUCTIONS:
Be careful how you reenter. Make sure your craft has a heatshield (the Mk1 pod has a built-in heatshield, as do stock spaceplanes; the Mk1-2 needs a heat shield from the Thermal tab). For a low Kerbin orbit reentry, try for a periapsis of about 20km.

To change DRE settings, click the DRE menu button (looks like a capsule reentering)
DRE menu icon created by lajoswinkler
==========
Changelog:
v8.0.0
* Recompile and version bump for KSP 1.10+
* Changes to the operational-temp mechanic. Now it is much more generous for very low-temperature parts and very high-temperature parts. Solves issues where low-temp wings burn up too easily (or spaceplane performance changes drastically between DRE and non-DRE installs).
* Heatshields do not use the operational temp system.
* Get rid of FINAL patches for parachutes, now other mods can still interact with DRE's changes.

* Lots more code cleanup.
v7.8.0
* Refactoring of code
* Removed Monobehaviour calls from any constructors or code which would result in Monobehaviour being called from a constructor
* Moved temperature rebalancing to after a game save has been loaded (once only)
* The previous two items addresses KSP hanging at main menu. It cannot be guaranteed to correct such behaviour as I don't believe any one mod is responsible and in fact the more mods you have the more likely it is to happen. This only minimizes DREs contribution to the problem.
* Added DRE PAW grouping. (DRE fields are grouped and can be collapsed in the PAW)

v7.7.4
* Compile of The Maat Edition for KSP 1.8.x
* Functionally identical to 7.7.3.1.
* Players should not try to use this with versions of KSP older than 1.8.0

v7.7.3.1
* Recompile for KSP 1.7.x
* Released as The Maat Edition in honor of my cat Maat (named for the Egyptian goddess of balance and justice)

v7.7.3
* Fixed versioning
* Forced ModuleAeroReentry to Awake when added at runtime.
* Added DR reentry heat override in DR menu (up to 200% by default. Can be patched to support any arbitrary integer. 1 = 100%, 2 = 200%, 3 = 300% etc etc)
* WARNING: If the stock reentry slider is used to scale heat it will revert to the stock cap of 150%. I can't actually override the stock slider which is why I added one to the Deadly Reentry menu.
// example scale patch:
{
    maxHeatScale = 3 // this caps the reentry heat scale override menu slider at %300
}

v7.7.2
* Updated for KSP 1.6
* Fix for OPT config
* Support for future compatibility with all EVA Kerbals. (as long as the Kerbal has ModuleKerbalEVA then it is supported)

v7.7.1
* Compiled for KSP 1.5.1
* Compatibility restrictions removed. DR will no longer disable itself if invalid version detected. This does NOT guarantee compatibility. DR will try to work with invalid versions but you will still be warned of compatibility issues so you are warned.
* Changed how depleted conductivity is handled. Uses prefab.heatConductivity when ablator depleted.
* Making History configs added for Mk2Pod and round pods.
* Support added for new part config of old Mk1Pod. (Mk1Pod_v2)
* Updated KerbalEVA config to catch any PART with MODULE KerbalEVA (new kerbal PART's were added so I changed how this was being detected to guard against future additions)

v7.7.0
* Compatibility release for KSP 1.4.*
* Other configuration tweaks

v7.6.2
* Recompiled for KSP 1.3.1
* Adjusted heat shield lossConst and pyrolysisLossFactor for improved survivability.

v7.6.1
* Recompiled for KSP 1.3.0
* Rescaled tempRatio for inner kerbal temps. Lower limit = 317K and upper = 322 (scalding) - Clamped to 0-1 for sound volume. Unclamped for fear reaction animation
* Fixed Kerbals not spawning with correct inner temp.
* Fixed Kerbals not healing at *fixedDeltaTime
* Updated files for main DeadlyReentry.cfg,
* Scaled up star systems support. 
* (may not be adequate for all custom star systems. This is REALLY something that should be handled by those star system mods)
* Updated RealChute settings
* Moved inflatable configs into their own file
* Fixed 15m HIAD mass (what, like YOU were never off by three orders of magnitude???)
* Updated categories
* Disable overheating destruction. (Parts still burn up due to stock heating)
* skinMaxTemp wasn't being handled during temperature rebalancing.
* NaN detection changed to use Double.isNaN instead of comparing to Double.NaN

v7.6.0
* Finalized implementation of Max Operational Temp system
* Implemented 'damage cubes' similar to drag cubes except that hull damage is now directional.
* If vehicle skin takes enough damage facing its velocity vector then hot reentry gasses can enter the ship and damage internals directly
* Internal damage handled separately and is not directionally based.
* Kerbal max temp rebalance finalized. Suits (skin) can withstand up to ~900 K but the Kerbal inside starts taking damage at 317 K
* EVA Kerbals now have a small amount of cooling. (500 watts but subject to rebalancing)
* Kerbals scream when on fire.
* Reinstituted menu setting for turning off crew g-force warning message.
* Fixed problem with calling part.Modules[*] (replaced with FindModuleImplementing)

v7.5.0
* KSP compatibility 1.2.2 Update.
* Commented out StrutConnector fixes. (StrutConnector changes? Have to monitor strut situation and see if original problem still exists)
* Fixes to RSSROConfig handling
* Added ModuleTransform2Value (works like ModuleAnimation2Value except that the value depends on the state (active/inactive) of a designated mesh object) (all chutes use this now both stock and RC)
* Added framework configuring max/operation temp values inModuleAeroReentry

v7.4.7.1
* Removed ablator from PF fairings. (addresses negative cost issue)

v7.4.7
Compiled for KSP 1.1.3
Updated versioning information

v7.4.5.1
* Don't delete leaveTemp. (causes errors in latest versions of Module Manager)
* Changes to vernier thermals. (increased survivability of the Vernor RCS part)

v7.4.5
* Fix flag exploding when switching to flag. (prevent flag from having
ModuleAeroReentry added to it)
* Add additional debug logging to FixMaxTemps(). Status of parts that
are skipped due to leaveTemp = true are logged. Parts that are adjusted
are logged.
* Adjusting RSS fallback config. (used when Real Solar System is
installed but Realism Overhaul is not and RSSROConfig is not set.
* Possible fix for explosion/burning sounds being too loud for distant
objects.
* Added additional case handling for #leaveTemp.
* Removed toolbar from Main Menu

v7.4.4
* Adjusting RSS fallback config. (used when Real Solar System is installed but Realism Overhaul is not and RSSROConfig is not set.
* Possible fix for explosion/burning sounds being too loud for distant objects.

v7.4.3
* Reworked previous fix for KIS/KAS. (catch KerbalEVA and prevent damage code from running on it during part initialization)
* Updated ModuleManager for KSP 1.1.2
* Recompiled for KSP 1.1.2
* Updated versioning information
* Extended compatibility checking to more code sections.

v7.4.2
* Fixed DefaultSettings.cfg (crew G limits and crew G Min were accidentally reverted to older bad values)
* Adjusted crew G limits (metric is 20g starts to be dangerous after 10 sec. 6g after 77sec)

v7.4.1
* Disabled some damage system code if part is KerbalEVA. (esp in OnStart())
* Inflatable heat shield rebalancing: Reduced mass to 1 ton. Reduced thermal mass modifier to to 1. Increased skin thermal mass modifier to 1.41. Adjusted absorptiveConstant. Adjusted conductive factors.

v7.4.0
* Updated and compiled for KSP 1.1

v7.3.2
* Reimplemented menu. (reverted previous changes to get it functional
again. Old menu duplication bug probably reverted as well)
* Fixed issue with settings changes not applied.
* Menu automatically writes changes to custom.cfg
* Moved g force settings out of ModuleAeroReentry to ReentryPhysics
* Added leaveTemp to spaceplane parts ModuleAeroReentry
* Tweaked Space Plane part configs
* Added missing gToleranceMult to default settings. (part G tolerance)
* Updated versioning info
* Removed deprecated config settings from code
* Reworked DRE Scenario Module

v7.3.1
* Added skill check for damage above 0.75 (requires skill level 5)
* No fire damage if CheatOptions.IgnoreMaxTemperature == true
* No G-Force damage if CheatOptions.UnbreakableJoints == true
* Only run toolbar code once. (addresses duplicates created when database reloaded)
* Tweaked Mk1 Pod thermals (max temp, heat shield) to address complaints that pod is burning up too easily.
* Updated RSS fallback heat shield configs

v7.3.0
* KSP 1.0.5 compatibility update
* Code cleanup of extraneous DRE 7.1.0 skin remnants.
* Fire damage reinstated
* Repairing of damage now requires an engineer on EVA - the  more badly damaged the part, the greater the skill required.
* Damaged parts have lowered tolerance to further overheating and may break loose easier. (skinMaxTemp, breakingForce and breakingTorque are all reduced)
* Part configuration patches tweaked.
* It's still the Melificent Edition.
* Almost reinstated DRE specific menu options.

v7.2.2
* Adjusted all DRE shield part cost and mass. (adjusted cost to account
for resource  problem described in issue #24 and adjusted heat shield
masses to saner values)
* Adjusted cost in Procedural Fairings to account for resource problem
described in issue #24. (both stock fairing and PF mod)
* screen message formatting
* Corrected flux formatting for displays.
* Approximating total absorbed heat in joules. (displayed in part
context menu for total convective heat when over Mach 1)
* Removed settings for chute warning messages since DRE no longer
implements chute failures.
* Version revision restriction. From this point on, revision restriction
	in effect. DRE will not run on anything older than 1.0.4 and will also
	fail on future updates until an updated version is released.
* RSS specific tweaks. (modify lossConst / pyrolysisLossFactor to allow shields to survive reentry)
* globally changed reentryConductivity to 0.001 (insulation allows 1 W / kW)
* Implemented depletion threshold for maxTemps/conductivity changes. 
* increased depletedConductivity to 20 from 1. 
  (insulation burns up and becomes useless. Fiery plasma sweeps through your craft incinerating all in its path. Hilarity ensues)
* Space is a tough place where wimps eat flaming plasma death.

v7.2.1
* Removed Modular Flight Integrator dependency

v7.2.0
* Deadly Reentry no longer implements reentry heating. Instead it tweaks parameters to make stock reentry deadlier.
* Deadly Reentry still handles G-force damage.
* Still no menu. (sorry! Cute cat still there!)
* Configs for all parts previously handled by Deadly Reentry have been edited to take advantage of new stock skin system.
* Spaceplane handling is a bit experimental and relies on having a skin with VERY low thermal mass which increases the heat loss from radiation. 
  (use VERY shallow reentries for spaceplanes and reentries will be survivable but difficult. Consider turning off the heat gauges or you will get a frightful scare when you do spaceplane reentries)
* (no, seriously, turn the heat gauges off...)
* skinMaxTemp tends to be higher than maxTemp which now represents internal temp, including resource temp.
* ModularFlightIntegrator is still a dependency but is not currently used by Deadly Reentry.

v7.1.0
* Added heat shield char support. (not all shields)
* Major changes to skin conduction, radiation and convection
* Skin percentage is now actually a percentage of thermal mass. (i.e. part thermal mass goes down as skin thermal mass goes up)
* Heat shield aerodynamics fixed. (stable when blunt end forwards for all DRE shields & ADEPT shields)
* Heat shield decoupler: texts fixed. Unused decouplers removed. 0.625m decoupler added.
* NaN checking
* MOAR NaN checking
* Moved away from foreach usage. (you shouldn't use foreach, m'kay? foreach is bad.... m'kay?)
* Delete audio on destroy
* reimplemented engine detection
* RO support
* Depleted shields burn easier
* 1kg minimum part mass enforced. (in calculations only; part mass is not touched)
* Fixed 3.75m shield normal map
* Patching of KSO parts to remove obsolete pre DRE 7 configs.


v7.0.3
* Calculate what pecentage of skin is actually facing the shockwave and use only that percent for thermalMass
* Add OnDestroy() and null the FlightIntegrator cache
* Added additional check for part.ShieldedFromAistream
* Buffed fuel tank maxTemp
* Fixed typo in DRE heat shields

v7.0.2
* Removed legacy engine configurations which were adding pre-KSP 1.0 levels of heat production. (FIRE BAD!)
* Fixed duplicate toolbar button issue
* Tweaked convection heating to start EARLIER. Tweaked stock shields to (more or less)
* Put in checks and guards against null ref errors in UpdateConvection()

v7.0.1
* Fixed stack bottom attach nodes.

v7.0
* Deadly Reentry rewritten from the ground up to take advantage of stock thermodynamics.
* Skin temperature implemented. part.temperature now represents a parts interior  temperature. Skin and part temperatures are tracked separately. Because the skin tends to be thinner it will usually be very much easier to burn through.
* ModuleHeatShield uses the same format as KSP 1.0's ModuleAblator. The old heat shield format is deprecated and no longer used.
* Heat shield reflective property is now replaced by the part's emissiveConstant. In theory, subtract the reflective value from 1.0. That is the emissiveConstant. In practice, parts will have a minimum value of 0.4. Values of 0.6 - 0.95 represent fairings and passive heat shields such as space shuttle tiles and other non ablatives.

v6.4
*Added toolbar button (for stock toolbar)
*Added Easy, Normal and Hard difficulty settings accessible from new menu
*Difficulty settings are per-save game!!! Use Easy for sandbox and Hard for Career! (if you want)
*Alternate lower density calculation (for use with Hard mode to prevent excessive heating for high speed aircraft)
*Fix for stuttering AeroFX cases. (thanks to Motokid600, Chezburgar7300, Zeenobit and Noio for feedback and/or testing)
*Reworked warning messages for visibility and/or optimization
*Optimized density calculations (moved all into ReentryPhysics; no more per-part calculations)
*Lowered part max-temperature cap to 1250 (other parts may be even lower)
*Heat shields now insulate attached parts against conducted heat
*Low grade heat shielding added to nose cones and fairings. (also to parachute 'cone' parts. Deploying chutes 'jettisons' the shield)
*Kerbals now react to reentry events such as overheating. (may need tweaking; even Jebediah gets scared now. Can't have that)
*Merged in fixes from NathanKell for FAR detection
*Merged in changes from NathanKell to support R&D / Technology requirements
*Added support for (currently unused)stock KSP airstream shielding
*Trapping and checking for of null reference errors in events.

v6.2.1
*Debug Menu saves survive quick load and reverting. (added extra save
function to update the loaded REENTRY_EFFECTS ConfigNode)
*Changed crewGKillChance from double to float. (fixes error in debug
menu when changing this field)
*Fixed bug with RealChutes not cutting and/or spamming FlightLog
*FixedparachuteTempMult not saving from debug menu.
*Added FlowerChild's fix for StrutConnectors not destructing their
reinforcing joints when they explode.

v6.2
*Fixed issue with Jool NaN temperature. (capped low end of getExternalTemperature() to -160)
*Capped low end of ambientTemperature to absolute zero.
*NaN protection for part.temperature
*Added density field to debug GUI
*Replaced hard coded gas constant with per-planet specificGasConstant. (to-do: move that data to config files)
*ReentryPhysics still uses hard coded 287.058 value
*Added flight event logging for parachute failures.
*Added legacyAero config file option. If present and true then density retrieved from vessel.atmDensity

v6.1
*Fixed typos in SPP.cfg and Wings.cfg (some parts were not getting
shielded)
*Additional sanity check when raycasting for parts shielding parts.
*Added logic check to make sure a chute was actually exposed to
damaging temperatures when deployed
*Groundwork for toolbar support. (in-game difficulty per save game
coming soon)

v6.0
*Support KSP 0.25
*Reverted to old density exponent
*Support SPP stock parts
*Give wings heat shielding

v5.3.2
*Revert to prior adjustCollider functionality; small parts should be shielded again.
*No longer ignore heating on physics-disabled parts.

v5.3.1
*Fixed stupid typos (thanks Starwaster). Apologies, folks.

v5.3
*DRE will now calculate atmospheric density from temperature and pressure, assuming an atmosphere like Earth's.
*Chute-cutting logic improved; tweakable max chute temperature added.
*Removed B9 part configs (done by the B9 mod itself).
*Display ambient temperature in right-click menu.
*Corrected Celsius/Kelvin conversions, clamp part temperature to absolute zero.
*Sped up the update loop a fair amount
*Fixed a shielding raycast bug
*Avoid some VAB/SPH slowdown/logspam
*Use vessel velocity as the reference frame, not the part (might help with rotors and wobbling parts)
*Made burnup FX (when a part is burning up) occur higher in the atmosphere
*Allow tweaking the reentry FX by applying an exponent to density as used by it. Defaults to 0.7, so they start appearing earlier on reentry than they used to.
*Tweak part burning rate and damage handling.
*Tweak drag etc of inflatable shield (Starwaster)
*FINAL PRE-STARWASTER EDITION

v5.2
*Updated for 0.24.2

v5.1
*Recompiled for 0.24.1
*Nerfed overpowered 1.25m heatshield, had double the dissipation it should.

v5.0
*Now will cut chutes when they burn up, rather than destroying the part. Max chute temperature defaults to 1/2 part max temperature
*Fixed issue with RealChutes compatibility

v4.8 \/
*Starwaster: Fix handling of FS animations, fix inflatable heat shield properties.

v4.7 \/
*Fixed heatshield floatcurves not having tangents (got some unexpected behavior).
*Fix for overriding a part's g tolerance not working
*Upgrade to ModuleManager v2.1.0

v4.61= \/
*Fixed 6.25m heatshield animation modules (thanks Sage!)
*Fixed typos in decouplers (thanks DispleasedScottie!)


v4.6 = \/
*Made AblativeShielding tweakable.
*Added fix from HoneyFox to detect shielded parts the way FAR does
*Added version checking (per Majiir's template)
*Recompiled for 0.23.5

v4.5 = \/
*Fixed compatibility with RealChutes (thanks Starwaster!)
*Attempted just-in-case fix for HotRockets, etc.

v4.4 = \/
*Removed redundant Awake() code - thanks a.g.!

v4.3 = \/
*(Engine) overheating bug fixed thanks to FlowerChild

v4.2 = \/
*Updated cfgs to support new lab (note: Rapier handled automatically)
*Now supports RealChutes
*Speed increase by a.g.
*Now should properly rescale engine heat when engine maxTemp changed (for ModuleEngineFX too)

v4.1 = \/
*0.23 compatibility by taniwha and arsenic87
*maxTemp no longer changed if ModuleHeatshield is present. This should allow thermal sink-style heatshields, when combined with high reflectivity.

v4 === \/
*Removed :Final from custom.cfg
*Moved tech defines to part cfgs
*Added decouplers, used Sentmassen's new texture.
*Fixed explode-on-launch/switch bug
*Added possibility to manually set G Tolerance (Add a ModuleAeroReentry, with the key gTolerance = x , to your part cfg)
*Fixed volume to use Ship volume setting.

v3 === \/
*Added two more tweakable variables: shockwaveExponent and shockwaveMultiplier. shockwaveExponent is applied to shockwave temperature after it's calculated; then the temperature is multiplied by shockwaveMultiplier. To simulate Earth-level heating, use shockwaveExponent = 1.17 (can't be perfect, but it's close: you get a max shockwave temperature of ~6150C on reentry, a bit low; and 11800C on Munar reentry, a bit higher than Apollo 10).

v2.1 = \/
*Forgot to include the tech file and the 0.625 heatshield. Fixed.

v2 === \/
*Added tech nodes for parts. Thanks, Specialist290!
*Upped prelaunch disabling to 2 seconds.
*Added crew death on over-G. Kerbals have human-like G-force tolerance, which means they can survive 5Gs for about 16 minutes, 10 Gs for 1 minute, 20Gs for 3.75 seconds, and 30+ Gs for less than three quarters of a second. The tracker is reset when G load < 5. All these are tweakable in the cfg or in the ingame debug menu. It's done by tracking cumulative Gs. The formula is for each timestep, tracker = tracker + G^crewGPower * timestep (where ^ = power). G is clamped to range [0, crewGClamp]. When tracker > crewGWarn, a warning is displayed. When tracker > crewGLimit, then each frame, per part, generate a random number 0-1, and if > crewGKillChance, a kerbal dies in that part. If G < crewGMin, tracker is reset to 0.

v1 === \/
*Disabled during the first second of prelaunch to fix explode/overheat on launch bug
*added gToleranceMult to tweak the g limits of parts. Works as a global scalar. Currently set to 2.5

DOCUMENTATION ON THE HEAT SHIELD MODULE
As of KSP 1.0, Deadly Reentry no longer implements its own heat shield. Use the same config as for the stock ModuleAblator. The following two fields are also allowed in ModuleHeatShield:

depletedMaxTemp // when shield ablator is depleted it's maximum allowed temperature is reduced to this value
depletedConductivity // When shield ablator is depleted, the shield will conduct heat at this value. (higher)

MODULE
{
	name = ModuleHeatShield
	ablativeResource = AblativeShielding
	lossExp = -7500
	lossConst = 1
	pyrolysisLossFactor = 6000
	reentryConductivity = 0.001
	ablationTempThresh = 500
	depletedMaxTemp = 1200
	depletedConductivity = 20
	charMin = 1
	charMax = 1
}


// Then add a resource node.
RESOURCE
{
	name = AblativeShielding
	amount = 250
	maxAmount = 250
}

