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
    class PropellantProperties
    {
        public PartResourceDefinition resource;

        //Heat exchanger variables
        public double specHeatOfVaporization;       //in kJ/tonne
        public double refBoilingTempK;
        public double refBoilingPresMPa;

        //Pressure loss in line variables
        public double dynViscosity;

        //Pump efficiency variables
        public double vaporPresMPaAtStorageTemp;
        public double suctionSpecificSpeed;
        public double pressureRisePerStageMPa;

        public PropellantProperties(ConfigNode node, PartResourceDefinition resource)
        {
            this.resource = resource;

            if (node.HasValue("specHeatOfVaporization"))
            {
                specHeatOfVaporization = double.Parse(node.GetValue("specHeatOfVaporization"));
                refBoilingTempK = double.Parse(node.GetValue("refBoilingTempK"));
                refBoilingPresMPa = double.Parse(node.GetValue("refBoilingPresMPa"));
            }
            else
            {       //defaults
                specHeatOfVaporization = 0.246;     //approx for RP-1
                refBoilingTempK = 489.45;
                refBoilingPresMPa = 0.1013;
            }

            if (node.HasValue("dynViscosity"))
            {
                dynViscosity = double.Parse(node.GetValue("dynViscosity"));
            }
            else if(node.HasValue("kinViscosity"))
            {
                dynViscosity = double.Parse(node.GetValue("kinViscosity"));
                dynViscosity *= resource.density * 1000.0;
            }
            else
            {
                dynViscosity = 1.875e-6;
                dynViscosity *= 0.82;
            }

            if (node.HasValue("vaporPresMPaAtStorageTemp"))
                vaporPresMPaAtStorageTemp = double.Parse(node.GetValue("vaporPresMPaAtStorageTemp"));
            else
                vaporPresMPaAtStorageTemp = 0.00227527;

            if (node.HasValue("suctionSpecificSpeed"))
                suctionSpecificSpeed = double.Parse(node.GetValue("suctionSpecificSpeed"));
            else
                suctionSpecificSpeed = 70;

            if (node.HasValue("pressureRisePerStageMPa"))
                pressureRisePerStageMPa = double.Parse(node.GetValue("pressureRisePerStageMPa"));
            else
                pressureRisePerStageMPa = 47;
        }

        public static bool CheckConfigResourcesExist(ConfigNode propConfig, out PartResourceDefinition res)
        {
            bool valid = true;
            res = null;
            string resString;

            valid &= propConfig.HasValue("resource");

            if (!valid)
                return false;

            resString = propConfig.GetValue("resource");

            res = PartResourceLibrary.Instance.GetDefinition(resString);
            valid &= res != null;

            if (!valid)
                return false;

            return true;
        }
    }
}
