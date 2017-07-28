using System;
using System.Collections.Generic;
using UnityEngine;
using KSP;

namespace ProcEngines
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class PropellantMixtureLibrary : MonoBehaviour
    {
        List<BiPropellantConfig> biPropConfigs;
        //TODO: support monoprops, perhaps triprops

        void Awake()
        {
            DontDestroyOnLoad(this);
            this.enabled = false;
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
                if(BiPropellantConfig.CheckConfigResourcesExist(node))
                    biPropConfigs[i] = new BiPropellantConfig(node);
            }
        }
    }
}
