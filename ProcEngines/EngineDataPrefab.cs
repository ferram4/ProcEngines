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
        public double frozenAreaRatio;

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
            prefab.frozenAreaRatio = a.frozenAreaRatio * b;

            return prefab;
        }
    }
}
