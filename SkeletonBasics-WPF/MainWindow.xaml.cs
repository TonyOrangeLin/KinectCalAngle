//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;   //for Math
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging; // forScreenShot
    using Microsoft.Kinect;
    using System.Collections.Generic;  //for list
    using System.Linq;   //for Math.average
    using Visifire.Charts; //for chart
    using System.Windows.Controls;// for grid
    using System.Windows.Input;  // for 点击事件
    using System.Windows.Forms; //timer
    using System.Windows.Shapes;

    using System.Threading;
    using System.Globalization; // For drawtext
    using System.Windows.Media.Media3D; //for Vector3D,Point3D

    using Microsoft.Kinect.Toolkit;
    using System.Windows.Data;


    using Word = Microsoft.Office.Interop.Word;
    using System.Reflection;

    public partial class MainWindow : Window
    {
        private const double AnticipatedUU = 60;
        private const double AnticipatedVV = 140;
        int cntemp;
        int ReduceFaceTrackingLoading = 0;
        int CatchSuccess = 0;
        System.Windows.Forms.Timer aTimer, pTimer;
        double FootCenterXin3D, FootCenterXin2D, FootCenterYin2D;
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;
        private const double JointThickness = 3;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;
        private const double NumbersOfTarget = 38;
       

        private KinectSensor sensor;
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        private WriteableBitmap colorBitmap;
        private ImageBrush ColorBackground = new ImageBrush();
        private byte[] colorPixels;
        private DepthImagePixel[] depthPixels;
        private short[] depthShortPixels;
        private DepthImagePixel[] depth4background;
        private int[] boolPixels;
        private Skeleton[] skeletons = new Skeleton[0];               //宣告
        SkeletonPoint[] ColorInSkeleton;

        #region Pen and Brush
        private readonly Pen PenGreen = new Pen(Brushes.Green, 1);
        private readonly Pen PenGray = new Pen(Brushes.Gray, 1);
        private readonly Pen PenRed = new Pen(Brushes.Red, 2);
        private readonly Pen PenYellow = new Pen(Brushes.Yellow, 2);
        private readonly Pen PenDeepSkyBlue = new Pen(Brushes.DeepSkyBlue, 2);
        private readonly Pen PenDarkBlue = new Pen(Brushes.DarkBlue, 2);
        private readonly Pen PenLightGreen = new Pen(Brushes.LightGreen, 2);

        private readonly Brush brushWhite = Brushes.White;
        private readonly Brush brushRed = Brushes.Red;
        private readonly Brush brushOrange = Brushes.Orange;
        private readonly Brush brushYellow = Brushes.Yellow;
        private readonly Brush brushGreen = Brushes.Green;
        private readonly Brush brushDeepSkyBlue = Brushes.DeepSkyBlue;
        private readonly Brush brushDarkBlue = Brushes.DarkBlue;
        private readonly Brush brushPurple = Brushes.Purple;
        private readonly Brush brushGreenYellow = Brushes.GreenYellow;
        private readonly Brush brushChocolate = Brushes.Chocolate;
        private readonly Brush brushDarkGray = Brushes.DarkGray;
        private readonly Brush brushLightGreen = Brushes.LightGreen;

        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        #endregion


        AngleMaxmin temp = new AngleMaxmin();
        AngleMaxmin headspin = new AngleMaxmin();
        AngleMaxmin headtilt = new AngleMaxmin();
        AngleMaxmin shoulderspin = new AngleMaxmin();
        AngleMaxmin shouldertilt = new AngleMaxmin();
        AngleMaxmin spinespin = new AngleMaxmin();
        AngleMaxmin spinetilt = new AngleMaxmin();
        AngleMaxmin leftheadtilt = new AngleMaxmin();
        AngleMaxmin leftshouldertilt = new AngleMaxmin();
        AngleMaxmin leftspinetilt = new AngleMaxmin();
        StandardDifference SD = new StandardDifference();         
       
        private PsudeoInverse A67 =  new PsudeoInverse();
        private PsudeoInverse A70 = new PsudeoInverse();
        private PsudeoInverse A73 = new PsudeoInverse();
        private PsudeoInverse A76 = new PsudeoInverse();
        private PsudeoInverse A79 = new PsudeoInverse();
        private PsudeoInverse A82 = new PsudeoInverse();
        private PsudeoInverse A85 = new PsudeoInverse();
        private PsudeoInverse A88 = new PsudeoInverse();
        private PsudeoInverse A91 = new PsudeoInverse();

        private AngleCalulator AngleCal = new AngleCalulator();
        private List<Target> TargetList = new List<Target>();
        private List<Double> Data = new List<Double>() { };
        private List<List<double>> DataList = new List<List<double>>() { };
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private int TrackX = 0;
        private int TrackY = 0;
        private Boolean triggerFlag = false;
        private double prevangle = 0;
        private double angleCount = 0;
        private double angleSumUp = 0;
        private double angleSumDown = 0;
        bool isUpFirstTime = false;
        bool isDownFirstTime = false;
        System.Timers.Timer AngleTimer;
        bool isSignShow = false;
        int previousR = 0;
        byte previousG = 0;
        byte previousB = 0;
        bool isFirstTime = true;

        public MainWindow()
        {
            InitializeComponent();            
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //SplashScreen splashScreen = new SplashScreen("Images/SplashScreenImage.bmp");
            //splashScreen.Show(true);

            this.drawingGroup = new DrawingGroup();                         // Create the drawing group we'll use for drawing
            this.imageSource = new DrawingImage(this.drawingGroup);         // Create an image source that we can use in our image control
            Image.Source = this.imageSource;                                // Display the drawing using our image control

            for (int t = 0; t <= NumbersOfTarget; t++)
            {
                Target target = new Target(t);
                TargetList.Add(target);
            }

            for (int d = 0; d <= 93; d++)   //指標特徵運算A1~A66
            {
                List<double> array = new List<double>() { };
                DataList.Add(array);
                Data.Add(0);
            }

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)        //Initailization
            {
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.SkeletonStream.Enable();

                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.depthShortPixels = new short[this.sensor.DepthStream.FramePixelDataLength];
                this.boolPixels = new int[this.sensor.DepthStream.FramePixelDataLength];
                this.sensor.AllFramesReady += this.SensorAllFramesReady;

                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.depth4background = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }

            AngleTimer = new System.Timers.Timer(1000);
            AngleTimer.Elapsed += new System.Timers.ElapsedEventHandler(theout);
            AngleTimer.AutoReset = true;
            AngleTimer.Enabled = true;
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();                
                faceTrackingViewer.Dispose();
            }
        }

        public int RGBtoH(byte r, byte g, byte b)
        {
            int delta;
            int h;
            byte temp = Math.Max(r, g);
            byte max = Math.Max(b, temp);
            byte min;
            temp = Math.Min(r, g);
            min = Math.Min(temp, b);

            delta = max - min;
            if (max != 0)
            {

            }
            else
            {
                return -1;
            }
            if (r == max)
            {
                h = (g - b) / delta;		// between yellow & magenta
            }
            else if (g == max)
            {
                h = 2 + (b - r) / delta;	// between cyan & yellow
            }
            else
            {
                h = 4 + (r - g) / delta;	// between magenta & cyan
            }
            h *= 60;				// degrees
            if (h < 0)
            {
                h += 360;
            }
            return h;
        }

        public int RGBtoV(byte r, byte g, byte b)
        {
            int delta;
            int v;
            byte temp = Math.Max(r, g);
            byte max = Math.Max(b, temp);
            byte min;
            temp = Math.Min(r, g);
            min = Math.Min(temp, b);
            v = max;
            return v;
        }

        public int AverageAll(byte[] pixels)
        {
            int sum = 0;
            int avaerageNumber = 0 ;
            for (int i = -5; i < 5; i++)
            {
                for (int j = -5; j < 5; j++)
                {
                    int index = TrackX + i + (TrackY + j) * 640 + 2;
                    if (index >= 0 && index < 640 * 480)
                    {
                        sum += colorPixels[index];
                        avaerageNumber++;
                    }
                }
            }
            return sum / avaerageNumber;    

        }

        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (null == this.sensor) return;

          

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);
                    depthFrame.CopyPixelDataTo(this.depthShortPixels);
                }
            }

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    ColorInSkeleton = ColorToSkeleton();  //ColorPoint to SkeletonPoint  必須每個frame都作!!
                    
                    #region mapping test
                    //for (int i = 0; i < 640; i++)
                    //{
                    //    for (int j = 0; j < 480; j++)
                    //    {
                    //        int di = (i + j * 640);   //di = depthpixel index                 
                    //        int ci = di * 4;          //ci = colorPixel index            

                    //        if (ColorInSkeleton[di].Z < 1.2 && ColorInSkeleton[di].Z > 0.8)
                    //        {
                    //            colorPixels[ci + 0] = 255; // (byte)depthPixels[depthIndex].Depth; 
                    //            colorPixels[ci + 1] = 255;
                    //            colorPixels[ci + 2] = 255;
                    //        }

                    //    }
                    //}
                    #endregion
                  
                    for (int i = 1; i <= NumbersOfTarget; i++)
                    {
                        TargetList[i].Cal(this.colorPixels, depthPixels, ColorInSkeleton, depth4background, boolPixels); ;
                    }

                    //colorPixels[TrackX + TrackY * 640 + 2] = 0;
                    //colorPixels[TrackX + TrackY * 640 + 1] = 0;
                    //colorPixels[TrackX + TrackY * 640 ] = 0;
                    this.colorBitmap.WritePixels(   //將colorPixels寫入colorBitmap
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                    
                    ColorBackground = new ImageBrush(colorBitmap);  //將Bitmap轉成ImageBrush
                    if (isFirstTime)
                    {
                        byte rChannel = colorPixels[TrackX + TrackY * 640 + 2]; //R
                        previousR = rChannel; 
                        triggerFlag = false;
                        isFirstTime = false;
                        listBox1.Items.Add("test");
                    }
                    else
                    {
                        byte rChannel = colorPixels[TrackX + TrackY * 640 + 2]; //R
                        int averageR = AverageAll(colorPixels);
                        Console.WriteLine("Red:  " + averageR.ToString());

                        if (Math.Abs(previousR - averageR) > 5)
                        {
                            triggerFlag = true;
                            Console.WriteLine("TRUE");
                        }
                        else
                        {
                            //Console.WriteLine("FALSE");
                            triggerFlag = false;
                        }
                        previousR = averageR;
                    }
                }

            }

            #region SkeletonFrameReady
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {               
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);

                    ReduceFaceTrackingLoading++;
                    if (ReduceFaceTrackingLoading % 10 == 0)   //每秒只做3次
                    {
                        label26.Content = sensor.AccelerometerGetCurrentReading().X.ToString("f2") + "  "
                                        + sensor.AccelerometerGetCurrentReading().Y.ToString("f2") + "  "
                                        + sensor.AccelerometerGetCurrentReading().Z.ToString("f2");
                        faceTrackingViewer.FaceTrackFramesReady(sensor, colorPixels, depthShortPixels, skeletons, skeletonFrame);                        
                        //UpdateUiValue();
                        ReduceFaceTrackingLoading = 0;
                    }
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                dc.DrawRectangle(ColorBackground, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));    //color貼到骨架上                
                if (triggerFlag)
                {
                    //brushWhite
                    dc.DrawRectangle(brushRed, null, new Rect(TrackX - 5, TrackY - 5, 10, 10)); 
                }
                else
                {
                    dc.DrawRectangle(brushWhite, null, new Rect(TrackX - 5, TrackY - 5, 10, 10)); 
                }
                //dc.DrawRectangle(brushGreenYellow, null, new Rect(TrackX - 10, TrackY - 10, 20, 20)); 
               // AngleCal.Length(dc, TargetList[25], TargetList[26]);
                angleCount = AngleCal.Hor(dc, TargetList[25], TargetList[26], true);
                //prevangle = 0;

                if (angleCount < 0)
                {
                    Console.Out.WriteLine("LOWER");
                    //手在下半部
                    if (angleCount < prevangle)
                    {
                        prevangle = angleCount;
                        //手往下擺
                        //Console.Out.WriteLine("HAND DOWN");
                        isDownFirstTime = true;
                    }
                    else if (angleCount == prevangle)
                    {
                        //Console.Out.WriteLine("HAND STILL");
                    }
                    else
                    {
                        //Console.Out.WriteLine("HAND UP");
                        if (isDownFirstTime)
                        {
                            angleSumDown = Math.Abs(prevangle);
                            Console.Out.WriteLine("下半部角度:" + angleSumDown.ToString());
                            isDownFirstTime = false;
                        }
                    }
                }
                else if (angleCount >= 0)
                {
                    //Console.Out.WriteLine("UPPER");
                    //手在上半部
                    if (angleCount > prevangle)
                    {
                        //手往上抬
                        prevangle = angleCount;
                        //Console.Out.WriteLine("HAND UP");
                        isUpFirstTime = true;
                    }
                    else if (angleCount == prevangle)
                    {
                        //Console.Out.WriteLine("HAND STILL");
                    }
                    else
                    {
                        //angleSumUp = angleCount;
                        //Console.Out.WriteLine("HAND DOWN");
                        if (isUpFirstTime)
                        {
                            angleSumDown = Math.Abs(prevangle);
                            Console.Out.WriteLine("上半部角度:" + angleSumDown.ToString());
                            isUpFirstTime = false;
                        }
                    }
                }
                
                

                for (int t = 1; t <= NumbersOfTarget; t++)
                {
                    TargetList[t].LabelTarget(dc, brushRed, checkBoxShowXYZ.IsChecked == true);
                }

                //if (skeletons.Length != 0)
                //{
                    
                //    foreach (Skeleton skel in skeletons)
                //    {
                //        RenderClippedEdges(skel, dc);
                //        LockClosestSkeletons(skel);
                       
                //        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                //        {
                           
                //            this.DrawBonesAndJoints(skel, dc);  //**姿勢判定寫在這裡                           
                //            this.CalVerticalLineAndPts(skel ,dc);                            

                //            if (skel.Position.Z > 2.3)
                //                statusBarText.Text = "請往前移動" + (skel.Position.Z - 2.3).ToString("f2") + "公尺，以達到最佳量測距離";
                //            else if (skel.Position.Z < 2.2)
                //                statusBarText.Text = "請往後移動" + (2.2 - skel.Position.Z).ToString("f2") + "公尺，以達到最佳量測距離";
                //        }

                //        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                //        {
                //            dc.DrawEllipse(this.centerPointBrush, null, this.SkeletonPointToScreen(skel.Position), BodyCenterThickness, BodyCenterThickness);
                //        }
                //    }                
                //}
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
            #endregion
        }

        public void theout(object source, System.Timers.ElapsedEventArgs e) 
        {
            if (triggerFlag)
            {
                triggerFlag = false;
               
            }

        }
  
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)       //**姿態判定寫在這裡
        {           
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);



            // Right Leg
            bool KneeRightNotTracked = false,
                 HipRightNotTracked = false,               
                 KneeLeftNotTracked = false,
                 HipLeftNotTracked = false,
                 HandRightNotTracked = false,
                 ElbowRightNotTracked = false,
                 HandLeftNotTracked = false,
                 ElbowLeftNotTracked = false,
                 AllTracked = true;

            //label24.Content = AngleCal.CalSkelVector(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ShoulderRight]);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                    AllTracked = false;
                    // Right Leg
                    if (joint.JointType == JointType.HipRight) HipRightNotTracked = true;
                    if (joint.JointType == JointType.KneeRight) KneeRightNotTracked = true;
                    if (joint.JointType == JointType.HipLeft) HipLeftNotTracked = true;
                    if (joint.JointType == JointType.KneeLeft) KneeLeftNotTracked = true;
                    if (joint.JointType == JointType.HandRight) HandRightNotTracked = true;
                    if (joint.JointType == JointType.ElbowRight) ElbowRightNotTracked = true;
                    if (joint.JointType == JointType.HandLeft) HandLeftNotTracked = true;
                    if (joint.JointType == JointType.ElbowLeft) ElbowLeftNotTracked = true;
                }

               // //if (drawBrush != null )
                //if (drawBrush != null && RadioLeftLateral.IsChecked == false) //側面不畫關節點    
                //{
                //    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                //}
            }

            //只要有抓到face就視為正面
            if (FaceTrackingViewer.isFaceTracked)  
            {
               // RadioAnterior.IsChecked = true;
                statusBarText.Text = "正面追蹤中...";
                if (pTimer != null) pTimer.Stop();
            }

            //若只有face沒追到，則認為此round的joints是背面
            if (!FaceTrackingViewer.isFaceTracked && AllTracked)
            {
                if (pTimer == null || !pTimer.Enabled)
                {
                    //pTimer = new System.Windows.Forms.Timer();
                    //pTimer.Interval = 3000;
                    //pTimer.Tick += new EventHandler(pTimerTick);
                    //pTimer.Start();
                }
            }

            //若有這些關節沒追到，則認為此round的joints是側面
            //if (!FaceTrackingViewer.isFaceTracked
            //&& (HipRightNotTracked == true || KneeRightNotTracked == true   // Right Leg 
            //|| HipLeftNotTracked == true || KneeLeftNotTracked == true))
            //|| HandRightNotTracked == true || HandLeftNotTracked == true 
          //  || ElbowRightNotTracked == true|| ElbowLeftNotTracked == true)  // Left Leg              
            //{
                //RadioLeftLateral.IsChecked = true;
              //  statusBarText.Text = "側面追蹤中...";
               // if (pTimer != null) pTimer.Stop();
            //}
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.PenGray;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.PenGreen;
            }
            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));           
        }

        #region button/RadioBtn part////////////////////////////////////////////////////////////////////////////////

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        private SkeletonPoint[] ColorToSkeleton()
        {
            SkeletonPoint[] ColorInSkel = new SkeletonPoint[640 * 480];
            this.sensor.CoordinateMapper.MapColorFrameToSkeletonFrame(ColorImageFormat.RgbResolution640x480Fps30, DepthImageFormat.Resolution640x480Fps30, this.depthPixels, ColorInSkel);
            SkeletonPoint temp = ColorInSkel[320 * 240];
            
            return ColorInSkel;
        }

        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        private void CalVerticalLineAndPts(Skeleton skeleton, DrawingContext dc)
        {
            //正面
            //if (RadioAnterior.IsChecked == true && TargetList[15].IsTracked() && TargetList[18].IsTracked()
            //    && TargetList[15].point3D().Z != 0 && TargetList[18].point3D().Z != 0)
            //{
            //    FootCenterXin2D = (TargetList[15].point2D().X + TargetList[18].point2D().X) / 2;
            //    FootCenterYin2D = (TargetList[15].point2D().Y + TargetList[18].point2D().Y) / 2;
            //    FootCenterXin3D = (TargetList[15].point3D().X + TargetList[18].point3D().X) / 2;

            //    if (checkBoxVerticalLine.IsChecked == true)
            //        dc.DrawLine(PenRed, new Point(FootCenterXin2D, FootCenterYin2D),
            //                            new Point(FootCenterXin2D, SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position).Y));
            //}
            ////背面
            //if (RadioPosterior.IsChecked == true && TargetList[24].IsTracked() && TargetList[27].IsTracked()
            //     && TargetList[24].point3D().Z != 0 && TargetList[27].point3D().Z != 0)
            //{
            //    FootCenterXin2D = (TargetList[24].point2D().X + TargetList[27].point2D().X) / 2;
            //    FootCenterYin2D = (TargetList[24].point2D().Y + TargetList[27].point2D().Y) / 2;
            //    FootCenterXin3D = (TargetList[24].point3D().X + TargetList[27].point3D().X) / 2;

            //    if (checkBoxVerticalLine.IsChecked == true)
            //        dc.DrawLine(PenRed, new Point(FootCenterXin2D, FootCenterYin2D),
            //                            new Point(FootCenterXin2D, SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position).Y));
            //}
            ////側面
            //else if (RadioLeftLateral.IsChecked == true && TargetList[35].IsTracked()
            //     && TargetList[35].point3D().Z != 0)
            //{
            //    FootCenterXin2D = TargetList[35].point2D().X;
            //    FootCenterYin2D = TargetList[35].point2D().Y;
            //    FootCenterXin3D = TargetList[35].point3D().X;

            //    if (checkBoxVerticalLine.IsChecked == true)
            //        dc.DrawLine(PenRed, new Point(FootCenterXin2D, FootCenterYin2D),
            //                            new Point(FootCenterXin2D, SkeletonPointToScreen(skeleton.Joints[JointType.Head].Position).Y));
            //}
        }

        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            Screenshot();
        }

        private void Screenshot()
        {
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            #region drawingGroup to bitmap
            var drawingImage = new DrawingImage(this.drawingGroup);
            var snapImage = new Image { Source = drawingImage };
            snapImage.Arrange(new Rect(0, 0, 640, 480));
            var snapbitmap = new RenderTargetBitmap(640, 480, 96, 96, PixelFormats.Pbgra32);
            snapbitmap.Render(snapImage);

            #endregion

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(snapbitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string date = System.DateTime.Today.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            //string myPhotos = Environment.CurrentDirectory;

            string path = System.IO.Path.Combine(myPhotos, "KinectSnapshot-" + time + ".jpg");


            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            }
        }

        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        private void Btn_Background_Click(object sender, RoutedEventArgs e)
        {
            depthPixels.CopyTo(depth4background, 0);
        }

             #endregion

        private void Image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)      //拖拉Target Func
        {
            int XX = (int)e.GetPosition(Image).X;
            int YY = (int)e.GetPosition(Image).Y;
            int ci = (XX + YY * 640) * 4;    //ci = colorPixel index          
               
            double UU = -0.169 * colorPixels[ci + 2] - 0.331 * colorPixels[ci + 1] + 0.5 * colorPixels[ci] + 128;
            double VV = 0.5 * colorPixels[ci + 2] - 0.419 * colorPixels[ci + 1] - 0.081 * colorPixels[ci] + 128;


            
            Point Center = new Point(XX, YY);
            int searchrange = 10;
            for (int i = (int)Center.X - searchrange; i < Center.X + searchrange; i++)
            {
                for (int j = (int)Center.Y - searchrange; j < Center.Y + searchrange; j++)
                {
                    if (i <= 0 || i >= 639 || j <= 0 || j >= 479) break;   //avoid edge prob.                   
                  
                    Point ThisPoint = new Point(i, j);
                    for (int t = 1; t <= NumbersOfTarget; t++)
                    {
                        if (ThisPoint == TargetList[t].point2D())
                        {
                            CatchSuccess = t;
                            TargetList[t].Del(ThisPoint, boolPixels);
                        }
                    }
                }
            }

            //if (CatchSuccess == 0) DefineTargetsPosition(AnticipatedUU, AnticipatedVV);
        }

        private void Image_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)         //取消Target
        {
            TrackX = (int)e.GetPosition(Image).X;
            TrackY = (int)e.GetPosition(Image).Y;
            isFirstTime = true;
        //{
        //    int XX = (int)e.GetPosition(Image).X;
        //    int YY = (int)e.GetPosition(Image).Y;
        //    Point Center = new Point(XX, YY);
        //    int SearchRange = 10;

        //    for (int i = (int)Center.X - SearchRange; i < Center.X + SearchRange; i++)
        //    {
        //        for (int j = (int)Center.Y - SearchRange; j < Center.Y + SearchRange; j++)
        //        {
        //            if (i <= 0 || i >= 639 || j <= 0 || j >= 479) break;   //avoid edge prob.
        //            int di = (i + j * 640);

        //            if (boolPixels[di] != 0)
        //            {
        //                int t = boolPixels[di];
        //                TargetList[t].Del(Center, boolPixels);
        //            }
        //        }
        //    }
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int XX = (int)e.GetPosition(Image).X;
            int YY = (int)e.GetPosition(Image).Y;

            for (int t = 1; t <= NumbersOfTarget; t++)
            {
                if (CatchSuccess == t) TargetList[t].Setting(XX, YY, AnticipatedUU, AnticipatedVV, boolPixels);
            }

        }

        private void LockClosestSkeletons(Skeleton skeleton)
        {
            SortedList<float, int> depthSorted = new SortedList<float, int>();

            foreach (Skeleton s in skeletons)
            {
                if (s.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    float valueZ = s.Position.Z;
                    while (depthSorted.ContainsKey(valueZ))
                    {
                        valueZ += 0.0001f;
                    }

                    depthSorted.Add(valueZ, s.TrackingId);
                }
            }
            if (this.sensor.SkeletonStream.IsEnabled == true)
            {
                if (depthSorted.Count == 0)
                {
                    if (this.sensor.SkeletonStream.AppChoosesSkeletons == true)
                    {
                        this.sensor.SkeletonStream.ChooseSkeletons();
                        this.sensor.SkeletonStream.AppChoosesSkeletons = false;
                    }
                }
                else
                {
                    if (this.sensor.SkeletonStream.AppChoosesSkeletons == false)
                    {
                        this.sensor.SkeletonStream.AppChoosesSkeletons = true;
                    }
                    this.sensor.SkeletonStream.ChooseSkeletons(depthSorted.Values[0]);
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            headtilt.thetalist.Clear();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CatchSuccess = 0; cntemp++;
            //TargetList[37].Setting((int)e.GetPosition(Image).X,(int)  e.GetPosition(Image).Y , AnticipatedUU, AnticipatedVV, boolPixels);
            if (cntemp == 1) {TargetList[25].Setting((int)e.GetPosition(Image).X, (int)e.GetPosition(Image).Y, AnticipatedUU, AnticipatedVV, boolPixels); }
            if (cntemp == 2) { TargetList[26].Setting((int)e.GetPosition(Image).X, (int)e.GetPosition(Image).Y, AnticipatedUU, AnticipatedVV, boolPixels); cntemp = 0; }
            //if (cntemp == 3) { TargetList[27].Setting((int)e.GetPosition(Image).X, (int)e.GetPosition(Image).Y, AnticipatedUU, AnticipatedVV, boolPixels);  }

        }
    }
    class AngleMaxmin
    {
        public double max = -10;
        public double min = 10;
        public double mid = 10;
        public List<double> thetalist = new List<double>();
    }

}