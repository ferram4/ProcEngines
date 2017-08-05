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
using KSP;

namespace ProcEngines.PropellantConfig
{
    class PropellantMixtureLibrary
    {
        static PropellantMixtureLibrary instance;
        public static PropellantMixtureLibrary Instance
        {
            get
            {
                if (instance == null)
                    instance = new PropellantMixtureLibrary();
                return instance;
            }
        }
        List<BiPropellantConfig> biPropConfigs;

        public static List<BiPropellantConfig> BiPropConfigs
        {
            get { return Instance.biPropConfigs; }
        }

        //TODO: support monoprops, perhaps triprops

        PropellantMixtureLibrary()
        {
            LoadMixtures();
        }

        void LoadMixtures()
        {
            //Load BiProps
            ConfigNode[] biPropNodes = GameDatabase.Instance.GetConfigNodes("ProcEngBiPropMixture");

            biPropConfigs = new List<BiPropellantConfig>();
            for(int i = 0; i < biPropNodes.Length; ++i)
            {
                ConfigNode node = biPropNodes[i];
                if (BiPropellantConfig.CheckConfigResourcesExist(node))
                    biPropConfigs.Add(new BiPropellantConfig(node));
            }
        }

        public static BiPropellantConfig GetBiPropellantConfig(string mixtureTitle)
        {
            List<BiPropellantConfig> biPropConfigs = BiPropConfigs;

            for(int i = 0; i < biPropConfigs.Count; ++i)
            {
                BiPropellantConfig config = biPropConfigs[i];
                if (config.MixtureTitle == mixtureTitle)
                    return config;
            }
            Debug.LogError("[ProcEngines] Error; propellant mixture " + mixtureTitle + " could not be found");

            return null;
        }
    }
}
