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

namespace ProcEngines.PropellantConfig
{
    public class BiPropellantConfig
    {
        string mixtureTitle;
        public string MixtureTitle
        {
            get { return mixtureTitle; }
        }
        double frozenAreaRatio;
        public double FrozenAreaRatio
        {
            get { return frozenAreaRatio; }
        }
        
        double chamberOFLimitLean;
        double chamberOFLimitRich;

        public double ChamberOFLimitLean
        {
            get { return chamberOFLimitLean; }
        }
        public double ChamberOFLimitRich
        {
            get { return chamberOFLimitRich; }
        }

        double chamberPresLimLow;
        double chamberPresLimHigh;
        double maxPresAvailable;

        public double ChamberPresLimLow
        {
            get { return chamberPresLimLow; }
        }
        public double ChamberPresLimHigh
        {
            get { return chamberPresLimHigh; }
        }
        public double MaxPresAvailable
        {
            get { return maxPresAvailable; }
        }        

        PartResourceDefinition oxidizer;
        PartResourceDefinition fuel;
        string oxidizerString;
        string fuelString;
        public PartResourceDefinition Oxidizer { get { return oxidizer; } }
        public PartResourceDefinition Fuel { get { return fuel; } }


        BiPropMixtureRatioData[] mixtureData;
        double[] mixtureOFRatios;


        public BiPropellantConfig(ConfigNode biPropNode)
        {
            mixtureTitle = biPropNode.GetValue("title");
            oxidizerString = biPropNode.GetValue("oxidizer");
            fuelString = biPropNode.GetValue("fuel");

            oxidizer = PartResourceLibrary.Instance.GetDefinition(oxidizerString);
            fuel = PartResourceLibrary.Instance.GetDefinition(fuelString);

            frozenAreaRatio = double.Parse(biPropNode.GetValue("frozenAreaRatio"));
            chamberOFLimitLean = double.Parse(biPropNode.GetValue("chamberOFLimitLean"));
            chamberOFLimitRich = double.Parse(biPropNode.GetValue("chamberOFLimitRich"));

            ConfigNode[] mixtureDataNodes = biPropNode.GetNodes("MixtureRatioData");

            mixtureData = new BiPropMixtureRatioData[mixtureDataNodes.Length];
            mixtureOFRatios = new double[mixtureDataNodes.Length];

            for (int i = 0; i < mixtureDataNodes.Length; ++i)
            {
                mixtureData[i] = new BiPropMixtureRatioData(mixtureDataNodes[i], mixtureTitle, frozenAreaRatio);
                mixtureOFRatios[i] = mixtureData[i].OFRatio;
            }
            chamberPresLimLow = mixtureData[0].MinChamPres;
            maxPresAvailable = mixtureData[0].MaxChamPres;
            chamberPresLimHigh = maxPresAvailable * 0.5;
        }

        public double GetOxDensity()
        {
            return oxidizer.density * 1000.0;   //convert from t/L to t/m^3
        }
        public double GetFuelDensity()
        {
            return fuel.density * 1000.0;   //convert from t/L to t/m^3
        }

        public EngineDataPrefab CalcPrefabData(double oFRatio, double chamberPres)
        {
            for (int i = 1; i < mixtureOFRatios.Length; ++i)
            {
                double oF2 = mixtureOFRatios[i];
                if(oF2 < oFRatio)
                    continue;

                double oF1 = mixtureOFRatios[i - 1];

                EngineDataPrefab prefab1, prefab2;
                prefab1 = mixtureData[i - 1].CalcData(chamberPres);
                prefab2 = mixtureData[i].CalcData(chamberPres);

                double indexFactor = oFRatio - oF1;
                indexFactor /= (oF2 - oF1);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

                EngineDataPrefab results = (prefab2 - prefab1) * indexFactor + prefab1;
                return results;
            }

            Debug.LogError("[ProcEngines] Error in data tables, could not solve");

            return new EngineDataPrefab();
        }

        public EngineDataPrefab CalcPrefabData(double oFRatio, double chamberPres, int indexLow)
        {
            double oF2 = mixtureOFRatios[indexLow + 1];
            double oF1 = mixtureOFRatios[indexLow];

            EngineDataPrefab prefab1, prefab2;
            prefab1 = mixtureData[indexLow].CalcData(chamberPres);
            prefab2 = mixtureData[indexLow + 1].CalcData(chamberPres);

            double indexFactor = oFRatio - oF1;
            indexFactor /= (oF2 - oF1);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

            EngineDataPrefab results = (prefab2 - prefab1) * indexFactor + prefab1;
            return results;
        }
        
        public EngineDataPrefab CalcDataAtPresAndTemp(double combustorPressure, double turbineInletTemp, bool oxRich)
        {
            if(oxRich)      //start at the higher OF ratios and count down
            {
                EngineDataPrefab prefab1 = mixtureData[mixtureData.Length - 1].CalcData(combustorPressure);
                for (int i = mixtureData.Length - 2; i >= 0; --i)
                {
                    EngineDataPrefab prefab2 = mixtureData[i].CalcData(combustorPressure);

                    if (prefab2.chamberTempK < turbineInletTemp)        //if prefab2 (closer to stoich) is too low, then we're not hot enough yet.
                    {                                                   //switch prefab1 to be prefab2 and calc a new prefab2 that is hopefully hotter than the temp we want
                        prefab1 = prefab2;
                        continue;
                    }
                    double indexFactor = turbineInletTemp - prefab2.chamberTempK;
                    indexFactor /= (prefab1.chamberTempK - prefab2.chamberTempK);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

                    double desiredOF = (prefab1.OFRatio - prefab2.OFRatio) * indexFactor + prefab2.OFRatio;

                    EngineDataPrefab results = (prefab1 - prefab2) * indexFactor + prefab2;
                    return results;
                }            
            }
            else
            {
                EngineDataPrefab prefab1 = mixtureData[0].CalcData(combustorPressure);
                for (int i = 1; i < mixtureData.Length; ++i)
                {
                    EngineDataPrefab prefab2 = mixtureData[i].CalcData(combustorPressure);

                    if (prefab2.chamberTempK < turbineInletTemp)
                    {
                        prefab1 = prefab2;
                        continue;
                    }
                    double indexFactor = turbineInletTemp - prefab1.chamberTempK;
                    indexFactor /= (prefab2.chamberTempK - prefab1.chamberTempK);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

                    double desiredOF = (prefab2.OFRatio - prefab1.OFRatio) * indexFactor + prefab1.OFRatio;

                    EngineDataPrefab results = (prefab2 - prefab1) * indexFactor + prefab1;
                    return results;
                }
            }

            Debug.LogError("[ProcEngines] Error in data tables, could not solve");

            return new EngineDataPrefab();
        }

        public static bool CheckConfigResourcesExist(ConfigNode biPropConfig)
        {
            bool valid = true;
            
            string oxString, fuelString;

            valid &= biPropConfig.HasValue("oxidizer");
            valid &= biPropConfig.HasValue("fuel");

            if (!valid)
                return false;

            oxString = biPropConfig.GetValue("oxidizer");
            fuelString = biPropConfig.GetValue("fuel");

            valid &= PartResourceLibrary.Instance.GetDefinition(oxString) != null;
            valid &= PartResourceLibrary.Instance.GetDefinition(fuelString) != null;

            if (!valid)
                return false;

            return true;
        }
    }
}
