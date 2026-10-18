using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Freeserf.Render;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class CheckBoxTests
    {
        [TestMethod]
        public void Displayed_RestoresButtonAndLabelAfterRepeatedHideShowCycles()
        {
            // Bypass graphics initialization so the visibility lifecycle can run without a GPU or game assets.
            var checkBox = CreateControl<CheckBox>();
            var button = CreateControl<Button>();
            var label = CreateControl<TextField>();
            var sprite = DispatchProxy.Create<ILayerSprite, RenderProxy>();
            var renderView = DispatchProxy.Create<IRenderView, RenderProxy>();
            SetField(typeof(Icon), button, "sprite", sprite);
            SetField(typeof(TextField), label, "index", -1);
            SetField(typeof(TextField), label, "text", "");
            SetField(typeof(TextField), label, "textRenderer", new TextRenderer(renderView));
            SetField(typeof(CheckBox), checkBox, "checkButton", button);
            SetField(typeof(CheckBox), checkBox, "textField", label);
            checkBox.AddChild(button, 0, 0);
            checkBox.AddChild(label, 18, 4);

            for (int cycle = 0; cycle < 3; ++cycle)
            {
                checkBox.Displayed = true;
                checkBox.Draw();
                Assert.IsTrue(button.Displayed, $"Cycle {cycle}: button");
                Assert.IsTrue(label.Displayed, $"Cycle {cycle}: label");
                Assert.IsTrue(sprite.Visible, $"Cycle {cycle}: sprite");

                checkBox.Displayed = false;
                Assert.IsFalse(button.Displayed);
                Assert.IsFalse(label.Displayed);
                Assert.IsFalse(sprite.Visible);
            }
        }

        static T CreateControl<T>() where T : GuiObject
        {
            var control = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
            SetField(typeof(GuiObject), control, "children", new List<GuiObject>());
            return control;
        }

        static void SetField(Type type, object instance, string name, object value)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, name);
            field.SetValue(instance, value);
        }

        public class RenderProxy : DispatchProxy
        {
            readonly Dictionary<string, object> properties = new Dictionary<string, object>();

            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name.StartsWith("set_", StringComparison.Ordinal))
                {
                    properties[method.Name.Substring(4)] = args[0];
                    return null;
                }

                if (method.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    if (properties.TryGetValue(method.Name.Substring(4), out var value))
                        return value;
                    return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
                }

                if (method.Name == nameof(IRenderView.GetLayer))
                    return null;

                throw new NotSupportedException(method.Name);
            }
        }
    }
}
