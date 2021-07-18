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
    public class DeadlyReentrySettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Settings"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Deadly Reentry"; } }
        public override string DisplaySection { get { return "Deadly Reentry"; } }
        public override int SectionOrder { get { return 1; } }

        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("Display Crew Over-G Warning", toolTip = "Display a warning message when crew members are reaching their G limits and may die soon.")]
        public bool DisplayCrewGForceWarning = true;

        [GameParameters.CustomParameterUI("Crew Death from Over-G", toolTip = "Can crew members die from being subjected to long periods at very high G?")]
        public bool CrewDieFromG = true;

        [GameParameters.CustomFloatParameterUI("Part G Tolerance Threshold", toolTip = "Threshold of part G tolerance after which the part starts taking damage", minValue = 0.5f, maxValue = 1.25f, stepCount = 15, displayFormat = "N2")]
        public float PartGToleranceThreshold = 0.85f;

        [GameParameters.CustomFloatParameterUI("Operational Temperature Threshold", toolTip = "Threshold of maximum temperature at which a part starts melting", minValue = 0.8f, maxValue = 1.0f, stepCount = 20, displayFormat = "N2")]
        public float PartOperationalTempThreshold = 0.85f;
    }
}