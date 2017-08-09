/*MIT License

Copyright (c) 2017 Michael Ferrara

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/
using System;
using System.Collections.Generic;
using UnityEngine;
using ProcEngines.PropellantConfig;
using ProcEngines.EngineGUI;

namespace ProcEngines.EngineConfig
{
    class EngineCalculatorBase
    {
        const double GAS_CONSTANT = 8314.459848;
        const double G0 = 9.80665;

        string mixtureTitle;
        public BiPropellantConfig biPropConfig;
        public EngineDataPrefab enginePrefab;

        public double chamberOFRatio;
        public double chamberPresMPa;
        public double throatDiameter;
        public double areaRatio;

        double throatArea;
        double nozzleArea;
        public double nozzleDiameter;
        NozzleCalculator nozzle;

        double effectiveFrozenAreaRatio;
        double exhaustVelocityOpt;
        double exitPressureMPa;

        double nozzleExtensionArea;

        protected double massFlowChamber;
        protected double massFlowChamberOx;
        protected double massFlowChamberFuel;

        double combustionChamberVol;
        double combustionChamberDiam;
        double combustionChamberLength;
        double combustionChamberMassT;
        double nozzleMassT;

        protected double injectorPressureRatioDrop = 0.2;     //TODO: make injector pres drop vary with throttle capability, tech level
        protected double regenerativeCoolingPresDrop = 0.15;  //TODO: make cooling pres draop vary with cooling needs, tech level
        protected double tankPresMPa = 0.2;                   //TODO: make variable of some kind;

        double minThrottle = 1.0;
        double minThrustVac;
        FloatCurve throttleInjectorCurve;
        static double currentMinThrottleTech = 0.1;

        int ignitionCount = 1;
        
        public double thrustVac;
        public double thrustSL;
        public double massFlowTotal;
        public double specImpulseVac;
        public double specImpulseSL;
        public double overallOFRatio;

        public double nozzleDivEfficiency = 1.0;
        public double nozzleFrictionEfficiency = 1.0;

        #region Constructor
        public EngineCalculatorBase(BiPropellantConfig mixture, double oFRatio, double chamberPresMPa, double areaRatio, double throatDiameter)
        {
            nozzle = new NozzleCalculator(0.8, areaRatio, NozzleShapeType.BELL);
            SetEngineProperties(mixture, oFRatio, chamberPresMPa, areaRatio, throatDiameter);
            nozzleExtensionArea = areaRatio;
        }
        #endregion

        #region EngineParameterUpdate
        public void SetEngineProperties(BiPropellantConfig mixture, double oFRatio, double chamberPresMPa, double areaRatio, double throatDiameter)
        {
            bool unchanged = true;

            unchanged &= biPropConfig == mixture;
            this.mixtureTitle = mixture.MixtureTitle;
            biPropConfig = mixture;

            if (oFRatio < biPropConfig.ChamberOFLimitLean)
                oFRatio = biPropConfig.ChamberOFLimitLean;
            if (oFRatio > biPropConfig.ChamberOFLimitRich)
                oFRatio = biPropConfig.ChamberOFLimitRich;

            unchanged &= this.chamberOFRatio == oFRatio;
            this.chamberOFRatio = oFRatio;

            if (chamberPresMPa > biPropConfig.ChamberPresLimHigh)
                chamberPresMPa = biPropConfig.ChamberPresLimHigh;
            if (chamberPresMPa < biPropConfig.ChamberPresLimLow)
                chamberPresMPa = biPropConfig.ChamberPresLimLow;

            unchanged &= this.chamberPresMPa == oFRatio;
            this.chamberPresMPa = chamberPresMPa;

            if (areaRatio < biPropConfig.FrozenAreaRatio)
                areaRatio = biPropConfig.FrozenAreaRatio;

            unchanged &= this.areaRatio == areaRatio;
            double extensionRatio = nozzleExtensionArea / this.areaRatio;

            this.areaRatio = areaRatio;
            nozzleExtensionArea = extensionRatio * this.areaRatio;

            unchanged &= this.throatDiameter == throatDiameter;
            this.throatDiameter = throatDiameter;

            if(!unchanged)
                CalculateEngineProperties();
        }

        void UpdateThrottleInjectorProperties(double minThrottle, double techLevel)
        {
            if (minThrottle < currentMinThrottleTech)
                minThrottle = currentMinThrottleTech;
            if (minThrottle > 1.0)
                minThrottle = 1.0;

            if (this.minThrottle == minThrottle)
                return;


            this.minThrottle = minThrottle;
            UpdateInjectorPerformance();
            CalculateEngineProperties();
        }

        void UpdateIgnitionProperties(int ignitionCount)
        {
            if (this.ignitionCount == ignitionCount)
                return;

            this.ignitionCount = ignitionCount;
            CalculateEngineProperties();
        }

        void UpdateNozzleExtension(double relLength, int shapeIndex)
        {
            NozzleShapeType shape;
            if (shapeIndex == 0)
                shape = NozzleShapeType.CONICAL;
            else
                shape = NozzleShapeType.BELL;

            nozzle.UpdateNozzleStatus(relLength, areaRatio, shape);
            CalculateEngineProperties();
        }
        #endregion

        public virtual string EngineCalculatorType()
        {
            return "NULL";
        }

        protected virtual void CalculateEngineProperties()
        {
        }

        #region ChamberPerformanceCalc
        protected void CalculateMainCombustionChamberParameters()
        {
            //Calc geometry
            throatArea = throatDiameter * throatDiameter * 0.25 * Math.PI;
            nozzleArea = throatArea * areaRatio;
            nozzleDiameter = Math.Sqrt(nozzleArea / (0.25 * Math.PI));

            //Generate engine prefab for this OF ratio and cham pres
            enginePrefab = biPropConfig.CalcPrefabData(chamberOFRatio, chamberPresMPa);

            //Calc mass flow for a choked nozzle
            massFlowChamber = (enginePrefab.nozzleGamma + 1.0) / (enginePrefab.nozzleGamma - 1.0);
            massFlowChamber = Math.Pow(2.0 / (enginePrefab.nozzleGamma + 1.0), massFlowChamber);
            massFlowChamber *= enginePrefab.nozzleGamma * enginePrefab.nozzleMWgMol;
            massFlowChamber /= (GAS_CONSTANT * enginePrefab.chamberTempK);
            massFlowChamber = Math.Sqrt(massFlowChamber);
            massFlowChamber *= enginePrefab.chamberPresMPa * throatArea;
            massFlowChamber *= 1000.0;       //convert from 1000 t/s (due to MPa) to t/s

            massFlowChamberFuel = massFlowChamber / (chamberOFRatio + 1.0);
            massFlowChamberOx = massFlowChamberFuel * chamberOFRatio;

            massFlowTotal = massFlowChamber;
            overallOFRatio = chamberOFRatio;
        }

        protected void CalculateEngineAndNozzlePerformanceProperties()
        {
            effectiveFrozenAreaRatio = NozzleUtils.AreaRatioFromMach(enginePrefab.nozzleMach, enginePrefab.nozzleGamma);
            double effectiveExitAreaRatio = areaRatio * enginePrefab.frozenAreaRatio / effectiveFrozenAreaRatio;

            double exitMach = NozzleUtils.MachFromAreaRatio(effectiveExitAreaRatio, enginePrefab.nozzleGamma);

            double isentropicRatio = 0.5 * (enginePrefab.nozzleGamma - 1.0);
            isentropicRatio = (1.0 + isentropicRatio * enginePrefab.nozzleMach * enginePrefab.nozzleMach) / (1.0 + isentropicRatio * exitMach * exitMach);

            double exitTemp = isentropicRatio * enginePrefab.nozzleTempK;
            
            double exitSonicVelocity = Math.Sqrt(enginePrefab.nozzleGamma * GAS_CONSTANT / enginePrefab.nozzleMWgMol * exitTemp);

            exhaustVelocityOpt = exitSonicVelocity * exitMach;

            exitPressureMPa = Math.Pow(isentropicRatio, enginePrefab.nozzleGamma / (enginePrefab.nozzleGamma - 1.0)) * enginePrefab.nozzlePresMPa;

            nozzle.UpdateNozzleStatus(areaRatio);

            nozzleDivEfficiency = nozzle.GetDivergenceEff();
            nozzleFrictionEfficiency = nozzle.GetFrictionEff(exhaustVelocityOpt, massFlowChamber, chamberPresMPa, enginePrefab.nozzleGamma, throatDiameter * 0.5);

            thrustVac = (exhaustVelocityOpt * massFlowChamber * nozzleDivEfficiency * nozzleFrictionEfficiency + exitPressureMPa * nozzleDivEfficiency * nozzleArea * 1000.0);
            thrustSL = (exhaustVelocityOpt * massFlowChamber * nozzleDivEfficiency * nozzleFrictionEfficiency + (exitPressureMPa * nozzleDivEfficiency - 0.1013) * nozzleArea * 1000.0);
            minThrustVac = thrustVac * minThrottle;

            specImpulseVac = thrustVac / (massFlowTotal * G0);
            specImpulseSL = thrustSL / (massFlowTotal * G0);
        }
        #endregion

        #region ThrottleAndInjector
        void UpdateInjectorPerformance()
        {
            if(throttleInjectorCurve == null)
            {
                throttleInjectorCurve = new FloatCurve();
                throttleInjectorCurve.Add(0.1f, 0.67f, -0.65f, -0.65f);
                throttleInjectorCurve.Add(1f, 0.2f, 0f, 0);
            }

            injectorPressureRatioDrop = throttleInjectorCurve.Evaluate((float)minThrottle);
            //TODO: handle techlevel
        }
        #endregion

        #region EngineDimensioning
        void CalculateCombustionChamberDimensions()
        {
            double characteristicLength = 1.27;     //TODO: make variable in BiPropellantConfig and take from there

            double areaCombustionChamber = CalcCombustionChamberArea();

            combustionChamberDiam = 2.0 * Math.Sqrt(areaCombustionChamber / Math.PI);
            combustionChamberVol = throatArea * characteristicLength;       //Use of throat area is correct with throat area
            combustionChamberLength = combustionChamberVol / areaCombustionChamber;

            StructuralMaterial combustionChamberMat;
            StructuralMaterialLibrary.TryGetMaterial(out combustionChamberMat, "Inconel");

            double nozzleAndChamberMatFactor = combustionChamberMat.densitykg_m / combustionChamberMat.ultimateStrengthMPa * 2.0;     //materials properties and safety factor
            combustionChamberMassT = nozzleAndChamberMatFactor * chamberPresMPa * combustionChamberVol;
            combustionChamberMassT *= (2 * combustionChamberDiam + 1 / combustionChamberLength);

            nozzleMassT = (areaRatio - 1) / Math.Sin(15.0 * Math.PI / 180.0);
            nozzleMassT *= throatArea * chamberPresMPa * combustionChamberDiam * nozzleAndChamberMatFactor * 0.5;

            combustionChamberMassT *= 0.001 * 1.52;
            nozzleMassT *= 0.001 * 1.52;            //convert to tonnes, correction factor
        }

        //Empirical forumla to related A_cc to A_throat
        double CalcCombustionChamberArea()
        {
            double areaCombustionChamber = Math.Sqrt(throatArea / Math.PI) * 200.0;       //calc throat diameter in cm
            areaCombustionChamber = Math.Pow(areaCombustionChamber, -0.6);
            areaCombustionChamber *= 8.0;
            areaCombustionChamber += 1.25;
            areaCombustionChamber *= throatArea;

            return areaCombustionChamber;
        }
        #endregion

        #region GUI
        static bool showThrottleInjector = false;
        static bool showNozzleShape = false;
        static bool showCooling = false;
        static bool showGimbalDesign = false;
        static bool showIgnitionSystem = false;

        public void CycleEngineGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(290));

            if (GUILayout.Button("Nozzle Shape"))
                showNozzleShape = !showNozzleShape;
            if (showNozzleShape)
            {
                int shapeIndex = nozzle.NozzleTypeSelect();
                GUILayout.Label("Select regen nozzle material");

                double tmpRelLength = nozzle.relLength;
                tmpRelLength = GUIUtils.TextEntryForDoubleWithButtons("Frac 15 Cone Length:", 125, tmpRelLength, 0.01, 0.1, 50);

                //Nozzle Divergence Losses
                GUILayout.BeginHorizontal();
                GUILayout.Label("Exit Angle: ", GUILayout.Width(125));
                GUILayout.Label(nozzle.exitAngleString);
                GUILayout.EndHorizontal();

                //Nozzle Divergence Losses
                GUILayout.BeginHorizontal();
                GUILayout.Label("Divergence Eff: ", GUILayout.Width(125));
                GUILayout.Label((nozzleDivEfficiency * 100.0).ToString("F3") + " %");
                GUILayout.EndHorizontal();

                //Nozzle Friction Losses
                GUILayout.BeginHorizontal();
                GUILayout.Label("Friction Eff: ", GUILayout.Width(125));
                GUILayout.Label((nozzleFrictionEfficiency * 100.0).ToString("F3") + " %");
                GUILayout.EndHorizontal();

                UpdateNozzleExtension(tmpRelLength, shapeIndex);
            }

            if (GUILayout.Button("Nozzle and Chamber Cooling"))
                showCooling = !showCooling;
            if (showCooling)
            {
                GUILayout.Label("Select regen chamber material");
                GUILayout.Label("Select regen nozzle material");

                GUILayout.Label("Toggle chamber film cooling");

                double tmpNozzleExtensionArea = nozzleExtensionArea;
                tmpNozzleExtensionArea = GUIUtils.TextEntryForDoubleWithButtons("Regen Cooling Area End:", 125, tmpNozzleExtensionArea, 0.1, 1, 50);

                if (nozzleExtensionArea != areaRatio)
                {
                    GUILayout.Label("Select ablative or radiative extension");
                    GUILayout.Label("Select extension material");
                }
            } 
            
            if (GUILayout.Button("Ignition System"))
                showIgnitionSystem = !showIgnitionSystem;
            if (showIgnitionSystem)
            {
                GUILayout.Label("Select ignition system");

                int tmpIgnitionCount = ignitionCount;
                tmpIgnitionCount = GUIUtils.TextEntryForIntWithButtons("Num Ignitions:", 125, tmpIgnitionCount, 50);

                UpdateIgnitionProperties(tmpIgnitionCount);
            } 
            
            LeftSideEngineGUI();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(290));
            if (GUILayout.Button("Throttling and Injector"))
                showThrottleInjector = !showThrottleInjector;
            if (showThrottleInjector)
            {
                double minThrottleTmp = minThrottle;
                minThrottleTmp = GUIUtils.TextEntryForDoubleWithButtons("Min Throttle:", 125, minThrottleTmp, 0.01, 0.1, 50);
                //Min Vac Thrust
                GUILayout.BeginHorizontal();
                GUILayout.Label("Min Vac Thrust: ", GUILayout.Width(125));
                GUILayout.Label(minThrustVac.ToString("F3") + " kN");
                GUILayout.EndHorizontal();

                //Injector Pres. Loss
                GUILayout.BeginHorizontal();
                GUILayout.Label("Injector % Pres Drop: ", GUILayout.Width(125));
                GUILayout.Label((injectorPressureRatioDrop * 100.0).ToString("F1") + " %");
                GUILayout.EndHorizontal();

                UpdateThrottleInjectorProperties(minThrottleTmp, 1.0);
            }

            if (GUILayout.Button("Gimbal and Thrust Vectoring"))
                showGimbalDesign = !showGimbalDesign;
            if (showGimbalDesign)
            {
                GUILayout.Label("Select gimbal method");
                GUILayout.Label("Select gimbal range");

            } 
            
            RightSideEngineGUI();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        protected virtual void LeftSideEngineGUI() { }
        protected virtual void RightSideEngineGUI() { }

        #endregion
    }
}
