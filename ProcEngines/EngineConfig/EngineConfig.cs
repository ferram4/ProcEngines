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
using ProcEngines.PropellantConfigs;

namespace ProcEngines.EngineConfig
{
    class EngineConfigBase
    {
        const double GAS_CONSTANT = 8314.459848;
        const double G0 = 9.80665;

        protected double chamberOFRatio;
        string mixtureTitle;
        protected BiPropellantConfig propConfig;
        protected EngineDataPrefab enginePrefab;

        protected double chamberPresMPa;
        double nozzleDiameter;
        double nozzleArea;
        double throatArea;
        double areaRatio;

        double exhaustVelocityOpt;
        double exitPressureMPa;

        protected double massFlowChamber;
        protected double massFlowChamberOx;
        protected double massFlowChamberFuel;

        double combustionChamberVol;
        double combustionChamberDiam;
        double combustionChamberLength;
        double combustionChamberMassT;
        double nozzleMassT;

        protected double injectorPressureRatioDrop = 0.3;     //TODO: make injector pres drop vary with throttle capability, tech level
        protected double regenerativeCoolingPresDrop = 0.15;  //TODO: make cooling pres draop vary with cooling needs, tech level
        protected double tankPresMPa = 0.2;                   //TODO: make variable of some kind;

        protected double oxPumpPresRiseMPa;
        protected double fuelPumpPresRiseMPa;
        protected double oxPumpPower;
        protected double fuelPumpPower;

        protected double turbinePresRatio;
        protected double turbineInletTempK = 900;
        protected double turbineMassFlow;
        protected double turbinePower;

        double thrustChamberVac;
        double thrustChamberSL;
        protected double massFlowTotal;
        double specImpulseVac;
        double specImpulseSL;



        protected virtual void CalculateEngineProperties()
        {
        }

        protected void CalculateMainCombustionChamberParameters()
        {
            //Calc geometry
            nozzleArea = nozzleDiameter * nozzleDiameter * 0.25 * Math.PI;
            throatArea = nozzleArea * areaRatio;

            //Generate engine prefab for this OF ratio and cham pres
            enginePrefab = propConfig.CalcPrefabData(chamberOFRatio, chamberPresMPa);

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
        }

        protected void CalculateEngineAndNozzlePerformanceProperties()
        {
            double effectiveFrozenAreaRatio = NozzleAeroUtils.AreaRatioFromMach(enginePrefab.nozzleMach, enginePrefab.nozzleGamma);
            double effectiveExitAreaRatio = areaRatio * enginePrefab.frozenAreaRatio / effectiveFrozenAreaRatio;

            double exitMach = NozzleAeroUtils.MachFromAreaRatio(effectiveExitAreaRatio, enginePrefab.nozzleGamma);

            double isentropicRatio = 0.5 * (enginePrefab.nozzleGamma - 1.0);
            isentropicRatio = (1.0 + isentropicRatio * enginePrefab.nozzleMach * enginePrefab.nozzleMach) / (1.0 + isentropicRatio * exitMach * exitMach);

            double exitTemp = isentropicRatio * enginePrefab.nozzleTempK;
            
            double exitSonicVelocity = Math.Sqrt(enginePrefab.nozzleGamma * GAS_CONSTANT / enginePrefab.nozzleMWgMol * exitTemp);

            exhaustVelocityOpt = exitSonicVelocity * exitMach;

            exitPressureMPa = Math.Pow(isentropicRatio, enginePrefab.nozzleGamma / (enginePrefab.nozzleGamma - 1.0)) * enginePrefab.nozzlePresMPa;

            thrustChamberVac = exhaustVelocityOpt * massFlowChamber + exitPressureMPa * nozzleArea * 1000.0;
            thrustChamberSL = exhaustVelocityOpt * massFlowChamber + (exitPressureMPa - 0.1013) * nozzleArea * 1000.0;

            specImpulseVac = thrustChamberVac / (massFlowTotal * G0);
            specImpulseSL = thrustChamberSL / (massFlowTotal * G0);
        }

        /*void CalculatePreBurnerParameters(bool runOxRich)
        {
            double injectorPressureDrop = 0.3;          //TODO: make injector pres drop vary with throttle capability, tech level

            double preburnerTurbineOutputPresMPa = chamberPresMPa * (1.0 + injectorPressureDrop);



            double turbineInletTempK = 1000.0;             //TODO: make variable, add film cooling reqs for temps above 800

            IteratePreburnerAndTurbinePerformance(turbineInletTempK, preburnerTurbineOutputPresMPa);
        }*/

        /*void IteratePreburnerAndTurbinePerformance(double turbineInletTempK, double turbineOutputPres, double epsilon = 0.001)
        {
            double regenerativeCoolingPresDrop = 0.15;  //TODO: make cooling pres draop vary with cooling needs, tech level
            double tankPresMPa = 0.2;                   //TODO: make variable of some kind;

            double preburnerPressureMPa = turbineOutputPres * turbinePresRatio;    //TODO: allow separate ox, fuel turbines

            double oxPumpPresRiseMPa = preburnerPressureMPa - tankPresMPa;
            double fuelPumpPresRiseMPa = preburnerPressureMPa * (1.0 + regenerativeCoolingPresDrop) - tankPresMPa;      //assume that only fuel is used for regen cooling

            double pumpEfficiency = 0.8;                //TODO: make vary with fuel type and with tech level

            double oxPumpPower = massFlowChamberOx * oxPumpPresRiseMPa * 1000000.0 / (propConfig.GetOxDensity() * pumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel
            double fuelPumpPower = massFlowChamberFuel * fuelPumpPresRiseMPa * 1000000.0 / (propConfig.GetFuelDensity() * pumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel

            double turbineEfficiency = 0.7;             //TODO: make vary with tech level
            double turbinePowerReq = (oxPumpPower + fuelPumpPower) / turbineEfficiency;

            EngineDataPrefab combustorPrefab = propConfig.CalcDataAtPresAndTemp(preburnerPressureMPa, turbineInletTempK, false);

            double turbineMassFlow = massFlowChamberOx * (1 + 1/combustorPrefab.OFRatio);
            double turbineCp = combustorPrefab.CalculateCp();
            double turbineOutputPower = turbineEfficiency * turbineMassFlow * turbineCp * (1.0 - Math.Pow(turbinePresRatio, combustorPrefab.nozzleGamma / (combustorPrefab.nozzleGamma - 1.0)));
        }*/

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
    }
}
