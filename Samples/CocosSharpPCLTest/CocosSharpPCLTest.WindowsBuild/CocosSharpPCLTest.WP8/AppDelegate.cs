using System.Reflection;
using Microsoft.Xna.Framework;
using CocosSharp;
using CocosDenshion;

namespace CocosSharpPCLTest
{
    public class AppDelegate : CCApplication
    {
        public static string PlatformMessage()
        {
            return "From Windows Phone 8 - One PCL to rule them all.";
        }

        public AppDelegate(Game game, GraphicsDeviceManager graphics = null)
            : base(game, graphics)
        {

            SupportedOrientations = CCDisplayOrientation.LandscapeRight | CCDisplayOrientation.LandscapeLeft;
            AllowUserResizing = true;
            PreferMultiSampling = false;
            PreferredBackBufferWidth = 960;
            PreferredBackBufferHeight = 640;
        }

        public override bool ApplicationDidFinishLaunching()
        {
            ContentRootDirectory = "Content";

            CCSpriteFontCache.FontScale = 0.6f;
            CCSpriteFontCache.RegisterFont("MarkerFelt", 22);

            CCDirector director = CCDirector.SharedDirector;
            director.DisplayStats = true;
            director.AnimationInterval = 1.0 / 60;

            CCSize designSize = new CCSize(480, 320);

            if (CCDrawManager.FrameSize.Height > 320)
            {
                CCSize resourceSize = new CCSize(960, 640);
                ContentSearchPaths.Add("hd");
                director.ContentScaleFactor = resourceSize.Height / designSize.Height;
            }

            CCDrawManager.SetDesignResolutionSize(designSize.Width, designSize.Height, CCResolutionPolicy.ShowAll);

            CCScene scene = new CCScene();

            var label = TestClass.PCLLabel(AppDelegate.PlatformMessage());

            scene.AddChild(label);

            director.RunWithScene(scene);

            return true;
        }
    }
}