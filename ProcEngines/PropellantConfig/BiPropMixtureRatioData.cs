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

namespace ProcEngines.PropellantConfig
{
    public class BiPropMixtureRatioData
    {
        string mixtureName;
        double oFRatio;
        public double OFRatio
        {
            get { return oFRatio; }
            private set { oFRatio = value; }
        }

        double minChamPres;
        double maxChamPres;
        public double MinChamPres
        {
            get { return minChamPres; }
        }
        public double MaxChamPres
        {
            get { return maxChamPres; }
        }

        double[] chamberPresMPaData;
        double[] chamberTempKData;
        double[] nozzlePresMPaData;
        double[] nozzleTempKData;
        double[] nozzleMolWeightgMolData;
        double[] nozzleGammaData;
        double[] nozzleMachData;
        double frozenAreaRatio;

        public BiPropMixtureRatioData(ConfigNode mixtureRatioNode, string mixtureName, double frozenAreaRatio)
        {
            this.mixtureName = mixtureName;

            this.oFRatio = double.Parse(mixtureRatioNode.GetValue("OFratio"));
            ConfigNode pressureNode = mixtureRatioNode.GetNode("PressureData");

            string[] pressureNodeKeys = pressureNode.GetValues("key");

            this.chamberPresMPaData = new double[pressureNodeKeys.Length];
            this.chamberTempKData = new double[pressureNodeKeys.Length];
            this.nozzlePresMPaData = new double[pressureNodeKeys.Length];
            this.nozzleTempKData = new double[pressureNodeKeys.Length];
            this.nozzleMolWeightgMolData = new double[pressureNodeKeys.Length];
            this.nozzleGammaData = new double[pressureNodeKeys.Length];
            this.nozzleMachData = new double[pressureNodeKeys.Length];
            this.frozenAreaRatio = frozenAreaRatio;

            for (int i = 0; i < pressureNodeKeys.Length; ++i)
            {
                string[] splitSection = pressureNodeKeys[i].Split(new char[] { ',', ' ', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (splitSection.Length != 7)
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " is incomplete//has too many entries");

                double tmpVal;
                //Chamber Pressure
                if (double.TryParse(splitSection[0], out tmpVal))
                    chamberPresMPaData[i] = tmpVal;
                else
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " chamber pres could not be parsed as double; string is " + splitSection[0]);

                //Chamber Temperature
                if (double.TryParse(splitSection[1], out tmpVal))
                    chamberTempKData[i] = tmpVal;
                else
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " chamber temp could not be parsed as double; string is " + splitSection[1]);

                //Nozzle Temperature
                if (double.TryParse(splitSection[2], out tmpVal))
                    nozzleTempKData[i] = tmpVal;
                else
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " nozzle temp could not be parsed as double; string is " + splitSection[2]);

                //Nozzle Pressure
                if (double.TryParse(splitSection[3], out tmpVal))
                    nozzlePresMPaData[i] = tmpVal;
                else
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " nozzle pres could not be parsed as double; string is " + splitSection[3]);

                //Nozzle MW
                if (double.TryParse(splitSection[4], out tmpVal))
                    nozzleMolWeightgMolData[i] = tmpVal;
                else
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " nozzle MW could not be parsed as double; string is " + splitSection[4]);

                //Nozzle Gamma
                if (double.TryParse(splitSection[5], out tmpVal))
                    nozzleGammaData[i] = tmpVal;
                else
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " nozzle gamma could not be parsed as double; string is " + splitSection[5]);

                //Nozzle Mach
                if (double.TryParse(splitSection[6], out tmpVal))
                    nozzleMachData[i] = tmpVal;
                else
                    Debug.LogError("[ProcEngines] Error: Row " + i + " for " + mixtureName + " at O//F " + oFRatio + " nozzle Mach could not be parsed as double; string is " + splitSection[6]);
            }

            minChamPres = chamberPresMPaData[0];
            maxChamPres = chamberPresMPaData[chamberPresMPaData.Length - 1];
        }

        public EngineDataPrefab CalcData(double inputChamberPres)
        {
            EngineDataPrefab prefab = new EngineDataPrefab();
            prefab.OFRatio = oFRatio;
            prefab.chamberPresMPa = inputChamberPres;
            prefab.chamberTempK = 0;
            prefab.nozzleTempK = 0;
            prefab.nozzlePresMPa = 0;
            prefab.nozzleMWgMol = 0;
            prefab.nozzleGamma = 0;
            prefab.nozzleMach = 0;
            prefab.frozenAreaRatio = frozenAreaRatio;

            if (inputChamberPres < minChamPres)
            {
                Debug.LogError("[ProcEngines] Error: chamber pressure of " + inputChamberPres + " is below min pressure of " + minChamPres + " for " + mixtureName + " at O//F " + oFRatio);
                return prefab;
            }
            if (inputChamberPres > maxChamPres)
            {
                Debug.LogError("[ProcEngines] Error: chamber pressure of " + inputChamberPres + " is below min pressure of " + maxChamPres + " for " + mixtureName + " at O//F " + oFRatio);
                return prefab;
            }

            for (int i = 1; i < chamberPresMPaData.Length; ++i)
            {
                double chamPres2 = chamberPresMPaData[i];
                if (chamPres2 < inputChamberPres)
                    continue;

                double chamPres1 = chamberPresMPaData[i - 1];

                double indexFactor = inputChamberPres - chamPres1;
                indexFactor /= (chamPres2 - chamPres1);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

                prefab.chamberTempK = (chamberTempKData[i] - chamberTempKData[i - 1]) * indexFactor + chamberTempKData[i - 1];
                prefab.nozzleTempK = (nozzleTempKData[i] - nozzleTempKData[i - 1]) * indexFactor + nozzleTempKData[i - 1];
                prefab.nozzlePresMPa = (nozzlePresMPaData[i] - nozzlePresMPaData[i - 1]) * indexFactor + nozzlePresMPaData[i - 1];
                prefab.nozzleMWgMol = (nozzleMolWeightgMolData[i] - nozzleMolWeightgMolData[i - 1]) * indexFactor + nozzleMolWeightgMolData[i - 1];
                prefab.nozzleGamma = (nozzleGammaData[i] - nozzleGammaData[i - 1]) * indexFactor + nozzleGammaData[i - 1];
                prefab.nozzleMach = (nozzleMachData[i] - nozzleMachData[i - 1]) * indexFactor + nozzleMachData[i - 1];

                break;
            }

            if (prefab.chamberTempK <= 0)
                Debug.LogError("[ProcEngines] Error in data tables, could not solve");

            return prefab;
        }
    }
}
