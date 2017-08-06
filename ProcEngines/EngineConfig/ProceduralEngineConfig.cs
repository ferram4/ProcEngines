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
    class ProceduralEngineConfig
    {
        string type;
        string configuration;
        float origMass;

        string configurationName;
        string description;
        float minThrust;
        float maxThrust;
        float heatProduction = 100.0f;
        float massMult = 1.0f;

        bool ullage = true;
        bool pressureFed = false;
        int ignitions = 1;

        struct IgnitionResource
        {
            public string name;
            public float amount;
        }
        List<IgnitionResource> ignitionReqs;

        struct PropellantResource
        {
            public string name;
            public float ratio;
            public bool DrawGauge;
        }

        float ispV;
        float ispSL;
    }
}
