﻿//----------------------------------------------------------------------------
//  Copyright (C) 2004-2023 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util.TypeEnum;

using System.Runtime.InteropServices;

using Microsoft.Maui.Controls.PlatformConfiguration;

#if __MACCATALYST__
using AppKit;
using CoreGraphics;
#elif __IOS__
using UIKit;
using CoreGraphics;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
#elif __ANDROID__
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Android.Hardware.Lights;
#elif WINDOWS
using Visibility = Microsoft.UI.Xaml.Visibility;
using Microsoft.UI.Xaml.Media.Imaging;
#endif

namespace Emgu.CV.Platform.Maui.UI
{

    public class ButtonTextImagePage
#if __IOS__
        : Emgu.Util.AvCaptureSessionPage
#elif __ANDROID__
        : AndroidCameraPage
#else
        : ContentPage
#endif
    {
        private Picker _picker = new Picker();

        public Picker Picker
        {
            get { return _picker; }
        }

        private Microsoft.Maui.Controls.Button _topButton = new Microsoft.Maui.Controls.Button();
        public Microsoft.Maui.Controls.Button TopButton
        {
            get { return _topButton; }
        }

        private Label _messageLabel = new Label();
        public Label MessageLabel
        {
            get { return _messageLabel; }
        }
		
		private Editor _logEditor = new Editor();
        public Editor LogEditor
        {
            get { return _logEditor; }
        }

        private CvImageView _displayImage = new CvImageView();

        public CvImageView DisplayImage
        {
            get { return _displayImage; }
            //set { _displayImage = value; }
        }

#if __ANDROID__
        private Bitmap[] _renderBuffer = new Bitmap[2];
        private int _renderBufferIdx = 0;

        public virtual void SetImage(IInputArray image)
        {
            if (image == null)
            {
                this.Dispatcher.Dispatch(
                    () =>
                    {
                        DisplayImage.ImageView.SetImageBitmap(null);
                    });
                return;
            }

            int bufferIdx = _renderBufferIdx;
            Bitmap buffer;
            _renderBufferIdx = (_renderBufferIdx + 1) % _renderBuffer.Length;

            using (InputArray iaImage = image.GetInputArray())
            using (Mat mat = iaImage.GetMat())
            {
                if (_renderBuffer[bufferIdx] == null)
                {
                    buffer = mat.ToBitmap();
                    _renderBuffer[bufferIdx] = buffer;
                }
                else
                {
                    var size = iaImage.GetSize();
                    buffer = _renderBuffer[bufferIdx];
                    if (buffer.Width != size.Width || buffer.Height != size.Height)
                    {
                        buffer.Dispose();
                        _renderBuffer[bufferIdx] = mat.ToBitmap();
                    }
                    else
                    {
                        mat.ToBitmap(buffer);
                    }
                }
            }

            this.Dispatcher.Dispatch(
                () =>
                {
                    DisplayImage.ImageView.SetImageBitmap(buffer);
                });
        }
#else

        public virtual void SetImage(IInputArray image)
        {
            this.DisplayImage.SetImage(image);
            if (image == null)
                this.DisplayImage.IsVisible = false;
            else
            {
                this.DisplayImage.IsVisible = true;

                using (InputArray iaImage = image.GetInputArray())
                {
                    System.Drawing.Size size = iaImage.GetSize();
                    this.DisplayImage.WidthRequest = Math.Min(this.Width, size.Width);
                    this.DisplayImage.HeightRequest = size.Height;
                }
            }
        }
#endif

        private StackLayout _mainLayout = new StackLayout();

        public StackLayout MainLayout
        {
            get { return _mainLayout; }
        }



        private Microsoft.Maui.Controls.Button[] _additionalButtons;

        public Microsoft.Maui.Controls.Button[] AdditionalButtons
        {
            get
            {
                return _additionalButtons;
            }
        }

        public ButtonTextImagePage(Microsoft.Maui.Controls.Button[] additionalButtons=null)
        {
            var horizontalLayoutOptions = LayoutOptions.Center;

            TopButton.Text = "Click me";
            TopButton.IsEnabled = true;
            TopButton.HorizontalOptions = horizontalLayoutOptions;

            MessageLabel.Text = "";
            MessageLabel.HorizontalOptions = horizontalLayoutOptions;


            //DisplayImage.HeightRequest = 100;
            //DisplayImage.WidthRequest = 100;
            //DisplayImage.MinimumHeightRequest = 10;

            //StackLayout mainLayout = new StackLayout();
            _mainLayout.Children.Add(Picker);
            Picker.IsVisible = false;

            _mainLayout.Children.Add(TopButton);
            if (additionalButtons != null)
            {
                foreach (Microsoft.Maui.Controls.Button button in additionalButtons)
                {
                    button.HorizontalOptions = horizontalLayoutOptions;
                    _mainLayout.Children.Add(button);
                }
            }

            _additionalButtons = additionalButtons;

            _mainLayout.Children.Add(MessageLabel);


            //MessageLabel.BackgroundColor = Color.AliceBlue;
            //DisplayImage.BackgroundColor = Color.Aqua;
            //_mainLayout.BackgroundColor = Color.Blue;

#if __MACCATALYST__
            //NSImageView = new NSImageView();
            //NSImageView.ImageScaling = NSImageScale.None;
            //_mainLayout.Children.Add(NSImageView.ToView());
#elif __IOS__
            //UIImageView = new UIImageView ();
            //UIImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            //_mainLayout.Children.Add (UIImageView.ToView ());
#elif __ANDROID__
            //ImageView = new ImageView(Android.App.Application.Context);
            //_mainLayout.Children.Add(this.ImageView.ToView());
#elif WINDOWS
            //this.ImageView = new Microsoft.UI.Xaml.Controls.Image();
            //_mainLayout.Children.Add(this.ImageView.ToView());
            //this.ImageView.Stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
            //this.ImageView.Stretch = Windows.UI.Xaml.Media.Stretch.None;
#endif

            _mainLayout.Children.Add(DisplayImage);
            DisplayImage.BackgroundColor = 
                Microsoft.Maui.Graphics.Color.FromRgb(1.0, 0.0, 0.0);

            //_mainLayout.Children.Add(MessageLabel);
			
			LogEditor.Text = "";
            LogEditor.HorizontalOptions = LayoutOptions.Center;
            LogEditor.VerticalOptions = LayoutOptions.Center;
            LogEditor.FontSize = LogEditor.FontSize / 2;
			_mainLayout.Children.Add(LogEditor);
			
			SetLog(null);
			
            _mainLayout.Padding = new Thickness(10, 10, 10, 10);

            this.Content = new Microsoft.Maui.Controls.ScrollView()
            {
                Content = _mainLayout
            };
        }

        public bool HasCameraOption { get; set; }

        public virtual async Task<Mat[]> LoadImages(String[] imageNames, String[] labels = null)
        {
            Mat[] mats = new Mat[imageNames.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                String pickImgString = "Use Image from";
                if (labels != null && labels.Length > i)
                    pickImgString = labels[i];

                bool captureSupported;

                if (Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.WinUI
                    || Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.macOS)
                {
                    //Pick image from camera is not implemented on WPF.
                    captureSupported = false;
                }
                else
                {
                    captureSupported = MediaPicker.IsCaptureSupported;
                }

                String action;
                List<String> options = new List<string>();
                options.Add("Default");

                options.Add("Photo Library");

                if (captureSupported)
                    options.Add("Photo from Camera");

                if (this.HasCameraOption)
                {
                    if (Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.Android
                        || Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.iOS
                        || Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.WinUI)
                    {
                        if (captureSupported)
                            options.Add("Camera");
                    }
                    else if (Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.WinUI
                             || Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.macOS)
                    {
                        var openCVConfigDict = CvInvoke.ConfigDict;
                        bool haveVideoio = (openCVConfigDict["HAVE_OPENCV_VIDEOIO"] != 0);
                        if (haveVideoio)
                            options.Add("Camera");
                    }
                }


                if (options.Count == 1)
                {
                    action = "Default";
                }
                else
                {
                    action = await DisplayActionSheet(pickImgString, "Cancel", null, options.ToArray());
                    if (
                        action == null //user clicked outside of action sheet
                        || action.Equals("Cancel") // user cancel
                    )
                        return null;
                }

                if (action.Equals("Default"))
                {

                    using (Stream stream = await FileSystem.OpenAppPackageFileAsync(imageNames[i]))
                        using(MemoryStream ms = new MemoryStream())
                        {
                        stream.CopyTo(ms);
                        Mat m = new Mat();
                        CvInvoke.Imdecode(ms.ToArray(), ImreadModes.Color, m);
                        mats[i] = m;
                        }

                }
                else if (action.Equals("Photo Library"))
                {
                    /*
                    if (Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.WinUI)
                    {

#if !(__MACCATALYST__ || __ANDROID__ || __IOS__ || NETFX_CORE)
                        Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                        dialog.Multiselect = false;
                        dialog.Title = "Select an Image File";
                        dialog.Filter = "Image | *.jpg;*.jpeg;*.png;*.bmp;*.gif | All Files | *";
                        if (dialog.ShowDialog() == false)
                            return null;
                        mats[i] = CvInvoke.Imread(dialog.FileName, ImreadModes.AnyColor);
#endif
                    }
                    else
                    */
                    {
                        var fileResult = await FilePicker.PickAsync(PickOptions.Images);
                        if (fileResult == null) //canceled
                            return null;
                        using (Stream s = await fileResult.OpenReadAsync())
                            mats[i] = await ReadStream(s);
                    }
                }
                else if (action.Equals("Photo from Camera"))
                {
                    PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                    if (status == PermissionStatus.Denied)
                    {
                        this.SetMessage("Please grant permission to use camera");
                    }
                    else
                    {
                        var takePhotoResult = await MediaPicker.CapturePhotoAsync();

                        if (takePhotoResult == null) //canceled
                            return null;
                        using (Stream stream = await takePhotoResult.OpenReadAsync())
                            mats[i] = await ReadStream(stream);
                    }
                }
                else if (action.Equals("Camera"))
                {
                    mats = new Mat[0];
                }
            }

            return mats;
        }

        private static async Task<Mat> ReadStream(Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                byte[] data = ms.ToArray();
                Mat m = new Mat();
                CvInvoke.Imdecode(data, ImreadModes.Color, m);
                return m;
            }
        }


