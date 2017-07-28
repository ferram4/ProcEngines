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

        double oFRatio;
        string mixtureTitle;
        BiPropellantConfig propConfig;
        EngineDataPrefab enginePrefab;

        double chamberPres;
        double nozzleDiameter;
        double nozzleArea;
        double throatArea;
        double areaRatio;

        double exhaustVelocityOpt;
        double exitPressureMPa;

        double thrustVac;
        double thrustSL;
        double ispSL;
        double ispVac;
        double massFlow;

        void CalculateEngineParameters()
        {
            //Calc geometry
            nozzleArea = nozzleDiameter * nozzleDiameter * 0.25 * Math.PI;
            throatArea = nozzleArea * areaRatio;

            //Generate engine prefab for this OF ratio and cham pres
            enginePrefab = propConfig.CalcPrefabData(oFRatio, chamberPres);

            //Calc mass flow for a choked nozzle
            massFlow = (enginePrefab.nozzleGamma + 1) / (enginePrefab.nozzleGamma - 1);
            massFlow = Math.Pow(2 / (enginePrefab.nozzleGamma + 1), massFlow);
            massFlow *= enginePrefab.nozzleGamma * enginePrefab.nozzleMWgMol;
            massFlow /= (GAS_CONSTANT * enginePrefab.chamberTempK);
            massFlow = Math.Sqrt(massFlow);
            massFlow *= enginePrefab.chamberPresMPa * throatArea;
            massFlow *= 1000;       //convert from MN (due to MPa) to kN

            double effectiveFrozenAreaRatio = NozzleAeroUtils.AreaRatioFromMach(enginePrefab.nozzleMach, enginePrefab.nozzleGamma);
            double effectiveExitAreaRatio = areaRatio * enginePrefab.frozenAreaRatio / effectiveFrozenAreaRatio;

            double exitMach = NozzleAeroUtils.MachFromAreaRatio(effectiveExitAreaRatio, enginePrefab.nozzleGamma);

            double isentropicRatio = 0.5 * (enginePrefab.nozzleGamma - 1);
            isentropicRatio = (1 + isentropicRatio * enginePrefab.nozzleMach * enginePrefab.nozzleMach) / (1 + isentropicRatio * exitMach * exitMach);

            double exitTemp = isentropicRatio * enginePrefab.nozzleTempK;

            exitPressureMPa = Math.Pow(isentropicRatio, enginePrefab.nozzleGamma / (enginePrefab.nozzleGamma - 1)) * enginePrefab.nozzlePresMPa;

            double exitSonicVelocity = Math.Sqrt(enginePrefab.nozzleGamma * GAS_CONSTANT / enginePrefab.nozzleMWgMol * exitTemp);

            exhaustVelocityOpt = exitSonicVelocity * exitMach;

            thrustVac = exhaustVelocityOpt * massFlow + exitPressureMPa * nozzleArea * 1000;
            ispVac = thrustVac / (massFlow * G0);

            thrustSL = exhaustVelocityOpt * massFlow + (exitPressureMPa - 0.1013) * nozzleArea * 1000;
            ispSL = thrustSL / (massFlow * G0);
        }
    }
}
