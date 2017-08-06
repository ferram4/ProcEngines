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
using ProcEngines.EngineConfig;
using ProcEngines.PropellantConfig;

namespace ProcEngines.EngineGUI
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class EngineGUI : MonoBehaviour
    {
        static EngineGUI instance;
        public static EngineGUI Instance
        {
            get
            {
                return instance;
            }
        }

        static GUISkin skin;
        bool uiActive = false;
        Rect window = new Rect(200, 200, 600, 600);

        ProceduralEngineModule currentEngineModule;
        EngineCalculatorBase engineCalcBase;

        GUIDropDown<PowerCycleEnum> powerCycleDropdown;
        GUIDropDown<BiPropellantConfig> biPropellantConfigs;

        void Awake()
        {
            instance = this;
            Debug.Log("[ProcEngines] Editor GUI Started");
            CreatePowerCycleDropdown();
            CreateBipropDropdown();
        }

        void CreatePowerCycleDropdown()
        {
            PowerCycleEnum[] cycleEnum = new PowerCycleEnum[] {
                PowerCycleEnum.PRESSURE_FED, PowerCycleEnum.GAS_GENERATOR,
                PowerCycleEnum.COMBUSTION_TAPOFF, PowerCycleEnum.STAGED_COMBUSTION,
                PowerCycleEnum.CLOSED_EXPANDER, PowerCycleEnum.BLEED_EXPANDER};

            string[] cycleString = new string[] {
                "Pressure Fed", "Gas Generator",
                "Combustion Tapoff", "Staged Combustion",
                "Closed Expander", "Bleed Expander"};

            powerCycleDropdown = new GUIDropDown<PowerCycleEnum>(cycleString, cycleEnum, 1);
        }

        void CreateBipropDropdown()
        {
            BiPropellantConfig[] propConfigs = PropellantMixtureLibrary.BiPropConfigs.ToArray();

            string[] propString = new string[propConfigs.Length];

            for (int i = 0; i < propString.Length; ++i)
                propString[i] = propConfigs[i].MixtureTitle;

            biPropellantConfigs = new GUIDropDown<BiPropellantConfig>(propString, propConfigs, 0);
        }
        
        public void SetEngineModule(ProceduralEngineModule module)
        {
            uiActive = true;
            currentEngineModule = module;
            engineCalcBase = module.procEngineConfig;
            if (engineCalcBase == null)
            {
                BiPropellantConfig biprop = biPropellantConfigs.ActiveSelection;
                engineCalcBase = new EngineCalculatorGasGen(biprop, (biprop.ChamberOFLimitLean + biprop.ChamberOFLimitRich) * 0.5, 4, 5, 0.1);
            }
            else
            {
                biPropellantConfigs.SetOption(engineCalcBase.biPropConfig.MixtureTitle);
                powerCycleDropdown.SetOption(engineCalcBase.EngineCalculatorType());
            }
        }

        void OnGUI()
        {
            if (!uiActive)
                return;
            if (skin == null)
                GenerateSkin();

            window = GUI.Window(this.GetHashCode(), window, GUIWindow, "Proc Engines Selector");
        }

        void GUIWindow(int id)
        {
            GeneralEngineParameters();
            GUI.DragWindow();
        }


        void GeneralEngineParameters()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(299));

            double chamberPresMPa = engineCalcBase.chamberPresMPa;
            double oFRatio = engineCalcBase.oFRatio;
            double throatDiam = engineCalcBase.throatDiameter;
            double areaRatio = engineCalcBase.areaRatio;

            //Power Cycle Dropdown
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power Cycle:", GUILayout.Width(125));
            powerCycleDropdown.GUIDropDownDisplay();
            GUILayout.EndHorizontal();

            //Mixture Dropdown
            GUILayout.BeginHorizontal();
            GUILayout.Label("Propellants:", GUILayout.Width(125));
            biPropellantConfigs.GUIDropDownDisplay();
            GUILayout.EndHorizontal();

            //O/F Ratio
            GUILayout.BeginHorizontal();
            oFRatio = GUIUtils.TextEntryForDoubleWithButtons("O/F Ratio:", 125, oFRatio, 0.01, 0.1, 75);
            GUILayout.EndHorizontal();
            
            //Chamber Pressure
            GUILayout.BeginHorizontal();
            chamberPresMPa = GUIUtils.TextEntryForDoubleWithButtons("Chamber Pres, MPa:", 125, chamberPresMPa, 0.1, 0.5, 75);
            GUILayout.EndHorizontal();

            //Throat Diameter
            GUILayout.BeginHorizontal();
            throatDiam = GUIUtils.TextEntryForDoubleWithButtons("Throat Diam, m:", 125, throatDiam, 0.01, 0.1,75);
            GUILayout.EndHorizontal();

            //Area Ratio
            GUILayout.BeginHorizontal();
            areaRatio = GUIUtils.TextEntryForDoubleWithButtons("Expansion Ratio:", 125, areaRatio, 1, 0.1,75);
            GUILayout.EndHorizontal();

            engineCalcBase.SetEngineProperties(biPropellantConfigs.ActiveSelection, oFRatio, chamberPresMPa, areaRatio, throatDiam);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(299));

            //Vac Thrust
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vacuum Thrust: ", GUILayout.Width(125));
            GUILayout.Label(engineCalcBase.thrustVac.ToString("F3"));
            GUILayout.Label("kN");
            GUILayout.EndHorizontal();

            //SL Thrust
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sea Lvl Thrust: ", GUILayout.Width(125));
            GUILayout.Label(engineCalcBase.thrustSL.ToString("F3"));
            GUILayout.Label("kN");
            GUILayout.EndHorizontal();

            //Vac Isp
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vacuum Isp: ", GUILayout.Width(125));
            GUILayout.Label(engineCalcBase.specImpulseVac.ToString("F3"));
            GUILayout.Label("s");
            GUILayout.EndHorizontal();

            //SL Isp
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sea Lvl Isp: ", GUILayout.Width(125));
            GUILayout.Label(engineCalcBase.specImpulseSL.ToString("F3"));
            GUILayout.Label("s");
            GUILayout.EndHorizontal();

            //Exit Pressure
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nozzle Exit Pres: ", GUILayout.Width(125));
            GUILayout.Label(engineCalcBase.exitPressureMPa.ToString("F3"));
            GUILayout.Label("MPa");
            GUILayout.EndHorizontal(); 

            //Exit Diameter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nozzle Exit Diam: ", GUILayout.Width(125));
            GUILayout.Label(engineCalcBase.nozzleDiameter.ToString("F3"));
            GUILayout.Label("m");
            GUILayout.EndHorizontal();

            //Total Mass Flow
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Mass Flow: ", GUILayout.Width(125));
            GUILayout.Label(engineCalcBase.massFlowTotal.ToString("F3"));
            GUILayout.Label("t/s");
            GUILayout.EndHorizontal(); 
            
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void GenerateSkin()
        {
            GUI.skin = null;
            skin = UnityEngine.Object.Instantiate(GUI.skin);

            skin.label.margin = new RectOffset(1, 1, 1, 1);
            skin.label.padding = new RectOffset(0, 0, 2, 2);

            skin.button.margin = new RectOffset(1, 1, 1, 1);
            skin.button.padding = new RectOffset(4, 4, 2, 2);

            skin.toggle.margin = new RectOffset(1, 1, 1, 1);
            skin.toggle.padding = new RectOffset(15, 0, 2, 0);

            skin.textField.margin = new RectOffset(1, 1, 1, 1);
            skin.textField.padding = new RectOffset(2, 2, 2, 2);

            skin.textArea.margin = new RectOffset(1, 1, 1, 1);
            skin.textArea.padding = new RectOffset(2, 2, 2, 2);

            skin.window.margin = new RectOffset(0, 0, 0, 0);
            skin.window.padding = new RectOffset(5, 5, 20, 5);
        }

        void OnDestroy()
        {
            instance = null;
            Debug.Log("[ProcEngines] Editor GUI Destroyed");
        }
    }
}
