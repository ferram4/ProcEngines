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
    public struct EngineDataPrefab
    {

        public double OFRatio;
        public double chamberPresMPa;
        public double chamberTempK;
        public double nozzlePresMPa;
        public double nozzleTempK;
        public double nozzleMWgMol;
        public double nozzleGamma;
        public double nozzleMach;
        public double chamberCp;
        public double nozzleCp;
        public double frozenAreaRatio;

        public EngineDataPrefab(ConfigNode node)
        {
            OFRatio = double.Parse(node.GetValue("OFRatio"));
            chamberPresMPa = double.Parse(node.GetValue("chamberPresMPa"));
            chamberTempK = double.Parse(node.GetValue("chamberTempK"));
            nozzlePresMPa = double.Parse(node.GetValue("nozzlePresMPa"));
            nozzleTempK = double.Parse(node.GetValue("nozzleTempK"));
            nozzleMWgMol = double.Parse(node.GetValue("nozzleMWgMol"));
            nozzleGamma = double.Parse(node.GetValue("nozzleGamma"));
            nozzleMach = double.Parse(node.GetValue("nozzleMach"));
            chamberCp = double.Parse(node.GetValue("chamberCp"));
            nozzleCp = double.Parse(node.GetValue("nozzleCp"));
            frozenAreaRatio = double.Parse(node.GetValue("frozenAreaRatio"));
        }

        public ConfigNode CreateConfigNode()
        {
            ConfigNode node = new ConfigNode();
            node.AddValue("OFRatio", OFRatio);
            node.AddValue("chamberPresMPa", chamberPresMPa);
            node.AddValue("chamberTempK", chamberTempK);
            node.AddValue("nozzlePresMPa", nozzlePresMPa);
            node.AddValue("nozzleTempK", nozzleTempK);
            node.AddValue("nozzleMWgMol", nozzleMWgMol);
            node.AddValue("nozzleGamma", nozzleGamma);
            node.AddValue("nozzleMach", nozzleMach);
            node.AddValue("chamberCp", chamberCp);
            node.AddValue("nozzleCp", nozzleCp);
            node.AddValue("frozenAreaRatio", frozenAreaRatio);

            return node;
        }

        public void Print()
        {
            Debug.Log("EngineDataPrefab:\nOFRatio: " + OFRatio + "\nchamPresMPa: " + chamberPresMPa + "\nChamTempK: " + chamberTempK
                + "\nNozzlePresMPa: " + nozzlePresMPa + "\nNozzleTempK: " + nozzleTempK + "\nNozzleMW: " + nozzleMWgMol
                + "\nNozzleGamma: " + nozzleGamma + "\nNozzleMach: " + nozzleMach);
        }

        public static EngineDataPrefab operator +(EngineDataPrefab a, EngineDataPrefab b)
        {
            EngineDataPrefab prefab = new EngineDataPrefab();
            prefab.OFRatio = a.OFRatio + b.OFRatio;
            prefab.chamberPresMPa = a.chamberPresMPa + b.chamberPresMPa;
            prefab.chamberTempK = a.chamberTempK + b.chamberTempK;
            prefab.nozzlePresMPa = a.nozzlePresMPa + b.nozzlePresMPa;
            prefab.nozzleTempK = a.nozzleTempK + b.nozzleTempK;
            prefab.nozzleMWgMol = a.nozzleMWgMol + b.nozzleMWgMol;
            prefab.nozzleGamma = a.nozzleGamma + b.nozzleGamma;
            prefab.nozzleMach = a.nozzleMach + b.nozzleMach;
            prefab.chamberCp = a.chamberCp + b.chamberCp;
            prefab.nozzleCp = a.nozzleCp + b.nozzleCp;
            prefab.frozenAreaRatio = a.frozenAreaRatio + b.frozenAreaRatio;

            return prefab;
        }
        public static EngineDataPrefab operator -(EngineDataPrefab a, EngineDataPrefab b)
        {
            EngineDataPrefab prefab = new EngineDataPrefab();
            prefab.OFRatio = a.OFRatio - b.OFRatio;
            prefab.chamberPresMPa = a.chamberPresMPa - b.chamberPresMPa;
            prefab.chamberTempK = a.chamberTempK - b.chamberTempK;
            prefab.nozzlePresMPa = a.nozzlePresMPa - b.nozzlePresMPa;
            prefab.nozzleTempK = a.nozzleTempK - b.nozzleTempK;
            prefab.nozzleMWgMol = a.nozzleMWgMol - b.nozzleMWgMol;
            prefab.nozzleGamma = a.nozzleGamma - b.nozzleGamma;
            prefab.nozzleMach = a.nozzleMach - b.nozzleMach;
            prefab.chamberCp = a.chamberCp - b.chamberCp;
            prefab.nozzleCp = a.nozzleCp - b.nozzleCp;
            prefab.frozenAreaRatio = a.frozenAreaRatio - b.frozenAreaRatio;

            return prefab;
        }
        public static EngineDataPrefab operator *(EngineDataPrefab a, double b)
        {
            EngineDataPrefab prefab = new EngineDataPrefab();
            prefab.OFRatio = a.OFRatio * b;
            prefab.chamberPresMPa = a.chamberPresMPa * b;
            prefab.chamberTempK = a.chamberTempK * b;
            prefab.nozzlePresMPa = a.nozzlePresMPa * b;
            prefab.nozzleTempK = a.nozzleTempK * b;
            prefab.nozzleMWgMol = a.nozzleMWgMol * b;
            prefab.nozzleGamma = a.nozzleGamma * b;
            prefab.nozzleMach = a.nozzleMach * b;
            prefab.chamberCp = a.chamberCp * b;
            prefab.nozzleCp = a.nozzleCp * b;
            prefab.frozenAreaRatio = a.frozenAreaRatio * b;

            return prefab;
        }
    }
}
