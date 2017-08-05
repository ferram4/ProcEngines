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

namespace ProcEngines.EngineConfig
{
    class EngineConfigGasGen : EngineConfigBase
    {
        protected override void CalculateEngineProperties()
        {
            CalculateMainCombustionChamberParameters();
            AssumePumpPressureRise();
            SolveGasGenTurbine(false);
            CalculateEngineAndNozzlePerformanceProperties();
        }

        void AssumePumpPressureRise()
        {
            oxPumpPresRiseMPa = chamberPresMPa * (1 + injectorPressureRatioDrop) - tankPresMPa;
            fuelPumpPresRiseMPa = chamberPresMPa * (1 + injectorPressureRatioDrop) * (1.0 + regenerativeCoolingPresDrop) - tankPresMPa;      //assume that only fuel is used for regen cooling
        }

        void SolveGasGenTurbine(bool oxRich)
        {
            turbinePresRatio = chamberPresMPa / (0.2);       //assume ~2 atm ~= 0.2 MPa backpressure

            EngineDataPrefab gasGenPrefab = biPropConfig.CalcDataAtPresAndTemp(chamberPresMPa, turbineInletTempK, oxRich);        //assume that gas gen runs at same pressure as chamber

            /*double gasGenOFRatio = gasGenPrefab.OFRatio;
            double gammaPower = gasGenPrefab.nozzleGamma / (gasGenPrefab.nozzleGamma - 1.0);
            double Cp = gasGenPrefab.CalculateCp();*/

            double[] gasGenOFRatio_gammaPower_Cp = new double[] { gasGenPrefab.OFRatio,
                gasGenPrefab.nozzleGamma / (gasGenPrefab.nozzleGamma - 1.0),
                gasGenPrefab.CalculateCp() };

            turbineMassFlow = MathUtils.BrentsMethod(IterateSolveGasGenTurbine, gasGenOFRatio_gammaPower_Cp, 0.000001 * massFlowChamber, massFlowChamber);

            massFlowTotal += turbineMassFlow;
        }

        double IterateSolveGasGenTurbine(double turbineMassFlow, double[] gasGenOFRatio_gammaPower_Cp)
        {
            double pumpEfficiency = 0.8;                //TODO: make vary with fuel type and with tech level
            double turbineEfficiency = 0.7;             //TODO: make vary with tech level


            double turbineMassFlowFuel = turbineMassFlow / (gasGenOFRatio_gammaPower_Cp[0] + 1.0);
            double turbineMassFlowOx = turbineMassFlowFuel * gasGenOFRatio_gammaPower_Cp[0];

            double massFlowFuelTotal = turbineMassFlowFuel + massFlowChamberFuel;
            double massFlowOxTotal = turbineMassFlowOx + massFlowChamberOx;

            oxPumpPower = massFlowOxTotal * oxPumpPresRiseMPa * 1000000.0 / (biPropConfig.GetOxDensity() * pumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel
            fuelPumpPower = massFlowFuelTotal * fuelPumpPresRiseMPa * 1000000.0 / (biPropConfig.GetFuelDensity() * pumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel

            turbinePower = (oxPumpPower + fuelPumpPower) / (turbineEfficiency);

            double checkTurbineMassFlow = (1.0 - Math.Pow(turbinePresRatio, gasGenOFRatio_gammaPower_Cp[1]));
            checkTurbineMassFlow *= gasGenOFRatio_gammaPower_Cp[2] * turbineInletTempK;
            checkTurbineMassFlow = turbinePower / (1000.0 * checkTurbineMassFlow);   //convert to tonnes

            return (checkTurbineMassFlow - turbineMassFlow) / checkTurbineMassFlow;
        }


    }
}
