using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProcEngines
{
    public class BiPropellantMixture
    {
        string mixtureTitle;
        double frozenAreaRatio;

        PartResourceDefinition oxidizer;
        PartResourceDefinition fuel;
        string oxidizerString;
        string fuelString;

        BiPropMixtureRatioData[] mixtureData;
        double[] mixtureOFRatios;

        public BiPropellantMixture(ConfigNode mixtureNode)
        {
            mixtureTitle = mixtureNode.GetValue("title");
            oxidizerString = mixtureNode.GetValue("oxidizer");
            fuelString = mixtureNode.GetValue("fuel");

            oxidizer = PartResourceLibrary.Instance.GetDefinition(oxidizerString);
            fuel = PartResourceLibrary.Instance.GetDefinition(fuelString);

            frozenAreaRatio = double.Parse(mixtureNode.GetValue("frozenAreaRatio"));

            ConfigNode[] mixtureDataNodes = mixtureNode.GetNodes("MixtureRatioData");

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
    }
}
