using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CocosSharp;
using Random = CocosSharp.CCRandom;

namespace tests
{
    public class SpriteBatchNode1 : SpriteTestDemo
    {
        #region Properties

        public override string Title
        {
            get { return "Testing SpriteBatchNode"; }
        }

        public override string Subtitle
        {
            get
            {
                return "Tap screen to add more sprites";
            }
        }
        #endregion Properties


        #region Constructors

        public SpriteBatchNode1()
        {
            CCSpriteBatchNode BatchNode = new CCSpriteBatchNode("Images/grossini_dance_atlas", 50);
            AddChild(BatchNode, 0, (int)kTags.kTagSpriteBatchNode);

            // Register Touch Event
            var touchListener = new CCEventListenerTouchAllAtOnce();
            touchListener.OnTouchesEnded = OnTouchesEnded;

            AddEventListener(touchListener);
        }

        #endregion Constructors


        #region Setup content

        public override void OnEnter()
        {
            base.OnEnter();
            AddNewSpriteWithCoords(VisibleBoundsWorldspace.Center);
        }

        #endregion Setup content


        void AddNewSpriteWithCoords(CCPoint p)
        {
            CCSpriteBatchNode BatchNode = (CCSpriteBatchNode)this[(int)kTags.kTagSpriteBatchNode];

            int idx = (int)(CCRandom.NextDouble() * 1400 / 100);
            int x = (idx % 5) * 85;
            int y = (idx / 5) * 121;


            CCSprite sprite = new CCSprite(BatchNode.Texture, new CCRect(x, y, 85, 121));
            sprite.Position = (new CCPoint(p.X, p.Y));
            BatchNode.AddChild(sprite);


            CCFiniteTimeAction action = null;
            float random = (float)CCRandom.NextDouble();

            if (random < 0.20)
                action = new CCScaleBy(3, 2);
            else if (random < 0.40)
                action = new CCRotateBy (3, 360);
            else if (random < 0.60)
                action = new CCBlink (1, 3);
            else if (random < 0.8)
                action = new CCTintBy (2, 0, -255, -255);
            else
                action = new CCFadeOut  (2);

            CCFiniteTimeAction action_back = (CCFiniteTimeAction)action.Reverse();
            CCFiniteTimeAction seq = (CCFiniteTimeAction)(new CCSequence(action, action_back));

            sprite.RunAction(new CCRepeatForever (seq));
        }

        void OnTouchesEnded(List<CCTouch> touches, CCEvent touchEvent)
        {
            foreach (CCTouch item in touches)
            {
                if (item == null)
                {
                    break;
                }

                var location = Layer.ScreenToWorldspace(item.LocationOnScreen);

                AddNewSpriteWithCoords(location);
            }
        }
    }
}
