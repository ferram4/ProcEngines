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
    static class NozzleAeroUtils
    {
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
        public static double MachFromAreaRatio(double desiredAreaRatio, double gamma, double epsilon = 0.001, int maxIter = int.MaxValue)
        {
            double[] args = new double[] { gamma, desiredAreaRatio };
            return MathUtils.BrentsMethod(AreaRatioFromMachDiff, args, 1.1, 1000.0);
        }
    }
}
