﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

namespace UITextBlockControl {
   public class UITextBlock : TextBlock {
      private double? originalFontSize;
      private bool changingFontSize;

      private static readonly DependencyPropertyKey IsTextTrimmedKey = DependencyProperty.RegisterReadOnly(
          "IsTextTrimmed",
          typeof(bool),
          typeof(UITextBlock),
          new PropertyMetadata(false));

      private static readonly DependencyProperty IsTextTrimmedProperty = IsTextTrimmedKey.DependencyProperty;

      public static DependencyProperty ShrinkFontSizeToFitProperty = DependencyProperty.Register(
          "ShrinkFontSizeToFit",
          typeof(bool),
          typeof(UITextBlock),
          new PropertyMetadata(false, ShrinkFontSizeToFitChanged));

      private static void ShrinkFontSizeToFitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         UITextBlock uiTextBlock = (UITextBlock)d;
         uiTextBlock.originalFontSize = uiTextBlock.FontSize;
      }

      public static readonly DependencyProperty MinFontSizeProperty =
          DependencyProperty.Register("MinFontSize", typeof(double), typeof(UITextBlock), new PropertyMetadata(1d));

      public double MinFontSize {
         get => (double)GetValue(MinFontSizeProperty);
         set => SetValue(MinFontSizeProperty, value);
      }

      static UITextBlock() {
      }

      public UITextBlock() {

         SizeChanged += UITextBlockSizeChanged;
         Loaded += AddValueChangedToTextProperty;
         Unloaded += RemoveValueChangedToTextProperty;

         DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(FontSizeProperty, typeof(TextBlock));
         descriptor.AddValueChanged(this, FontSizeChanged);
      }

      private void FontSizeChanged(object sender, EventArgs eventArgs) {
         if (!changingFontSize) {
            originalFontSize = FontSize;
         }
      }

