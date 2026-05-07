/*
 * MaskedTriangleShader.cs - Shader for masked map triangles
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
    internal sealed class MaskedTriangleShader : TextureShader
    {
        static MaskedTriangleShader maskedTriangleShader = null;
        internal static readonly string DefaultMaskTexCoordName = "maskTexCoord";

        readonly string maskTexCoordName;

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

        private static string GenerateMaskedTriangleVertexShader()
        {
            string header = GLSLVersionHeader();

            bool ints = SupportsIntegerAttributes();

            string posType = ints ? "ivec2" : "vec2";
            string texType = ints ? "ivec2" : "vec2";
            string maskType = ints ? "ivec2" : "vec2";
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

            string maskExpr = ints
                ? $"    varMaskTexCoord = vec2({DefaultMaskTexCoordName}.x, {DefaultMaskTexCoordName}.y);"
                : $"    varMaskTexCoord = {DefaultMaskTexCoordName};";

            return string.Join("\n", new[]
            {
                header,
                $"{InQualifier(false)} {posType} {DefaultPositionName};",
                $"{InQualifier(false)} {texType} {DefaultTexCoordName};",
                $"{InQualifier(false)} {maskType} {DefaultMaskTexCoordName};",
                $"uniform {atlasType} {DefaultAtlasSizeName};",
                $"uniform float {DefaultZName};",
                $"uniform mat4 {DefaultProjectionMatrixName};",
                $"uniform mat4 {DefaultModelViewMatrixName};",
                $"{OutQualifier()} vec2 varTexCoord;",
                $"{OutQualifier()} vec2 varMaskTexCoord;",
                "",
                "void main()",
                "{",
                atlasFactorExpr,
                posExpr,
                texExpr,
                maskExpr,
                "",
                $"    varTexCoord *= atlasFactor;",
                $"    varMaskTexCoord *= atlasFactor;",
                $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0 - {DefaultZName}, 1.0);",
                "}"
            });
        }

        private static string GenerateMaskedTriangleFragmentShader()
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
                $"{InQualifier(true)} vec2 varMaskTexCoord;",
                "",
                "void main()",
                "{",
                $"    vec4 pixelColor = texture({DefaultSamplerName}, varTexCoord);",
                $"    vec4 maskColor  = texture({DefaultSamplerName}, varMaskTexCoord);",
                "",
                $"    if (pixelColor.r == {DefaultColorKeyName}.r && pixelColor.g == {DefaultColorKeyName}.g && pixelColor.b == {DefaultColorKeyName}.b)",
                $"        pixelColor.a = 0.0;",
                $"    else",
                $"    {{",
                $"        pixelColor *= maskColor;",
                $"        pixelColor *= {DefaultColorOverlayName};",
                $"    }}",
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

        static readonly string[] MaskedTriangleFragmentShader = new string[]
        {
            GenerateMaskedTriangleFragmentShader()
        };

        static readonly string[] MaskedTriangleVertexShader = new string[]
        {
            GenerateMaskedTriangleVertexShader()
        };

        // -------------------------------------------------------------
        // Constructors
        // -------------------------------------------------------------

        MaskedTriangleShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultColorKeyName, DefaultColorOverlayName,
                  DefaultAtlasSizeName, DefaultMaskTexCoordName,
                  MaskedTriangleFragmentShader, MaskedTriangleVertexShader)
        {
        }

        MaskedTriangleShader(string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string colorKeyName, string colorOverlayName,
            string atlasSizeName, string maskTexCoordName,
            string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(modelViewMatrixName, projectionMatrixName, zName, positionName,
                  texCoordName, samplerName, colorKeyName, colorOverlayName,
                  atlasSizeName, DefaultLayerName, fragmentShaderLines, vertexShaderLines)
        {
            this.maskTexCoordName = maskTexCoordName;
        }

        // -------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------

        public new static MaskedTriangleShader Instance
        {
            get
            {
                if (maskedTriangleShader == null)
                    maskedTriangleShader = new MaskedTriangleShader();

                return maskedTriangleShader;
            }
        }
    }
}
