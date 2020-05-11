/*
 * MaskedTextureShader.cs - Shader for masked textured objects
 *
 * Copyright (C) 2018-2019  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Freeserf.Renderer
{
    internal sealed class MaskedTextureShader : TextureShader
    {
        static MaskedTextureShader maskedTextureShader = null;
        internal static readonly string DefaultMaskTexCoordName = "maskTexCoord";

        readonly string maskTexCoordName;

        static readonly string[] MaskedTextureFragmentShader = new string[]
        {
            GetFragmentShaderHeader(),
            $"uniform vec3 {DefaultColorKeyName} = vec3(1, 0, 1);",
            $"uniform vec4 {DefaultColorOverlayName} = vec4(1, 1, 1, 1);",
            $"uniform sampler2D {DefaultSamplerName};",
            $"{GetInName(true)} vec2 varTexCoord;",
            $"{GetInName(true)} vec2 varMaskTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec4 pixelColor = texture({DefaultSamplerName}, varTexCoord);",
            $"    vec4 maskColor = texture({DefaultSamplerName}, varMaskTexCoord);",
            $"    ",
            $"    if (pixelColor.r == {DefaultColorKeyName}.r && pixelColor.g == {DefaultColorKeyName}.g && pixelColor.b == {DefaultColorKeyName}.b)",
            $"        pixelColor.a = 0;",
            $"    else",
            $"    {{",
            $"        pixelColor.r *= maskColor.r;",
            $"        pixelColor.g *= maskColor.g;",
            $"        pixelColor.b *= maskColor.b;",
            $"        pixelColor.a *= maskColor.a;",
            $"        pixelColor *= {DefaultColorOverlayName};",
            $"    }}",
            $"    ",
            $"    if (pixelColor.a < 0.5)",
            $"        discard;",
            $"    else",
            $"        {(HasGLFragColor() ? "gl_FragColor" : DefaultFragmentOutColorName)} = pixelColor;",
            $"}}"
        };

        static readonly string[] MaskedTextureVertexShader = new string[]
        {
            GetVertexShaderHeader(),
            $"{GetInName(false)} ivec2 {DefaultPositionName};",
            $"{GetInName(false)} ivec2 {DefaultTexCoordName};",
            $"{GetInName(false)} ivec2 {DefaultMaskTexCoordName};",
            $"{GetInName(false)} uint {DefaultLayerName};",
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
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    varMaskTexCoord = vec2({DefaultMaskTexCoordName}.x, {DefaultMaskTexCoordName}.y);",
            $"    ",
            $"    varTexCoord *= atlasFactor;",
            $"    varMaskTexCoord *= atlasFactor;",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        MaskedTextureShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultColorKeyName, DefaultColorOverlayName, DefaultAtlasSizeName,
                  DefaultMaskTexCoordName, DefaultLayerName, MaskedTextureFragmentShader, MaskedTextureVertexShader)
        {

        }

        MaskedTextureShader(string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string colorKeyName, string colorOverlayName,
            string atlasSizeName, string maskTexCoordName, string layerName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(modelViewMatrixName, projectionMatrixName, zName, positionName, texCoordName, samplerName,
                  colorKeyName, colorOverlayName, atlasSizeName, layerName, fragmentShaderLines, vertexShaderLines)
        {
            this.maskTexCoordName = maskTexCoordName;
        }

        public new static MaskedTextureShader Instance
        {
            get
            {
                if (maskedTextureShader == null)
                    maskedTextureShader = new MaskedTextureShader();

                return maskedTextureShader;
            }
        }
    }
}
