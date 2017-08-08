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
    class NozzleCalculato
    {
        public NozzleShapeType shapeType;
        public double exitAngle;
        public double inflectionAngle;

        Vector2d E, Q, N;     //used in definition of bell nozzle approximation, normalized to throat radius = 1

        public void CalculateNozzleProperties(double relLength, double areaRatio, NozzleShapeType shape)
        {
            shapeType = shape;

            CalculateExitInflectionAngles(relLength, areaRatio);
            CalculateNozzleCurveParameters(relLength, areaRatio);
        }

        void CalculateExitInflectionAngles(double relLength,double areaRatio)
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
        }

        void CalculateNozzleCurveParameters(double relLength, double areaRatio)
        {
            double sqrtArea = Math.Sqrt(areaRatio);

            E.y = sqrtArea;
            E.x = relLength * (sqrtArea - 1) / NozzleUtils.GetConical15DegConstant();

            N.x = 0.382 * Math.Cos(inflectionAngle - Math.PI / 180.0);
            N.y = 0.382 * Math.Sin(inflectionAngle - Math.PI / 180.0) + 1.382;

            double slope1, slope2;

            slope1 = Math.Tan(inflectionAngle);
            slope2 = Math.Tan(exitAngle);

            double intercept1, intercept2;

            intercept1 = N.y - slope1 * N.x;
            intercept2 = E.y - slope2 * E.x;

            Q.x = (intercept2 - intercept1) / (slope1 - slope2);
            Q.y = (intercept2 * slope1 - intercept1 * slope2) / (slope1 - slope2);
        }

        public double GetDivergenceLoss()
        {
            return (1 + Math.Cos(exitAngle)) * 0.5;
        }
    }

    enum NozzleShapeType
    {
        CONICAL,
        BELL
    }
}
