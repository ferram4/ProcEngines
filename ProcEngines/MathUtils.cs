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
    static class MathUtils
    {
        public static double BrentsMethod(Func<double,double[], double> func, double[] args, double a, double b, double epsilon = 0.001, int maxIter = int.MaxValue)
        {
            double delta = epsilon * 100;
            double fa, fb;
            fa = func(a, args);
            fb = func(b, args);

            if (fa * fb >= 0)
                return 0;

            if (Math.Abs(fa) < Math.Abs(fb))
            {
                double tmp = fa;
                fa = fb;
                fb = tmp;

                tmp = a;
                a = b;
                b = tmp;
            }

            double c = a, d = a, fc = func(c, args);

            double s = b, fs = fb;

            bool flag = true;
            int iter = 0;
            while (fs != 0 && Math.Abs(a - b) > epsilon && iter < maxIter)
            {
                if ((fa - fc) > double.Epsilon && (fb - fc) > double.Epsilon)    //inverse quadratic interpolation
                {
                    s = a * fc * fb / ((fa - fb) * (fa - fc));
                    s += b * fc * fa / ((fb - fa) * (fb - fc));
                    s += c * fc * fb / ((fc - fa) * (fc - fb));
                }
                else
                {
                    s = (b - a) / (fb - fa);    //secant method
                    s *= fb;
                    s = b - s;
                }

                double b_s = Math.Abs(b - s), b_c = Math.Abs(b - c), c_d = Math.Abs(c - d);

                //Conditions for bisection method
                bool condition1;
                double a3pb_over4 = (3.0 * a + b) * 0.25;

                if (a3pb_over4 > b)
                    if (s < a3pb_over4 && s > b)
                        condition1 = false;
                    else
                        condition1 = true;
                else
                    if (s > a3pb_over4 && s < b)
                        condition1 = false;
                    else
                        condition1 = true;

                bool condition2;

                if (flag && b_s >= b_c * 0.5)
                    condition2 = true;
                else
                    condition2 = false;

                bool condition3;

                if (!flag && b_s >= c_d * 0.5)
                    condition3 = true;
                else
                    condition3 = false;

                bool condition4;

                if (flag && b_c <= delta)
                    condition4 = true;
                else
                    condition4 = false;

                bool conditon5;

                if (!flag && c_d <= delta)
                    conditon5 = true;
                else
                    conditon5 = false;

                if (condition1 || condition2 || condition3 || condition4 || conditon5)
                {
                    s = a + b;
                    s *= 0.5;
                    flag = true;
                }
                else
                    flag = false;

                fs = func(s, args);
                d = c;
                c = b;

                if (fa * fs < 0)
                {
                    b = s;
                    fb = fs;
                }
                else
                {
                    a = s;
                    fa = fs;
                }

                if (Math.Abs(fa) < Math.Abs(fb))
                {
                    double tmp = fa;
                    fa = fb;
                    fb = tmp;

                    tmp = a;
                    a = b;
                    b = tmp;
                }
                iter++;
            }
            return s;
        }

        public static double BrentsMethod(Func<double, double> func, double a, double b, double epsilon = 0.001, int maxIter = int.MaxValue)
        {
            double delta = epsilon * 100;
            double fa, fb;
            fa = func(a);
            fb = func(b);

            if (fa * fb >= 0)
                return 0;

            if (Math.Abs(fa) < Math.Abs(fb))
            {
                double tmp = fa;
                fa = fb;
                fb = tmp;

                tmp = a;
                a = b;
                b = tmp;
            }

            double c = a, d = a, fc = func(c);

            double s = b, fs = fb;

            bool flag = true;
            int iter = 0;
            while (fs != 0 && Math.Abs(a - b) > epsilon && iter < maxIter)
            {
                if ((fa - fc) > double.Epsilon && (fb - fc) > double.Epsilon)    //inverse quadratic interpolation
                {
                    s = a * fc * fb / ((fa - fb) * (fa - fc));
                    s += b * fc * fa / ((fb - fa) * (fb - fc));
                    s += c * fc * fb / ((fc - fa) * (fc - fb));
                }
                else
                {
                    s = (b - a) / (fb - fa);    //secant method
                    s *= fb;
                    s = b - s;
                }

                double b_s = Math.Abs(b - s), b_c = Math.Abs(b - c), c_d = Math.Abs(c - d);

                //Conditions for bisection method
                bool condition1;
                double a3pb_over4 = (3.0 * a + b) * 0.25;

                if (a3pb_over4 > b)
                    if (s < a3pb_over4 && s > b)
                        condition1 = false;
                    else
                        condition1 = true;
                else
                    if (s > a3pb_over4 && s < b)
                        condition1 = false;
                    else
                        condition1 = true;

                bool condition2;

                if (flag && b_s >= b_c * 0.5)
                    condition2 = true;
                else
                    condition2 = false;

                bool condition3;

                if (!flag && b_s >= c_d * 0.5)
                    condition3 = true;
                else
                    condition3 = false;

                bool condition4;

                if (flag && b_c <= delta)
                    condition4 = true;
                else
                    condition4 = false;

                bool conditon5;

                if (!flag && c_d <= delta)
                    conditon5 = true;
                else
                    conditon5 = false;

                if (condition1 || condition2 || condition3 || condition4 || conditon5)
                {
                    s = a + b;
                    s *= 0.5;
                    flag = true;
                }
                else
                    flag = false;

                fs = func(s);
                d = c;
                c = b;

                if (fa * fs < 0)
                {
                    b = s;
                    fb = fs;
                }
                else
                {
                    a = s;
                    fa = fs;
                }

                if (Math.Abs(fa) < Math.Abs(fb))
                {
                    double tmp = fa;
                    fa = fb;
                    fb = tmp;

                    tmp = a;
                    a = b;
                    b = tmp;
                }
                iter++;
            }
            return s;
        }
    }
}
