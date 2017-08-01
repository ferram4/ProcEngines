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

namespace ProcEngines
{
    class EngineConfig
    {
        const double GAS_CONSTANT = 8314.459848;
        const double G0 = 9.80665;

        double chamberOFRatio;
        string mixtureTitle;
        BiPropellantConfig propConfig;
        EngineDataPrefab enginePrefab;

        double chamberPresMPa;
        double nozzleDiameter;
        double nozzleArea;
        double throatArea;
        double areaRatio;

        double exhaustVelocityOpt;
        double exitPressureMPa;

        double thrustChamberVac;
        double thrustChamberSL;
        double massFlowChamber;
        double massFlowChamberOx;
        double massFlowChamberFuel;

        double turbinePresRatio = 2;        //keep it low for staged combustion

        void CalculateChamberParameters()
        {
            //Calc geometry
            nozzleArea = nozzleDiameter * nozzleDiameter * 0.25 * Math.PI;
            throatArea = nozzleArea * areaRatio;

            //Generate engine prefab for this OF ratio and cham pres
            enginePrefab = propConfig.CalcPrefabData(chamberOFRatio, chamberPresMPa);

            //Calc mass flow for a choked nozzle
            massFlowChamber = (enginePrefab.nozzleGamma + 1) / (enginePrefab.nozzleGamma - 1);
            massFlowChamber = Math.Pow(2 / (enginePrefab.nozzleGamma + 1), massFlowChamber);
            massFlowChamber *= enginePrefab.nozzleGamma * enginePrefab.nozzleMWgMol;
            massFlowChamber /= (GAS_CONSTANT * enginePrefab.chamberTempK);
            massFlowChamber = Math.Sqrt(massFlowChamber);
            massFlowChamber *= enginePrefab.chamberPresMPa * throatArea;
            massFlowChamber *= 1000;       //convert from 1000 t/s (due to MPa) to t/s

            double effectiveFrozenAreaRatio = NozzleAeroUtils.AreaRatioFromMach(enginePrefab.nozzleMach, enginePrefab.nozzleGamma);
            double effectiveExitAreaRatio = areaRatio * enginePrefab.frozenAreaRatio / effectiveFrozenAreaRatio;

            double exitMach = NozzleAeroUtils.MachFromAreaRatio(effectiveExitAreaRatio, enginePrefab.nozzleGamma);

            double isentropicRatio = 0.5 * (enginePrefab.nozzleGamma - 1);
            isentropicRatio = (1 + isentropicRatio * enginePrefab.nozzleMach * enginePrefab.nozzleMach) / (1 + isentropicRatio * exitMach * exitMach);

            double exitTemp = isentropicRatio * enginePrefab.nozzleTempK;

            exitPressureMPa = Math.Pow(isentropicRatio, enginePrefab.nozzleGamma / (enginePrefab.nozzleGamma - 1)) * enginePrefab.nozzlePresMPa;

            double exitSonicVelocity = Math.Sqrt(enginePrefab.nozzleGamma * GAS_CONSTANT / enginePrefab.nozzleMWgMol * exitTemp);

            exhaustVelocityOpt = exitSonicVelocity * exitMach;

            thrustChamberVac = exhaustVelocityOpt * massFlowChamber + exitPressureMPa * nozzleArea * 1000;
            thrustChamberSL = exhaustVelocityOpt * massFlowChamber + (exitPressureMPa - 0.1013) * nozzleArea * 1000;

            massFlowChamberFuel = massFlowChamber / (chamberOFRatio + 1);
            massFlowChamberOx = massFlowChamberFuel * chamberOFRatio;
        }

        void CalculatePreBurnerParameters(bool runOxRich)
        {
            double injectorPressureDrop = 0.3;          //TODO: make injector pres drop vary with throttle capability, tech level
            double regenerativeCoolingPresDrop = 0.15;  //TODO: make cooling pres draop vary with cooling needs, tech level
            double tankPresMPa = 0.2;                   //TODO: make variable of some kind;

            double preburnerTurbineOutputPresMPa = chamberPresMPa * (1 + injectorPressureDrop);

            double preburnerPressureMPa = preburnerTurbineOutputPresMPa * turbinePresRatio;    //TODO: allow separate ox, fuel turbines

            double oxPumpPresRiseMPa = preburnerPressureMPa - tankPresMPa;
            double fuelPumpPresRiseMPa = preburnerPressureMPa * (1 + regenerativeCoolingPresDrop) - tankPresMPa;      //assume that only fuel is used for regen cooling

            double pumpEfficiency = 0.8;                //TODO: make vary with fuel type and with tech level

            double oxPumpPower = massFlowChamberOx * oxPumpPresRiseMPa * 1000000 / (propConfig.GetOxDensity() * pumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel
            double fuelPumpPower = massFlowChamberFuel * fuelPumpPresRiseMPa * 1000000 / (propConfig.GetFuelDensity() * pumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel
            double turbinePowerReq = oxPumpPower + fuelPumpPower;

            double turbineEfficiency = 0.7;             //TODO: make vary with tech level

            double turbineInletTempK = 1000;             //TODO: make variable, add film cooling reqs for temps above 800

            EngineDataPrefab combustorPrefab = propConfig.CalcDataAtPresAndTemp(preburnerPressureMPa, turbineInletTempK, false);

        }
    }
}
