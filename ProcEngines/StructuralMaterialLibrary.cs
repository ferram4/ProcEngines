using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
