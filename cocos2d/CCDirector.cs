using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace cocos2d
{
    internal interface CCDirectorDelegate
    {
        void UpdateProjection();
    }

    /// <summary>
    /// Class that creates and handle the main Window and manages how and when to execute the Scenes.
    /// 
    /// The CCDirector is also responsible for:
    ///     - initializing the OpenGL context
    ///     - setting the OpenGL pixel format (default on is RGB565)
    ///     - setting the OpenGL buffer depth (default one is 0-bit)
    ///     - setting the projection (default one is 3D)
    ///     - setting the orientation (default one is Portrait)
    /// 
    /// Since the CCDirector is a singleton, the standard way to use it is by calling: _
    /// CCDirector::sharedDirector()->methodName();
    /// 
    /// The CCDirector also sets the default OpenGL context:
    ///     - GL_TEXTURE_2D is enabled
    ///     - GL_VERTEX_ARRAY is enabled
    ///     - GL_COLOR_ARRAY is enabled
    ///     - GL_TEXTURE_COORD_ARRAY is enabled.  
    /// </summary>

    public abstract class CCDirector : CCObject
    {
        private static CCDirector s_sharedDirector;

        private readonly float kDefaultFPS = 60f;
        private readonly List<CCScene> m_pobScenesStack = new List<CCScene>();
        private bool m_bNextDeltaTimeZero;
        private bool m_bPaused;
        protected bool m_bPurgeDirecotorInNextLoop; // this flag will be set to true in end()
        private bool m_bSendCleanupToScene;
        protected double m_dAnimationInterval;
        protected double m_dOldAnimationInterval;
        private ccDirectorProjection m_eProjection;
        private float m_fContentScaleFactor = 1.0f;
        private float m_fDeltaTime;
        internal CCSize m_obWinSizeInPixels;
        internal CCSize m_obWinSizeInPoints;
#if !PSM
        private CCAccelerometer m_pAccelerometer;
#endif
		private CCActionManager m_pActionManager;
        private CCKeypadDispatcher m_pKeypadDispatcher;
        private CCScene m_pNextScene;
        private CCNode m_pNotificationNode;

        private CCDirectorDelegate m_pProjectionDelegate;
        private CCScene m_pRunningScene;
        private CCScheduler m_pScheduler;
        private CCTouchDispatcher m_pTouchDispatcher;

        private bool m_bDisplayStats;
        private uint m_uFrames;
        private uint m_uTotalFrames;
        private float m_fAccumDt;
        private float m_fFrameRate;
        private float m_fSecondsPerFrame;

        private CCLabelTTF m_pFPSLabel;
        private CCLabelTTF m_pSPFLabel;
        private CCLabelTTF m_pDrawsLabel;

        public ccDirectorProjection Projection
        {
            get { return m_eProjection; }
            set
            {
                CCSize size = m_obWinSizeInPoints;

                DrawManager.SetViewPortInPoints(0, 0, (int)size.width, (int)size.height);

                switch (value)
                {
                    case ccDirectorProjection.kCCDirectorProjection2D:

                        
                        DrawManager.ProjectionMatrix = Matrix.CreateOrthographicOffCenter(
                            0, size.width,
                            0, size.height,
                            -1024.0f, 1024.0f
                            );

                        DrawManager.ViewMatrix = Matrix.Identity;
                        DrawManager.WorldMatrix = Matrix.Identity;
                        
                        /*
                        DrawManager.ProjectionMatrix = Matrix.Identity;
                        Matrix projection = Matrix.CreateOrthographicOffCenter(0, size.width, 0, size.height, -1024.0f, 1024.0f);
				        Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
                        DrawManager.WorldMatrix = (halfPixelOffset * projection);
                        */
                        break;

                    case ccDirectorProjection.kCCDirectorProjection3D:

                        DrawManager.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                            MathHelper.Pi / 3.0f,
                            size.width / size.height,
                            0.1f, ZEye * 2f
                            );

                        DrawManager.ViewMatrix = Matrix.CreateLookAt(
                            new Vector3(size.width / 2.0f, size.height / 2.0f, ZEye),
                            new Vector3(size.width / 2.0f, size.height / 2.0f, 0f),
                            Vector3.Up
                            );

                        DrawManager.WorldMatrix = Matrix.Identity;

                        /*
                        DrawManager.ProjectionMatrix = Matrix.Identity;
                        DrawManager.ViewMatrix = Matrix.Identity;
                        DrawManager.WorldMatrix = Matrix.Identity;


                        Matrix projection1 = Matrix.CreateOrthographicOffCenter(0, size.width, 0, size.height, -1024.0f, 1024.0f);
				        Matrix halfPixelOffset1 = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
                        DrawManager.WorldMatrix = (halfPixelOffset1 * projection1);
                        */

                        break;

                    case ccDirectorProjection.kCCDirectorProjectionCustom:
                        if (m_pProjectionDelegate != null)
                        {
                            m_pProjectionDelegate.UpdateProjection();
                        }
                        break;

                    default:
                        Debug.Assert(true, "cocos2d: Director: unrecognized projecgtion");
                        break;
                }

                m_eProjection = value;
            }
        }

        public float ZEye
        {
            get { return (m_obWinSizeInPoints.height / 1.1566f); }
        }

        public CCSize WinSizeInPixels
        {
            get { return m_obWinSizeInPixels; }
        }

        public CCSize VisibleSize
        {
            get { return DrawManager.VisibleSize; }
        }

        public CCPoint VisibleOrigin
        {
            get { return DrawManager.VisibleOrigin; }
        }

        public CCScheduler Scheduler
        {
            get { return m_pScheduler; }
            set { m_pScheduler = value; }
        }

        public CCActionManager ActionManager
        {
            get { return m_pActionManager; }
            set { m_pActionManager = value; }
        }

        public CCTouchDispatcher TouchDispatcher
        {
            get { return m_pTouchDispatcher; }
            set { m_pTouchDispatcher = value; }
        }

        public CCKeypadDispatcher KeypadDispatcher
        {
            get { return m_pKeypadDispatcher; }
            set { m_pKeypadDispatcher = value; }
        }

#if !PSM
		public CCAccelerometer Accelerometer
        {
            get { return m_pAccelerometer; }
            set { m_pAccelerometer = value; }
        }
#endif
        public CCScene RunningScene
        {
            get { return m_pRunningScene; }
        }

        public virtual double AnimationInterval
        {
            get { return m_dAnimationInterval; }
            set { m_dAnimationInterval = value; }
        }

        public bool DisplayStats
        {
            get { return m_bDisplayStats; }
            set { m_bDisplayStats = value; }
        }

        public bool IsPaused
        {
            get { return m_bPaused; }
        }

        public CCNode NotificationNode
        {
            get { return m_pNotificationNode; }
            set { m_pNotificationNode = value; }
        }


        /// <summary>
        /// returns a shared instance of the director
        /// </summary>
        /// <value> </value>
        public static CCDirector SharedDirector
        {
            get
            {
                if (s_sharedDirector == null)
                {
                    s_sharedDirector = new CCDisplayLinkDirector();
                    s_sharedDirector.Init();
                }
                return s_sharedDirector;
            }
        }

        public virtual bool Init()
        {
            // scenes
            m_pRunningScene = null;
            m_pNextScene = null;

            m_pNotificationNode = null;

            m_dOldAnimationInterval = m_dAnimationInterval = 1.0 / kDefaultFPS;

            // Set default projection (3D)
            m_eProjection = ccDirectorProjection.kCCDirectorProjectionDefault;

            // projection delegate if "Custom" projection is used
            m_pProjectionDelegate = null;

            // FPS
            m_fAccumDt = 0.0f;
            m_fFrameRate = 0.0f;
            m_pFPSLabel = null;
            m_pSPFLabel = null;
            m_pDrawsLabel = null;
            m_bDisplayStats = false;
            m_uTotalFrames = m_uFrames = 0;

            // paused ?
            m_bPaused = false;

            // purge ?
            m_bPurgeDirecotorInNextLoop = false;

            m_obWinSizeInPixels = m_obWinSizeInPoints = CCSize.Zero;

            //m_pobOpenGLView = null;

            m_fContentScaleFactor = 1.0f;

            // scheduler
            m_pScheduler = new CCScheduler();
            // action manager
            m_pActionManager = new CCActionManager();
            m_pScheduler.ScheduleUpdateForTarget(m_pActionManager, CCScheduler.kCCPrioritySystem, false);
            // touchDispatcher
            m_pTouchDispatcher = new CCTouchDispatcher();
            m_pTouchDispatcher.Init();

            // KeypadDispatcher
            m_pKeypadDispatcher = new CCKeypadDispatcher();


#if !PSM
			// Accelerometer
            m_pAccelerometer = new CCAccelerometer();
#endif
            // create autorelease pool
            //CCPoolManager::sharedPoolManager()->push();

            return true;
        }

        internal void SetGlDefaultValues()
        {
            SetAlphaBlending(true);
            SetDepthTest(false);

            Projection = m_eProjection;

            // set other opengl default values
            //ClearColor = new Color(0, 0, 0, 255);
        }

        public void Update(GameTime gameTime)
        {
            if (!m_bPaused)
            {
                if (m_bNextDeltaTimeZero)
                {
                    m_fDeltaTime = 0;
                    m_bNextDeltaTimeZero = false;
                }
                else
                {
                    m_fDeltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
                }

#if DEBUG
                if (m_fDeltaTime > 0.2f)
                {
                    m_fDeltaTime = 1 / 60.0f;
                }
#endif

                m_pScheduler.update(m_fDeltaTime);
            }

            /* to avoid flickr, nextScene MUST be here: after tick and before draw.
             XXX: Which bug is this one. It seems that it can't be reproduced with v0.9 */
            if (m_pNextScene != null)
            {
                SetNextScene();
            }
        }

        /// <summary>
        /// Draw the scene.
        /// This method is called every frame. Don't call it manually.
        /// </summary>
        protected void DrawScene(GameTime gameTime)
        {
            DrawManager.PushMatrix();

            // draw the scene
            if (m_pRunningScene != null)
            {
                m_pRunningScene.Visit();
            }

            // draw the notifications node
            if (m_pNotificationNode != null)
            {
                NotificationNode.Visit();
            }

            if (m_bDisplayStats)
            {
                ShowStats();
            }

            DrawManager.PopMatrix();

            m_uTotalFrames++;

            if (m_bDisplayStats)
            {
                CalculateMPF();
            }
        }


        public abstract void MainLoop(GameTime gameTime);

        public void SetOpenGlView()
        {
            // set size
            m_obWinSizeInPoints = DrawManager.Size;
            m_obWinSizeInPixels = new CCSize(m_obWinSizeInPoints.width * m_fContentScaleFactor,
                                             m_obWinSizeInPoints.height * m_fContentScaleFactor);

            //createStatsLabel();

            SetGlDefaultValues();

            CCApplication.SharedApplication.TouchDelegate = m_pTouchDispatcher;
            m_pTouchDispatcher.IsDispatchEvents = true;
        }

        public void SetNextDeltaTimeZero(bool bNextDeltaTimeZero)
        {
            m_bNextDeltaTimeZero = bNextDeltaTimeZero;
        }

        public void PurgeCachedData()
        {
            CCLabelBMFont.PurgeCachedData();
            CCTextureCache.SharedTextureCache.RemoveUnusedTextures();
            //CCFileUtils::sharedFileUtils()->purgeCachedEntries();
        }

        /// <summary>
        /// enables/disables OpenGL alpha blending 
        /// </summary>
        /// <param name="bOn"></param>
        public void SetAlphaBlending(bool bOn)
        {
            if (bOn)
            {
                //glEnable(GL_BLEND);
                //glBlendFunc(CC_BLEND_SRC, CC_BLEND_DST);
                //DrawManager.BlendFunc(new ccBlendFunc(ccMacros.CC_BLEND_SRC, ccMacros.CC_BLEND_DST));
            }
            else
            {
                //glDisable(GL_BLEND);
            }
        }

        /// <summary>
        /// enables/disables OpenGL depth test
        /// </summary>
        /// <param name="bOn"></param>
        public void SetDepthTest(bool bOn)
        {
            DrawManager.DepthTest = bOn;
        }

        public CCPoint ConvertToGl(CCPoint uiPoint)
        {
            return new CCPoint(uiPoint.x, m_obWinSizeInPoints.height - uiPoint.y);
        }

        public CCPoint ConvertToUi(CCPoint glPoint)
        {
            return new CCPoint(glPoint.x, m_obWinSizeInPoints.height - glPoint.y);
        }

        public CCSize WinSize
        {
            get { return m_obWinSizeInPoints; }
        }

        public void ReshapeProjection(CCSize newWindowSize)
        {
            m_obWinSizeInPoints = DrawManager.Size;
            m_obWinSizeInPixels = new CCSize(m_obWinSizeInPoints.width * m_fContentScaleFactor,
                                             m_obWinSizeInPoints.height * m_fContentScaleFactor);

            Projection = m_eProjection;
        }

        public void End()
        {
            m_bPurgeDirecotorInNextLoop = true;
        }

        protected void PurgeDirector()
        {
            // cleanup scheduler
            Scheduler.UnscheduleAllSelectors();

            // don't release the event handlers
            // They are needed in case the director is run again
            m_pTouchDispatcher.RemoveAllDelegates();

            if (m_pRunningScene != null)
            {
                m_pRunningScene.OnExit();
                m_pRunningScene.Cleanup();
            }

            m_pRunningScene = null;
            m_pNextScene = null;

            // remove all objects, but don't release it.
            // runWithScene might be executed after 'end'.
            m_pobScenesStack.Clear();

            StopAnimation();

            // purge bitmap cache
            CCLabelBMFont.PurgeCachedData();

            // purge all managed caches
            CCAnimationCache.PurgeSharedAnimationCache();
            CCSpriteFrameCache.PurgeSharedSpriteFrameCache();
            CCTextureCache.PurgeSharedTextureCache();
            //CCFileUtils.purgeFileUtils();
            //CCConfiguration.purgeConfiguration();

            // cocos2d-x specific data structures
            //CCUserDefault.purgeSharedUserDefault();
            //CCNotificationCenter.purgeNotificationCenter();
        }

        public void Pause()
        {
            if (m_bPaused)
            {
                return;
            }

            m_dOldAnimationInterval = m_dAnimationInterval;

            // when paused, don't consume CPU
            AnimationInterval = 1 / 4.0;
            m_bPaused = true;
        }

        public void Resume()
        {
            if (!m_bPaused)
            {
                return;
            }

            AnimationInterval = m_dOldAnimationInterval;

            m_bPaused = false;
            m_fDeltaTime = 0;
        }

        public bool IsSendCleanupToScene()
        {
            return m_bSendCleanupToScene;
        }


        public uint Frames
        {
            get { return m_uFrames; }
        }

        public abstract void StopAnimation();

        public abstract void StartAnimation();

        #region mobile platforms specific functions

        public float ContentScaleFactor
        {
            get { return m_fContentScaleFactor; }
            set
            {
                if (value != m_fContentScaleFactor)
                {
                    m_fContentScaleFactor = value;
                }
            }
        }

        #endregion

        #region Scene Management

        public void RunWithScene(CCScene pScene)
        {
            Debug.Assert(pScene != null, "pScene cannot be null");
            Debug.Assert(m_pRunningScene == null, "m_pRunningScene cannot be null");

            PushScene(pScene);
            StartAnimation();
        }

        public void ReplaceScene(CCScene pScene)
        {
            Debug.Assert(pScene != null, "pScene cannot be null");

            int index = m_pobScenesStack.Count;

            m_bSendCleanupToScene = true;
            m_pobScenesStack[index - 1] = pScene;

            m_pNextScene = pScene;
        }

        public void PushScene(CCScene pScene)
        {
            Debug.Assert(pScene != null, "pScene cannot be null");

            m_bSendCleanupToScene = false;

            m_pobScenesStack.Add(pScene);
            m_pNextScene = pScene;
        }

        public void PopScene()
        {
            Debug.Assert(m_pRunningScene != null, "m_pRunningScene cannot be null");

            if (m_pobScenesStack.Count > 0)
            {
                m_pobScenesStack.RemoveAt(m_pobScenesStack.Count - 1);
            }
            int c = m_pobScenesStack.Count;

            if (c == 0)
            {
                End();
            }
            else
            {
                m_bSendCleanupToScene = true;
                m_pNextScene = m_pobScenesStack[c - 1];
            }
        }

        public void PopToRootScene()
        {
            Debug.Assert(m_pRunningScene != null, "A running Scene is needed");
            int c = m_pobScenesStack.Count;

            if (c == 1)
            {
                m_pobScenesStack.RemoveAt(0);
                End();
            }
            else
            {
                while (c > 1)
                {
                    CCScene current = m_pobScenesStack[m_pobScenesStack.Count - 1];
                    if (current.IsRunning)
                    {
                        current.OnExit();
                    }
                    current.Cleanup();

                    m_pobScenesStack.RemoveAt(m_pobScenesStack.Count - 1);
                    c--;
                }
                m_pNextScene = m_pobScenesStack[m_pobScenesStack.Count - 1];
                m_bSendCleanupToScene = false;

                GC.Collect();
            }
        }

        protected void SetNextScene()
        {
            bool runningIsTransition = m_pRunningScene is CCTransitionScene;

            // If it is not a transition, call onExit/cleanup
            if (!(m_pNextScene is CCTransitionScene))
            {
                if (m_pRunningScene != null)
                {
                    m_pRunningScene.OnExit();
                }

                // issue #709. the root node (scene) should receive the cleanup message too
                // otherwise it might be leaked.
                if (m_bSendCleanupToScene && m_pRunningScene != null)
                {
                    m_pRunningScene.Cleanup();
                    
                    GC.Collect();
                }
            }

            m_pRunningScene = m_pNextScene;
            m_pNextScene = null;

            if (!runningIsTransition && m_pRunningScene != null)
            {
                m_pRunningScene.OnEnter();
                m_pRunningScene.OnEnterTransitionDidFinish();
            }
        }

        #endregion

        private void CalculateMPF()
        {
//            var now = DateTime.Now.To;
//            struct cc_timeval now;
//            CCTime::gettimeofdayCocos2d(&now, NULL);
//    
//            m_fSecondsPerFrame = now.;
        }

        public void CreateStatsLabel()
        {
            int fontSize = (int) (m_obWinSizeInPoints.height / 320.0f * 24);
            m_pFPSLabel = CCLabelTTF.Create("00.0", "Arial", 24);
            m_pFPSLabel.Scale = m_obWinSizeInPoints.height / 320.0f; // Use 320 here b/c we are optimizing at that scale.
            m_pSPFLabel = CCLabelTTF.Create("0.000", "Arial", 24);
            m_pSPFLabel.Scale = m_obWinSizeInPoints.height / 320.0f;
            m_pDrawsLabel = CCLabelTTF.Create("000", "Arial", 24);
            m_pDrawsLabel.Scale = m_obWinSizeInPoints.height / 320.0f;

            //CCTexture2D::setDefaultAlphaPixelFormat(currentFormat);

            var pos = CCDirector.SharedDirector.VisibleOrigin;

            CCSize contentSize = m_pDrawsLabel.ContentSize;
            m_pDrawsLabel.Position = new CCPoint(contentSize.width / 2, contentSize.height * 5 / 2) + pos;
            contentSize = m_pSPFLabel.ContentSize;
            m_pSPFLabel.Position = new CCPoint(contentSize.width / 2, contentSize.height * 3 / 2) + pos;
            contentSize = m_pFPSLabel.ContentSize;
            m_pFPSLabel.Position = new CCPoint(contentSize.width / 2, contentSize.height / 2) + pos;
        }

        // display the FPS using a LabelAtlas
        // updates the FPS every frame
        private void ShowStats()
        {
            m_uFrames++;
            m_fAccumDt += m_fDeltaTime;
    
            if (m_bDisplayStats)
            {
                if (m_pFPSLabel != null && m_pSPFLabel != null && m_pDrawsLabel != null)
                {
                    if (m_fAccumDt > ccMacros.CC_DIRECTOR_STATS_INTERVAL)
                    {
                        m_pSPFLabel.SetString(String.Format("{0:0.000}", m_fSecondsPerFrame));
                
                        m_fFrameRate = m_uFrames / m_fAccumDt;
                        m_uFrames = 0;
                        m_fAccumDt = 0;
                
                        m_pFPSLabel.SetString(String.Format("{0:00.0}", m_fFrameRate));
                
                        m_pDrawsLabel.SetString(String.Format("{0:000}", DrawManager.DrawCount));
                    }
            
                    m_pDrawsLabel.Visit();
                    m_pFPSLabel.Visit();
                    m_pSPFLabel.Visit();
                }
            }    
        }
    }

    /// <summary>
    ///  Possible OpenGL projections used by director
    /// </summary>
    public enum ccDirectorProjection
    {
        /// sets a 2D projection (orthogonal projection)
        kCCDirectorProjection2D,

        /// sets a 3D projection with a fovy=60, znear=0.5f and zfar=1500.
        kCCDirectorProjection3D,

        /// it calls "updateProjection" on the projection delegate.
        kCCDirectorProjectionCustom,

        /// Default projection is 3D projection
        kCCDirectorProjectionDefault = kCCDirectorProjection3D
    }
}