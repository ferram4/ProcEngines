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
        static FloatCurve turbineEffFromIsentropicVelRatio;

        public static GUIDropDown<TurbopumpArrangementEnum> turbopumpArrangementSelector;
        TurbopumpArrangementEnum turbopumpArrangement = TurbopumpArrangementEnum.DIRECT_DRIVE;

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
        double turbineTempRatio;
        double turbineEfficiency;
        EngineDataPrefab turbineInletConditions;
        double turbineMassFlow;

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

        public void UpdateMixture(BiPropellantConfig config)
        {
            biPropConfig = config;
            fuelAux = PropellantMixtureLibrary.AuxPropellantProperties[config.Fuel];
            oxAux = PropellantMixtureLibrary.AuxPropellantProperties[config.Oxidizer];
        }

        public void UpdateTurbopumpArrangement(TurbopumpArrangementEnum newArranagement, int oxPumpStages, int fuelPumpStages)
        {
            bool unchanged = true;

            unchanged &= newArranagement == turbopumpArrangement;
            unchanged &= this.oxPumpStages == oxPumpStages;
            unchanged &= this.fuelPumpStages == fuelPumpStages;

            if (!unchanged)
            {
                turbopumpArrangement = newArranagement;
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

            if(turbopumpArrangement == TurbopumpArrangementEnum.DIRECT_DRIVE)
                oxPumpRotationRate = fuelPumpRotationRate = Math.Min(oxPumpRotationRate, fuelPumpRotationRate);     //if direct drive, them both pumps are on a common shaft with a common speed, limited to the slower one

            oxPumpHead = (this.oxPumpPresRiseMPa) / (oxDens * G0);
            fuelPumpHead = (this.fuelPumpPresRiseMPa) / (fuelDens * G0);

            oxPumpStages = Math.Max((int)((this.oxPumpPresRiseMPa) / oxAux.pressureRisePerStageMPa + 1.0), 1);
            fuelPumpStages = Math.Max((int)((this.fuelPumpPresRiseMPa) / fuelAux.pressureRisePerStageMPa + 1.0), 1);

            oxPumpSpecificSpeed = oxPumpRotationRate * sqrtOxFlow * Math.Pow(oxPumpStages / (oxPumpHead * G0), 0.75);
            fuelPumpSpecificSpeed = fuelPumpRotationRate * sqrtFuelFlow * Math.Pow(fuelPumpStages / (fuelPumpHead * G0), 0.75);

            oxPumpEfficiency = CalculatePumpEfficiency(oxPumpSpecificSpeed, oxPumpVolFlowrate, oxAux.dynViscosity, oxPumpStages, true);
            fuelPumpEfficiency = CalculatePumpEfficiency(fuelPumpSpecificSpeed, fuelPumpVolFlowrate, fuelAux.dynViscosity, fuelPumpStages, false);

            if (turbopumpArrangement == TurbopumpArrangementEnum.GEAR_REDUCTION)
            {
                mechanicalEfficiency = 0.975;
            }
            else
                mechanicalEfficiency = 1.0;

            oxPumpPower = oxPumpVolFlowrate * this.oxPumpPresRiseMPa * 1000000.0 / oxPumpEfficiency;        //convert MPa to Pa, but allow tonnes to cancel
            fuelPumpPower = fuelPumpVolFlowrate * this.fuelPumpPresRiseMPa * 1000000.0 / fuelPumpEfficiency;        //convert MPa to Pa, but allow tonnes to cancel
        }

        public double GetTotalRequiredPower()
        {
            double powerReq = oxPumpPower;
            powerReq += fuelPumpPower;
            return powerReq / mechanicalEfficiency;
        }

        public double GetTurbineExitTemp()
        {
            return turbineInletConditions.chamberTempK / turbineTempRatio;
        }

        public void CalculateTurbineProperties(double turbinePresRatio, EngineDataPrefab turbineInletConditions)
        {
            this.turbineInletConditions = turbineInletConditions;
            CalculateTurbineProperties(turbinePresRatio);
        }

        public void CalculateTurbineProperties(double turbinePresRatio)
        {
            this.turbinePresRatio = turbinePresRatio;

            double gammaPower = (turbineInletConditions.nozzleGamma - 1.0) / turbineInletConditions.nozzleGamma;
            turbineTempRatio = Math.Pow(turbinePresRatio, gammaPower);

            turbineEfficiency = CalculateTurbineEfficiency();

            //turbineMassFlow = (1.0 - Math.Pow(turbinePresRatio, gammaPower));
            //turbineMassFlow *= turbineInletConditions.chamberCp * turbineInletConditions.chamberTempK;
            //turbineMassFlow = GetTotalRequiredPower() / (1000.0 * turbineMassFlow * turbineEfficiency);   //convert to tonnes
        }

        public double GetTurbineMassFlow()
        {
            turbineMassFlow = (1.0 - 1.0 / turbineTempRatio);
            turbineMassFlow *= turbineInletConditions.chamberCp * turbineInletConditions.chamberTempK;
            turbineMassFlow = GetTotalRequiredPower() / (1000.0 * turbineMassFlow * turbineEfficiency);   //convert to tonnes

            return turbineMassFlow;
        }

        public double GetTurbinePower(double turbineMassFlow)
        {
            this.turbineMassFlow = turbineMassFlow;


            double power = turbineMassFlow * turbineEfficiency * 1000.0;
            power *= turbineInletConditions.chamberCp * turbineInletConditions.chamberTempK;
            power *= (1.0 - 1.0 / turbineTempRatio);

            return power;
        }

        public double PowerExtractedByTurbineFlow()
        {
            return GetTotalRequiredPower() / turbineEfficiency;
        }

        double CalculateNetPositiveSuctionHead(PropellantProperties properties, double dens, double tankPresMPa)
        {
            double NPSH = tankPresMPa - properties.vaporPresMPaAtStorageTemp;
            NPSH /= G0 * dens;

            return NPSH;
        }

        double CalculatePumpEfficiency(double specificSpeed, double volFlowRate, double viscosity, int stages, bool oxPump)
        {
            /*double eff = pumpEffFromSpecificSpeed.Evaluate((float)specificSpeed) * pumpEffMultFromFlowrate.Evaluate((float)volFlowRate);

            //eff *= (1.0 - 500.0 * viscosity * stages);
            if (oxPump)
                eff *= 0.98;    //tolerance efficiency loss for ox pumps to avoid friction destroying them*/

            double eff = specificSpeed;
            if(eff < 0.8)
            {
                eff = 0.41989
                    + eff * (2.1524
                    + eff * (-3.1434
                    + eff * 1.5673));
            }
            else
            {
                eff = 1.020 - 0.120 * eff;
            }

            return eff;
        }

        double CalculateTurbineEfficiency()
        {
            double pitchLineVelocity = 250;// more typical value //500;     //max value possible

            double theoreticalSpoutingVel = 1.0 - Math.Pow(turbinePresRatio, (1.0 - turbineInletConditions.nozzleGamma) / turbineInletConditions.nozzleGamma);
            theoreticalSpoutingVel *= turbineInletConditions.chamberTempK * turbineInletConditions.chamberCp * 2.0;
            theoreticalSpoutingVel = Math.Sqrt(theoreticalSpoutingVel);

            double isentropicVelRatio = pitchLineVelocity / theoreticalSpoutingVel;

            return (double)turbineEffFromIsentropicVelRatio.Evaluate((float)isentropicVelRatio);
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

            node = turboPropEffNode.GetNode("TurbineEffFromIsentropicVelRatio");
            turbineEffFromIsentropicVelRatio = new FloatCurve();

            vals = node.GetValues("key");
            for (int i = 0; i < vals.Length; ++i)
            {
                string[] splitString = vals[i].Split(new char[] { ',', ' ', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (splitString.Length == 2)
                    turbineEffFromIsentropicVelRatio.Add(float.Parse(splitString[0]), float.Parse(splitString[1]));
                else if (splitString.Length == 4)
                    turbineEffFromIsentropicVelRatio.Add(float.Parse(splitString[0]), float.Parse(splitString[1]), float.Parse(splitString[2]), float.Parse(splitString[3]));
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

                //Fuel Pump Stages
                int tmpoxPumpStages = oxPumpStages;
                tmpoxPumpStages = GUIUtils.TextEntryForIntWithButton("Ox Pump Stages:", 125, tmpoxPumpStages, 50);

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
                //Ox Pump vol
                GUILayout.BeginHorizontal();
                GUILayout.Label("Ox Vol Flow: ", GUILayout.Width(125));
                GUILayout.Label((oxPumpVolFlowrate).ToString("F1") + " m^3/s");
                GUILayout.EndHorizontal();

                //Fuel Pump Stages
                int tmpfuelPumpStages = fuelPumpStages;
                tmpfuelPumpStages = GUIUtils.TextEntryForIntWithButton("Fuel Pump Stages:", 125, tmpfuelPumpStages, 50);

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
                //Fuel Pump vol
                GUILayout.BeginHorizontal();
                GUILayout.Label("Fuel Vol Flow: ", GUILayout.Width(125));
                GUILayout.Label((fuelPumpVolFlowrate).ToString("F1") + " m^3/s");
                GUILayout.EndHorizontal();

                //Turbine Pres Ratio
                GUILayout.BeginHorizontal();
                GUILayout.Label("Turbine Pres Ratio: ", GUILayout.Width(125));
                GUILayout.Label(turbinePresRatio.ToString("F3"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Turbine Eff: ", GUILayout.Width(125));
                GUILayout.Label((turbineEfficiency * 100).ToString("F1") + " %");
                GUILayout.EndHorizontal(); 
                
                UpdateTurbopumpArrangement(turbopumpArrangementSelector.ActiveSelection, tmpoxPumpStages, tmpfuelPumpStages);
            }
        }
        #endregion
    }
}