        public Label GetLabel()
        {
            //return null;
            return this.MessageLabel;
        }

        public void SetMessage(String message)
        {
            this.Dispatcher.Dispatch(
                () =>
                {
                    Label label = GetLabel();
                    label.Text = message;

                    label.LineBreakMode = LineBreakMode.WordWrap;
                 //label.WidthRequest = this.Width;
             }
            );
        }

        private String _log = String.Empty;

        public void ClearLog()
        {
            SetLog(String.Empty);
        }

        public void SetLog(String log)
        {
            _log = log;
            RenderLog(_log);
        }

        public void AppendLog(String log)
        {
            if (!String.IsNullOrEmpty(_log))
                _log = log + _log;
            RenderLog(_log);
        }

        private void RenderLog(String log)
        {
            this.Dispatcher.Dispatch(
                () =>
                {
                    if (String.IsNullOrEmpty(log))
                    {
                        this.LogEditor.IsVisible = false;
                    }
                    else
                    {
                        this.LogEditor.IsVisible = true;
                    }

                    this.LogEditor.Text = log;
                    this.LogEditor.WidthRequest = this.Width;
                    this.LogEditor.HeightRequest = 120;

                    //this.LogLabel.LineBreakMode = LineBreakMode.WordWrap;
                    this.LogEditor.Focus();
                }
            );
        }

        public Microsoft.Maui.Controls.Button GetButton()
        {
            //return null;
            return this.TopButton;
        }

    }


}
