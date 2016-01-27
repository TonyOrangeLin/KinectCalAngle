using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D; //for Point3D

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    class StandardDifference
    {
        
        /// <summary> 
        /// 標準差(StandardDifference) 
        /// </summary> 
        /// <param name="list">歷史報酬率</param> 
        /// <returns>標準差</returns> 
        public List<double> GetSD(List<Double> list)
        {
          
            double AVG = list.Average();
            double sigma = 0,sum = 0;
            for (int i = 0; i < list.Count; i++)
            {
                sum += Math.Pow(list[i] - AVG, 2);                
            }

            sigma = Math.Sqrt( sum / list.Count);
           
            //Romove Errors
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] < AVG - 2 * sigma || list[i] > AVG + 2 * sigma) //xbar - 3*sigma < list[i] < xbar + 3*sigma  ( 65_95_99.7)
                // list[i]= AVG;
                {
                    list.RemoveAt(i);                  
                    i--;
                }
            }         
          
            return list;
        }
     
    }
}
