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
    class EngineCalculatorGasGen : EngineCalculatorBase
    {
        double oxPumpPresRiseMPa;
        double fuelPumpPresRiseMPa;

        double turbinePresRatio;
        double turbineInletTempK = 1000;
        double turbineMassFlow;
        double turbineBackPresMPa;

        static double maxTurbineInletK = 1350;
        static double minTurbineInletK = 700;

        public static GUIDropDown<TurbineExhaustEnum> turbineExhaustSelector;
        TurbineExhaustEnum turbineExhaustType = TurbineExhaustEnum.DIRECT;
        double exhaustArea;
        double nozzleExhaustArea;

        TurbopumpCalculator turbopump;

        EngineDataPrefab gasGenPrefab;
        double massFlowFrac;
        bool oxRich;

        public EngineCalculatorGasGen(BiPropellantConfig mixture, double oFRatio, double chamberPresMPa, double areaRatio, double throatDiameter)
            : base(mixture, oFRatio, chamberPresMPa, areaRatio, throatDiameter) { }

        public override string EngineCalculatorType()
        {
            return "Gas Generator";
        }

        #region EnginePerformanceCalc
        public override void CalculateEngineProperties()
        {
            if (turbopump == null)
            {
                turbopump = new TurbopumpCalculator(biPropConfig, this);

                string[] turbineExhaustString = new string[]{
                    "Direct",
                    "Into Nozzle"
                };
                TurbineExhaustEnum[] turbExhaustEnums = new TurbineExhaustEnum[]{
                    TurbineExhaustEnum.DIRECT,
                    TurbineExhaustEnum.INTO_NOZZLE
                };

                turbineExhaustSelector = new GUIDropDown<TurbineExhaustEnum>(turbineExhaustString, turbExhaustEnums);
            }


            CalculateMainCombustionChamberParameters();
            AssumePumpPressureRise();
            SolveGasGenTurbine(oxRich);
            CalculateEngineAndNozzlePerformanceProperties();
        }

        void UpdateGasGenProperties(int oxRichInt, double turbineInletTemp)
        {
            bool unchanged = true;

            if(oxRichInt <= 0 && oxRich)
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

        void UpdateTurbineExhaust(TurbineExhaustEnum turbineExhaustType, double nozzleExhaustArea)
        {
            bool unchanged = true;

            if (nozzleExhaustArea > areaRatio)
                nozzleExhaustArea = areaRatio;
            if (nozzleExhaustArea < enginePrefab.frozenAreaRatio)
                nozzleExhaustArea = enginePrefab.frozenAreaRatio;

            unchanged &= this.nozzleExhaustArea == nozzleExhaustArea;
            unchanged &= this.turbineExhaustType == turbineExhaustType;

            if (!unchanged)
            {
                this.nozzleExhaustArea = nozzleExhaustArea;
                this.turbineExhaustType = turbineExhaustType;
                CalculateEngineProperties();
            }
        }
        
        #endregion

        #region TurbopumpCalc
        void AssumePumpPressureRise()
        {
            oxPumpPresRiseMPa = chamberPresMPa * (1.0 + injectorPressureRatioDrop) - tankPresMPa;
            fuelPumpPresRiseMPa = chamberPresMPa * (1.0 + injectorPressureRatioDrop) * (1.0 + regenerativeCoolingPresDrop) - tankPresMPa;      //assume that only fuel is used for regen cooling
        }

        void SolveGasGenTurbine(bool oxRich)
        {
            double gasGenInjectorPresDrop = injectorPressureRatioDrop * 3.0 / 2.0;
            if (gasGenInjectorPresDrop < 0.2)
                gasGenInjectorPresDrop = 0.2;

            double gasGenPresMPa = chamberPresMPa * (1.0 + injectorPressureRatioDrop) / (1.0 + gasGenInjectorPresDrop);

            gasGenPrefab = biPropConfig.CalcDataAtPresAndTemp(gasGenPresMPa, turbineInletTempK, oxRich);        //assume that gas gen runs at same pressure as chamber

            CalcTurbineExhaustBackPressure();
            turbinePresRatio = gasGenPresMPa / turbineBackPresMPa;


            double gammaPower = -(gasGenPrefab.nozzleGamma - 1.0) / gasGenPrefab.nozzleGamma;
            double[] gasGenOFRatio = new double[] { gasGenPrefab.OFRatio };


            turbopump.UpdateMixture(biPropConfig);
            turbopump.CalculateTurbineProperties(turbinePresRatio, gasGenPrefab);

            turbineMassFlow = MathUtils.BrentsMethod(IterateSolveGasGenTurbine, gasGenOFRatio, 0, 1.5 * massFlowChamber, 0.000001, int.MaxValue);

            massFlowTotal += turbineMassFlow;

            overallOFRatio = chamberOFRatio;
            overallOFRatio *= massFlowChamber / massFlowTotal;
            overallOFRatio += gasGenPrefab.OFRatio * turbineMassFlow / massFlowTotal;

            massFlowFrac = turbineMassFlow / massFlowTotal;

            CalcTurbineExhaustThrust();
        }

        double IterateSolveGasGenTurbine(double turbineMassFlow, double[] gasGenOFRatio)
        {
            double turbineMassFlowFuel = turbineMassFlow / (gasGenOFRatio[0] + 1.0);
            double turbineMassFlowOx = turbineMassFlowFuel * gasGenOFRatio[0];

            double massFlowFuelTotal = turbineMassFlowFuel + massFlowChamberFuel;
            double massFlowOxTotal = turbineMassFlowOx + massFlowChamberOx;

            turbopump.CalculatePumpProperties(massFlowOxTotal, massFlowFuelTotal, tankPresMPa, oxPumpPresRiseMPa, fuelPumpPresRiseMPa);
            turbopump.CalculateTurbineProperties(turbinePresRatio, gasGenPrefab);

            double massFlowDiff = (turbopump.GetTurbineMassFlow() - turbineMassFlow);
            return massFlowDiff;
        }

        void CalcTurbineExhaustBackPressure()
        {
            double exhaustPresMPa = 0.1013 * 1.5;      //1.5 atm

            if(turbineExhaustType == TurbineExhaustEnum.INTO_NOZZLE)
            {
                double machAtArea = NozzleUtils.MachFromAreaRatio(nozzleExhaustArea, modGamma);

                double isentropicRatio = 0.5 * (modGamma - 1.0);
                isentropicRatio = (1.0 + isentropicRatio * enginePrefab.nozzleMach * enginePrefab.nozzleMach) / (1.0 + isentropicRatio * machAtArea * machAtArea);

                exhaustPresMPa = Math.Pow(isentropicRatio, modGamma / (modGamma - 1.0)) * enginePrefab.nozzlePresMPa;       //gets the pressure in the nozzle at this area ratio

                exhaustPresMPa *= (1.05);   //add some extra backpressure due to piping
            }

            turbineBackPresMPa = (gasGenPrefab.nozzleGamma - 1.0) * 0.5 + 1.0;
            turbineBackPresMPa = Math.Pow(turbineBackPresMPa, gasGenPrefab.nozzleGamma / (gasGenPrefab.nozzleGamma - 1.0));
            turbineBackPresMPa *= exhaustPresMPa;
        }

        void CalcTurbineExhaustThrust()
        {
            double nozzleInletTemp = turbopump.GetTurbineExitTemp();
            
            if (turbineExhaustType == TurbineExhaustEnum.DIRECT)
            {
                //Calc exit area, which is the throat area since this is a choked converging nozzle
                exhaustArea = (gasGenPrefab.nozzleGamma + 1.0) / (gasGenPrefab.nozzleGamma - 1.0);
                exhaustArea = Math.Pow(2.0 / (gasGenPrefab.nozzleGamma + 1.0), exhaustArea);
                exhaustArea *= gasGenPrefab.nozzleGamma * enginePrefab.nozzleMWgMol;
                exhaustArea /= (GAS_CONSTANT * nozzleInletTemp);
                exhaustArea = Math.Sqrt(exhaustArea);
                exhaustArea *= turbineBackPresMPa;
                exhaustArea = turbineMassFlow / exhaustArea;
                exhaustArea *= 0.001;
                
                double exhaustExitTemp = nozzleInletTemp / (1.0 + (gasGenPrefab.nozzleGamma - 1.0) * 0.5);

                double exhaustExitVel = Math.Sqrt(gasGenPrefab.nozzleGamma * exhaustExitTemp * GAS_CONSTANT / gasGenPrefab.nozzleMWgMol);

                double exhaustThrustMomentum = exhaustExitVel * turbineMassFlow;
                thrustVacAux += exhaustThrustMomentum;
                thrustSLAux += exhaustThrustMomentum;

                double exhaustExitPres = turbineBackPresMPa / Math.Pow((1.0 + (gasGenPrefab.nozzleGamma - 1.0) * 0.5), gasGenPrefab.nozzleGamma / (gasGenPrefab.nozzleGamma - 1.0));

                thrustVacAux += exhaustExitPres * exhaustArea * 1000.0;
                thrustSLAux += (exhaustExitPres - 0.1013) * exhaustArea * 1000.0;
            }
            else if(turbineExhaustType == TurbineExhaustEnum.INTO_NOZZLE)
            {
                double exhaustExpansionRatio = areaRatio / nozzleExhaustArea;

                double machThroughExpansion = exhaustExpansionRatio <= 1.0 ? 1.0 : NozzleUtils.MachFromAreaRatio(exhaustExpansionRatio, gasGenPrefab.nozzleGamma);

                double exhaustExitTemp = nozzleInletTemp / (1.0 + (gasGenPrefab.nozzleGamma - 1.0) * 0.5 * machThroughExpansion * machThroughExpansion);

                double exhaustExitVel = Math.Sqrt(gasGenPrefab.nozzleGamma * exhaustExitTemp * GAS_CONSTANT / gasGenPrefab.nozzleMWgMol) * machThroughExpansion;

                double exhaustThrustMomentum = exhaustExitVel * turbineMassFlow;

                exhaustThrustMomentum *= nozzleDivEfficiency;

                double relDistanceDownNozzle = nozzle.GetFracLengthAtArea(nozzleExhaustArea);

                exhaustThrustMomentum *= (1.0 * relDistanceDownNozzle + nozzleFrictionEfficiency * (1.0 - relDistanceDownNozzle));

                thrustVacAux += exhaustThrustMomentum;
                thrustSLAux += exhaustThrustMomentum;
            }
        }
        #endregion

        #region GUI
        bool showGasGen = false;
        bool showExhaust = false;

        int oxRichInt = 0;
        static string[] fuelOxRichString = new string[] { "Fuel Rich", "Ox Rich" };

        protected override void LeftSideEngineGUI()
        {
            if (GUILayout.Button("Gas Generator Design"))
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
                GUILayout.Label("GG Pres MPa: ", GUILayout.Width(125));
                GUILayout.Label(gasGenPrefab.chamberPresMPa.ToString("F3"));
                GUILayout.EndHorizontal();
                //Gas Gen O/F
                GUILayout.BeginHorizontal();
                GUILayout.Label("GG O/F: ", GUILayout.Width(125));
                GUILayout.Label(gasGenPrefab.OFRatio.ToString("F3"));
                GUILayout.EndHorizontal();
                //Gas Gen Mass Flow %
                GUILayout.BeginHorizontal();
                GUILayout.Label("GG % Mass Flow: ", GUILayout.Width(125));
                GUILayout.Label((massFlowFrac * 100.0).ToString("F1") + " %");
                GUILayout.EndHorizontal();

                UpdateGasGenProperties(oxRichInt, tmpTurbineInletTempK);
            }
        }

        protected override void RightSideEngineGUI()
        {
            turbopump.TurbopumpGUI();
            if (GUILayout.Button("Turbine Exhaust Design"))
                showExhaust = !showExhaust;
            if (showExhaust)
            {
                //Turbopump Pump Setup
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turbine Exhaust:", GUILayout.Width(125));
                turbineExhaustSelector.GUIDropDownDisplay();
                GUILayout.EndHorizontal();

                double tmpNozzleExhaustArea = nozzleExhaustArea;
                if(turbineExhaustType == TurbineExhaustEnum.DIRECT)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Turbine Exhaust Area: ", GUILayout.Width(125));
                    GUILayout.Label(exhaustArea.ToString("F3") + " m^3");
                    GUILayout.EndHorizontal();
                }
                else if (turbineExhaustType == TurbineExhaustEnum.INTO_NOZZLE)
                {
                    tmpNozzleExhaustArea = GUIUtils.TextEntryForDoubleWithButtons("Exhaust Area Ratio:", 125, tmpNozzleExhaustArea, 0.1, 1.0, 50);
                }

                //Turbine Exhaust Thrust
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turbine Thrust Vac: ", GUILayout.Width(125));
                GUILayout.Label(thrustVacAux.ToString("F3") + " kN");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Turbine Thrust SL: ", GUILayout.Width(125));
                GUILayout.Label(thrustSLAux.ToString("F3") + " kN");
                GUILayout.EndHorizontal();

                UpdateTurbineExhaust(turbineExhaustSelector.ActiveSelection, tmpNozzleExhaustArea);
            }
        }
        #endregion
    }
}
