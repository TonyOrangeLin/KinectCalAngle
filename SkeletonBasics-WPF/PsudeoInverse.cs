using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;// for grid
using System.Windows.Media;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    class PsudeoInverse
    {
        public int GlobalCount,upperbound = 100;
        public double[,] Phi = new double[100, 3];
        public double[,] PhiTranspose = new double[3, 100];
        public double[,] PhiSquare = new double[3, 3];
        public double[,] PhiSquareInverse = new double[3, 3];
        public double[,] Omega = new double[3, 1];
        public double[,] Ybar = new double[100, 1];
        
        public List<double> ThetaList = new List<double>();
        public List<double> ThetaListLeft = new List<double>();
        public List<double> ThetaListRight = new List<double>();


        private readonly Brush brushDeepSkyBlue = Brushes.DeepSkyBlue;
        private readonly Brush brushDarkBlue = Brushes.DarkBlue;
        public double theta, tmaxR, tminR, thetaR, tmaxL, tminL, thetaL;
      
        

        private double[,] mInverse(double[,] send) // 求反矩陣 http://maxclapton.blogspot.tw/2009/03/inverse-matrix.html
        {                                            //          http://www.csie.ntnu.edu.tw/~u91029/Matrix.html

            double[,] matrix = send;      //避免call by ref  
            double[,] inverse = new double[3, 3] {{ 1, 0, 0}, 
                                                  { 0, 1, 0},
                                                  { 0, 0, 1}};

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                if (matrix[i, i] == 0)    // 預處理：如果此row的首項係數為零，則考慮與下方row交換。
                {
                    for (int j = i + 1; j < matrix.GetLength(1); j++)// 交換此row與下方row
                    {
                        if (matrix[j, i] != 0) //若下方row首項!=0
                        {
                            for (int k = 0; k < matrix.GetLength(1); k++) //交換
                            {
                                double swap = matrix[i, k];
                                matrix[i, k] = matrix[j, k];
                                matrix[j, k] = swap;
                            }
                            break;
                        }
                    }
                    if (matrix[i, i] == 0) //換完首項係數仍=0的話則無反矩陣
                    {
                        System.Windows.MessageBox.Show("no inverse");
                        break;
                    }
                }
                else  //主要部分
                {
                    double temp = matrix[i, i];
                    for (int j = 0; j < matrix.GetLength(1); j++)  //使首項=1
                    {
                        matrix[i, j] /= temp;
                        inverse[i, j] /= temp;
                    }
                    for (int j = 0; j < matrix.GetLength(0); j++)  //根據其他列首項倍數與其他列相減
                    {
                        if (i != j)
                        {
                            temp = matrix[j, i];
                            for (int k = 0; k < matrix.GetLength(1); k++)
                            {
                                matrix[j, k] -= temp * matrix[i, k];
                                inverse[j, k] -= temp * inverse[i, k];
                            }
                        }
                    }
                }
            }
            return inverse;
        }

        private double[,] mProd(double[,] mA, double[,] mB) // 相乘函式  http://hwfbminic.blogspot.tw/2013/09/c_4888.html
        {
            double[,] Prod = new double[mA.GetLength(0), mB.GetLength(1)];
            for (int row = 0; row < mA.GetLength(0); row++)
            {
                for (int col = 0; col < mB.GetLength(1); col++)
                {
                    // Multiply the row of A by the column of B to get the row, column of product.
                    for (int inner = 0; inner < mB.GetLength(0); inner++)
                    {
                        Prod[row, col] += mA[row, inner] * mB[inner, col];
                    }
                }
            }
            return Prod;
        }

        public void CalPhiSpin(Target TargetA)
        {
            if (GlobalCount < upperbound)
            {
                Phi[GlobalCount, 0] = 2 * TargetA.point3D().X;
                Phi[GlobalCount, 1] = 2 * TargetA.point3D().Z;
                Phi[GlobalCount, 2] = 1;
                Ybar[GlobalCount, 0] = Math.Pow(TargetA.point3D().X, 2) + Math.Pow(TargetA.point3D().Z, 2);             
               
                if (GlobalCount == upperbound-1)
                {                   
                    for (int i = 0; i < GlobalCount; i++)   //Calulate Phi Transpose
                    {
                        for (int j = 0; j < Phi.GetLength(1); j++)
                        {
                            PhiTranspose[j, i] = Phi[i, j];
                        }
                    }
                    PhiSquare = mProd(PhiTranspose, Phi); // 3*3 matrix
                    PhiSquareInverse = mInverse(PhiSquare);
                    Omega = mProd(mProd(PhiSquareInverse, PhiTranspose), Ybar);
                    double randius = Math.Sqrt(Omega[2, 0] + Math.Pow(Omega[0, 0], 2) + Math.Pow(Omega[1, 0], 2)); 

                    for (int i = 0; i < GlobalCount; i++)//Cal theta
                    {
                        {
                            Vector vec = new Vector(Phi[i, 0] / 2 - Omega[0, 0], Phi[i, 1] / 2 - Omega[1, 0]);  //(2X,2Y)/2 - (Rx,Ry)
                            Vector mid = new Vector(0, -1);
                            ThetaList.Add(Vector.AngleBetween(vec, mid));
                        }
                    }

                    thetaL = ThetaList.Max();
                    thetaR = ThetaList.Min();
                    theta = thetaL - thetaR;
                    drawpoints(theta, thetaL, thetaR);                    
                }
                GlobalCount++;    
            }
            else GlobalCount = 0;
        }

        public void CalPhiTilt(Target TargetA)
        {
            if (GlobalCount < upperbound)
            {
                Phi[GlobalCount, 0] = 2 * TargetA.point3D().X;
                Phi[GlobalCount, 1] = 2 * TargetA.point3D().Y;
                Phi[GlobalCount, 2] = 1;
                Ybar[GlobalCount, 0] = Math.Pow(TargetA.point3D().X, 2) + Math.Pow(TargetA.point3D().Y, 2);

                if (GlobalCount == upperbound - 1)
                {
                    for (int i = 0; i < GlobalCount; i++)   //Calulate Phi Transpose
                    {
                        for (int j = 0; j < Phi.GetLength(1); j++)
                        {
                            PhiTranspose[j, i] = Phi[i, j];
                        }
                    }
                    PhiSquare = mProd(PhiTranspose, Phi); // 3*3 matrix
                    PhiSquareInverse = mInverse(PhiSquare);
                    Omega = mProd(mProd(PhiSquareInverse, PhiTranspose), Ybar);
                    double randius = Math.Sqrt(Omega[2, 0] + Math.Pow(Omega[0, 0], 2) + Math.Pow(Omega[1, 0], 2));

                    for (int i = 0; i < GlobalCount; i++)//Cal theta
                    {
                        {
                            Vector vec = new Vector(Phi[i, 0] / 2 - Omega[0, 0], Phi[i, 1] / 2 - Omega[1, 0]);  //(2X,2Y)/2 - (Rx,Ry)
                            Vector mid = new Vector(0, 1);
                            ThetaList.Add(Vector.AngleBetween(vec, mid));
                        }
                    }

                    thetaL = ThetaList.Max();
                    thetaR = ThetaList.Min();
                    theta = thetaL - thetaR;
                    drawpoints(theta, thetaL, thetaR);
                }
                GlobalCount++;
            }
            else GlobalCount = 0;
        }

        public void CalPhiTwoTargetSpin(Target TargetA, Target TargetB)
        {
            if (GlobalCount < upperbound)
            {
                Phi[GlobalCount, 0] = 2 * TargetA.point3D().X;
                Phi[GlobalCount, 1] = 2 * TargetA.point3D().Z;
                Phi[GlobalCount, 2] = 1;
                Ybar[GlobalCount, 0] = Math.Pow(TargetA.point3D().X, 2) + Math.Pow(TargetA.point3D().Z, 2);

                Phi[GlobalCount + 1, 0] = 2 * TargetB.point3D().X;
                Phi[GlobalCount + 1, 1] = 2 * TargetB.point3D().Z;
                Phi[GlobalCount + 1, 2] = 1;
                Ybar[GlobalCount + 1, 0] = Math.Pow(TargetB.point3D().X, 2) + Math.Pow(TargetB.point3D().Z, 2);

            
                if (GlobalCount == upperbound-2)
                {
                    //Calulate Phi Transpose
                    for (int i = 0; i < GlobalCount; i++)
                    {
                        for (int j = 0; j < Phi.GetLength(1); j++)
                        {
                            PhiTranspose[j, i] = Phi[i, j];
                        }
                    }
                    PhiSquare = mProd(PhiTranspose, Phi); // 3*3 matrix
                    PhiSquareInverse = mInverse(PhiSquare);
                    Omega = mProd(mProd(PhiSquareInverse, PhiTranspose), Ybar);
                    double randius = Math.Sqrt(Omega[2, 0] + Math.Pow(Omega[0, 0], 2) + Math.Pow(Omega[1, 0], 2));                   
                    
                    for (int i = 0; i < GlobalCount; i++)//Cal theta
                    {
                        if (i % 2 == 0)
                        {
                            Vector vec = new Vector(Phi[i, 0] / 2 - Omega[0, 0], Phi[i, 1] / 2 - Omega[1, 0]);  //(X,Y)-(Rx,Ry)
                            Vector mid = new Vector(0, 1);
                            ThetaListRight.Add(Vector.AngleBetween(vec, mid));
                        }
                        else 
                        {
                            Vector vec = new Vector(Phi[i, 0] / 2 - Omega[0, 0], Phi[i, 1] / 2 - Omega[1, 0]);  //(X,Y)-(Rx,Ry)
                            Vector mid = new Vector(0, 1);
                            ThetaListLeft.Add(Vector.AngleBetween(vec, mid));                        
                        }
                    }

                    tmaxR = ThetaListRight.Max();
                    tminR = ThetaListRight.Min();
                    thetaR =Math.Abs( tmaxR - tminR);

                    tmaxL = ThetaListLeft.Max();
                    tminL = ThetaListLeft.Min();
                    thetaL = Math.Abs(tmaxL - tminL);

                    theta = (thetaR + thetaL) / 2;


                    drawpoints(theta, thetaR, thetaL);
                }
                GlobalCount += 2;
            }
            else GlobalCount = 0;
        }

        public void CalPhiTwoTargetTilt(Target TargetA, Target TargetB)
        {
            if (GlobalCount < upperbound)
            {
                Phi[GlobalCount, 0] = 2 * TargetA.point3D().X;
                Phi[GlobalCount, 1] = 2 * TargetA.point3D().Y;
                Phi[GlobalCount, 2] = 1;
                Ybar[GlobalCount, 0] = Math.Pow(TargetA.point3D().X, 2) + Math.Pow(TargetA.point3D().Y, 2);

                Phi[GlobalCount + 1, 0] = 2 * TargetB.point3D().X;
                Phi[GlobalCount + 1, 1] = 2 * TargetB.point3D().Y;
                Phi[GlobalCount + 1, 2] = 1;
                Ybar[GlobalCount + 1, 0] = Math.Pow(TargetB.point3D().X, 2) + Math.Pow(TargetB.point3D().Y, 2);

                if (GlobalCount == upperbound)
                {
                    //Calulate Phi Transpose
                    for (int i = 0; i < GlobalCount; i++)
                    {
                        for (int j = 0; j < Phi.GetLength(1); j++)
                        {
                            PhiTranspose[j, i] = Phi[i, j];
                        }
                    }

                    PhiSquare = mProd(PhiTranspose, Phi); // 3*3 matrix

                    PhiSquareInverse = mInverse(PhiSquare);

                    Omega = mProd(mProd(PhiSquareInverse, PhiTranspose), Ybar);

                    double randius = Math.Sqrt(Omega[2, 0] + Math.Pow(Omega[0, 0], 2) + Math.Pow(Omega[1, 0], 2));

                    //Cal theta
                    for (int i = 0; i < GlobalCount; i++)
                    {
                        {
                            Vector vec = new Vector(Phi[i, 0] / 2 - Omega[0, 0], Phi[i, 1] / 2 - Omega[1, 0]);  //(2X,2Y)/2 - (Rx,Ry)
                            Vector mid = new Vector(0, 1);

                            ThetaList.Add(Vector.AngleBetween(vec, mid));
                        }
                    }

                    thetaL = ThetaList.Max();
                    thetaR = ThetaList.Min();
                    theta = thetaL - thetaR;
                    drawpoints(theta, thetaL, thetaR);
                }
                GlobalCount += 2;
            }
            else GlobalCount = 0;
        }

        public void drawpoints(double theta, double thetaL, double thetaR)
        {
            ThetaList.Clear();
            ThetaListLeft.Clear();
            ThetaListRight.Clear();


            Window mainWindow = new Window();
            mainWindow.Title = theta.ToString("f2") + " , L = " + thetaL.ToString("f2") + " , R =" + thetaR.ToString("f2");

            Canvas myParentCanvas = new Canvas();
            myParentCanvas.Width = 400;
            myParentCanvas.Height = 400;
            Label label = new Label();

            label.Content = theta;


            int DrawingWeighta = 200 , DrawingWeightb =100;
            for (int i = 0; i < upperbound; i++)
            {               
                Line PhiPoints = new Line();
                PhiPoints.Stroke = brushDarkBlue; ;             //用brush畫
                PhiPoints.StrokeThickness = 5;        //brush厚度=5
                PhiPoints.X1 = Phi[i, 0] / 2 * DrawingWeighta + DrawingWeightb;
                PhiPoints.Y1 = Phi[i, 1] / 2 * DrawingWeighta + DrawingWeightb;
                PhiPoints.X2 = Phi[i, 0] / 2 * DrawingWeighta + +DrawingWeightb+1;
                PhiPoints.Y2 = Phi[i, 1] / 2 * DrawingWeighta + +DrawingWeightb+1;
                myParentCanvas.Children.Add(PhiPoints);


                Line CenterPoint = new Line();
                CenterPoint.Stroke = brushDeepSkyBlue;             //用brush畫
                CenterPoint.StrokeThickness = 5;        //brush厚度=5
                CenterPoint.X1 = Omega[0, 0] * DrawingWeighta + DrawingWeightb;
                CenterPoint.Y1 = Omega[1, 0] * DrawingWeighta + DrawingWeightb;
                CenterPoint.X2 = Omega[0, 0] * DrawingWeighta + +DrawingWeightb+1;
                CenterPoint.Y2 = Omega[1, 0] * DrawingWeighta + +DrawingWeightb+1;
                myParentCanvas.Children.Add(CenterPoint);
            }         
      
            // Add the parent Canvas as the Content of the Window Object
            mainWindow.Content = myParentCanvas;
            mainWindow.Show();
        
        
        }
    }
}
