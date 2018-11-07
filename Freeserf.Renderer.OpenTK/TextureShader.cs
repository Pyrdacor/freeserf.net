using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK
{
    internal class TextureShader : ColorShader
    {
        static TextureShader textureShader = null;
        internal static readonly string DefaultTexCoordName = "texCoord";
        internal static readonly string DefaultSamplerName = "sampler";
        internal static readonly string DefaultColorKeyName = "colorKey";
        internal static readonly string DefaultAtlasSizeName = "atlasSize";

        readonly string texCoordName;
        readonly string samplerName;
        readonly string colorKeyName;
        readonly string atlasSizeName;

        static readonly string[] TextureFragmentShader = new string[]
        {
            GetFragmentShaderHeader(),
            $"uniform vec3 {DefaultColorKeyName} = vec3(1, 0, 1);",
            $"uniform sampler2D {DefaultSamplerName};",
            $"{GetInName(true)} vec2 varTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec4 pixelColor = texture2D({DefaultSamplerName}, varTexCoord);",
            $"    ",
            $"    if (pixelColor.r == {DefaultColorKeyName}.r && pixelColor.g == {DefaultColorKeyName}.g && pixelColor.b == {DefaultColorKeyName}.b)",
            $"        pixelColor.a = 0;",
            $"    ",
            $"    {(HasGLFragColor() ? "gl_FragColor" : DefaultFragmentOutColorName)} = pixelColor;",
            $"}}"
        };

        static readonly string[] TextureVertexShader = new string[]
        {
            GetVertexShaderHeader(),
            $"{GetInName(false)} ivec2 {DefaultPositionName};",
            $"{GetInName(false)} ivec2 {DefaultTexCoordName};",
            $"{GetInName(false)} uvec2 {DefaultSizeName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"{GetOutName()} vec2 varTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
            $"    varTexCoord = vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    ",
            $"    uint i = gl_VertexID % 4;",
            $"    ",
            $"    if (i == 1)",
            $"    {{",
            $"        pos.x += {DefaultSizeName}.x;",
            $"        varTexCoord.x += {DefaultSizeName}.x;",
            $"    }}",
            $"    else if (i == 2)",
            $"    {{",
            $"        pos.x += {DefaultSizeName}.x;",
            $"        pos.y += {DefaultSizeName}.y;",
            $"        varTexCoord.x += {DefaultSizeName}.x;",
            $"        varTexCoord.y += {DefaultSizeName}.y;",
            $"    }}",
            $"    else if (i == 3)",
            $"    {{",
            $"        pos.y += {DefaultSizeName}.y;",
            $"        varTexCoord.y += {DefaultSizeName}.y;",
            $"    }}",
            $"    ",
            $"    varTexCoord *= atlasFactor;",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, {DefaultZName}, 1.0f);",
            $"}}"
        };

        TextureShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName, 
                  DefaultSizeName, DefaultTexCoordName, DefaultSamplerName, DefaultColorKeyName,
                  DefaultAtlasSizeName, TextureFragmentShader, TextureVertexShader)
        {

        }

        protected TextureShader(string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string sizeName, string texCoordName, string samplerName, string colorKeyName,
            string atlasSizeName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(modelViewMatrixName, projectionMatrixName, DefaultColorName, zName, positionName, sizeName, fragmentShaderLines, vertexShaderLines)
        {
            this.texCoordName = texCoordName;
            this.samplerName = samplerName;
            this.colorKeyName = colorKeyName;
            this.atlasSizeName = atlasSizeName;
        }

        public void SetSampler(int textureUnit = 0)
        {
            shaderProgram.SetInput(samplerName, textureUnit);
        }

        public void SetColorKey(float r, float g, float b)
        {
            shaderProgram.SetInputVector3(colorKeyName, r, g, b);
        }

        public void SetAtlasSize(uint width, uint height)
        {
            shaderProgram.SetInputVector2(atlasSizeName, width, height);
        }

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
