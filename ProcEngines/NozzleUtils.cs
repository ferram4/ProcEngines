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
    class NozzleUtils
    {
        static NozzleUtils instance;
        public static NozzleUtils Instance
        {
            get
            {
                if (instance == null)
                    instance = new NozzleUtils();
                return instance;
            }
        }

        double minRelLength = -1;
        double[] bellNozzleRelLengths;
        double[,] bellNozzleExitAngles;
        double[,] bellNozzleInflectionAngles;

        double conical15DegConstant;

        NozzleUtils()
        {
            LoadBellNozzleData();
            conical15DegConstant = Math.Tan(Math.PI * 15.0 / 180.0);
        }

        public static double GetMinRelLength()
        {
            return Instance.minRelLength;
        }

        public static Vector2d GetBellExitInflectionAngles(double nozzleRelLength, double areaRatio)
        {
            double relLengthIndexFactor = 0;
            int relLengthIndex = 0;

            double relLength1 = Instance.bellNozzleRelLengths[0];
            for (int i = 1; i < Instance.bellNozzleRelLengths.Length; ++i)
            {
                double relLength2 = Instance.bellNozzleRelLengths[i];
                if (relLength2 < nozzleRelLength)
                {
                    relLength1 = relLength2;
                    continue;
                }

                relLengthIndexFactor = nozzleRelLength - relLength1;
                relLengthIndexFactor /= (relLength2 - relLength1);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

                relLengthIndex = i;     //remember, off-by-one offset for 2D arrays

                double areaRatio1 = Instance.bellNozzleInflectionAngles[0, 0];
                for (int j = 1; j < Instance.bellNozzleInflectionAngles.GetLength(0); ++j)
                {
                    double areaRatio2 = Instance.bellNozzleInflectionAngles[j, 0];

                    if (areaRatio2 < areaRatio)
                    {
                        areaRatio1 = areaRatio2;
                        continue;
                    }

                    double areaRatioIndexFactor = areaRatio - areaRatio1;
                    areaRatioIndexFactor /= (areaRatio2 - areaRatio1);        //this gives us a pseudo-index factor that can be used to calculate properties between the input data

                    int areaRatioIndex = j - 1;

                    Vector2d exitInflectionAngle = new Vector2d();

                    //Get and interp exit angles
                    double exitAngle11, exitAngle21, exitAngle12, exitAngle22;

                    exitAngle11 = Instance.bellNozzleExitAngles[areaRatioIndex, relLengthIndex];
                    exitAngle12 = Instance.bellNozzleExitAngles[areaRatioIndex, relLengthIndex + 1];
                    exitAngle21 = Instance.bellNozzleExitAngles[areaRatioIndex + 1, relLengthIndex];
                    exitAngle22 = Instance.bellNozzleExitAngles[areaRatioIndex + 1, relLengthIndex + 1];

                    double exitAngle1Interp, exitAngle2Interp;

                    exitAngle1Interp = (exitAngle12 - exitAngle11) * relLengthIndexFactor + exitAngle11;
                    exitAngle2Interp = (exitAngle22 - exitAngle21) * relLengthIndexFactor + exitAngle21;

                    exitInflectionAngle.x = (exitAngle2Interp - exitAngle1Interp) * areaRatioIndexFactor + exitAngle1Interp;

                    //Get and interp inflection angles
                    double inflectionAngle11, inflectionAngle21, inflectionAngle12, inflectionAngle22;

                    inflectionAngle11 = Instance.bellNozzleInflectionAngles[areaRatioIndex, relLengthIndex];
                    inflectionAngle12 = Instance.bellNozzleInflectionAngles[areaRatioIndex, relLengthIndex + 1];
                    inflectionAngle21 = Instance.bellNozzleInflectionAngles[areaRatioIndex + 1, relLengthIndex];
                    inflectionAngle22 = Instance.bellNozzleInflectionAngles[areaRatioIndex + 1, relLengthIndex + 1];

                    double inflectionAngle1Interp, inflectionAngle2Interp;

                    inflectionAngle1Interp = (inflectionAngle12 - inflectionAngle11) * relLengthIndexFactor + inflectionAngle11;
                    inflectionAngle2Interp = (inflectionAngle22 - inflectionAngle21) * relLengthIndexFactor + inflectionAngle21;

                    exitInflectionAngle.y = (inflectionAngle2Interp - inflectionAngle1Interp) * areaRatioIndexFactor + inflectionAngle1Interp;

                    return exitInflectionAngle;
                }
            }
            Debug.LogError("[ProcEngines] Error in data tables, could not solve for nozzle angles");

            return new Vector2d(0, 0.48 * Math.PI);
        }

        public static double GetConicalExitAngle(double nozzleRelLength)
        {
            return Math.Atan(Instance.conical15DegConstant / nozzleRelLength);
        }

        public static double GetConical15DegConstant()
        {
            return Instance.conical15DegConstant;
        }

        public static double AreaRatioFromMach(double mach, double gamma)
        {
            double result = gamma - 1.0;
            result *= 0.5 * mach * mach;
            result++;
            result *= 2.0;
            result /= gamma + 1.0;

            result = Math.Pow(result, 0.5 * (gamma + 1.0) / (gamma - 1.0));
            result /= mach;

            return result;
        }

        public static double AreaRatioFromMachDiff(double mach, double[] gamma_DesiredAreaRatio)
        {
            return AreaRatioFromMach(mach, gamma_DesiredAreaRatio[0]) - gamma_DesiredAreaRatio[1];
        }
        //solves the mach-area relation for mach number using Brent's Method
        public static double MachFromAreaRatio(double desiredAreaRatio, double gamma, double epsilon = 0.0001, int maxIter = int.MaxValue)
        {
            double[] args = new double[] { gamma, desiredAreaRatio };
            return MathUtils.BrentsMethod(AreaRatioFromMachDiff, args, 1.000000000000000, 500.0, epsilon, maxIter);
        }
        public static double MachFromAreaRatioSubsonic(double desiredAreaRatio, double gamma, double epsilon = 0.0001, int maxIter = int.MaxValue)
        {
            double[] args = new double[] { gamma, desiredAreaRatio };
            return MathUtils.BrentsMethod(AreaRatioFromMachDiff, args, 0.001, 1.0, epsilon, maxIter);
        }

        void LoadBellNozzleData()
        {
            ConfigNode bellNozzleParams = GameDatabase.Instance.GetConfigNodes("ProcEnginesBellNozzleParams")[0];

            string nozzleRelLengthsString = bellNozzleParams.GetValue("nozzleRelLengths");
            string[] nozzleRelLengthsSplitString = nozzleRelLengthsString.Split(new char[] { ',', ' ', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

            bellNozzleRelLengths = new double[nozzleRelLengthsSplitString.Length];
            for(int i = 0; i < nozzleRelLengthsSplitString.Length; ++i)
            {
                bellNozzleRelLengths[i] = double.Parse(nozzleRelLengthsSplitString[i]);
            }

            ConfigNode exitAngleDegree = bellNozzleParams.GetNode("ExitAngleDegree");

            string[] exitAngleStrings = exitAngleDegree.GetValues("key");
            bellNozzleExitAngles = new double[exitAngleStrings.Length, bellNozzleRelLengths.Length + 1];

            for (int i = 0; i < exitAngleStrings.Length; ++i)
            {
                string[] exitAngleSplitString = exitAngleStrings[i].Split(new char[] { ',', ' ', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < exitAngleSplitString.Length; ++j)
                {
                    double val = double.Parse(exitAngleSplitString[j]);
                    if (j > 0)
                        val *= Math.PI / 180.0;
                    bellNozzleExitAngles[i, j] = val;
                }
            }

            ConfigNode inflectionAngleDegree = bellNozzleParams.GetNode("InflectionAngleDegree");

            string[] inflectionAngleStrings = inflectionAngleDegree.GetValues("key");
            bellNozzleInflectionAngles = new double[inflectionAngleStrings.Length, bellNozzleRelLengths.Length + 1];

            for (int i = 0; i < inflectionAngleStrings.Length; ++i)
            {
                string[] inflectionAngleSplitString = inflectionAngleStrings[i].Split(new char[] { ',', ' ', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j < inflectionAngleSplitString.Length; ++j)
                {
                    double val = double.Parse(inflectionAngleSplitString[j]);
                    if (j > 0)
                        val *= Math.PI / 180.0;
                    bellNozzleInflectionAngles[i, j] = val;
                }
            }

            minRelLength = bellNozzleRelLengths[0];
        }
    }
}
