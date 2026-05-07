// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using Android.App;
using Freeserf;
using FreeserfAndroid;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.Android;
using Debug = System.Diagnostics.Debug;


namespace AndroidInputDemo
{
    /// <summary>
    /// Simple demo on how to use handle user input with Silk.
    /// </summary>
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : SilkActivity
    {
        // Instead of IWindow, we use IView. 
        // IWindow inherits IView, so you can also use this with your desktop code.
        private static IView view;

        /// <summary>
        /// This is where the application starts.
        /// Note that when using net6-android, you do not need to have a main method.
        /// </summary>
        protected override void OnRun()
        {
            FileManager.AssetManager = Assets;
            string[] args = [];
            view = MainView.Create(args).window;
            view.Run();
            view.Dispose();
        }
    }
}