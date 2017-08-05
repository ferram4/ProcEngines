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
using ProcEngines.PropellantConfig;

namespace ProcEngines
{
    class StructuralMaterialLibrary
    {
        static StructuralMaterialLibrary instance;
        public static StructuralMaterialLibrary Instance
        {
            get
            {
                if (instance == null)
                    instance = new StructuralMaterialLibrary();

                return instance;
            }
        }
        List<StructuralMaterial> materials;

        StructuralMaterialLibrary()
        {
            materials = new List<StructuralMaterial>();
            LoadStructuralMaterials();
        }

        void LoadStructuralMaterials()
        {
            materials.Clear();

            ConfigNode[] structuralNodes = GameDatabase.Instance.GetConfigNodes("ProcEngStructuralMaterial");

            for (int i = 0; i < structuralNodes.Length; ++i)
            {
                ConfigNode node = structuralNodes[i];
                if (BiPropellantConfig.CheckConfigResourcesExist(node))
                    materials[i] = new StructuralMaterial(node);
            }
        }

        public static bool TryGetMaterial(out StructuralMaterial material, string name)
        {
            for(int i = 0; i < Instance.materials.Count; ++i)
            {
                StructuralMaterial materialTry = Instance.materials[i];
                if (materialTry.name == name)
                {
                    material = materialTry;
                    return true;
                }
            }

            material = null;
            return false;
        }

        public static StructuralMaterial GetMaterial(int index)
        {
            return Instance.materials[index];
        }
    
    }

    class StructuralMaterial
    {
        public string name;
        public double densitykg_m;
        public double ultimateStrengthMPa;
        public double tempLimitK;

        public StructuralMaterial(ConfigNode node)
        {
            name = node.GetValue("name");
            densitykg_m = double.Parse(node.GetValue("densitykg_m"));
            ultimateStrengthMPa = double.Parse(node.GetValue("ultimateStrengthMPa"));
            tempLimitK = double.Parse(node.GetValue("tempLimitK"));
        }

    }
}
