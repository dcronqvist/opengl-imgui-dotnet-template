using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static opengl_dotnet_template.OpenGL.GL;
using System.Runtime.InteropServices;
using opengl_dotnet_template.GLFW;
using System.Numerics;

namespace opengl_dotnet_template
{
    /// <summary>
    /// A modified version of Veldrid.ImGui's ImGuiRenderer.
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
    /// </summary>
    public class ImGuiController
    {
        private bool _frameBegun;

        private int _vertexArray;
        private int _vertexBuffer;
        private int _vertexBufferSize;
        private int _indexBuffer;
        private int _indexBufferSize;

        private uint _fontTexture;
        private uint _shader;

        private int _windowWidth;
        private int _windowHeight;

        private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

        private Window _windowHandle;

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public unsafe ImGuiController(Window windowHandle, int width, int height)
        {
            _windowHandle = windowHandle;
            _windowWidth = width;
            _windowHeight = height;

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            var io = ImGui.GetIO();
            io.Fonts.AddFontDefault();

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            CreateDeviceResources();
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();
            _frameBegun = true;

            Glfw.SetCharCallback(windowHandle, (window, codePoint) =>
            {
                PressChar((char)codePoint);
            });

            Glfw.SetScrollCallback(windowHandle, (window, x, y) =>
            {
                MouseScroll(new Vector2((float)x, (float)y));
            });
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        private uint _vao;
        private uint _vbo;
        private uint _ebo;

        private unsafe uint CreateShader(int shadertype, string source)
        {
            // Create either fragment or vertex shader
            var shader = glCreateShader(shadertype);

            // Set shader source
            glShaderSource(shader, source);

            // Compile shader
            glCompileShader(shader);

            return shader;
        }

        private unsafe uint CreateProgram(uint vertex, uint fragment)
        {
            // Create shader program
            var program = glCreateProgram();

            // Attach shaders
            glAttachShader(program, vertex);
            glAttachShader(program, fragment);

            // Link program
            glLinkProgram(program);

            // Delete shaders
            glDeleteShader(vertex);
            glDeleteShader(fragment);

            // Use program
            glUseProgram(program);

            return program;
        }

        public unsafe void CreateDeviceResources()
        {
            _vao = glGenVertexArray();

            _vertexBufferSize = 10000;
            _indexBufferSize = 2000;

            _vbo = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, _vbo);
            glBufferData(GL_ARRAY_BUFFER, _vertexBufferSize, null, GL_DYNAMIC_DRAW);

            _ebo = glGenBuffer();
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _ebo);
            glBufferData(GL_ELEMENT_ARRAY_BUFFER, _indexBufferSize, null, GL_DYNAMIC_DRAW);

            glBindVertexArray(0);

            RecreateFontDeviceTexture();

            string VertexSource = @"#version 330 core

uniform mat4 projection_matrix;

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;

out vec4 color;
out vec2 texCoord;

void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";
            string FragmentSource = @"#version 330 core

uniform sampler2D in_fontTexture;

in vec4 color;
in vec2 texCoord;

out vec4 outputColor;

void main()
{
    outputColor = color * texture(in_fontTexture, texCoord);
}";

            var vs = CreateShader(GL_VERTEX_SHADER, VertexSource);
            var fs = CreateShader(GL_FRAGMENT_SHADER, FragmentSource);
            _shader = CreateProgram(vs, fs);

            glBindVertexArray(_vao);
            glBindBuffer(GL_ARRAY_BUFFER, _vbo);
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _ebo);

            glEnableVertexAttribArray(0);
            glVertexAttribPointer(0, 2, GL_FLOAT, false, Unsafe.SizeOf<ImDrawVert>(), (void*)0);

            glEnableVertexAttribArray(1);
            glVertexAttribPointer(1, 2, GL_FLOAT, false, Unsafe.SizeOf<ImDrawVert>(), (void*)8);