      private void RemoveValueChangedToTextProperty(object sender, RoutedEventArgs e) {
         if (e.OriginalSource != this) {
            return;
         }

         DependencyPropertyDescriptor textDescriptor = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(UITextBlock));
         textDescriptor.RemoveValueChanged(this, TextChanged);
      }

      private void AddValueChangedToTextProperty(object sender, RoutedEventArgs e) {
         DependencyPropertyDescriptor textDescriptor = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(UITextBlock));
         textDescriptor.AddValueChanged(this, TextChanged);
      }

      public bool IsTextTrimmed {
         get => GetIsTextTrimmed(this);
         set => SetIsTextTrimmed(this, value);
      }

      public bool ShrinkFontSizeToFit {
         get => (bool)GetValue(ShrinkFontSizeToFitProperty);
         set => SetValue(ShrinkFontSizeToFitProperty, value);
      }

      public static bool GetIsTextTrimmed(DependencyObject o) {
         return (bool)o.GetValue(IsTextTrimmedProperty);
      }

      private static void SetIsTextTrimmed(DependencyObject target, bool value) {
         target.SetValue(IsTextTrimmedKey, value);

         UITextBlock textBlock = target as UITextBlock;
         textBlock.ToolTip = TextTrimming.None != textBlock.TextTrimming ? textBlock.Text : textBlock.ToolTip;
      }

      private static void TextChanged(object sender, EventArgs e) {
         UITextBlock textBlock = sender as UITextBlock;
         if (null == textBlock) {
            return;
         }

         PerformShrinkIfNeeded(textBlock);

         SetIsTextTrimmed(textBlock, TextTrimming.None != textBlock.TextTrimming && CalculateIsTextTrimmed(textBlock));

      }

      private static void PerformShrinkIfNeeded(UITextBlock textBlock) {
         if (!textBlock.ShrinkFontSizeToFit) {
            return;
         }

         double newFontSize = textBlock.FontSize;
         if (newFontSize == default(double)) {
            return;
         }

         FormattedText formattedText = BuildFormattedTextFrom(textBlock, newFontSize);

         ShrinkForWidth(textBlock, formattedText, newFontSize);

         // Only calculate height if it is explicit
         object height = textBlock.ReadLocalValue(HeightProperty);
         object maxHeight = textBlock.ReadLocalValue(MaxHeightProperty);
         bool heightSet = height != DependencyProperty.UnsetValue;
         bool maxHeightSet = maxHeight != DependencyProperty.UnsetValue;
         if (heightSet || maxHeightSet) {
            ShrinkForHeight((maxHeightSet ? (double)maxHeight : (double)height), textBlock, formattedText, newFontSize);
         }
      }

      private static void ShrinkForWidth(UITextBlock textBlock, FormattedText formattedText, double newFontSize) {
         double desiredWidth = textBlock.DesiredSize.Width;
         double maxWidth = BuildFormattedTextFrom(textBlock, textBlock.originalFontSize).Width;

         while (formattedText.Width > desiredWidth && newFontSize > textBlock.MinFontSize) {
            newFontSize -= 1;
            formattedText = BuildFormattedTextFrom(textBlock, newFontSize);
         }

         double width = BuildFormattedTextFrom(textBlock, newFontSize + 1).Width;
         while (width <= maxWidth && width < textBlock.ActualWidth && newFontSize < textBlock.originalFontSize) {
            newFontSize += 1;
            width = BuildFormattedTextFrom(textBlock, newFontSize + 1).Width;
         }

         UpdateFontSizeIfNeeded(textBlock, newFontSize);
      }

      private static void ShrinkForHeight(double desiredHeight, UITextBlock textBlock, FormattedText formattedText, double newFontSize) {
         double maxHeight = BuildFormattedTextFrom(textBlock, textBlock.originalFontSize).Height;

         while (formattedText.Height > desiredHeight) {
            newFontSize -= 1;
            formattedText = BuildFormattedTextFrom(textBlock, newFontSize);
         }

         double height = BuildFormattedTextFrom(textBlock, newFontSize + 1).Height;
         while (height <= maxHeight && height < textBlock.ActualHeight && newFontSize < textBlock.originalFontSize) {
            newFontSize += 1;
            height = BuildFormattedTextFrom(textBlock, newFontSize + 1).Height;
         }

         UpdateFontSizeIfNeeded(textBlock, newFontSize);
      }

      private static void UpdateFontSizeIfNeeded(UITextBlock textBlock, double newFontSize) {
         if (Math.Abs(textBlock.FontSize - newFontSize) > 0.1 && newFontSize >= textBlock.MinFontSize) {
            textBlock.changingFontSize = true;
            textBlock.FontSize = newFontSize;
            textBlock.changingFontSize = false;
         }
      }

      private static void UITextBlockSizeChanged(object sender, SizeChangedEventArgs e) {
         UITextBlock textBlock = sender as UITextBlock;
         if (null == textBlock) {
            return;
         }

         PerformShrinkIfNeeded(textBlock);

         bool textIsTrimmed = textBlock.TextTrimming != TextTrimming.None && CalculateIsTextTrimmed(textBlock);
         SetIsTextTrimmed(textBlock, textIsTrimmed);
      }

      protected override AutomationPeer OnCreateAutomationPeer() {
         return new UITextBlockAutomationPeer(this);
      }

      public class UITextBlockAutomationPeer : TextBlockAutomationPeer {
         public UITextBlockAutomationPeer(TextBlock owner) : base(owner) {
         }

         protected override bool IsControlElementCore() {
            return true;
         }
      }

      private static bool CalculateIsTextTrimmed(TextBlock textBlock) {
         FormattedText formattedText = BuildFormattedTextFrom(textBlock);

         return (formattedText.Width > textBlock.ActualWidth);
      }

      private static FormattedText BuildFormattedTextFrom(TextBlock textBlock, double? fontSize = null) {
         Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

         // FormattedText is used to measure the whole width of the text held up by TextBlock container
         FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                fontSize ?? textBlock.FontSize,
                textBlock.Foreground);

         return formattedText;
      }
   }
}
