/*
 * MaskedTriangleShader.cs - Shader for masked map triangles
 *
 * Copyright (C) 2018  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Freeserf.Renderer.OpenTK
{
    internal sealed class MaskedTriangleShader : TextureShader
    {
        static MaskedTriangleShader maskedTriangleShader = null;
        internal static readonly string DefaultMaskTexCoordName = "maskTexCoord";

        readonly string maskTexCoordName;

        static readonly string[] MaskedTriangleFragmentShader = new string[]
        {
            GetFragmentShaderHeader(),
            $"uniform vec3 {DefaultColorKeyName} = vec3(1, 0, 1);",
            $"uniform sampler2D {DefaultSamplerName};",
            $"{GetInName(true)} vec2 varTexCoord;",
            $"{GetInName(true)} vec2 varMaskTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec4 pixelColor = texture2D({DefaultSamplerName}, varTexCoord);",
            $"    vec4 maskColor = texture2D({DefaultSamplerName}, varMaskTexCoord);",
            $"    ",
            $"    if (pixelColor.r == {DefaultColorKeyName}.r && pixelColor.g == {DefaultColorKeyName}.g && pixelColor.b == {DefaultColorKeyName}.b)",
            $"        pixelColor.a = 0;",
            $"    else",
            $"    {{",
            $"        pixelColor.r *= maskColor.r;",
            $"        pixelColor.g *= maskColor.g;",
            $"        pixelColor.b *= maskColor.b;",
            $"        pixelColor.a *= maskColor.a;",
            $"    }}",
            $"    ",
            $"    {(HasGLFragColor() ? "gl_FragColor" : DefaultFragmentOutColorName)} = pixelColor;",
            $"}}"
        };

        static readonly string[] MaskedTriangleVertexShader = new string[]
        {
            GetVertexShaderHeader(),
            $"{GetInName(false)} ivec2 {DefaultPositionName};",
            $"{GetInName(false)} ivec2 {DefaultTexCoordName};",
            $"{GetInName(false)} ivec2 {DefaultMaskTexCoordName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"{GetOutName()} vec2 varTexCoord;",
            $"{GetOutName()} vec2 varMaskTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
            $"    varTexCoord = vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    varMaskTexCoord = vec2({DefaultMaskTexCoordName}.x, {DefaultMaskTexCoordName}.y);",
            $"    ",
            $"    varTexCoord *= atlasFactor;",
            $"    varMaskTexCoord *= atlasFactor;",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName}, 1.0f);",
            $"}}"
        };

        MaskedTriangleShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultColorKeyName,
                  DefaultAtlasSizeName, DefaultMaskTexCoordName, MaskedTriangleFragmentShader, MaskedTriangleVertexShader)
        {

        }

        MaskedTriangleShader(string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string colorKeyName,
            string atlasSizeName, string maskTexCoordName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(modelViewMatrixName, projectionMatrixName, zName, positionName, texCoordName,
                  samplerName, colorKeyName, atlasSizeName, DefaultLayerName, fragmentShaderLines, vertexShaderLines)
        {
            this.maskTexCoordName = maskTexCoordName;
        }

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
