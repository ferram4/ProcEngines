using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProcEngines
{
    public class BiPropMixtureRatioData
    {
        string mixtureName;
        double oFRatio;

        double minChamPres;
        double maxChamPres;

        double[] chamberPresMPaData;
        double[] chamberTempKData;
        double[] nozzlePresMPaData;
        double[] nozzleTempKData;
        double[] nozzleMolWeightgMolData;
        double[] nozzleGammaData;
        double[] nozzleMachData;

        public BiPropMixtureRatioData(ConfigNode mixtureRatioNode, string mixtureName)
        {
            this.mixtureName = mixtureName;

            this.oFRatio = double.Parse(mixtureRatioNode.GetValue("OFRatio"));
            ConfigNode pressureNode = mixtureRatioNode.GetNode("PressureData");

            string[] pressureNodeKeys = pressureNode.GetValues("key");

            this.chamberPresMPaData = new double[pressureNodeKeys.Length];
            this.chamberTempKData = new double[pressureNodeKeys.Length];
            this.nozzlePresMPaData = new double[pressureNodeKeys.Length];
            this.nozzleTempKData = new double[pressureNodeKeys.Length];
            this.nozzleMolWeightgMolData = new double[pressureNodeKeys.Length];
            this.nozzleGammaData = new double[pressureNodeKeys.Length];
            this.nozzleMachData = new double[pressureNodeKeys.Length];

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

        public void CalcData(double inputChamberPres, out double chamberTemp, out double nozzleTemp, out double nozzlePres, out double nozzleMW, out double nozzleGamma, out double nozzleMach)
        {
            chamberTemp = 0;
            nozzleTemp = 0;
            nozzlePres = 0;
            nozzleMW = 0;
            nozzleGamma = 0;
            nozzleMach = 0;

            if (inputChamberPres < minChamPres)
            {
                Debug.LogError("[ProcEngines] Error: chamber pressure of " + inputChamberPres + " is below min pressure of " + minChamPres + " for " + mixtureName + " at O//F " + oFRatio);
                return;
            }
            if (inputChamberPres > maxChamPres)
            {
                Debug.LogError("[ProcEngines] Error: chamber pressure of " + inputChamberPres + " is below min pressure of " + maxChamPres + " for " + mixtureName + " at O//F " + oFRatio);
                return;
            }

            for (int i = 1; i < chamberPresMPaData.Length; ++i)
            {
                double chamPres2 = chamberPresMPaData[i];
                if (chamPres2 <= inputChamberPres)
                    continue;
                double chamPres1 = chamberPresMPaData[i - 1];

                double indexFactor = inputChamberPres - chamPres1;
                indexFactor /= (chamPres2 - chamPres1);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

                chamberTemp = (chamberTempKData[i] - chamberTempKData[i - 1]) * indexFactor + chamberTempKData[i - 1];
                nozzleTemp = (nozzleTempKData[i] - nozzleTempKData[i - 1]) * indexFactor + nozzleTempKData[i - 1];
                nozzlePres = (nozzlePresMPaData[i] - nozzlePresMPaData[i - 1]) * indexFactor + nozzlePresMPaData[i - 1];
                nozzleMW = (nozzleMolWeightgMolData[i] - nozzleMolWeightgMolData[i - 1]) * indexFactor + nozzleMolWeightgMolData[i - 1];
                nozzleGamma = (nozzleGammaData[i] - nozzleGammaData[i - 1]) * indexFactor + nozzleGammaData[i - 1];
                nozzleMach = (nozzleMachData[i] - nozzleMachData[i - 1]) * indexFactor + nozzleMachData[i - 1];

                break;
            }

            if (chamberTemp <= 0)
                Debug.LogError("[ProcEngines] Error in data tables, could not solve");
        }
    }
}
