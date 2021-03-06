﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using ColorPicker.Helpers;
using ColorPicker.Settings;
using ColorPicker.Telemetry;
using Microsoft.PowerToys.Settings.UI.Lib.Utilities;
using Microsoft.PowerToys.Telemetry;
using static ColorPicker.NativeMethods;

namespace ColorPicker.Keyboard
{
    [Export(typeof(KeyboardMonitor))]
    public class KeyboardMonitor : IDisposable
    {
        private readonly AppStateHandler _appStateHandler;
        private readonly IUserSettings _userSettings;

        private List<string> _activationKeys = new List<string>();
        private GlobalKeyboardHook _keyboardHook;
        private bool disposedValue;

        [ImportingConstructor]
        public KeyboardMonitor(AppStateHandler appStateHandler, IUserSettings userSettings)
        {
            _appStateHandler = appStateHandler;
            _userSettings = userSettings;
            _userSettings.ActivationShortcut.PropertyChanged += ActivationShortcut_PropertyChanged;
            SetActivationKeys();
        }

        public void Start()
        {
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyboardPressed += Hook_KeyboardPressed;
        }

        private void SetActivationKeys()
        {
            _activationKeys.Clear();

            if (!string.IsNullOrEmpty(_userSettings.ActivationShortcut.Value))
            {
                var keys = _userSettings.ActivationShortcut.Value.Split('+');
                foreach (var key in keys)
                {
                    _activationKeys.Add(key.Trim());
                }

                _activationKeys.Sort();
            }
        }

        private void ActivationShortcut_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SetActivationKeys();
        }

        private void Hook_KeyboardPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            var currentlyPressedKeys = new List<string>();
            var virtualCode = e.KeyboardData.VirtualCode;

            // ESC pressed
            if (virtualCode == KeyInterop.VirtualKeyFromKey(Key.Escape))
            {
                _appStateHandler.HideColorPicker();
                PowerToysTelemetry.Log.WriteEvent(new ColorPickerCancelledEvent());
                return;
            }

            var name = Helper.GetKeyName((uint)virtualCode);

            // If the last key pressed is a modifier key, then currentlyPressedKeys cannot possibly match with _activationKeys
            // because _activationKeys contains exactly 1 non-modifier key. Hence, there's no need to check if `name` is a
            // modifier key or to do any additional processing on it.

            // Check pressed modifier keys.
            AddModifierKeys(currentlyPressedKeys);

            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown || e.KeyboardState == GlobalKeyboardHook.KeyboardState.SysKeyDown)
            {
                currentlyPressedKeys.Add(name);
            }

            currentlyPressedKeys.Sort();

            if (ArraysAreSame(currentlyPressedKeys, _activationKeys))
            {
                _appStateHandler.ShowColorPicker();
            }
        }

        private static bool ArraysAreSame(List<string> first, List<string> second)
        {
            if (first.Count != second.Count || (first.Count == 0 && second.Count == 0))
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddModifierKeys(List<string> currentlyPressedKeys)
        {
            if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add("Shift");
            }

            if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add("Ctrl");
            }

            if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add("Alt");
            }

            if ((GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add("Win");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _keyboardHook.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
