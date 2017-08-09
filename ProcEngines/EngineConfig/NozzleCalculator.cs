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

namespace ProcEngines.EngineConfig
{
    class NozzleCalculator
    {
        const double INCOMPRESSIBLE_CF = 0.003;
        const double G0 = 9.80665;
        const int POINTS_CONVERGING_ARC = 30;
        const int POINTS_DIVERGING_ARC = 40;

        public NozzleShapeType shapeType;
        public double exitAngle;
        public string exitAngleString;
        public double inflectionAngle;
        public double relLength;
        public double areaRatio;

        Vector2d E, Q, N;     //used in definition of bell nozzle approximation, normalized to throat radius = 1

        Vector2d[] nozzlePoints;


        public NozzleCalculator(double relLength, double areaRatio, NozzleShapeType shape)
        {
            UpdateNozzleStatus(relLength, areaRatio, shape);
        }

        public void UpdateNozzleStatus(double relLength, double areaRatio, NozzleShapeType shape)
        {
            bool unchanged = true;

            if (relLength <= 0.6)
                relLength = 0.6;
            if (relLength >= 1.0)
                relLength = 1.0;


            unchanged &= shape == shapeType;
            unchanged &= this.relLength == relLength;
            unchanged &= this.areaRatio == areaRatio;

            shapeType = shape;
            this.relLength = relLength;
            this.areaRatio = areaRatio;

            if (!unchanged)
            {
                CalculateNozzleProperties();
                //DebugPrintNozzlePoints();
            }
        }

        public void UpdateNozzleStatus(double areaRatio)
        {
            bool unchanged = true;

            unchanged &= this.areaRatio == areaRatio;

            this.areaRatio = areaRatio;

            if (!unchanged)
            {
                CalculateNozzleProperties();
                //DebugPrintNozzlePoints();
            }
        }
        
        public void CalculateNozzleProperties()
        {
            CalculateExitInflectionAngles();
            CalculateNozzleCurveParameters();
            GenerateNozzleCurve();
        }

        void CalculateExitInflectionAngles()
        {
            switch (shapeType)
            {
                case NozzleShapeType.CONICAL:
                    exitAngle = NozzleUtils.GetConicalExitAngle(relLength);
                    inflectionAngle = exitAngle;
                    break;

                case NozzleShapeType.BELL:
                    Vector2d angles = NozzleUtils.GetBellExitInflectionAngles(relLength, areaRatio);
                    exitAngle = angles.x;
                    inflectionAngle = angles.y;
                    break;
            }

            exitAngleString = (exitAngle * 180.0 / Math.PI).ToString("F1");
        }

        void CalculateNozzleCurveParameters()
        {
            double sqrtArea = Math.Sqrt(areaRatio);

            E.y = sqrtArea;
            E.x = relLength * (sqrtArea - 1) / NozzleUtils.GetConical15DegConstant();

            N.x = 0.382 * Math.Cos(inflectionAngle - Math.PI / 180.0);
            N.y = 0.382 * Math.Sin(inflectionAngle - Math.PI / 180.0) + 1.382;

            if(shapeType == NozzleShapeType.CONICAL)
            {
                Q = 0.5 * (N + E);
                return;
            }
            double slope1, slope2;

            slope1 = Math.Tan(inflectionAngle);
            slope2 = Math.Tan(exitAngle);

            double intercept1, intercept2;

            intercept1 = N.y - slope1 * N.x;
            intercept2 = E.y - slope2 * E.x;

            Q.x = (intercept2 - intercept1) / (slope1 - slope2);
            Q.y = (intercept2 * slope1 - intercept1 * slope1) / (slope1 - slope2) + intercept1;
        }

        void GenerateNozzleCurve()
        {
            double distanceParaCurve = E.x - N.x;
            double distanceCircularCurves = NozzleThroatXYPoints(1).x - NozzleThroatXYPoints(-1).x;

            int countParaCurve = (int)((POINTS_DIVERGING_ARC + POINTS_CONVERGING_ARC) * distanceParaCurve / distanceCircularCurves) + 1;
            nozzlePoints = new Vector2d[(POINTS_DIVERGING_ARC + POINTS_CONVERGING_ARC) + countParaCurve];


            for (int i = 0; i < POINTS_CONVERGING_ARC; ++i)
            {
                double t = ((double)i) / (POINTS_CONVERGING_ARC) - 1.0;
                nozzlePoints[i] = NozzleThroatXYPoints(t);
            }
            for (int i = POINTS_CONVERGING_ARC; i < POINTS_CONVERGING_ARC + POINTS_DIVERGING_ARC; ++i)
            {
                double t = ((double)i - POINTS_CONVERGING_ARC) / (POINTS_DIVERGING_ARC);
                nozzlePoints[i] = NozzleThroatXYPoints(t);
            }
            for (int i = POINTS_CONVERGING_ARC + POINTS_DIVERGING_ARC; i < nozzlePoints.Length; ++i)
            {
                double t = ((double)(i - POINTS_CONVERGING_ARC + POINTS_DIVERGING_ARC)) / (nozzlePoints.Length - POINTS_CONVERGING_ARC + POINTS_DIVERGING_ARC - 1.0);
                nozzlePoints[i] = NozzleCurveXYPoints(t);
            }

        }

