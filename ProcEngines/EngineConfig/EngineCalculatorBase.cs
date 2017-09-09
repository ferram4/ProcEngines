﻿/*MIT License

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
        public const double GAS_CONSTANT = 8314.459848;
        public const double G0 = 9.80665;

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
        protected NozzleCalculator nozzle;

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
        protected double thrustVacAux;
        protected double thrustSLAux;
        protected double modGamma;

        public double nozzleDivEfficiency = 1.0;
        public double nozzleFrictionEfficiency = 1.0;
        public double reactionEfficiency = 1.0;

        #region Constructor
        public EngineCalculatorBase(EngineCalculatorBase engineCalc)
        {
            nozzle = engineCalc.nozzle;
            SetEngineProperties(engineCalc.biPropConfig, engineCalc.chamberOFRatio, engineCalc.chamberPresMPa, engineCalc.areaRatio, engineCalc.throatDiameter);
            nozzleExtensionArea = engineCalc.areaRatio;
        }
        
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

            unchanged &= this.chamberPresMPa == chamberPresMPa;
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
            bool unchanged = true;

            if (minThrottle < currentMinThrottleTech)
                minThrottle = currentMinThrottleTech;
            if (minThrottle > 1.0)
                minThrottle = 1.0;

            unchanged &= this.minThrottle == minThrottle;

            if (!unchanged)
            {
                this.minThrottle = minThrottle;
                UpdateInjectorPerformance();
                CalculateEngineProperties();
            }
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

        public virtual string EngineCalculatorTypeString()
        {
            return "NULL";
        }

        public virtual PowerCycleEnum EngineCalculatorType()
        {
            return PowerCycleEnum.NONE_SELECTED;
        }
        
        public virtual void CalculateEngineProperties()
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

            double gamma = enginePrefab.nozzleGamma * biPropConfig.GammaVaryFactor(enginePrefab.chamberTempK, enginePrefab.OFRatio);

            //Calc mass flow for a choked nozzle
            massFlowChamber = (gamma + 1.0) / (gamma - 1.0);
            massFlowChamber = Math.Pow(2.0 / (gamma + 1.0), massFlowChamber);
            massFlowChamber *= gamma * enginePrefab.nozzleMWgMol;
            massFlowChamber /= (GAS_CONSTANT * enginePrefab.chamberTempK);
            massFlowChamber = Math.Sqrt(massFlowChamber);
            massFlowChamber *= enginePrefab.chamberPresMPa * throatArea;
            massFlowChamber *= 1000.0;       //convert from 1000 t/s (due to MPa) to t/s

            reactionEfficiency = CalculateChamberReactionEfficiency();      //reaction efficiency acts to increase mass flow
            massFlowChamber /= reactionEfficiency;

            massFlowChamberFuel = massFlowChamber / (chamberOFRatio + 1.0);
            massFlowChamberOx = massFlowChamberFuel * chamberOFRatio;

            massFlowTotal = massFlowChamber;
            overallOFRatio = chamberOFRatio;

            thrustVacAux = thrustSLAux = 0;
        }

        double CalculateChamberReactionEfficiency()
        {
            double thermalReactionLoss = 0.1013 / chamberPresMPa;
            thermalReactionLoss = Math.Pow(thermalReactionLoss, 0.8);  //loss from pressure;
            thermalReactionLoss *= Math.Log10(nozzleDiameter / throatDiameter);     //loss from expansion ratio
            thermalReactionLoss *= Math.Pow(6.7e-4 / (0.5 * throatDiameter), 0.35); //loss from chamber size

            double lowPresChamberLoss = Math.Log(chamberPresMPa * 0.5);
            lowPresChamberLoss = 2.1 - lowPresChamberLoss;
            lowPresChamberLoss *= 0.01;
            lowPresChamberLoss = Math.Max(0, lowPresChamberLoss);

            double injectorMixingEfficiency = 0.99;

            return (1 - thermalReactionLoss) * (1 - lowPresChamberLoss) * injectorMixingEfficiency;
        }

        /*double CalculateGammaModified(double effExitAreaRatio)
        {
            double modGamma = -0.12 / (effExitAreaRatio - enginePrefab.frozenAreaRatio + 1.0) + 0.12;
            modGamma += enginePrefab.nozzleGamma;
            return modGamma;
        }*/

        protected void CalculateEngineAndNozzlePerformanceProperties(double exitTempOffset)
        {
            /*modGamma = CalculateGammaModified(areaRatio);       //avg gamma, modified for expansion ratio

            effectiveFrozenAreaRatio = NozzleUtils.AreaRatioFromMach(enginePrefab.nozzleMach, enginePrefab.nozzleGamma);
            effectiveExitAreaRatio = areaRatio * enginePrefab.frozenAreaRatio / effectiveFrozenAreaRatio;

            double exitMach = NozzleUtils.MachFromAreaRatio(effectiveExitAreaRatio, modGamma);

            double isentropicRatio = 0.5 * (modGamma - 1.0);
            isentropicRatio = (1.0 + isentropicRatio * enginePrefab.nozzleMach * enginePrefab.nozzleMach) / (1.0 + isentropicRatio * exitMach * exitMach);

            double exitTemp = isentropicRatio * (enginePrefab.nozzleTempK - exitTempOffset);

            double exitSonicVelocity = Math.Sqrt(modGamma * GAS_CONSTANT / enginePrefab.nozzleMWgMol * exitTemp);

            exhaustVelocityOpt = exitSonicVelocity * exitMach;

            exitPressureMPa = Math.Pow(isentropicRatio, modGamma / (modGamma - 1.0)) * enginePrefab.nozzlePresMPa;*/

            CalculateFrozenConfigNozzle();

            nozzle.UpdateNozzleStatus(areaRatio);

            nozzleDivEfficiency = nozzle.GetDivergenceEff();
            nozzleFrictionEfficiency = nozzle.GetFrictionEff(exhaustVelocityOpt, massFlowChamber, chamberPresMPa, enginePrefab.chamberTempK, enginePrefab.nozzleMWgMol, modGamma, throatDiameter * 0.5);

            thrustVac = exhaustVelocityOpt * massFlowChamber * nozzleDivEfficiency * nozzleFrictionEfficiency * reactionEfficiency;
            thrustSL = thrustVac;

            thrustVac += exitPressureMPa * nozzleDivEfficiency * nozzleArea * 1000.0;
            thrustSL += (exitPressureMPa * nozzleDivEfficiency - 0.1013) * nozzleArea * 1000.0;

            minThrustVac = thrustVac * minThrottle;

            thrustVac += thrustVacAux;
            thrustSL += thrustSLAux;

            specImpulseVac = thrustVac / (massFlowTotal * G0);
            specImpulseSL = thrustSL / (massFlowTotal * G0);
        }

        void CalculateFrozenConfigNozzle()
        {
            double specGasConst = GAS_CONSTANT / enginePrefab.nozzleMWgMol;

            double massFlow = massFlowChamber * reactionEfficiency;

            double U1 = 0, U2;
            double P1 = enginePrefab.chamberPresMPa, P2;
            double T2;

            T2 = enginePrefab.nozzleTempK;
            P2 = enginePrefab.nozzlePresMPa;

            U2 = massFlow * T2 * specGasConst;
            U2 /= throatArea * enginePrefab.frozenAreaRatio * P2 * 1000.0;
            U1 = U2;

            double dUdA1, dUdA2;
            double dPdU1, dPdU2;

            double areaStep = 0.01 * throatArea;

            double curArea = throatArea * enginePrefab.frozenAreaRatio;

            double curGamma = enginePrefab.nozzleGamma * biPropConfig.GammaVaryFactor(enginePrefab.nozzleTempK, enginePrefab.OFRatio);

            dUdA2 = curGamma * specGasConst * T2;
            dUdA2 = U2 * U2 / dUdA2 - 1.0;
            dUdA2 = U2 / (curArea * dUdA2);

            dPdU2 = -massFlow * 1000.0 / curArea;

            double invTempPresCalc = 1000.0 / (specGasConst * massFlow);

            modGamma = curGamma;

            double counter = 1;
            while (curArea < nozzleArea)
            {
                double curAreaRatio = curArea / throatArea;
                if (curAreaRatio > 3)
                {
                    areaStep = throatArea * 0.05;
                }
                if (curAreaRatio > 50)
                {
                    areaStep = throatArea * 0.1;
                } 
                
                counter++;
                curArea += areaStep;
                P1 = P2;
                U1 = U2;
                dUdA1 = dUdA2;
                dPdU1 = dPdU2;

                dPdU2 = -massFlow * 1000.0 / curArea;

                double lastU2 = U2;

                U2 = U1 + dUdA1 * areaStep;     //step forward

                int iter = 0;

                while(Math.Abs(U2 - lastU2) / U2 < 0.001 && iter < 10)       //use predictor-corrector method allowing for 0.1% error
                {
                    P2 = P1 + (0.5 * (dPdU1 + dPdU2) * (U2 - U1)) * 0.000001;

                    T2 = (curArea * P2 * U2) * invTempPresCalc;

                    curGamma = enginePrefab.nozzleGamma * biPropConfig.GammaVaryFactor(T2, enginePrefab.OFRatio);

                    dUdA2 = curGamma * specGasConst * T2;
                    dUdA2 = U2 * U2 / dUdA2 - 1.0;
                    dUdA2 = U2 / (curArea * dUdA2);

                    lastU2 = U2;

                    U2 = U1 + 0.5 * (dUdA1 + dUdA2) * areaStep;
                    iter++;
                }
                modGamma += curGamma;

            }

            if(curArea > nozzleArea)
            {
                double lastArea = curArea - areaStep;

                double linearScaleFactor = nozzleArea - lastArea / (curArea - lastArea);

                U2 = U1 + (U2 - U1) * linearScaleFactor;
                P2 = P1 + (P2 - P1) * linearScaleFactor;

                //do things to scale vel back down
            }
            modGamma /= counter;
            exhaustVelocityOpt = U2;
            exitPressureMPa = P2;
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
                tmpIgnitionCount = GUIUtils.TextEntryForIntWithButton("Num Ignitions:", 125, tmpIgnitionCount, 50);

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
