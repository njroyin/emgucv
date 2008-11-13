using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Emgu.CV
{
   /// <summary>
   /// Functions used form camera calibration
   /// </summary>
   public static class CameraCalibration
   {
      /// <summary>
      /// Estimates intrinsic camera parameters and extrinsic parameters for each of the views
      /// </summary>
      /// <param name="objectPoints">The 3D location of the object points. The first index is the index of image, second index is the index of the point</param>
      /// <param name="imagePoints">The 2D image location of the points. The first index is the index of the image, second index is the index of the point</param>
      /// <param name="imageSize">The size of the image, used only to initialize intrinsic camera matrix</param>
      /// <param name="intrinsicParam">The intrisinc parameters, might contains some initial values. The values will be modified by this function.</param>
      /// <param name="flags">Flags</param>
      /// <param name="extrinsicParams">The output array of extrinsic parameters.</param>
      public static void CalibrateCamera(Point3D<float>[][] objectPoints, Point2D<float>[][] imagePoints, ref MCvSize imageSize, IntrinsicCameraParameters intrinsicParam, CvEnum.CALIB_TYPE flags, out ExtrinsicCameraParameters[] extrinsicParams)
      {
         Debug.Assert(objectPoints.Length == imagePoints.Length, "The number of images for objects points should be equal to the number of images for image points");
         int imageCount = objectPoints.Length;

         #region get the matrix that represent the object points
         List<Point<float>> objectPointList = new List<Point<float>>();
         foreach (Point3D<float>[] pts in objectPoints)
            objectPointList.AddRange(pts);
         Matrix<float> objectPointMatrix = PointCollection.ToMatrix<float>(objectPointList);
         #endregion

         #region get the matrix that represent the image points
         List<Point<float>> imagePointList = new List<Point<float>>();
         foreach (Point2D<float>[] pts in imagePoints)
            imagePointList.AddRange(pts);
         Matrix<float> imagePointMatrix = PointCollection.ToMatrix<float>(imagePointList);
         #endregion

         #region get the matrix that represent the point counts
         int[] pointCounts = new int[objectPoints.Length];
         for (int i = 0; i < objectPoints.Length; i++)
         {
            Debug.Assert(objectPoints[i].Length == imagePoints[i].Length, String.Format("Number of 3D points and image points should be equal in the {0}th image", i));
            pointCounts[i] = objectPoints[i].Length;
         }
         Matrix<int> pointCountsMatrix = new Matrix<int>(pointCounts);
         #endregion

         Matrix<float> rotationVectors = new Matrix<float>(3, imageCount - 1);
         Matrix<float> translationVectors = new Matrix<float>(3, imageCount - 1);

         CvInvoke.cvCalibrateCamera2(
             objectPointMatrix.Ptr,
             imagePointMatrix.Ptr,
             pointCountsMatrix.Ptr,
             imageSize,
             intrinsicParam.IntrinsicMatrix,
             intrinsicParam.DistortionCoeffs,
             rotationVectors,
             translationVectors,
             flags);

         extrinsicParams = new ExtrinsicCameraParameters[imageCount - 1];
         for (int i = 0; i < imageCount - 1; i++)
         {
            RotationVector3D rot = new RotationVector3D(new float[] { rotationVectors[0, i], rotationVectors[1, i], rotationVectors[2, i] });
            extrinsicParams[i] = new ExtrinsicCameraParameters(rot, translationVectors.GetCol(i).Transpose());
         }
      }

      /// <summary>
      /// Estimates transformation between the 2 cameras making a stereo pair. If we have a stereo camera, where the relative position and orientatation of the 2 cameras is fixed, and if we computed poses of an object relative to the fist camera and to the second camera, (R1, T1) and (R2, T2), respectively (that can be done with cvFindExtrinsicCameraParams2), obviously, those poses will relate to each other, i.e. given (R1, T1) it should be possible to compute (R2, T2) - we only need to know the position and orientation of the 2nd camera relative to the 1st camera. That's what the described function does. It computes (R, T) such that:
      /// R2=R*R1,
      /// T2=R*T1 + T
      /// </summary>
      /// <param name="objectPoints">The 3D location of the object points. The first index is the index of image, second index is the index of the point</param>
      /// <param name="imagePoints1">The 2D image location of the points. The first index is the index of the image, second index is the index of the point</param>
      /// <param name="imagePoints2">The 2D image location of the points. The first index is the index of the image, second index is the index of the point</param>
      /// <param name="intrinsicParam1">The intrisinc parameters, might contains some initial values. The values will be modified by this function.</param>
      /// <param name="intrinsicParam2">The intrisinc parameters, might contains some initial values. The values will be modified by this function.</param>
      /// <param name="imageSize">Size of the image, used only to initialize intrinsic camera matrix</param>
      /// <param name="flags">Different flags</param>
      /// <param name="extrinsicParams">The extrinsic parameters which contains:
      /// R - The rotation matrix between the 1st and the 2nd cameras' coordinate systems; 
      /// T - The translation vector between the cameras' coordinate systems. </param>
      /// <param name="essentialMatrix">essential matrix</param>
      /// <param name="termCrit"> Termination criteria for the iterative optimiziation algorithm </param>
      /// <param name="foundamentalMatrix">fundamental matrix</param>
      public static void StereoCalibrate(Point3D<float>[][] objectPoints, Point2D<float>[][] imagePoints1, Point2D<float>[][] imagePoints2, IntrinsicCameraParameters intrinsicParam1, IntrinsicCameraParameters intrinsicParam2, ref MCvSize imageSize, CvEnum.CALIB_TYPE flags, ref MCvTermCriteria termCrit, out ExtrinsicCameraParameters extrinsicParams, out Matrix<float> foundamentalMatrix, out Matrix<float> essentialMatrix )
      {
         Debug.Assert(objectPoints.Length == imagePoints1.Length && objectPoints.Length == imagePoints2.Length, "The number of images for objects points should be equal to the number of images for image points");
         //int imageCount = objectPoints.Length;

         #region get the matrix that represent the object points
         List<Point<float>> objectPointList = new List<Point<float>>();
         foreach (Point3D<float>[] pts in objectPoints)
            objectPointList.AddRange(pts);
         Matrix<float> objectPointMatrix = PointCollection.ToMatrix<float>(objectPointList);
         #endregion

         #region get the matrix that represent the image points
         List<Point<float>> imagePointList1 = new List<Point<float>>();
         foreach (Point2D<float>[] pts in imagePoints1)
            imagePointList1.AddRange(pts);
         Matrix<float> imagePointMatrix1 = PointCollection.ToMatrix<float>(imagePointList1);
         #endregion

         #region get the matrix that represent the image points
         List<Point<float>> imagePointList2 = new List<Point<float>>();
         foreach (Point2D<float>[] pts in imagePoints2)
            imagePointList2.AddRange(pts);
         Matrix<float> imagePointMatrix2 = PointCollection.ToMatrix<float>(imagePointList2);
         #endregion

         #region get the matrix that represent the point counts
         int[] pointCounts = new int[objectPoints.Length];
         for (int i = 0; i < objectPoints.Length; i++)
         {
            Debug.Assert(objectPoints[i].Length == imagePoints1[i].Length && objectPoints[i].Length == imagePoints2[i].Length, String.Format("Number of 3D points and image points should be equal in the {0}th image", i));
            pointCounts[i] = objectPoints[i].Length;
         }
         Matrix<int> pointCountsMatrix = new Matrix<int>(pointCounts);
         #endregion
         extrinsicParams = new ExtrinsicCameraParameters();
         essentialMatrix = new Matrix<float>(3, 3);
         foundamentalMatrix = new Matrix<float>(3, 3);

         CvInvoke.cvStereoCalibrate(
            objectPointMatrix.Ptr, 
            imagePointMatrix1.Ptr, 
            imagePointMatrix2.Ptr, 
            pointCountsMatrix.Ptr,
            intrinsicParam1.IntrinsicMatrix, 
            intrinsicParam1.DistortionCoeffs,
            intrinsicParam2.IntrinsicMatrix,
            intrinsicParam2.DistortionCoeffs,
            imageSize, 
            extrinsicParams.RotationVector,
            extrinsicParams.TranslationVector,
            essentialMatrix.Ptr,
            foundamentalMatrix.Ptr,
            termCrit,
            flags);
      }

      /// <summary>
      /// Estimates extrinsic camera parameters using known intrinsic parameters and and extrinsic parameters for each view. The coordinates of 3D object points and their correspondent 2D projections must be specified. This function also minimizes back-projection error. 
      /// </summary>
      /// <param name="objectPoints">The array of object points</param>
      /// <param name="imagePoints">The array of corresponding image points</param>
      /// <param name="intrin">The intrinsic parameters</param>
      /// <returns>the extrinsic parameters</returns>
      public static ExtrinsicCameraParameters FindExtrinsicCameraParams2(
          Point3D<float>[] objectPoints,
          Point2D<float>[] imagePoints,
          IntrinsicCameraParameters intrin)
      {
         ExtrinsicCameraParameters res = new ExtrinsicCameraParameters();
         Matrix<float> translation = new Matrix<float>(3, 1);
         RotationVector3D rotation = new RotationVector3D();

         Matrix<float> objectPointMatrix = PointCollection.ToMatrix(Emgu.Util.Toolbox.IEnumConvertor<Point2D<float>, Point<float>>(objectPoints, delegate(Point2D<float> p) { return (Point<float>)p; }));
         Matrix<float> imagePointMatrix = PointCollection.ToMatrix(Emgu.Util.Toolbox.IEnumConvertor<Point2D<float>, Point<float>>(imagePoints, delegate(Point2D<float> p) { return (Point<float>)p; }));

         CvInvoke.cvFindExtrinsicCameraParams2(objectPointMatrix.Ptr, imagePointMatrix.Ptr, intrin.IntrinsicMatrix.Ptr, intrin.DistortionCoeffs.Ptr, rotation.Ptr, translation.Ptr);

         res.TranslationVector = translation;
         res.RotationVector = rotation;

         return res;
      }

      /// <summary>
      /// Transforms the image to compensate radial and tangential lens distortion. 
      /// The camera matrix and distortion parameters can be determined using cvCalibrateCamera2. For every pixel in the output image the function computes coordinates of the corresponding location in the input image using the formulae in the section beginning. Then, the pixel value is computed using bilinear interpolation. If the resolution of images is different from what was used at the calibration stage, fx, fy, cx and cy need to be adjusted appropriately, while the distortion coefficients remain the same
      /// </summary>
      /// <typeparam name="C">The color type of the image</typeparam>
      /// <typeparam name="D">The depth of the image</typeparam>
      /// <param name="src">The distorted image</param>
      /// <param name="intrin">The intrinsic camera parameters</param>
      /// <returns>The corrected image</returns>
      public static Image<C, D> Undistort2<C, D>(Image<C, D> src, IntrinsicCameraParameters intrin)
         where C : ColorType, new()
         where D : IComparable, new()
      {
         Image<C, D> res = src.CopyBlank();
         CvInvoke.cvUndistort2(src.Ptr, res.Ptr, intrin.IntrinsicMatrix.Ptr, intrin.DistortionCoeffs.Ptr);
         return res;
      }

      /// <summary>
      /// Computes projections of 3D points to the image plane given intrinsic and extrinsic camera parameters. 
      /// Optionally, the function computes jacobians - matrices of partial derivatives of image points as functions of all the input parameters w.r.t. the particular parameters, intrinsic and/or extrinsic. 
      /// The jacobians are used during the global optimization in cvCalibrateCamera2 and cvFindExtrinsicCameraParams2. 
      /// The function itself is also used to compute back-projection error for with current intrinsic and extrinsic parameters. 
      /// </summary>
      /// <remarks>Note, that with intrinsic and/or extrinsic parameters set to special values, the function can be used to compute just extrinsic transformation or just intrinsic transformation (i.e. distortion of a sparse set of points) </remarks>
      /// <param name="objectPoints">The array of object points, 3xN or Nx3, where N is the number of points in the view</param>
      /// <param name="extrin">extrinsic parameters</param>
      /// <param name="intrin">intrinsic parameters</param>
      /// <param name="mats">Optional matrix supplied in the following order: dpdrot, dpdt, dpdf, dpdc, dpddist</param>
      /// <returns>The array of image points which is the projection of <paramref name="objectPoints"/></returns>
      public static Point2D<float>[] ProjectPoints2(
          Point3D<float>[] objectPoints,
          ExtrinsicCameraParameters extrin,
          IntrinsicCameraParameters intrin,
          params Matrix<float>[] mats)
      {
         Matrix<float> pointMatrix = PointCollection.ToMatrix(Emgu.Util.Toolbox.IEnumConvertor<Point2D<float>, Point<float>>(objectPoints, delegate(Point2D<float> p) { return (Point<float>)p; }));
         IntPtr dpdrot = mats.Length > 0 ? mats[0].Ptr : IntPtr.Zero;
         IntPtr dpdt = mats.Length > 1 ? mats[1].Ptr : IntPtr.Zero;
         IntPtr dpdf = mats.Length > 2 ? mats[2].Ptr : IntPtr.Zero;
         IntPtr dpdc = mats.Length > 3 ? mats[3].Ptr : IntPtr.Zero;
         IntPtr dpddist = mats.Length > 4 ? mats[4].Ptr : IntPtr.Zero;

         Matrix<float> point2DMatrix = new Matrix<float>(objectPoints.Length, 2);
         CvInvoke.cvProjectPoints2(pointMatrix.Ptr, extrin.RotationVector.Ptr, extrin.TranslationVector.Ptr, intrin.IntrinsicMatrix.Ptr, intrin.DistortionCoeffs.Ptr, point2DMatrix.Ptr, dpdrot, dpdt, dpdf, dpdc, dpddist);
         Point2D<float>[] imagePoints = new Point2D<float>[objectPoints.Length];
         for (int i = 0; i < imagePoints.Length; i++)
            imagePoints[i] = new Point2D<float>(point2DMatrix[i, 0], point2DMatrix[i, 1]);
         return imagePoints;
      }

      /// <summary>
      /// Use all points to find perspective transformation H=||h_ij|| between the source and the destination planes
      /// </summary>
      /// <param name="srcPoints">Point coordinates in the original plane, 2xN, Nx2, 3xN or Nx3 array (the latter two are for representation in homogenious coordinates), where N is the number of points</param>
      /// <param name="dstPoints">Point coordinates in the destination plane, 2xN, Nx2, 3xN or Nx3 array (the latter two are for representation in homogenious coordinates) </param>
      /// <returns>The 3x3 homography matrix. </returns>
      public static Matrix<float> FindHomography(Matrix<float> srcPoints, Matrix<float> dstPoints)
      {
         return FindHomography(srcPoints, dstPoints, Emgu.CV.CvEnum.HOMOGRAPHY_METHOD.DEFAULT, 0.0);
      }

      /// <summary>
      /// Use RANDSAC to finds perspective transformation H=||h_ij|| between the source and the destination planes
      /// </summary>
      /// <param name="srcPoints">Point coordinates in the original plane, 2xN, Nx2, 3xN or Nx3 array (the latter two are for representation in homogenious coordinates), where N is the number of points</param>
      /// <param name="dstPoints">Point coordinates in the destination plane, 2xN, Nx2, 3xN or Nx3 array (the latter two are for representation in homogenious coordinates) </param>
      /// <param name="ransacReprojThreshold">The maximum allowed reprojection error to treat a point pair as an inlier. The parameter is only used in RANSAC-based homography estimation. E.g. if dst_points coordinates are measured in pixels with pixel-accurate precision, it makes sense to set this parameter somewhere in the range ~1..3</param>
      /// <returns>The 3x3 homography matrix. </returns>
      public static Matrix<float> FindHomography(Matrix<float> srcPoints, Matrix<float> dstPoints, double ransacReprojThreshold)
      {
         return FindHomography(srcPoints, dstPoints, CvEnum.HOMOGRAPHY_METHOD.RANSAC, ransacReprojThreshold);
      }

      /// <summary>
      /// Use the specific method to find perspective transformation H=||h_ij|| between the source and the destination planes 
      /// </summary>
      /// <param name="srcPoints">Point coordinates in the original plane, 2xN, Nx2, 3xN or Nx3 array (the latter two are for representation in homogenious coordinates), where N is the number of points</param>
      /// <param name="dstPoints">Point coordinates in the destination plane, 2xN, Nx2, 3xN or Nx3 array (the latter two are for representation in homogenious coordinates) </param>
      /// <param name="method">FindHomography method</param>
      /// <param name="ransacReprojThreshold">The maximum allowed reprojection error to treat a point pair as an inlier. The parameter is only used in RANSAC-based homography estimation. E.g. if dst_points coordinates are measured in pixels with pixel-accurate precision, it makes sense to set this parameter somewhere in the range ~1..3</param>
      /// <returns>The 3x3 homography matrix. </returns>
      public static Matrix<float> FindHomography(Matrix<float> srcPoints, Matrix<float> dstPoints, CvEnum.HOMOGRAPHY_METHOD method, double ransacReprojThreshold)
      {
         Matrix<float> homography = new Matrix<float>(3, 3);
         CvInvoke.cvFindHomography(srcPoints.Ptr, dstPoints.Ptr, homography.Ptr, method, ransacReprojThreshold, IntPtr.Zero);
         return homography;
      }

      /// <summary>
      /// Finds perspective transformation H=||h_ij|| between the source and the destination planes
      /// </summary>
      /// <param name="srcPoints">Point coordinates in the original plane</param>
      /// <param name="dstPoints">Point coordinates in the destination plane</param>
      /// <param name="method">FindHomography method</param>
      /// <param name="ransacReprojThreshold">The maximum allowed reprojection error to treat a point pair as an inlier. The parameter is only used in RANSAC-based homography estimation. E.g. if dst_points coordinates are measured in pixels with pixel-accurate precision, it makes sense to set this parameter somewhere in the range ~1..3</param>
      /// <returns>The 3x3 homography matrix. </returns>
      public static Matrix<float> FindHomography(Point2D<float>[] srcPoints, Point2D<float>[] dstPoints, CvEnum.HOMOGRAPHY_METHOD method, double ransacReprojThreshold)
      {
         Matrix<float> srcPointMatrix = PointCollection.ToMatrix<float>(Emgu.Util.Toolbox.IEnumConvertor<Point2D<float>, Point<float>>(srcPoints, delegate(Point2D<float> p) { return (Point<float>)p; }));
         Matrix<float> dstPointMatrix = PointCollection.ToMatrix<float>(Emgu.Util.Toolbox.IEnumConvertor<Point2D<float>, Point<float>>(dstPoints, delegate(Point2D<float> p) { return (Point<float>)p; }));
         return FindHomography(srcPointMatrix, dstPointMatrix, method, ransacReprojThreshold);
      }

      /// <summary>
      /// attempts to determine whether the input image is a view of the chessboard pattern and locate internal chessboard corners
      /// </summary>
      /// <param name="image">Source chessboard view</param>
      /// <param name="pattern_size">The number of inner corners per chessboard row and column</param>
      /// <param name="corners">The corners detected</param>
      /// <param name="flags">Various operation flags</param>
      /// <returns>If the chess board pattern is found</returns>
      public static bool FindChessboardCorners(
         Image<Gray, Byte> image,
         MCvSize pattern_size,
         CvEnum.CALIB_CB_TYPE flags,
         out Point2D<float>[] corners)
      {
         float[,] cornersCoordinates = new float[pattern_size.width * pattern_size.height, 2];
         int cornerCount = 0;

         bool patternFound =
         CvInvoke.cvFindChessboardCorners(
            image.Ptr,
            pattern_size,
            cornersCoordinates,
            ref cornerCount,
            flags) != 0; 

         corners = new Point2D<float>[cornerCount];
         for (int i = 0; i < corners.Length; i++)
         {
            corners[i] = new Point2D<float>(cornersCoordinates[i, 0], cornersCoordinates[i, 1]);
         }

         return patternFound;
      }

      /// <summary>
      ///  Draws the individual chessboard corners detected (as red circles) in case if the board was not found (patternWasFound== false) or the colored corners connected with lines when the board was found (patternWasFound == true). 
      /// </summary>
      /// <param name="image">The destination image</param>
      /// <param name="patternSize">The number of inner corners per chessboard row and column</param>
      /// <param name="corners">The array of corners detected</param>
      /// <param name="patternWasFound">Result of FindChessboardCorners</param>
      public static void DrawChessboardCorners(
         Image<Gray, Byte> image,
         MCvSize patternSize,
         Point2D<float>[] corners,
         bool patternWasFound)
      {
         CvInvoke.cvDrawChessboardCorners(
            image.Ptr,
            patternSize, 
            PointCollection.ToArray<float>(corners), 
            corners.Length, 
            patternWasFound ? 1 : 0);
      }
   }
}
