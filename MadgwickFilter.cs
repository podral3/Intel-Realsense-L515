using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace L515_Realsense_App
{
    internal class MadgwickFilter
    {
        private float DELTA = 0.01f;
        private const float GYRO_MEAN_ERROR = (float)Math.PI * (5.0f / 180.0f);
        private float BETA = (float)Math.Sqrt(0.75) * GYRO_MEAN_ERROR;

        Quaternion q_est = new Quaternion(0, 0, 0, 1);
        public MadgwickFilter()
        {
            
        }
        /// <summary>
        /// Madgwick filter loop
        /// </summary>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="az"></param>
        /// <param name="gx"></param>
        /// <param name="gy"></param>
        /// <param name="gz"></param>
        public Quaternion IMU_Filter(float ax, float ay, float az, float gx, float gy, float gz)
        {
            Quaternion q_est_prev = q_est;
            Quaternion q_est_dot = new Quaternion();

            Quaternion q_accel = new Quaternion(ax, ay, az, 0); //not normalized accel quaternion

            float[] F_g = new float[3]; // equation(15/21/25) objective function for gravity
            float[,] J_g = new float[3,4]; //Jacobian

            Quaternion gradient = new Quaternion();

            Quaternion q_gyro= new Quaternion(gx, gy, gz, 0); //q_w

            q_gyro *= 0.5f; // equation (12) dq/dt = (1/2)q*w
            q_gyro *= q_est_prev; // equation (12)

            q_accel = Quaternion.Normalize(q_accel);

            //Compute the objective function for gravity, equation(15), simplified to equation (25) due to the 0's in the acceleration reference quaternion
            F_g[0] = 2 * (q_est_prev.X * q_est_prev.Z - q_est_prev.W * q_est_prev.Y) - q_accel.X;
            F_g[1] = 2 * (q_est_prev.W * q_est_prev.X + q_est_prev.Y * q_est_prev.Z) - q_accel.Y;
            F_g[2] = 2 * ( 0.5f - q_est_prev.X * q_est_prev.X - q_est_prev.Y * q_est_prev.Y) - q_accel.Z;

            //Compute the Jacobian matrix, equation (26), for gravity
            J_g[0,0] = -2 * q_est_prev.Y;
            J_g[0,1] = 2 * q_est_prev.Z;
            J_g[0,2] = -2 * q_est_prev.W;
            J_g[0,3] = 2 * q_est_prev.X;

            J_g[1, 0] = 2 * q_est_prev.X;
            J_g[1, 1] = 2 * q_est_prev.W;
            J_g[1, 2] = 2 * q_est_prev.Y;
            J_g[1, 3] = 2 * q_est_prev.Y;

            J_g[2, 0] = 0;
            J_g[2, 1] = -4 * q_est_prev.X;
            J_g[2, 2] = -4 * q_est_prev.Y;
            J_g[2, 3] = 0;

            // now computer the gradient, equation (20), gradient = J_g'*F_g
            gradient.W = J_g[0, 0] * F_g[0] + J_g[1, 0] * F_g[1] + J_g[2, 0] * F_g[2];
            gradient.X = J_g[0, 1] * F_g[0] + J_g[1, 1] * F_g[1] + J_g[2, 1] * F_g[2];
            gradient.Y = J_g[0, 2] * F_g[0] + J_g[1, 2] * F_g[1] + J_g[2, 2] * F_g[2];
            gradient.Z = J_g[0, 3] * F_g[0] + J_g[1, 3] * F_g[1] + J_g[2, 3] * F_g[2];

            gradient = Quaternion.Normalize(gradient);

            gradient *= BETA;
            q_est_dot = q_gyro - gradient;
            q_est_dot *= DELTA;
            q_est = q_est_prev + q_est_dot;
            q_est = Quaternion.Normalize(q_est); //q_est should be returned
            return q_est;
        }

        public Vector3 EulerAngles(Quaternion q)
        {
            float yaw = (float)Math.Atan2((2*q.X*q.Y - 2*q.W*q.Z), (2*q.W*q.W + 2*q.X*q.X));
            float pitch = (float)Math.Asin(2*q.X*q.Z + 2*q.W*q.Y) * -1.0f;
            float roll = (float)Math.Atan2((2*q.Y*q.Z - 2*q.W*q.X), (2*q.W*q.W + 2*q.Z*q.Z - 1));

            yaw *= (float)(180 / Math.PI);
            pitch *= (float)(180 / Math.PI);
            roll *= (float)(180 / Math.PI);

            return new Vector3(yaw, pitch, roll);
        }
    }
}

