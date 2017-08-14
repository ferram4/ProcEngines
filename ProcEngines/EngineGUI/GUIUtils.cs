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
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProcEngines.EngineGUI
{
    static class GUIUtils
    {
        public static Rect ClampToScreen(Rect window)
        {
            window.x = Mathf.Clamp(window.x, -window.width + 20, Screen.width - 20);
            window.y = Mathf.Clamp(window.y, -window.height + 20, Screen.height - 20);

            return window;
        }

        public static double TextEntryForDoubleWithButtons(string label, int labelWidth, double prevValue, double smallInc, double bigInc, int fieldWidth, string format = "F3")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));

            if (GUILayout.Button("◂◂"))
                prevValue -= bigInc;
            if (GUILayout.Button("◂"))
                prevValue -= smallInc;

            string valString = prevValue.ToString(format);

            valString = GUILayout.TextField(valString, GUILayout.Width(fieldWidth));

            if (Regex.IsMatch(valString, @"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?$"))
                prevValue = double.Parse(valString);

            if (GUILayout.Button("▸"))
                prevValue += smallInc;
            if (GUILayout.Button("▸▸"))
                prevValue += bigInc;

            GUILayout.EndHorizontal();
            return prevValue;
        }

        public static int TextEntryForIntWithButton(string label, int labelWidth, int prevValue, int fieldWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));

            if (GUILayout.Button("◂"))
                prevValue -= 1;

            string valString = prevValue.ToString("F0");

            valString = GUILayout.TextField(valString, GUILayout.Width(fieldWidth));

            if (Regex.IsMatch(valString, @"^[-+]?[0-9]*"))
                prevValue = int.Parse(valString);

            if (GUILayout.Button("▸"))
                prevValue += 1;

            GUILayout.EndHorizontal();
            return prevValue;
        }
        
        public static double TextEntryForDouble(string label, int labelWidth, double prevValue)
        {
            string valString = prevValue.ToString("F5");
            TextEntryField(label, labelWidth, ref valString);

            if (!Regex.IsMatch(valString, @"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?$"))
                return prevValue;

            return double.Parse(valString);
        }

        public static int TextEntryForInt(string label, int labelWidth, int prevValue)
        {
            string valString = prevValue.ToString();
            TextEntryField(label, labelWidth, ref valString);

            if (!Regex.IsMatch(valString, @"^[-+]?[0-9]*"))
                return prevValue;

            return int.Parse(valString);
        }

        public static void TextEntryField(string label, int labelWidth, ref string inputOutput)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            inputOutput = GUILayout.TextField(inputOutput);
            GUILayout.EndHorizontal();
        }

        public static Vector3 GetMousePos()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;
            return mousePos;
        }
    }
}
