/*
 * TextureShader.cs - Shader for textured objects
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

namespace Freeserf.Renderer
{
    internal class TextureShader : ColorShader
    {
        static TextureShader textureShader = null;
        internal static readonly string DefaultTexCoordName = "texCoord";
        internal static readonly string DefaultSamplerName = "sampler";
        internal static readonly string DefaultColorKeyName = "colorKey";
        internal static readonly string DefaultColorOverlayName = "color";
        internal static readonly string DefaultAtlasSizeName = "atlasSize";

        readonly string texCoordName;
        readonly string samplerName;
        readonly string colorKeyName;
        readonly string colorOverlayName;
        readonly string atlasSizeName;

        // -------------------------------------------------------------
        // Local GLSL / GLES helpers
        // -------------------------------------------------------------

        private static bool IsGLES()
        {
            return !string.IsNullOrEmpty(State.GLSLVersionSuffix) &&
                   State.GLSLVersionSuffix.ToLower().Contains("es");
        }

        private static bool IsLegacyGL()
        {
            return !IsGLES() && State.GLSLVersionMajor == 1 && State.GLSLVersionMinor < 3;
        }

        private static string GLSLVersionHeader()
        {
            if (IsGLES())
            {
                if (State.GLSLVersionMajor == 1 && State.GLSLVersionMinor == 0)
                {
                    return "#version 100\n" +
                           "precision mediump float;\n" +
                           "precision mediump int;\n\n";
                }

                return $"#version {State.GLSLVersionMajor}{State.GLSLVersionMinor} es\n" +
                       "precision mediump float;\n" +
                       "precision mediump int;\n\n";
            }

            return $"#version {State.GLSLVersionMajor}{State.GLSLVersionMinor} {State.GLSLVersionSuffix}\n\n";
        }

        private static string InQualifier(bool fragment)
        {
            if (IsLegacyGL())
                return fragment ? "varying" : "attribute";

            return "in";
        }

        private static string OutQualifier()
        {
            if (IsLegacyGL())
                return "varying";

            return "out";
        }

        private static bool SupportsIntegerAttributes()
        {
            if (IsLegacyGL())
                return false;

            if (IsGLES() && State.GLSLVersionMajor < 3)
                return false;

            return true;
        }

        private static bool UsesLegacyFragColor()
        {
            if (IsLegacyGL())
                return true;

            if (IsGLES() && State.GLSLVersionMajor < 3)
                return true;

            return false;
        }

        // -------------------------------------------------------------
        // Unified shader generators
        // -------------------------------------------------------------

        private static string GenerateTextureVertexShader()
        {
            string header = GLSLVersionHeader();

            bool ints = SupportsIntegerAttributes();

            // GLES2 path → all floats
            string posType = ints ? "ivec2" : "vec2";
            string texType = ints ? "ivec2" : "vec2";
            string layerType = ints ? "uint" : "float";
            string atlasType = ints ? "uvec2" : "vec2";

            string atlasFactorExpr = ints
                ? $"    vec2 atlasFactor = vec2(1.0 / float({DefaultAtlasSizeName}.x), 1.0 / float({DefaultAtlasSizeName}.y));"
                : $"    vec2 atlasFactor = vec2(1.0 / {DefaultAtlasSizeName}.x, 1.0 / {DefaultAtlasSizeName}.y);";

            string posExpr = ints
                ? $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49, float({DefaultPositionName}.y) + 0.49);"
                : $"    vec2 pos = {DefaultPositionName} + vec2(0.49, 0.49);";

            string texExpr = ints
                ? $"    varTexCoord = vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);"
                : $"    varTexCoord = {DefaultTexCoordName};";

            // Layer handling
            string layerExpr = ints
                ? $"    float layerValue = float({DefaultLayerName});"
                : $"    float layerValue = {DefaultLayerName};";

            return string.Join("\n", new[]
            {
                header,
                $"{InQualifier(false)} {posType} {DefaultPositionName};",
                $"{InQualifier(false)} {texType} {DefaultTexCoordName};",
                $"{InQualifier(false)} {layerType} {DefaultLayerName};",
                $"uniform {atlasType} {DefaultAtlasSizeName};",
                $"uniform float {DefaultZName};",
                $"uniform mat4 {DefaultProjectionMatrixName};",
                $"uniform mat4 {DefaultModelViewMatrixName};",
                $"{OutQualifier()} vec2 varTexCoord;",
                "",
                "void main()",
                "{",
                atlasFactorExpr,
                posExpr,
                texExpr,
                layerExpr,
                "",
                $"    varTexCoord *= atlasFactor;",
                $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0 - {DefaultZName} - layerValue * 0.00001, 1.0);",
                "}"
            });
        }

        private static string GenerateTextureFragmentShader()
        {
            string header = GLSLVersionHeader();

            bool legacyFragColor = UsesLegacyFragColor();

            string outputDecl = legacyFragColor
                ? ""
                : $"out vec4 {DefaultFragmentOutColorName};\n";

            string outputAssign = legacyFragColor
                ? "gl_FragColor = pixelColor;"
                : $"{DefaultFragmentOutColorName} = pixelColor;";

            return string.Join("\n", new[]
            {
                header,
                outputDecl,
                $"uniform vec3 {DefaultColorKeyName};",
                $"uniform vec4 {DefaultColorOverlayName};",
                $"uniform sampler2D {DefaultSamplerName};",
                $"{InQualifier(true)} vec2 varTexCoord;",
                "",
                "void main()",
                "{",
                $"    vec4 pixelColor = texture({DefaultSamplerName}, varTexCoord);",
                "",
                $"    if (pixelColor.r == {DefaultColorKeyName}.r && pixelColor.g == {DefaultColorKeyName}.g && pixelColor.b == {DefaultColorKeyName}.b)",
                $"        pixelColor.a = 0.0;",
                $"    else",
                $"        pixelColor *= {DefaultColorOverlayName};",
                "",
                $"    if (pixelColor.a < 0.5)",
                $"        discard;",
                $"    else",
                $"        {outputAssign}",
                "}"
            });
        }

        // -------------------------------------------------------------
        // Static shader arrays
        // -------------------------------------------------------------

        static readonly string[] TextureFragmentShader = new string[]
        {
            GenerateTextureFragmentShader()
        };

        static readonly string[] TextureVertexShader = new string[]
        {
            GenerateTextureVertexShader()
        };

        // -------------------------------------------------------------
        // Constructors
        // -------------------------------------------------------------

        TextureShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultColorKeyName, DefaultColorOverlayName,
                  DefaultAtlasSizeName, DefaultLayerName, TextureFragmentShader, TextureVertexShader)
        {
        }

        protected TextureShader(string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string colorKeyName, string colorOverlayName,
            string atlasSizeName, string layerName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(modelViewMatrixName, projectionMatrixName, DefaultColorName, zName, positionName, layerName, fragmentShaderLines, vertexShaderLines)
        {
            this.texCoordName = texCoordName;
            this.samplerName = samplerName;
            this.colorKeyName = colorKeyName;
            this.colorOverlayName = colorOverlayName;
            this.atlasSizeName = atlasSizeName;
        }

        // -------------------------------------------------------------
        // Uniform setters
        // -------------------------------------------------------------

        public void SetSampler(int textureUnit = 0)
        {
            shaderProgram.SetInput(samplerName, textureUnit);
        }

        public void SetColorKey(float r, float g, float b)
        {
            shaderProgram.SetInputVector3(colorKeyName, r, g, b);
        }

        public void SetColorOverlay(float r, float g, float b, float a)
        {
            shaderProgram.SetInputVector4(colorOverlayName, r, g, b, a);
        }

        public void SetAtlasSize(uint width, uint height)
        {
            shaderProgram.SetInputVector2(atlasSizeName, width, height);
        }

        // -------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------

        public new static TextureShader Instance
        {
            get
            {
                if (textureShader == null)
                    textureShader = new TextureShader();

                return textureShader;
            }
        }
    }
}
