﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading;

namespace LaserGRBL.RasterConverter
{
	public partial class RasterToLaserForm : Form
	{
		ImageProcessor IP;
		bool preventClose;
		
		private RasterToLaserForm(GrblCore core, string filename)
		{
			InitializeComponent();

			IP = new ImageProcessor(core, filename, PbConverted.Size);
			PbOriginal.Image = IP.Original;
			ImageProcessor.PreviewReady += OnPreviewReady;
			ImageProcessor.PreviewBegin += OnPreviewBegin;
			ImageProcessor.GenerationComplete += OnGenerationComplete;
			
			LblGrayscale.Visible = CbMode.Visible = !IP.IsGrayScale;
			
			CbResize.SuspendLayout();
			CbResize.Items.Add(InterpolationMode.HighQualityBicubic);
			CbResize.Items.Add(InterpolationMode.NearestNeighbor);
			CbResize.ResumeLayout();
			CbMode.SuspendLayout();
			foreach (ImageTransform.Formula formula in Enum.GetValues(typeof(ImageTransform.Formula)))
				CbMode.Items.Add(formula);
			CbMode.SelectedIndex = 0;
			CbMode.ResumeLayout();
			CbDirections.SuspendLayout();
			foreach (ImageProcessor.Direction direction in Enum.GetValues(typeof(ImageProcessor.Direction)))
				if (direction != ImageProcessor.Direction.None)
					CbDirections.Items.Add(direction);
			CbDirections.SelectedIndex = 0;
			CbDirections.ResumeLayout();

			CbFillingDirection.SuspendLayout();
			foreach (ImageProcessor.Direction direction in Enum.GetValues(typeof(ImageProcessor.Direction)))
				CbFillingDirection.Items.Add(direction);
			CbFillingDirection.SelectedIndex = 0;
			CbFillingDirection.ResumeLayout();
			
			LoadSettings();
			RefreshVE();
		}
		
		void OnPreviewBegin()
		{
			preventClose = true;
			if (InvokeRequired)
			{
				try
				{
					Invoke(new ImageProcessor.PreviewBeginDlg(OnPreviewBegin));
				}
				catch { }
			}
			else
			{
				WT.Enabled = true;
				BtnCreate.Enabled = false;				
			}
		}
		void OnPreviewReady(Image img)
		{
			if (InvokeRequired)
			{
				try
				{
					Invoke(new ImageProcessor.PreviewReadyDlg(OnPreviewReady), img);
				}
				catch { }
			}
			else
			{
				Image old = PbConverted.Image;
				PbConverted.Image = img.Clone() as Image;
				if (old != null)
					old.Dispose();
				WT.Enabled = false;
				WB.Visible = false;
				WB.Running = false;
				BtnCreate.Enabled = true;
				preventClose = false;
			}
		}

		void OnGenerationComplete(Exception ex)
		{
			if (InvokeRequired)
			{
				Invoke(new ImageProcessor.GenerationCompleteDlg(OnGenerationComplete), ex);
			}
			else
			{
				Cursor = Cursors.Default;
				if (ex != null)
					System.Windows.Forms.MessageBox.Show(ex.Message);
				preventClose = false;
				Close();
			}
		}
		
		void WTTick(object sender, EventArgs e)
		{
			WT.Enabled = false;
			WB.Visible = true;
			WB.Running = true;
		}
		
		internal static void CreateAndShowDialog(GrblCore core, string filename)
		{
			using (RasterToLaserForm f = new RasterToLaserForm(core, filename))
				f.ShowDialog();
		}

		void GoodInput(object sender, KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
				e.Handled = true;
		}

		void BtnCreateClick(object sender, EventArgs e)
		{
			using (ConvertSizeAndOptionForm f = new ConvertSizeAndOptionForm())
			{
				f.ShowDialog(IP);
				if (f.DialogResult == DialogResult.OK)
				{
					preventClose = true;
					Cursor = Cursors.WaitCursor;
					SuspendLayout();
					TCOriginalPreview.SelectedIndex = 0;
					FlipControl.Enabled = false;
					BtnCreate.Enabled = false;
					WB.Visible = true;
					WB.Running = true;
					ResumeLayout();
			
					StoreSettings();
		
					ImageProcessor targetProcessor = IP.Clone() as ImageProcessor;
					IP.GenerateGCode();
				}
			}
		}