        void DebugPrintNozzlePoints()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.AppendLine(exitAngle.ToString("F3"));
            builder.AppendLine(inflectionAngle.ToString("F3"));
            builder.AppendLine();

            builder.Append(N.x.ToString("F3"));
            builder.Append(",");
            builder.AppendLine(N.y.ToString("F3"));

            builder.Append(Q.x.ToString("F3"));
            builder.Append(",");
            builder.AppendLine(Q.y.ToString("F3"));
            
            builder.Append(E.x.ToString("F3"));
            builder.Append(",");
            builder.AppendLine(E.y.ToString("F3"));

            builder.AppendLine();

            for(int i = 0; i < nozzlePoints.Length; ++i)
            {
                builder.Append(nozzlePoints[i].x.ToString("F3"));
                builder.Append(",");
                builder.AppendLine(nozzlePoints[i].y.ToString("F3"));
            }

            Debug.Log(builder.ToString());
        }

        //takes t = 0 at inflection point to t = 1 to exit point
        Vector2d NozzleCurveXYPoints(double t)
        {
            return (1 - t) * (1 - t) * N
                + 2 * (1 - t) * t * Q
                + t * t * E;
        }

        Vector2d NozzleThroatXYPoints(double t)
        {
            if(t < 0)
            {
                double theta = -90 - 45 * Math.Abs(t);
                theta *= Math.PI / 180.0;

                Vector2d point = new Vector2d();

                point.x = 1.5 * Math.Cos(theta);
                point.y = 1.5 * Math.Sin(theta) + 2.5;

                return point;
            }
            else
            {
                double theta = -Math.PI * 0.5 + inflectionAngle * t;

                Vector2d point = new Vector2d();

                point.x = 0.382 * Math.Cos(theta);
                point.y = 0.382 * Math.Sin(theta) + 1.382;

                return point;
            }
        }

        public double GetFrictionEff(double exhaustVelOpt, double massFlow, double chamberPresMPa, double gamma, double throatRadius)
        {
            double delV = CalcExhaustVelChangeFromFriction(massFlow, chamberPresMPa, gamma, throatRadius);

            double eff = exhaustVelOpt - delV;
            eff /= exhaustVelOpt;

            return eff;
        }

        public double CalcExhaustVelChangeFromFriction(double massFlow, double chamberPresMPa, double gamma, double throatRadius)
        {
            double delV = 0;

            double gammaDrag = 1 * gamma;

            double gammaFactor = gammaDrag - 1.0;
            gammaFactor *= 0.5;

            double gammaExp = -gammaDrag / (gammaDrag - 1.0);

            Vector2d point1 = nozzlePoints[POINTS_CONVERGING_ARC];
            double areaRat1 = point1.y * point1.y;
            double mach1 = 1.0;// NozzleUtils.MachFromAreaRatioSubsonic(areaRat1, gamma);    //should be Mach 1 here
            double drag1 = mach1 * mach1 * gammaFactor + 1.0;
            drag1 = Math.Pow(drag1, gammaExp);
            drag1 *= mach1 * mach1 * CalcCompressFriction(mach1, gammaDrag);
            drag1 *= point1.y;

            for (int i = POINTS_CONVERGING_ARC + 1; i < nozzlePoints.Length; ++i)
            {
                Vector2d point2 = nozzlePoints[i];
                double areaRat2 = point2.y * point2.y;
                double mach2;

                if (i > POINTS_CONVERGING_ARC)
                    mach2 = NozzleUtils.MachFromAreaRatio(areaRat2, gamma);
                else if (i < POINTS_CONVERGING_ARC)
                    mach2 = NozzleUtils.MachFromAreaRatioSubsonic(areaRat2, gamma);
                else
                    mach2 = 1.0;

                double drag2 = mach2 * mach2 * gammaFactor + 1.0;
                drag2 = Math.Pow(drag2, gammaExp);
                drag2 *= mach2 * mach2 * CalcCompressFriction(mach2, gammaDrag);
                drag2 *= point2.y;

                double distanceX = point2.x - point1.x;
                double distanceY = point2.y - point1.y;
                double distance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

                delV += (drag2 + drag1) * 0.5 * distance;       //trapezoid method

                point1 = point2;
                drag1 = drag2;
            }

            delV *= chamberPresMPa;
            delV *= gammaDrag;
            delV /= massFlow;
            delV *= 1000.0;

            delV *= throatRadius * Math.PI;

            return delV;
        }

        public double CalcCompressFriction(double mach, double gamma)
        {
            double result = gamma - 1.0;
            result *= 0.5 * 0.72;
            result *= mach * mach;
            result++;

            result = Math.Pow(result, -0.578);

            result *= INCOMPRESSIBLE_CF;

            return result;
        }

        public double GetDivergenceEff()
        {
            return (1 + Math.Cos(exitAngle)) * 0.5;
        }

        #region GUI
        int nozzleTypeIndex = 1;
        static string[] nozzleTypeString = new string[] { "Conical Nozzle", "Bell Nozzle" };

        public int NozzleTypeSelect()
        {
            nozzleTypeIndex = GUILayout.SelectionGrid(nozzleTypeIndex, nozzleTypeString, 2);
            return nozzleTypeIndex;
        }

        #endregion
    }

    enum NozzleShapeType
    {
        CONICAL,
        BELL
    }
}
