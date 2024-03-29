﻿using System;
using ImGuiNET;
using DotGLFW;
using static DotGL.GL;

namespace opengl_dotnet_template;

class Program
{

    /// <summary>
    /// Obligatory name for your first OpenGL example program.
    /// </summary>
    private const string TITLE = "Hello Triangle!";

    static void Main(string[] args)
    {
        // Set context creation hints
        PrepareContext();
        // Create a window and shader program
        var window = CreateWindow(1280, 720);
        var program = CreateProgram();

        // Define a simple triangle
        CreateVertices(out var vao, out var vbo);
        rand = new Random();

        var location = glGetUniformLocation(program, "color");
        SetRandomColor(location);
        long n = 0;

        var imguiController = new ImGuiController(window, 1280, 720);

        Keyboard.Init(window);
        Mouse.Init(window);

        while (!Glfw.WindowShouldClose(window))
        {
            // Swap fore/back framebuffers, and poll for operating system events.
            Glfw.SwapBuffers(window);
            Glfw.PollEvents();

            Keyboard.Begin(window);
            Mouse.Begin(window);

            // Clear the framebuffer to defined background color
            glClear(GL_COLOR_BUFFER_BIT);

            glBindVertexArray(vao);
            glUseProgram(program);
            if (n++ % 60 == 0)
                SetRandomColor(location);
            // Draw the triangle.
            glDrawArrays(GL_TRIANGLES, 0, 3);

            imguiController.Update(1);

            ImGui.ShowDemoWindow();

            imguiController.Render();

            Keyboard.End();
            Mouse.End();
        }

        Glfw.Terminate();
    }

    private static void SetRandomColor(int location)
    {
        var r = (float)rand.NextDouble();
        var g = (float)rand.NextDouble();
        var b = (float)rand.NextDouble();
        glUniform3f(location, r, g, b);
    }

    private static void PrepareContext()
    {
        Glfw.Init();

        // Set some common hints for the OpenGL profile creation
        Glfw.WindowHint(Hint.ClientAPI, ClientAPI.OpenGLAPI);
        Glfw.WindowHint(Hint.ContextVersionMajor, 3);
        Glfw.WindowHint(Hint.ContextVersionMinor, 3);
        Glfw.WindowHint(Hint.OpenGLProfile, OpenGLProfile.CoreProfile);
        Glfw.WindowHint(Hint.DoubleBuffer, true);
        Glfw.WindowHint(Hint.Decorated, true);
    }

    /// <summary>
    /// Creates and returns a handle to a GLFW window with a current OpenGL context.
    /// </summary>
    /// <param name="width">The width of the client area, in pixels.</param>
    /// <param name="height">The height of the client area, in pixels.</param>
    /// <returns>A handle to the created window.</returns>
    private static Window CreateWindow(int width, int height)
    {
        // Create window, make the OpenGL context current on the thread, and import graphics functions
        var window = Glfw.CreateWindow(width, height, TITLE, Monitor.NULL, Window.NULL);

        // Center window
        Monitor primaryMonitor = Glfw.GetPrimaryMonitor();
        Glfw.GetMonitorWorkarea(primaryMonitor, out var _, out var _, out int ww, out int wh);

        var x = (ww - width) / 2;
        var y = (wh - height) / 2;
        Glfw.SetWindowPos(window, x, y);

        Glfw.MakeContextCurrent(window);
        Import(Glfw.GetProcAddress);

        return window;
    }

    /// <summary>
    /// Creates an extremely basic shader program that is capable of displaying a triangle on screen.
    /// </summary>
    /// <returns>The created shader program. No error checking is performed for this basic example.</returns>
    private static uint CreateProgram()
    {
        var vertex = CreateShader(GL_VERTEX_SHADER, @"#version 330 core
                                                    layout (location = 0) in vec3 pos;

                                                    void main()
                                                    {
                                                        gl_Position = vec4(pos.x, pos.y, pos.z, 1.0);
                                                    }");
        var fragment = CreateShader(GL_FRAGMENT_SHADER, @"#version 330 core
                                                        out vec4 result;

                                                        uniform vec3 color;

                                                        void main()
                                                        {
                                                            result = vec4(color, 1.0);
                                                        } ");

        var program = glCreateProgram();
        glAttachShader(program, vertex);
        glAttachShader(program, fragment);

        glLinkProgram(program);

        glDeleteShader(vertex);
        glDeleteShader(fragment);

        glUseProgram(program);
        return program;
    }

    /// <summary>
    /// Creates a shader of the specified type from the given source string.
    /// </summary>
    /// <param name="type">An OpenGL enum for the shader type.</param>
    /// <param name="source">The source code of the shader.</param>
    /// <returns>The created shader. No error checking is performed for this basic example.</returns>
    private static uint CreateShader(int type, string source)
    {
        var shader = glCreateShader(type);
        glShaderSource(shader, source);
        glCompileShader(shader);
        return shader;
    }

    /// <summary>
    /// Creates a VBO and VAO to store the vertices for a triangle.
    /// </summary>
    /// <param name="vao">The created vertex array object for the triangle.</param>
    /// <param name="vbo">The created vertex buffer object for the triangle.</param>
    private static unsafe void CreateVertices(out uint vao, out uint vbo)
    {

        var vertices = new[] {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            0.0f,  0.5f, 0.0f
        };

        vao = glGenVertexArray();
        vbo = glGenBuffer();

        glBindVertexArray(vao);

        glBindBuffer(GL_ARRAY_BUFFER, vbo);
        fixed (float* v = &vertices[0])
        {
            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
        }

        glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
        glEnableVertexAttribArray(0);
    }

    private static Random rand;
}