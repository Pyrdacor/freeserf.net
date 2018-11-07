using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK
{
    internal class ColorShader
    {
        static ColorShader colorShader = null;
        internal static readonly string DefaultFragmentOutColorName = "outColor";
        internal static readonly string DefaultPositionName = "position";
        internal static readonly string DefaultSizeName = "size";
        internal static readonly string DefaultModelViewMatrixName = "mvMat";
        internal static readonly string DefaultProjectionMatrixName = "projMat";
        internal static readonly string DefaultColorName = "color";
        internal static readonly string DefaultZName = "z";

        internal ShaderProgram shaderProgram;
        readonly string fragmentOutColorName;
        readonly string modelViewMatrixName;
        readonly string projectionMatrixName;
        readonly string colorName;
        readonly string zName;
        readonly string positionName;
        readonly string sizeName;

        // gl_FragColor is deprecated beginning in GLSL version 1.30
        protected static bool HasGLFragColor()
        {
            return State.GLSLVersionMajor == 1 && State.GLSLVersionMinor < 3;
        }

        protected static string GetFragmentShaderHeader()
        {
            string header = $"#version {State.GLSLVersionMajor}{State.GLSLVersionMinor}0\n";

            header += "\n";
            header += "#ifdef GL_ES\n";
            header += " precision mediump float;\n";
            header += " precision highp int;\n";
            header += "#endif\n";
            header += "\n";
            
            if (!HasGLFragColor())
                header += $"out vec4 {DefaultFragmentOutColorName};\n";

            header += "\n";

            return header;
        }

        protected static string GetVertexShaderHeader()
        {
            return $"#version {State.GLSLVersionMajor}{State.GLSLVersionMinor}0\n\n";
        }

        protected static string GetInName(bool fragment)
        {
            if (State.GLSLVersionMajor == 1 && State.GLSLVersionMinor < 3)
            {
                if (fragment)
                    return "varying";
                else
                    return "attribute";
            }
            else
                return "in";
        }

        protected static string GetOutName()
        {
            if (State.GLSLVersionMajor == 1 && State.GLSLVersionMinor < 3)
                return "varying";
            else
                return "out";
        }

        static readonly string[] ColorFragmentShader = new string[]
        {
            GetFragmentShaderHeader(),
            $"uniform vec4 {DefaultColorName} = vec4(1, 1, 1, 1);",
            $"",
            $"void main()",
            $"{{",
            $"    {(HasGLFragColor() ? "gl_FragColor" : DefaultFragmentOutColorName)} = {DefaultColorName};",
            $"}}"
        };

        static readonly string[] ColorVertexShader = new string[]
        {
            GetVertexShaderHeader(),
            $"{GetInName(false)} ivec2 {DefaultPositionName};",
            $"uniform uvec2 {DefaultSizeName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x), float({DefaultPositionName}.y));",
            $"    ",
            $"    uint i = gl_VertexID % 4;",
            $"    ",
            $"    if (i == 1)",
            $"    {{",
            $"        pos.x += float({DefaultSizeName}.x);",
            $"    }}",
            $"    else if (i == 2)",
            $"    {{",
            $"        pos.x += {DefaultSizeName}.x;",
            $"        pos.y += {DefaultSizeName}.y;",
            $"    }}",
            $"    else if (i == 3)",
            $"    {{",
            $"        pos.y += {DefaultSizeName}.y;",
            $"    }}",
            $"    ",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, {DefaultZName}, 1.0f);",
            $"}}"
        };

        public void UpdateMatrices()
        {
            shaderProgram.SetInputMatrix(modelViewMatrixName, State.CurrentModelViewMatrix.ToArray(), true);
            shaderProgram.SetInputMatrix(projectionMatrixName, State.CurrentProjectionMatrix.ToArray(), true);
        }

        public void Use()
        {
            if (shaderProgram != ShaderProgram.ActiveProgram)
                shaderProgram.Use();
        }

        ColorShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultColorName, DefaultZName,
                  DefaultPositionName, DefaultSizeName, ColorFragmentShader, ColorVertexShader)
        {

        }

        protected ColorShader(string modelViewMatrixName, string projectionMatrixName, string colorName, string zName,
            string positionName, string sizeName, string[] fragmentShaderLines, string[] vertexShaderLines)
        {
            fragmentOutColorName = (State.OpenGLVersionMajor > 2) ? DefaultFragmentOutColorName : "gl_FragColor";

            this.modelViewMatrixName = modelViewMatrixName;
            this.projectionMatrixName = projectionMatrixName;
            this.colorName = colorName;
            this.zName = zName;
            this.positionName = positionName;
            this.sizeName = sizeName;

            var fragmentShader = new Shader(Shader.Type.Fragment, string.Join("\n", fragmentShaderLines));
            var vertexShader = new Shader(Shader.Type.Vertex, string.Join("\n", vertexShaderLines));

            shaderProgram = new ShaderProgram(fragmentShader, vertexShader);

            shaderProgram.SetFragmentColorOutputName(fragmentOutColorName);
        }

        public ShaderProgram ShaderProgram => shaderProgram;

        public void SetColor(float r, float g, float b, float a)
        {
            shaderProgram.SetInputVector4(colorName, r, g, b, a);
        }

        public void SetZ(float z)
        {
            shaderProgram.SetInput(zName, z);
        }

        public static ColorShader Instance
        {
            get
            {
                if (colorShader == null)
                    colorShader = new ColorShader();

                return colorShader;
            }
        }
    }
}