		private void StoreSettings()
		{
			Settings.SetObject("GrayScaleConversion.RasterConversionTool", RbLineToLineTracing.Checked ? ImageProcessor.Tool.Line2Line : RbDithering.Checked ? ImageProcessor.Tool.Dithering : ImageProcessor.Tool.Vectorize);
			
			Settings.SetObject("GrayScaleConversion.Line2LineOptions.Direction", (ImageProcessor.Direction)CbDirections.SelectedItem);
			Settings.SetObject("GrayScaleConversion.Line2LineOptions.Quality", UDQuality.Value);
			Settings.SetObject("GrayScaleConversion.Line2LineOptions.Preview", CbLinePreview.Checked);

			Settings.SetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Enabled", CbSpotRemoval.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Value", UDSpotRemoval.Value);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Smooting.Enabled", CbSmoothing.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Smooting.Value", UDSmoothing.Value);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Optimize.Enabled", CbOptimize.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.Optimize.Value", UDOptimize.Value);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.DownSample.Enabled", CbDownSample.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.DownSample.Value", UDDownSample.Value);
//			Settings.SetObject("GrayScaleConversion.VectorizeOptions.ShowDots.Enabled", CbShowDots.Checked);
//			Settings.SetObject("GrayScaleConversion.VectorizeOptions.ShowImage.Enabled", CbShowImage.Checked);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.FillingDirection", (ImageProcessor.Direction)CbFillingDirection.SelectedItem);
			Settings.SetObject("GrayScaleConversion.VectorizeOptions.FillingQuality", UDFillingQuality.Value);

			Settings.SetObject("GrayScaleConversion.Parameters.Interpolation", (InterpolationMode)CbResize.SelectedItem);
			Settings.SetObject("GrayScaleConversion.Parameters.Mode", (ImageTransform.Formula)CbMode.SelectedItem);
			Settings.SetObject("GrayScaleConversion.Parameters.R", TBRed.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.G", TBGreen.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.B", TBBlue.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.Brightness", TbBright.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.Contrast", TbContrast.Value);
			Settings.SetObject("GrayScaleConversion.Parameters.Threshold.Enabled", CbThreshold.Checked);
			Settings.SetObject("GrayScaleConversion.Parameters.Threshold.Value", TbThreshold.Value);

			Settings.SetObject("GrayScaleConversion.VectorizeOptions.BorderSpeed", IP.BorderSpeed);
			Settings.SetObject("GrayScaleConversion.Gcode.Speed.Mark", IP.MarkSpeed);
			Settings.SetObject("GrayScaleConversion.Gcode.Speed.Travel", IP.TravelSpeed);

			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOn", IP.LaserOn);
			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.LaserOff", IP.LaserOff);
			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMin", IP.MinPower);
			Settings.SetObject("GrayScaleConversion.Gcode.LaserOptions.PowerMax", IP.MaxPower);

			Settings.Save(); // Saves settings in application configuration file
		}

		private void LoadSettings()
		{
			if ((IP.SelectedTool = (ImageProcessor.Tool)Settings.GetObject("GrayScaleConversion.RasterConversionTool", ImageProcessor.Tool.Line2Line)) == ImageProcessor.Tool.Line2Line)
				RbLineToLineTracing.Checked = true;
			else if ((IP.SelectedTool = (ImageProcessor.Tool)Settings.GetObject("GrayScaleConversion.RasterConversionTool", ImageProcessor.Tool.Line2Line)) == ImageProcessor.Tool.Dithering)
				RbDithering.Checked = true;
			else
				RbVectorize.Checked = true;

			CbDirections.SelectedItem = IP.LineDirection = (ImageProcessor.Direction)Settings.GetObject("GrayScaleConversion.Line2LineOptions.Direction", ImageProcessor.Direction.Horizontal);
			UDQuality.Value = IP.Quality = Convert.ToInt32(Settings.GetObject("GrayScaleConversion.Line2LineOptions.Quality", 3));
			CbLinePreview.Checked = IP.LinePreview = (bool)Settings.GetObject("GrayScaleConversion.Line2LineOptions.Preview", false);

			CbSpotRemoval.Checked = IP.UseSpotRemoval = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Enabled", false);
			UDSpotRemoval.Value = IP.SpotRemoval = (decimal)Settings.GetObject("GrayScaleConversion.VectorizeOptions.SpotRemoval.Value", 2.0m);
			CbSmoothing.Checked = IP.UseSmoothing = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Smooting.Enabled", false);
			UDSmoothing.Value = IP.Smoothing = (decimal)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Smooting.Value", 1.0m);
			CbOptimize.Checked = IP.UseOptimize = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Optimize.Enabled", false);
			UDOptimize.Value = IP.Optimize = (decimal)Settings.GetObject("GrayScaleConversion.VectorizeOptions.Optimize.Value", 0.2m);
			CbDownSample.Checked = IP.UseDownSampling = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.DownSample.Enabled", false);
			UDDownSample.Value = IP.DownSampling = (decimal)Settings.GetObject("GrayScaleConversion.VectorizeOptions.DownSample.Value", 2.0m);

			//CbShowDots.Checked = IP.ShowDots = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.ShowDots.Enabled", false);
			//CbShowImage.Checked = IP.ShowImage = (bool)Settings.GetObject("GrayScaleConversion.VectorizeOptions.ShowImage.Enabled", true);
			CbFillingDirection.SelectedItem = IP.FillingDirection = (ImageProcessor.Direction)Settings.GetObject("GrayScaleConversion.VectorizeOptions.FillingDirection", ImageProcessor.Direction.None);
			UDFillingQuality.Value = IP.FillingQuality = Convert.ToInt32(Settings.GetObject("GrayScaleConversion.VectorizeOptions.FillingQuality", 3));

			CbResize.SelectedItem = IP.Interpolation = (InterpolationMode)Settings.GetObject("GrayScaleConversion.Parameters.Interpolation", InterpolationMode.HighQualityBicubic);
			CbMode.SelectedItem = IP.Formula = (ImageTransform.Formula)Settings.GetObject("GrayScaleConversion.Parameters.Mode", ImageTransform.Formula.SimpleAverage);
			TBRed.Value = IP.Red = (int)Settings.GetObject("GrayScaleConversion.Parameters.R", 100);
			TBGreen.Value = IP.Green = (int)Settings.GetObject("GrayScaleConversion.Parameters.G", 100);
			TBBlue.Value = IP.Blue = (int)Settings.GetObject("GrayScaleConversion.Parameters.B", 100);
			TbBright.Value = IP.Brightness = (int)Settings.GetObject("GrayScaleConversion.Parameters.Brightness", 100);
			TbContrast.Value = IP.Contrast = (int)Settings.GetObject("GrayScaleConversion.Parameters.Contrast", 100);
			CbThreshold.Checked = IP.UseThreshold = (bool)Settings.GetObject("GrayScaleConversion.Parameters.Threshold.Enabled", false);
			TbThreshold.Value = IP.Threshold = (int)Settings.GetObject("GrayScaleConversion.Parameters.Threshold.Value", 50);
		}

		void OnRGBCBDoubleClick(object sender, EventArgs e)
		{((UserControls.ColorSlider)sender).Value = 100;}

		void OnThresholdDoubleClick(object sender, EventArgs e)
		{((UserControls.ColorSlider)sender).Value = 50;}

		private void CbMode_SelectedIndexChanged(object sender, EventArgs e)
		{ 
			IP.Formula = (ImageTransform.Formula)CbMode.SelectedItem;

			SuspendLayout();
			TBRed.Enabled = TBGreen.Enabled = TBBlue.Enabled = (IP.Formula == ImageTransform.Formula.Custom && !IP.IsGrayScale);
			LblRed.Enabled = LblGreen.Enabled = LblBlue.Enabled = (IP.Formula == ImageTransform.Formula.Custom && !IP.IsGrayScale);
			ResumeLayout();
		}

		private void TBRed_ValueChanged(object sender, EventArgs e)
		{ IP.Red = TBRed.Value; }

		private void TBGreen_ValueChanged(object sender, EventArgs e)
		{ IP.Green = TBGreen.Value; }

		private void TBBlue_ValueChanged(object sender, EventArgs e)
		{ IP.Blue = TBBlue.Value; }

		private void TbBright_ValueChanged(object sender, EventArgs e)
		{ IP.Brightness = TbBright.Value; }

		private void TbContrast_ValueChanged(object sender, EventArgs e)
		{ IP.Contrast = TbContrast.Value; }

		private void CbThreshold_CheckedChanged(object sender, EventArgs e)
		{ 
			IP.UseThreshold = CbThreshold.Checked;
			RefreshVE();
		}

		private void RefreshVE()
		{
			GbVectorizeOptions.Visible = RbVectorize.Checked;
			GbLineToLineOptions.Visible = RbLineToLineTracing.Checked || RbDithering.Checked;
			GbLineToLineOptions.Text = RbLineToLineTracing.Checked ? "Line To Line Options" : "Dithering Options";

			CbThreshold.Visible = !RbDithering.Checked;
			TbThreshold.Visible = !RbDithering.Checked && CbThreshold.Checked;
		}

		private void TbThreshold_ValueChanged(object sender, EventArgs e)
		{ IP.Threshold = TbThreshold.Value; }

		private void RbLineToLineTracing_CheckedChanged(object sender, EventArgs e)
		{
			if (RbLineToLineTracing.Checked)
				IP.SelectedTool = ImageProcessor.Tool.Line2Line;
			RefreshVE();
		}
		
		private void RbVectorize_CheckedChanged(object sender, EventArgs e)
		{
			if (RbVectorize.Checked)
				IP.SelectedTool = ImageProcessor.Tool.Vectorize;
			RefreshVE();
		}

		private void UDQuality_ValueChanged(object sender, EventArgs e)
		{ IP.Quality = (int)UDQuality.Value; }

		private void CbLinePreview_CheckedChanged(object sender, EventArgs e)
		{ IP.LinePreview = CbLinePreview.Checked; }

		private void UDSpotRemoval_ValueChanged(object sender, EventArgs e)
		{ IP.SpotRemoval = (int)UDSpotRemoval.Value; }

		private void CbSpotRemoval_CheckedChanged(object sender, EventArgs e)
		{ 
			IP.UseSpotRemoval = CbSpotRemoval.Checked;
			UDSpotRemoval.Enabled = CbSpotRemoval.Checked;
		}

		private void UDSmoothing_ValueChanged(object sender, EventArgs e)
		{ IP.Smoothing = UDSmoothing.Value; }

		private void CbSmoothing_CheckedChanged(object sender, EventArgs e)
		{ 
			IP.UseSmoothing = CbSmoothing.Checked;
			UDSmoothing.Enabled = CbSmoothing.Checked;
		}

		private void UDOptimize_ValueChanged(object sender, EventArgs e)
		{ IP.Optimize = UDOptimize.Value; }

		private void CbOptimize_CheckedChanged(object sender, EventArgs e)
		{
			IP.UseOptimize = CbOptimize.Checked;
			UDOptimize.Enabled = CbOptimize.Checked;
		}

		private void RasterToLaserForm_Load(object sender, EventArgs e)
		{IP.Resume();}
		
		void RasterToLaserFormFormClosing(object sender, FormClosingEventArgs e)
		{
			if (preventClose)
			{
				e.Cancel = true;
			}
			else
			{
				ImageProcessor.PreviewReady -= OnPreviewReady;
				ImageProcessor.PreviewBegin -= OnPreviewBegin;
				ImageProcessor.GenerationComplete -= OnGenerationComplete;
				IP.Dispose();
			}
		}

		void CbDirectionsSelectedIndexChanged(object sender, EventArgs e)
		{ IP.LineDirection = (ImageProcessor.Direction)CbDirections.SelectedItem; }

		void CbResizeSelectedIndexChanged(object sender, EventArgs e)
		{ 
			IP.Interpolation = (InterpolationMode)CbResize.SelectedItem;
			PbOriginal.Image = IP.Original;
		}
		void BtRotateCWClick(object sender, EventArgs e)
		{
			IP.RotateCW();
			PbOriginal.Image = IP.Original;
		}
		void BtRotateCCWClick(object sender, EventArgs e)
		{
			IP.RotateCCW();
			PbOriginal.Image = IP.Original;
		}
		void BtFlipHClick(object sender, EventArgs e)
		{
			IP.FlipH();
			PbOriginal.Image = IP.Original;
		}
		void BtFlipVClick(object sender, EventArgs e)
		{
			IP.FlipV();
			PbOriginal.Image = IP.Original;	
		}
		
		void BtnRevertClick(object sender, EventArgs e)
		{
			IP.Revert();
			PbOriginal.Image = IP.Original;	
		}

		private void CbFillingDirection_SelectedIndexChanged(object sender, EventArgs e)
		{
			IP.FillingDirection = (ImageProcessor.Direction)CbFillingDirection.SelectedItem;
			LblFillingLineLbl.Visible = LblFillingQuality.Visible = UDFillingQuality.Visible = ((ImageProcessor.Direction)CbFillingDirection.SelectedItem != ImageProcessor.Direction.None);
		}

		private void UDFillingQuality_ValueChanged(object sender, EventArgs e)
		{
			IP.FillingQuality = (int)UDFillingQuality.Value;
		}
		
		
		bool isDrag = false;
		Rectangle imageRectangle;
  		Rectangle theRectangle = new Rectangle(new Point(0, 0), new Size(0, 0));
		Point sP;
		Point eP;
	  	
		void PbConvertedMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button==MouseButtons.Left && Cropping)
			{
				int left = (PbConverted.Width - PbConverted.Image.Width) / 2;
				int top = (PbConverted.Height - PbConverted.Image.Height) / 2;
				int right = PbConverted.Width - left;
				int bottom = PbConverted.Height - top;

				imageRectangle = new Rectangle(left, top, PbConverted.Image.Width, PbConverted.Image.Height);
				
				if ((e.X >= left && e.Y >= top) && (e.X <= right && e.Y <= bottom))
				{
					isDrag = true;
					sP = e.Location;
					eP = e.Location;
				}
			}
	
		}
		void PbConvertedMouseMove(object sender, MouseEventArgs e)
		{
			if (isDrag)
			{
				//erase old rectangle
				ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);

				eP = e.Location;
				
				//limit eP to image rectangle
				int left = (PbConverted.Width - PbConverted.Image.Width) / 2;
				int top = (PbConverted.Height - PbConverted.Image.Height) / 2;
				int right = PbConverted.Width - left;
				int bottom = PbConverted.Height - top;
				eP.X = Math.Min(Math.Max(eP.X, left), right);
				eP.Y = Math.Min(Math.Max(eP.Y, top), bottom);
				
				theRectangle = new Rectangle(PbConverted.PointToScreen(sP), new Size(eP.X-sP.X, eP.Y-sP.Y));
		
				// Draw the new rectangle by calling DrawReversibleFrame
				ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
			}
		}
		
		void PbConvertedMouseUp(object sender, MouseEventArgs e)
		{
			// If the MouseUp event occurs, the user is not dragging.
			if (isDrag)
			{
				isDrag = false;
				
				//erase old rectangle
				ControlPaint.DrawReversibleFrame(theRectangle, this.BackColor, FrameStyle.Dashed);
				

				int left = (PbConverted.Width - PbConverted.Image.Width) / 2;
				int top = (PbConverted.Height - PbConverted.Image.Height) / 2;
				
				Rectangle CropRect = new Rectangle(Math.Min(sP.X, eP.X) - left,
			                                         Math.Min(sP.Y, eP.Y) - top,
			                                         Math.Abs(eP.X-sP.X),
			                                         Math.Abs(eP.Y-sP.Y));
				
				//Rectangle CropRect = new Rectangle(p.X-left, p.Y-top, orientedRect.Width, orientedRect.Height);
				
				IP.CropImage(CropRect, PbConverted.Image.Size);
				
				PbOriginal.Image = IP.Original;
				
				// Reset the rectangle.
				theRectangle = new Rectangle(0, 0, 0, 0);
				Cropping = false;
				Cursor.Clip = new Rectangle();
				UpdateCropping();
			}
		}
		
		bool Cropping;
		void BtnCropClick(object sender, EventArgs e)
		{
			Cropping = !Cropping;
			UpdateCropping();
		}
		
		void UpdateCropping()
		{
			if (Cropping)
				BtnCrop.BackColor = Color.Orange;
			else
				BtnCrop.BackColor = DefaultBackColor;
		}
		void BtnCancelClick(object sender, EventArgs e)
		{
			Close();
		}

		private void RbDithering_CheckedChanged(object sender, EventArgs e)
		{
			if (RbDithering.Checked)
				IP.SelectedTool = ImageProcessor.Tool.Dithering;
			RefreshVE();
		}

		private void CbDownSample_CheckedChanged(object sender, EventArgs e)
		{
			IP.UseDownSampling = CbDownSample.Checked;
			UDDownSample.Enabled = CbDownSample.Checked;
		}

		private void UDDownSample_ValueChanged(object sender, EventArgs e)
		{
			IP.DownSampling = UDDownSample.Value;
		}
	
	}
}
