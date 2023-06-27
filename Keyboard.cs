using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DotGLFW;

namespace opengl_dotnet_template;

public static class Keyboard
{
    public static Dictionary<Keys, bool> currentKeyboardState;
    public static Dictionary<Keys, bool> previousKeyboardState;

    public static event EventHandler<char> OnChar;
    // public static event EventHandler OnBackspace;
    // public static event EventHandler OnEnterPressed;
    public static event EventHandler<(Keys, ModifierKeys)> OnKeyPress;
    public static event EventHandler<(Keys, ModifierKeys)> OnKeyPressOrRepeat;
    public static event EventHandler<(Keys, ModifierKeys)> OnKeyRelease;

    public static void Init(Window window)
    {
        currentKeyboardState = GetKeyboardState(window);
        previousKeyboardState = currentKeyboardState;

        Glfw.SetCharCallback(window, (Window, codePoint) =>
        {
            OnChar?.Invoke(null, (char)codePoint);
        });

        Glfw.SetKeyCallback(window, (Window, key, scanCode, state, mods) =>
        {
            string s = Glfw.GetKeyName(key, scanCode);
            if (state.HasFlag(InputState.Press) && !state.HasFlag(InputState.Repeat))
            {
                OnKeyPress?.Invoke(null, (key, mods));
            }
            else if ((state.HasFlag(InputState.Press) || state.HasFlag(InputState.Repeat)))
            {
                OnKeyPressOrRepeat?.Invoke(null, (key, mods));
            }
            else if (state.HasFlag(InputState.Release))
            {
                OnKeyRelease?.Invoke(null, (key, mods));
            }
        });
    }

    public static Dictionary<Keys, bool> GetKeyboardState(Window window)
    {
        Keys[] keys = Enum.GetValues<Keys>();
        Dictionary<Keys, bool> dic = new Dictionary<Keys, bool>();
        foreach (Keys key in keys)
        {
            if (key != Keys.Unknown)
            {
                dic.Add(key, Glfw.GetKey(window, key) == InputState.Press);
            }
        }
        return dic;
    }

    public static void Begin(Window window)
    {
        currentKeyboardState = GetKeyboardState(window);

    }

    public static void End()
    {
        previousKeyboardState = currentKeyboardState;
    }

    public static bool IsKeyDown(Keys key)
    {
        return currentKeyboardState[key];
    }

    public static bool IsKeyPressed(Keys key)
    {
        return currentKeyboardState[key] && !previousKeyboardState[key];
    }

    public static bool IsKeyReleased(Keys key)
    {
        return !currentKeyboardState[key] && previousKeyboardState[key];
    }

    public static bool IsKeyComboPressed(params Keys[] keys)
    {
        foreach (var key in keys.Take(keys.Length - 1))
        {
            if (!IsKeyDown(key))
            {
                return false;
            }
        }

        var lastPressed = IsKeyPressed(keys.Last());
        var current = currentKeyboardState.Where(kvp => kvp.Value == true).Select(kvp => kvp.Key);

        // Return lastPressed & NO OTHER KEY IS PRESSED
        return lastPressed && current.Except(keys).Count() == 0;
    }

    public static bool TryGetNextKeyPressed(out Keys key)
    {
        if (currentKeyboardState.Any(kvp => kvp.Value == true && previousKeyboardState[kvp.Key] == false))
        {
            key = currentKeyboardState.First(kvp => kvp.Value == true && previousKeyboardState[kvp.Key] == false).Key;
            return true;
        }
        key = Keys.Unknown;
        return false;
    }

    public static int[] GetAllPressedKeys()
    {
        return currentKeyboardState.Where(kvp => kvp.Value == true).Select(kvp => (int)kvp.Key).ToArray();
    }
}