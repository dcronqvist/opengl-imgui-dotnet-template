using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DotGLFW;

namespace opengl_dotnet_template;

public static class Mouse
{
    public static Dictionary<MouseButton, bool> currentMouseState;
    public static Dictionary<MouseButton, bool> previousMouseState;

    public static double currentMouseScroll;
    public static double previousMouseScroll;

    public static Vector2 currentMousePosition;
    public static Vector2 previousMousePosition;

    public static event EventHandler<float> OnScroll;

    public static void Init(Window window)
    {
        currentMouseState = GetMouseState(window);
        previousMouseState = currentMouseState;

        currentMousePosition = GetMousePositionInWindow(window);
        previousMousePosition = currentMousePosition;

        Glfw.SetScrollCallback(window, (w, x, y) =>
        {
            currentMouseScroll += y;
            OnScroll?.Invoke(null, (float)y);
        });
    }

    public static void Begin(Window window)
    {
        currentMouseState = GetMouseState(window);
        currentMousePosition = GetMousePositionInWindow(window);
    }

    public static void End()
    {
        previousMouseState = currentMouseState;
        previousMouseScroll = currentMouseScroll;
        previousMousePosition = currentMousePosition;
    }

    public static Dictionary<MouseButton, bool> GetMouseState(Window window)
    {
        MouseButton[] mouseButtons = Enum.GetValues<MouseButton>();
        Dictionary<MouseButton, bool> dic = new Dictionary<MouseButton, bool>();

        foreach (MouseButton button in mouseButtons)
        {
            if (!dic.ContainsKey(button))
            {
                dic.Add(button, Glfw.GetMouseButton(window, button) == InputState.Press);
            }
        }

        return dic;
    }

    public static bool IsMouseButtonDown(MouseButton button)
    {
        return currentMouseState[button];
    }

    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return currentMouseState[button] && !previousMouseState[button];
    }

    public static bool IsMouseButtonReleased(MouseButton button)
    {
        return !currentMouseState[button] && previousMouseState[button];
    }

    public static Vector2 GetMousePositionInWindow(Window window)
    {
        Glfw.GetCursorPos(window, out double x, out double y);
        return new Vector2((float)x, (float)y);
    }

    public static Vector2 GetMouseWindowDelta()
    {
        return currentMousePosition - previousMousePosition;
    }

    public static int GetScroll()
    {
        if (currentMouseScroll == previousMouseScroll)
        {
            return 0;
        }

        return currentMouseScroll > previousMouseScroll ? 1 : -1;
    }

    public static int[] GetAllPressedButtons()
    {
        return currentMouseState.Where(x => x.Value).Select(x => (int)x.Key).ToArray();
    }
}