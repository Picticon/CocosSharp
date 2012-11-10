using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cocos2d;
using Random = cocos2d.Random;

namespace tests
{
    public class SpriteBatchNodeNewTexture : SpriteTestDemo
    {
        CCTexture2D m_texture1;
        CCTexture2D m_texture2;

        public SpriteBatchNodeNewTexture()
        {
            TouchEnabled = true;

            CCSpriteBatchNode batch = CCSpriteBatchNode.Create("Images/grossini_dance_atlas", 50);
            AddChild(batch, 0, (int)kTags.kTagSpriteBatchNode);

            m_texture1 = batch.Texture;
            m_texture2 = CCTextureCache.SharedTextureCache.AddImage("Images/grossini_dance_atlas-mono");

            for (int i = 0; i < 30; i++)
                addNewSprite();
        }

        public void addNewSprite()
        {
            CCSize s = CCDirector.SharedDirector.WinSize;

            CCPoint p = new CCPoint((float)(Random.NextDouble() * s.width), (float)(Random.NextDouble() * s.height));

            CCSpriteBatchNode batch = (CCSpriteBatchNode)GetChildByTag((int)kTags.kTagSpriteBatchNode);

            int idx = (int)(Random.NextDouble() * 1400 / 100);
            int x = (idx % 5) * 85;
            int y = (idx / 5) * 121;


            CCSprite sprite = CCSprite.Create(batch.Texture, new CCRect(x, y, 85, 121));
            batch.AddChild(sprite);

            sprite.Position = (new CCPoint(p.x, p.y));

            CCActionInterval action;
            float random = (float)Random.NextDouble();

            if (random < 0.20)
                action = CCScaleBy.Create(3, 2);
            else if (random < 0.40)
                action = CCRotateBy.Create(3, 360);
            else if (random < 0.60)
                action = CCBlink.Create(1, 3);
            else if (random < 0.8)
                action = CCTintBy.Create(2, 0, -255, -255);
            else
                action = CCFadeOut.Create(2);
            CCActionInterval action_back = (CCActionInterval)action.Reverse();
            CCActionInterval seq = (CCActionInterval)(CCSequence.Create(action, action_back));

            sprite.RunAction(CCRepeatForever.Create(seq));
        }

        public override void TouchesEnded(List<CCTouch> touches, CCEvent event_)
        {
            CCSpriteBatchNode batch = (CCSpriteBatchNode)GetChildByTag((int)kTags. kTagSpriteBatchNode);

            if (batch.Texture== m_texture1)
                batch.Texture=m_texture2;
            else
                batch.Texture=m_texture1;   
            //base.ccTouchesEnded(touches, event_);
        }

        public override string title()
        {
            return "SpriteBatchNode new texture (tap)";
        }
    }
}