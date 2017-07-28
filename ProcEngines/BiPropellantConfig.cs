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
    public class BiPropellantConfig
    {
        string mixtureTitle;
        public string MixtureTitle
        {
            get { return mixtureTitle; }
        }
        double frozenAreaRatio;
        double chamberOFLimitLean;
        double chamberOFLimitRich;

        PartResourceDefinition oxidizer;
        PartResourceDefinition fuel;
        string oxidizerString;
        string fuelString;

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
                mixtureData[i] = new BiPropMixtureRatioData(mixtureDataNodes[i], mixtureTitle);
                mixtureOFRatios[i] = mixtureData[i].OFRatio;
            }
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

            valid &= PartResourceLibrary.Instance.GetDefinition(oxString) == null;
            valid &= PartResourceLibrary.Instance.GetDefinition(fuelString) == null;

            if (!valid)
                return false;

            return true;
        }
    }
}