            glEnableVertexAttribArray(2);
            glVertexAttribPointer(2, 4, GL_UNSIGNED_BYTE, true, Unsafe.SizeOf<ImDrawVert>(), (void*)16);
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public unsafe void RecreateFontDeviceTexture()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            // Create opengl texture with data
            _fontTexture = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, _fontTexture);


            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, pixels);

            // Set texture options
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            // Store our identifier
            io.Fonts.SetTexID((IntPtr)_fontTexture);
            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
        /// or index data has increased beyond the capacity of the existing buffers.
        /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
        /// </summary>
        public void Render()
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData());
            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds)
        {
            if (_frameBegun)
            {
                ImGui.Render();
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput();

            _frameBegun = true;
            ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(
                _windowWidth / _scaleFactor.X,
                _windowHeight / _scaleFactor.Y);
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        readonly List<char> PressedChars = new List<char>();

        private bool IsMouseButtonDown(MouseButton button)
        {
            return Glfw.GetMouseButton(_windowHandle, button) == InputState.Press;
        }

        private bool IsKeyDown(Keys key)
        {
            return Glfw.GetKey(_windowHandle, key) == InputState.Press;
        }

        private Vector2 GetMousePositionInWindow()
        {
            Glfw.GetCursorPosition(_windowHandle, out double x, out double y);
            return new Vector2((float)x, (float)y);
        }

        private void UpdateImGuiInput()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            io.MouseDown[0] = IsMouseButtonDown(MouseButton.Left);
            io.MouseDown[1] = IsMouseButtonDown(MouseButton.Right);
            io.MouseDown[2] = IsMouseButtonDown(MouseButton.Middle);

            var screenPoint = GetMousePositionInWindow();
            var point = screenPoint;
            io.MousePos = new System.Numerics.Vector2(point.X, point.Y);

            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key == Keys.Unknown)
                {
                    continue;
                }
                io.KeysDown[(int)key] = IsKeyDown(key);
            }

            foreach (var c in PressedChars)
            {
                io.AddInputCharacter(c);
            }
            PressedChars.Clear();

            io.KeyCtrl = IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
            io.KeyAlt = IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt);
            io.KeyShift = IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift);
            io.KeySuper = IsKeyDown(Keys.LeftSuper) || IsKeyDown(Keys.RightSuper);
        }

        internal void PressChar(char keyChar)
        {
            PressedChars.Add(keyChar);
        }

        internal void MouseScroll(Vector2 offset)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            io.MouseWheel = offset.Y;
            io.MouseWheelH = offset.X;
        }

        private static void SetKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
        }

        private unsafe void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
                if (vertexSize > _vertexBufferSize)
                {
                    int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);
                    glBindBuffer(GL_ARRAY_BUFFER, _vbo);
                    glBufferData(GL_ARRAY_BUFFER, newSize, IntPtr.Zero, GL_DYNAMIC_DRAW);
                    _vertexBufferSize = newSize;
                }

                int indexSize = cmd_list.IdxBuffer.Size * sizeof(ushort);
                if (indexSize > _indexBufferSize)
                {
                    int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _ebo);
                    glBufferData(GL_ELEMENT_ARRAY_BUFFER, newSize, IntPtr.Zero, GL_DYNAMIC_DRAW);
                    _indexBufferSize = newSize;
                }
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                0.0f,
                io.DisplaySize.X,
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);


            glUseProgram(_shader);


            var loc = glGetUniformLocation(_shader, "projection_matrix");
            glUniformMatrix4fv(loc, 1, false, (float*)&mvp);

            loc = glGetUniformLocation(_shader, "in_fontTexture");
            glUniform1i(loc, 0);

            glBindVertexArray(_vao);
            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            //GL.Enable(EnableCap.Blend);
            int oldBlend = glGetInteger(GL_BLEND);
            glEnable(GL_BLEND);
            //GL.Enable(EnableCap.ScissorTest);
            int oldScissor = glGetInteger(GL_SCISSOR_TEST);
            glEnable(GL_SCISSOR_TEST);
            //GL.BlendEquation(BlendEquationMode.FuncAdd);
            int oldBlendEquation = glGetInteger(GL_BLEND_EQUATION);
            //glBlendEquation(GL_FUNC_ADD);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            int oldBlendSrc = glGetInteger(GL_BLEND_SRC);
            int oldBlendDst = glGetInteger(GL_BLEND_DST);
            glBlendFuncSeparate(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA, GL_ONE, GL_ONE_MINUS_SRC_ALPHA);
            //GL.Disable(EnableCap.CullFace);
            int oldCullFace = glGetInteger(GL_CULL_FACE);
            glDisable(GL_CULL_FACE);
            //GL.Disable(EnableCap.DepthTest);
            int oldDepthTest = glGetInteger(GL_DEPTH_TEST);
            glDisable(GL_DEPTH_TEST);

            // Render command lists
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];

                //GL.NamedBufferSubData(_vertexBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
                glBindBuffer(GL_ARRAY_BUFFER, _vbo);
                glBufferData(GL_ARRAY_BUFFER, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data, GL_DYNAMIC_DRAW);

                //GL.NamedBufferSubData(_indexBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);
                glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _ebo);
                glBufferData(GL_ELEMENT_ARRAY_BUFFER, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data, GL_DYNAMIC_DRAW);
                //Util.CheckGLError($"Data Idx {n}");

                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        //GL.ActiveTexture(TextureUnit.Texture0);
                        glActiveTexture(GL_TEXTURE0);
                        //GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                        glBindTexture(GL_TEXTURE_2D, (uint)pcmd.TextureId);
                        //Util.CheckGLError("Texture");

                        // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                        var clip = pcmd.ClipRect;
                        //GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                        glScissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                        //Util.CheckGLError("Scissor");

                        if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                        {
                            //GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(idx_offset * sizeof(ushort)), vtx_offset);
                            glDrawElementsBaseVertex(GL_TRIANGLES, (int)pcmd.ElemCount, GL_UNSIGNED_SHORT, (void*)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                        }
                        else
                        {
                            //GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                            glDrawElements(GL_TRIANGLES, (int)pcmd.ElemCount, GL_UNSIGNED_INT, (void*)(pcmd.IdxOffset * sizeof(ushort)));
                        }
                        //Util.CheckGLError("Draw");
                    }

                    //idx_offset += (int)pcmd.ElemCount;
                }
            }

            //GL.Disable(EnableCap.Blend);
            glDisable(GL_BLEND);
            //GL.Disable(EnableCap.ScissorTest);
            glDisable(GL_SCISSOR_TEST);
            if (oldBlend == 0)
            {
                glDisable(GL_BLEND);
            }
            if (oldScissor == 0)
            {
                glDisable(GL_SCISSOR_TEST);
            }
            glBlendEquation(oldBlendEquation);
            glBlendFunc(oldBlendSrc, oldBlendDst);
            if (oldCullFace == 1)
            {
                glEnable(GL_CULL_FACE);
            }
            if (oldDepthTest == 1)
            {
                glEnable(GL_DEPTH_TEST);
            }

            glUseProgram(0);
        }
    }
}