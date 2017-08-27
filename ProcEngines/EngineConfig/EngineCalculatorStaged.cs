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
    class EngineCalculatorStaged : EngineCalculatorBase
    {
        double oxPumpPresRiseMPa;
        double fuelPumpPresRiseMPa;

        double turbinePresRatio;
        double turbineInletTempK = 1000;
        double turbineMassFlow;

        static double maxTurbineInletK = 1350;
        static double minTurbineInletK = 700;

        TurbopumpCalculator turbopump;

        EngineDataPrefab preBurnerPrefab;
        bool oxRich;

        public EngineCalculatorStaged(EngineCalculatorBase engineCalc)
            : base(engineCalc) { }

        public EngineCalculatorStaged(BiPropellantConfig mixture, double oFRatio, double chamberPresMPa, double areaRatio, double throatDiameter)
            : base(mixture, oFRatio, chamberPresMPa, areaRatio, throatDiameter) { }

        public override string EngineCalculatorTypeString()
        {
            return "Staged Combustion";
        }

        public override PowerCycleEnum EngineCalculatorType()
        {
            return PowerCycleEnum.STAGED_COMBUSTION;
        }

        #region EnginePerformanceCalc
        public override void CalculateEngineProperties()
        {
            if (turbopump == null)
            {
                turbopump = new TurbopumpCalculator(biPropConfig, this);
            }


            CalculateMainCombustionChamberParameters();
            SolvePreburnerTurbine(oxRich);

            double tempShiftDueToPowerExtracted = turbopump.PowerExtractedByTurbineFlow();
            tempShiftDueToPowerExtracted /= massFlowTotal * enginePrefab.nozzleCp * 1000.0;

            CalculateEngineAndNozzlePerformanceProperties(tempShiftDueToPowerExtracted);
        }

        void UpdateGasGenProperties(int oxRichInt, double turbineInletTemp)
        {
            bool unchanged = true;

            if (oxRichInt <= 0 && oxRich)
            {
                oxRich = false;
                unchanged = false;
            }
            else if (oxRichInt > 0 && !oxRich)
            {
                oxRich = true;
                unchanged = false;
            }

            if (turbineInletTemp < minTurbineInletK)
                turbineInletTemp = minTurbineInletK;
            if (turbineInletTemp > maxTurbineInletK)
                turbineInletTemp = maxTurbineInletK;

            unchanged &= turbineInletTemp == turbineInletTempK;
            turbineInletTempK = turbineInletTemp;

            if (!unchanged)
                CalculateEngineProperties();
        }

        #endregion

        #region TurbopumpCalc

        void SolvePreburnerTurbine(bool oxRich)
        {
            turbopump.UpdateMixture(biPropConfig);

            double maxPresRatio = biPropConfig.MaxPresAvailable / (chamberPresMPa * (1.0 + injectorPressureRatioDrop)) * 0.99;     //ratio to drop from max chamber pres to the actual chamber pres

            turbinePresRatio = 0.0;
            double lowerBound = 1.0001;
            double upperBound = maxPresRatio;

            double interval = upperBound - lowerBound;
            interval *= 0.05;
            upperBound = lowerBound + interval;

            while(turbinePresRatio == 0.0 && upperBound <= maxPresRatio)
            {
                turbinePresRatio = MathUtils.BrentsMethod(IterateSolvePreburnerTurbine, null, lowerBound, upperBound, 0.000001, int.MaxValue);

                if(turbinePresRatio == 0.0)
                {
                    lowerBound += interval;
                    upperBound += interval;
                }
            }
        }

        double IterateSolvePreburnerTurbine(double turbinePresRatio, double[] nullArray)
        {
            double preBurnerPresMPa = chamberPresMPa * (1.0 + injectorPressureRatioDrop) * turbinePresRatio;
            preBurnerPrefab = biPropConfig.CalcDataAtPresAndTemp(preBurnerPresMPa, turbineInletTempK, oxRich);        //assume that gas gen runs at same pressure as chamber

            double turbineMassFlowOx, turbineMassFlowFuel;

            if (oxRich)
            {
                turbineMassFlowOx = massFlowChamberOx;
                turbineMassFlowFuel = turbineMassFlowOx / preBurnerPrefab.OFRatio;
            }
            else
            {
                turbineMassFlowFuel = massFlowChamberFuel;
                turbineMassFlowOx = turbineMassFlowFuel * preBurnerPrefab.OFRatio;
            }

            turbineMassFlow = turbineMassFlowOx + turbineMassFlowFuel;

            oxPumpPresRiseMPa = preBurnerPresMPa * (1.0 + injectorPressureRatioDrop) - tankPresMPa;
            fuelPumpPresRiseMPa = preBurnerPresMPa * (1.0 + injectorPressureRatioDrop) * (1.0 + regenerativeCoolingPresDrop) - tankPresMPa;

            turbopump.CalculatePumpProperties(massFlowChamberOx, massFlowChamberFuel, tankPresMPa, oxPumpPresRiseMPa, fuelPumpPresRiseMPa);
            turbopump.CalculateTurbineProperties(turbinePresRatio, preBurnerPrefab);

            double massRatioDiff = (turbopump.GetTurbineMassFlow() - turbineMassFlow);
            return massRatioDiff;
        }

        #endregion

        #region GUI
        bool showGasGen = false;

        int oxRichInt = 0;
        static string[] fuelOxRichString = new string[] { "Fuel Rich", "Ox Rich" };

        protected override void LeftSideEngineGUI()
        {
            if (GUILayout.Button("Preburner Design"))
                showGasGen = !showGasGen;
            if (showGasGen)
            {
                double tmpTurbineInletTempK = turbineInletTempK;
                tmpTurbineInletTempK = GUIUtils.TextEntryForDoubleWithButtons("Temperature, K:", 125, tmpTurbineInletTempK, 10, 100, 50, "F0");

                //Gas Gen OxRich, FuelRich
                GUILayout.BeginHorizontal();
                oxRichInt = GUILayout.SelectionGrid(oxRichInt, fuelOxRichString, 2);
                GUILayout.EndHorizontal();

                //Gas Gen Pres
                GUILayout.BeginHorizontal();
                GUILayout.Label("Preburner Pres MPa: ", GUILayout.Width(125));
                GUILayout.Label(preBurnerPrefab.chamberPresMPa.ToString("F3"));
                GUILayout.EndHorizontal();
                //Gas Gen O/F
                GUILayout.BeginHorizontal();
                GUILayout.Label("Preburner O/F: ", GUILayout.Width(125));
                GUILayout.Label(preBurnerPrefab.OFRatio.ToString("F3"));
                GUILayout.EndHorizontal();

                UpdateGasGenProperties(oxRichInt, tmpTurbineInletTempK);
            }
        }

        protected override void RightSideEngineGUI()
        {
            turbopump.TurbopumpGUI();
        }
        #endregion
    }
}

