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
    class TurbopumpCalculator
    {
        const double G0 = 9.80665;

        EngineCalculatorBase engineCalc;
        BiPropellantConfig biPropConfig;
        PropellantProperties fuelAux;
        PropellantProperties oxAux;

        static FloatCurve pumpEffFromSpecificSpeed;
        static FloatCurve pumpEffMultFromFlowrate;

        public static GUIDropDown<TurbopumpArrangementEnum> turbopumpArrangementSelector;
        TurbopumpArrangementEnum turbopumpArrangement = TurbopumpArrangementEnum.DIRECT_DRIVE;
        bool hasInducers = true;

        double fuelPumpPower;
        double oxPumpPower;

        double massFlowOxTotal;
        double massFlowFuelTotal;

        double oxPumpPresRiseMPa;
        double fuelPumpPresRiseMPa;

        double fuelPumpEfficiency;
        double oxPumpEfficiency;
        double mechanicalEfficiency;

        double fuelPumpSpecificSpeed;
        double oxPumpSpecificSpeed;

        double fuelPumpVolFlowrate;
        double oxPumpVolFlowrate;

        double fuelPumpRotationRate;
        double oxPumpRotationRate;

        double fuelPumpHead;
        double oxPumpHead;

        int fuelPumpStages;
        int oxPumpStages;

        double turbinePresRatio;

        public TurbopumpCalculator(BiPropellantConfig config, EngineCalculatorBase engineCalc)
        {
            this.engineCalc = engineCalc;
            this.biPropConfig = config;
            fuelAux = PropellantMixtureLibrary.AuxPropellantProperties[config.Fuel];
            oxAux = PropellantMixtureLibrary.AuxPropellantProperties[config.Oxidizer];
            if ((object)pumpEffFromSpecificSpeed == null)
                LoadEfficiencyCurves();
            if (turbopumpArrangementSelector == null)
            {
                string[] turbopumpString = new string[]{
                    "Direct Drive",
                    "Gear Reduction",
                    "Two Turbine"
                };
                TurbopumpArrangementEnum[] turbopumpEnums = new TurbopumpArrangementEnum[]{
                    TurbopumpArrangementEnum.DIRECT_DRIVE,
                    TurbopumpArrangementEnum.GEAR_REDUCTION,
                    TurbopumpArrangementEnum.TWO_TURBINE
                };
                turbopumpArrangementSelector = new GUIDropDown<TurbopumpArrangementEnum>(turbopumpString, turbopumpEnums);
            }
        }

        public void UpdateTurbopumpArrangement(TurbopumpArrangementEnum newArranagement, bool hasInducers, int oxPumpStages, int fuelPumpStages)
        {
            bool unchanged = true;

            unchanged &= newArranagement == turbopumpArrangement;
            unchanged &= this.hasInducers == hasInducers;
            unchanged &= this.oxPumpStages == oxPumpStages;
            unchanged &= this.fuelPumpStages == fuelPumpStages;

            if (!unchanged)
            {
                turbopumpArrangement = newArranagement;
                this.hasInducers = hasInducers;
                this.oxPumpStages = oxPumpStages;
                this.fuelPumpStages = fuelPumpStages;
                engineCalc.CalculateEngineProperties();
            }
        }

        public void CalculatePumpProperties(double massFlowOxTotal, double massFlowFuelTotal, double tankPresMPa, double oxPumpPresRiseMPa, double fuelPumpPresRiseMPa)
        {
            this.massFlowOxTotal = massFlowOxTotal;
            this.massFlowFuelTotal = massFlowFuelTotal;

            this.oxPumpPresRiseMPa = oxPumpPresRiseMPa;
            this.fuelPumpPresRiseMPa = fuelPumpPresRiseMPa;

            double oxMaxNPSH, fuelMaxNPSH;
            double oxDens, fuelDens;

            oxDens = biPropConfig.GetOxDensity();
            fuelDens = biPropConfig.GetFuelDensity();
            oxMaxNPSH = CalculateNetPositiveSuctionHead(oxAux, oxDens, tankPresMPa);
            fuelMaxNPSH = CalculateNetPositiveSuctionHead(fuelAux, fuelDens, tankPresMPa);

            oxPumpVolFlowrate = massFlowOxTotal / oxDens;
            fuelPumpVolFlowrate = massFlowFuelTotal / fuelDens;

            double sqrtOxFlow, sqrtFuelFlow;

            sqrtOxFlow = Math.Sqrt(oxPumpVolFlowrate);
            sqrtFuelFlow = Math.Sqrt(fuelPumpVolFlowrate);

            oxPumpRotationRate = oxAux.suctionSpecificSpeed * Math.Pow(oxMaxNPSH, 0.75) / sqrtOxFlow;
            fuelPumpRotationRate = fuelAux.suctionSpecificSpeed * Math.Pow(fuelMaxNPSH, 0.75) / sqrtFuelFlow;       //this calculates the MAX rotation rate for the pumps

            if(!hasInducers)
            {
                oxPumpRotationRate *= 0.35;     //lack of inducers causes the suction specific speed to drop
                fuelPumpRotationRate *= 0.35;
            }


            if(turbopumpArrangement == TurbopumpArrangementEnum.DIRECT_DRIVE)
                oxPumpRotationRate = fuelPumpRotationRate = Math.Min(oxPumpRotationRate, fuelPumpRotationRate);     //if direct drive, them both pumps are on a common shaft with a common speed, limited to the slower one

            oxPumpHead = (this.oxPumpPresRiseMPa) / (oxDens * G0);
            fuelPumpHead = (this.fuelPumpPresRiseMPa) / (fuelDens * G0);

            oxPumpStages = Math.Max((int)((this.oxPumpPresRiseMPa) / oxAux.pressureRisePerStageMPa + 1.0), oxPumpStages);
            fuelPumpStages = Math.Max((int)((this.fuelPumpPresRiseMPa) / fuelAux.pressureRisePerStageMPa + 1.0), fuelPumpStages);

            oxPumpSpecificSpeed = oxPumpRotationRate * sqrtOxFlow * Math.Pow(oxPumpStages / oxPumpHead, 0.75);
            fuelPumpSpecificSpeed = fuelPumpRotationRate * sqrtFuelFlow * Math.Pow(fuelPumpStages / fuelPumpHead, 0.75);

            /*if(turbopumpArrangement == TurbopumpArrangementEnum.DIRECT_DRIVE)
            {
                if (oxPumpSpecificSpeed > fuelPumpSpecificSpeed && fuelPumpSpecificSpeed > 4.0)
                {

                    double speedMult = 4.0 / fuelPumpSpecificSpeed;
                    fuelPumpSpecificSpeed *= speedMult;
                    oxPumpSpecificSpeed *= speedMult;
                    oxPumpRotationRate *= speedMult;
                    fuelPumpRotationRate *= speedMult;

                }
                else if (oxPumpSpecificSpeed < fuelPumpSpecificSpeed && oxPumpSpecificSpeed > 4.0)
                {
                    double speedMult = 4.0 / oxPumpSpecificSpeed;
                    fuelPumpSpecificSpeed *= speedMult;
                    oxPumpSpecificSpeed *= speedMult;
                    oxPumpRotationRate *= speedMult;
                    fuelPumpRotationRate *= speedMult;
                }
            }*/

            oxPumpEfficiency = CalculateEfficiency(oxPumpSpecificSpeed, oxPumpVolFlowrate, oxAux.dynViscosity, oxPumpStages, true);
            fuelPumpEfficiency = CalculateEfficiency(fuelPumpSpecificSpeed, fuelPumpVolFlowrate, fuelAux.dynViscosity, fuelPumpStages, false);

            if (turbopumpArrangement == TurbopumpArrangementEnum.GEAR_REDUCTION)
            {
                mechanicalEfficiency = 0.975;
            }
            else
                mechanicalEfficiency = 1.0;

            oxPumpPower = this.massFlowOxTotal * this.oxPumpPresRiseMPa * 1000000.0 / (oxDens * oxPumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel
            fuelPumpPower = this.massFlowFuelTotal * this.fuelPumpPresRiseMPa * 1000000.0 / (fuelDens * fuelPumpEfficiency);        //convert MPa to Pa, but allow tonnes to cancel
        }

        public double RequiredPower()
        {
            double powerReq = oxPumpPower;
            powerReq += fuelPumpPower;
            return powerReq / mechanicalEfficiency;
        }

        double CalculateNetPositiveSuctionHead(PropellantProperties properties, double dens, double tankPresMPa)
        {
            double NPSH = tankPresMPa - properties.vaporPresMPaAtStorageTemp;
            NPSH /= G0 * dens;

            return NPSH;
        }

        double CalculateEfficiency(double specificSpeed, double volFlowRate, double viscosity, int stages, bool oxPump)
        {
            double eff = pumpEffFromSpecificSpeed.Evaluate((float)specificSpeed) * pumpEffMultFromFlowrate.Evaluate((float)volFlowRate);

            eff *= (1.0 - 500.0 * viscosity * stages);
            if (oxPump)
                eff *= 0.98;    //tolerance efficiency loss for ox pumps to avoid friction destroying them

            return eff;
        }

        void LoadEfficiencyCurves()
        {
            ConfigNode turboPropEffNode = GameDatabase.Instance.GetConfigNodes("ProcEngTurbopumpEff")[0];
            ConfigNode node = turboPropEffNode.GetNode("PumpEffFromSpecificSpeed");
            pumpEffFromSpecificSpeed = new FloatCurve();

            string[] vals = node.GetValues("key");
            for(int i = 0; i < vals.Length; ++i)
            {
                string[] splitString = vals[i].Split(new char[] { ',', ' ', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (splitString.Length == 2)
                    pumpEffFromSpecificSpeed.Add(float.Parse(splitString[0]), float.Parse(splitString[1]));
                else if (splitString.Length == 4)
                    pumpEffFromSpecificSpeed.Add(float.Parse(splitString[0]), float.Parse(splitString[1]), float.Parse(splitString[2]), float.Parse(splitString[3]));
            }

            node = turboPropEffNode.GetNode("PumpEffMultFromFlowrate");
            pumpEffMultFromFlowrate = new FloatCurve();

            vals = node.GetValues("key");
            for (int i = 0; i < vals.Length; ++i)
            {
                string[] splitString = vals[i].Split(new char[] { ',', ' ', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (splitString.Length == 2)
                    pumpEffMultFromFlowrate.Add(float.Parse(splitString[0]), float.Parse(splitString[1]));
                else if (splitString.Length == 4)
                    pumpEffMultFromFlowrate.Add(float.Parse(splitString[0]), float.Parse(splitString[1]), float.Parse(splitString[2]), float.Parse(splitString[3]));
            }
        }

        #region GUI
        bool showTurbopump = false;

        public void TurbopumpGUI()
        {
            if (GUILayout.Button("Turbopump Design"))
                showTurbopump = !showTurbopump;
            if (showTurbopump)
            {
                //Turbopump Pump Setup
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turbopump Type:", GUILayout.Width(125));
                turbopumpArrangementSelector.GUIDropDownDisplay();
                GUILayout.EndHorizontal();

                //Turbopump Pump Setup
                GUILayout.BeginHorizontal();
                GUILayout.Label("Use Inducers:", GUILayout.Width(125));
                bool tmpHasInducers = GUILayout.Toggle(hasInducers,"");
                GUILayout.EndHorizontal();

                //Fuel Pump Stages
                int tmpoxPumpStages = oxPumpStages;
                tmpoxPumpStages = GUIUtils.TextEntryForIntWithButton("Ox Pump Stages:", 125, tmpoxPumpStages, 1);

                //Ox Pump Power
                GUILayout.BeginHorizontal();
                GUILayout.Label("Ox Pump Power: ", GUILayout.Width(125));
                GUILayout.Label((oxPumpPower * 0.001).ToString("F1") + " kW");
                GUILayout.EndHorizontal();
                //Ox Pump Eff
                GUILayout.BeginHorizontal();
                GUILayout.Label("Ox Pump Eff: ", GUILayout.Width(125));
                GUILayout.Label((oxPumpEfficiency * 100).ToString("F1") + " %");
                GUILayout.EndHorizontal();
                //Ox Pump SS
                GUILayout.BeginHorizontal();
                GUILayout.Label("Ox Spec Spd: ", GUILayout.Width(125));
                GUILayout.Label((oxPumpSpecificSpeed).ToString("F1"));
                GUILayout.EndHorizontal();

                //Fuel Pump Stages
                int tmpfuelPumpStages = fuelPumpStages;
                tmpfuelPumpStages = GUIUtils.TextEntryForIntWithButton("Fuel Pump Stages:", 125, tmpfuelPumpStages, 1);

                //Fuel Pump Power
                GUILayout.BeginHorizontal();
                GUILayout.Label("Fuel Pump Power: ", GUILayout.Width(125));
                GUILayout.Label((fuelPumpPower * 0.001).ToString("F1") + " kW");
                GUILayout.EndHorizontal();
                //Fuel Pump Eff
                GUILayout.BeginHorizontal();
                GUILayout.Label("Fuel Pump Eff: ", GUILayout.Width(125));
                GUILayout.Label((fuelPumpEfficiency * 100).ToString("F1") + " %");
                GUILayout.EndHorizontal();
                //Fuel Pump SS
                GUILayout.BeginHorizontal();
                GUILayout.Label("Fuel Spec Spd: ", GUILayout.Width(125));
                GUILayout.Label((fuelPumpSpecificSpeed).ToString("F1"));
                GUILayout.EndHorizontal();

                //Turbine Pres Ratio
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turbine Pres Ratio: ", GUILayout.Width(125));
                GUILayout.Label(turbinePresRatio.ToString("F3"));
                GUILayout.EndHorizontal();

                UpdateTurbopumpArrangement(turbopumpArrangementSelector.ActiveSelection, tmpHasInducers, tmpoxPumpStages, tmpfuelPumpStages);
            }
        }
        #endregion
    }
}
