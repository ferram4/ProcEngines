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
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProcEngines.EngineGUI
{
    public class GUIDropDown<T>
    {
        private int selectedOption;
        private bool isActive = false;
        private bool toggleBtnState = false;
        private Vector2 scrollPos;

        private string[] stringOptions;
        private T[] typeOptions;

        public T ActiveSelection
        {
            get { return typeOptions[selectedOption]; }
        }

        private static GUIStyle listStyle;
        private static GUIStyle toggleBtnStyle;
        private static GUIStyle dropdownItemStyle;
        private static GUIStyle selectedItemStyle;

        public GUIDropDown(string[] stringOptions, T[] typeOptions) : this(stringOptions, typeOptions, 0) { }

        public GUIDropDown(string[] stringOptions, T[] typeOptions, int defaultOption)
        {
            this.stringOptions = stringOptions;
            this.typeOptions = typeOptions;

            selectedOption = defaultOption;
        }

        public void SetOption(string title)
        {
            for(int i = 0; i < typeOptions.Length; ++i)
            {
                if (stringOptions[i] == title)
                {
                    selectedOption = i;
                    return;
                }
            }
        }

        public void GUIDropDownDisplay(params GUILayoutOption[] guiOptions)
        {
            InitStyles();

            GUIDropDownWindow display = GUIDropDownWindow.Instance;
            toggleBtnState = GUILayout.Toggle(toggleBtnState, "▼ " + stringOptions[selectedOption] + " ▼", toggleBtnStyle, guiOptions);

            // Calcuate absolute regions for the button and dropdown list, this only works when
            // Event.current.type == EventType.Repaint
            Vector2 relativePos = GUIUtility.GUIToScreenPoint(new Vector2(0, 0));
            Rect btnRect = GUILayoutUtility.GetLastRect();
            btnRect.x += relativePos.x;
            btnRect.y += relativePos.y;
            Rect dropdownRect = new Rect(btnRect.x, btnRect.y + btnRect.height, btnRect.width, 150);

            if (!isActive && toggleBtnState && Event.current.type == EventType.Repaint)
            {
                // User activated the dropdown
                ShowList(btnRect, dropdownRect);
            }
            else if (isActive && (!toggleBtnState || !display.ContainsMouse()))
            {
                // User deactivated the downdown or moved the mouse cursor away
                HideList();
            }
        }

        private void InitStyles()
        {
            if (listStyle == null)
            {
                listStyle = new GUIStyle(GUI.skin.window);
                listStyle.padding = new RectOffset(1, 1, 1, 1);
            }
            if (toggleBtnStyle == null)
            {
                toggleBtnStyle = new GUIStyle(GUI.skin.button);
                toggleBtnStyle.normal.textColor
                    = toggleBtnStyle.focused.textColor
                    = Color.white;
                toggleBtnStyle.hover.textColor
                    = toggleBtnStyle.active.textColor
                    = toggleBtnStyle.onActive.textColor
                    = Color.yellow;
                toggleBtnStyle.onNormal.textColor
                    = toggleBtnStyle.onFocused.textColor
                    = toggleBtnStyle.onHover.textColor
                    = Color.green;
            }
            if (dropdownItemStyle == null)
            {
                dropdownItemStyle = new GUIStyle(GUI.skin.button);
                dropdownItemStyle.padding = new RectOffset(2, 2, 2, 2);
                dropdownItemStyle.margin.top = 1;
                dropdownItemStyle.margin.bottom = 1;
            }
            if (selectedItemStyle == null)
            {
                selectedItemStyle = new GUIStyle(GUI.skin.button);
                selectedItemStyle.padding = new RectOffset(2, 2, 2, 2);
                selectedItemStyle.margin.top = 1;
                selectedItemStyle.margin.bottom = 1;
                selectedItemStyle.normal.textColor
                    = selectedItemStyle.focused.textColor
                    = selectedItemStyle.hover.textColor
                    = selectedItemStyle.active.textColor
                    = selectedItemStyle.onActive.textColor
                    = selectedItemStyle.onNormal.textColor
                    = selectedItemStyle.onFocused.textColor
                    = selectedItemStyle.onHover.textColor
                    = XKCDColors.KSPNotSoGoodOrange;
            }
        }

        private void ShowList(Rect btnRect, Rect dropdownRect)
        {
            if (!isActive)
            {
                toggleBtnState = isActive = true;
                GUIDropDownWindow.Instance.ActivateDisplay(this.GetHashCode(), btnRect, dropdownRect, OnDisplayList, listStyle);
                InputLockManager.SetControlLock(ControlTypes.All, "DropdownScrollLock");
            }
        }

        private void HideList()
        {
            if (isActive)
            {
                toggleBtnState = isActive = false;
                GUIDropDownWindow.Instance.DisableDisplay();
                InputLockManager.RemoveControlLock("DropdownScrollLock");
            }
        }

        private void OnDisplayList(int id)
        {
            GUI.BringWindowToFront(id);
            scrollPos = GUILayout.BeginScrollView(scrollPos, listStyle);
            for (int i = 0; i < stringOptions.Length; i++)
            {
                // Highlight the selected item
                GUIStyle tmpStyle = (selectedOption == i) ? selectedItemStyle : dropdownItemStyle;
                if (GUILayout.Button(stringOptions[i], tmpStyle))
                {
                    Debug.Log("Selected " + stringOptions[i]);
                    selectedOption = i;
                    HideList();
                }
            }
            GUILayout.EndScrollView();
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class GUIDropDownWindow : MonoBehaviour
    {
        private static GUIDropDownWindow instance;
        public static GUIDropDownWindow Instance
        {
            get { return instance; }
        }

        private Rect btnRect;
        private Rect displayRect;
        private int windowId;
        private GUI.WindowFunction windowFunction;
        private GUIStyle listStyle;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            this.enabled = true;
            GameObject.DontDestroyOnLoad(this);
        }


        private void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (windowFunction != null)
            {
                displayRect = GUILayout.Window(windowId, displayRect, windowFunction, "", listStyle, GUILayout.Height(0));
            }
        }

        public bool ContainsMouse()
        {
            return btnRect.Contains(GUIUtils.GetMousePos()) ||
                   displayRect.Contains(GUIUtils.GetMousePos());
        }

        public void ActivateDisplay(int id, Rect btnRect, Rect rect, GUI.WindowFunction func, GUIStyle style)
        {
            this.windowId = id;
            this.btnRect = btnRect;
            this.displayRect = rect;
            this.windowFunction = func;
            this.listStyle = style;
        }

        public void DisableDisplay()
        {
            windowFunction = null;
        }
    }
}
