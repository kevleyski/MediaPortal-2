﻿#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum VirtualKeyboardStyle
  {
    Full,
    Text,
    Numbers,
    CellPhoneStyle,
  }

  public enum VirtualKeyboardTextStyle
  {
    /// <summary>
    /// The virtual keyboard doesn't show the edited text.
    /// </summary>
    None,

    /// <summary>
    /// The virtual keyboard shows the edited text.
    /// </summary>
    Text,

    /// <summary>
    /// The virtual keyboard shows the characters which are edited as stars/bullets.
    /// </summary>
    PasswordText,
  }

  public class VirtualKeyboardSettings
  {
    protected VirtualKeyboardStyle _keyboardStyle = VirtualKeyboardStyle.Full;
    protected VirtualKeyboardTextStyle _textStyle = VirtualKeyboardTextStyle.None;
    protected RectangleF? _elementArrangeBounds = null;

    /// <summary>
    /// Gets or sets the style of the virtual keyboard.
    /// </summary>
    public VirtualKeyboardStyle KeyboardStyle
    {
      get { return _keyboardStyle; }
      set { _keyboardStyle = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating how the virtual keyboard shows the edited text.
    /// </summary>
    public VirtualKeyboardTextStyle TextStyle
    {
      get { return _textStyle; }
      set { _textStyle = value; }
    }

    /// <summary>
    /// Gets or sets the bounds of the UI element whose text the virtual keyboard will edit.
    /// The virtual keyboard will arrange around the given rectangle, if possible.
    /// </summary>
    public RectangleF? ElementArrangeBounds
    {
      get { return _elementArrangeBounds; }
      set { _elementArrangeBounds = value; }
    }
  }

  /// <summary>
  /// Control which shows a virtual keyboard.
  /// </summary>
  public class VirtualKeyboardControl : Control
  {
    #region Consts

    public const string VIRTUAL_KEYBOARD_FULL_RESOURCE_PREFIX = "VirtualKeyboardLayoutFull";
    public const string VIRTUAL_KEYBOARD_TEXT_RESOURCE_PREFIX = "VirtualKeyboardLayoutText";
    public const string VIRTUAL_KEYBOARD_NUMBERS_RESOURCE_PREFIX = "VirtualKeyboardLayoutNumbers";
    public const string VIRTUAL_KEYBOARD_CELLPHONESTYLE_RESOURCE_PREFIX = "VirtualKeyboardLayoutCellPhoneStyle";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected VirtualKeyboardStyle _keyboardStyle = VirtualKeyboardStyle.Full;
    protected AbstractProperty _textProperty = null;
    protected VirtualKeyboardSettings _settings = null;
    protected IDictionary<VirtualKeyboardStyle, FrameworkElement> _keyboardControlCache =
        new Dictionary<VirtualKeyboardStyle, FrameworkElement>();

    protected AbstractProperty _visibleTextProperty = new SProperty(typeof(string), string.Empty);
    protected AbstractProperty _shiftStateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _altGrStateProperty = new SProperty(typeof(bool), false);

    #endregion

    #region Ctor

    public VirtualKeyboardControl()
    {
      Attach();
      SubscribeToMessages();
    }

    public override void Dispose()
    {
      UnsubscribeFromMessages();
    }

    void Attach()
    {
      ScreenProperty.Attach(OnScreenChanged);
    }

    // Albert, 2010-08-20: Not needed yet
    //void Detach()
    //{
    //  ScreenProperty.Detach(OnScreenChanged);
    //}

    #endregion

    void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            LocalizationMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnScreenChanged(AbstractProperty prop, object oldVal)
    {
      if (Screen != null)
        Screen.SetVirtalKeyboardControl(this);
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == LocalizationMessaging.CHANNEL)
      {
        if (((LocalizationMessaging.MessageType) message.MessageType) ==
            LocalizationMessaging.MessageType.LanguageChanged)
          ClearKeyboardControlCache(); // Discard former cached keyboard layout controls because we now need controls for another language
      }
    }

    #region Public members

    /// <summary>
    /// Gets or sets the text which is edited by the virtual keyboard.
    /// </summary>
    public string Text
    {
      get
      {
        AbstractProperty textProperty = _textProperty;
        if (textProperty == null)
          return string.Empty;
        return (string) textProperty.GetValue();
      }
      set
      {
        AbstractProperty textProperty = _textProperty;
        if (textProperty == null)
          return;
        string val = value ?? string.Empty;
        textProperty.SetValue(val);
        if (_settings == null)
          VisibleText = string.Empty;
        else if (_settings.TextStyle == VirtualKeyboardTextStyle.PasswordText)
          VisibleText = StringUtils.Repeat("*", val.Length);
        else
          VisibleText = val;
      }
    }

    public AbstractProperty VisibleTextProperty
    {
      get { return _visibleTextProperty; }
    }

    /// <summary>
    /// Gets or sets the text to be displayed in the UI, if <see cref="ShowVisibleText"/> is <c>true</c>.
    /// This property contains <c>'*'</c> characters if the text style is set to <see cref="VirtualKeyboardTextStyle.PasswordText"/>
    /// in the settings.
    /// </summary>
    public string VisibleText
    {
      get { return (string) _visibleTextProperty.GetValue(); }
      protected set { _visibleTextProperty.SetValue(value); }
    }

    /// <summary>
    /// Returns <c>true</c> if the virtual keyboard UI shoud show the <see cref="VisibleText"/>.
    /// </summary>
    public bool ShowVisibleText
    {
      get { return _settings == null ? false : _settings.TextStyle != VirtualKeyboardTextStyle.None; }
    }

    public AbstractProperty ShiftStateProperty
    {
      get { return _shiftStateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the "Shift" key. This property doesn't change any internal processing,
    /// it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool ShiftState
    {
      get { return (bool) _shiftStateProperty.GetValue(); }
      set { _shiftStateProperty.SetValue(value); }
    }

    public AbstractProperty AltGrStateProperty
    {
      get { return _altGrStateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the "Alt Gr" key. This property doesn't change any internal processing,
    /// it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool AltGrState
    {
      get { return (bool) _altGrStateProperty.GetValue(); }
      set { _altGrStateProperty.SetValue(value); }
    }

    /// <summary>
    /// Adds a character at the end of the edited text.
    /// </summary>
    /// <param name="character">Character to be added.</param>
    public void Character(char character)
    {
      Text += character;
    }

    /// <summary>
    /// Adds a space at the end of the edited text.
    /// </summary>
    public void Space()
    {
      Text += ' ';
    }

    /// <summary>
    /// Removes the last character from the end of the edited text.
    /// </summary>
    public void Backspace()
    {
      string oldText = Text;
      if (string.IsNullOrEmpty(oldText))
        return;
      Text = oldText.Substring(0, oldText.Length - 1);
    }

    public void Show(AbstractProperty textProperty, VirtualKeyboardSettings settings)
    {
      InitializeStates();
      _settings = settings;
      _textProperty = textProperty;
      _keyboardStyle = settings.KeyboardStyle;
      FrameworkElement keyboardControl;
      FinishBindingsDlgt finishDlgt;
      if (!_keyboardControlCache.TryGetValue(_keyboardStyle, out keyboardControl))
      {
        ControlTemplate keyboardLayout = FindKeyboardLayout(_keyboardStyle);
        if (keyboardLayout == null)
          return;
        IList<TriggerBase> triggers;
        keyboardControl = keyboardLayout.LoadContent(out triggers, out finishDlgt) as FrameworkElement;
        AddToKeyboardControlCache(_keyboardStyle, keyboardControl);
      }
      else
        finishDlgt = () => { };
      SetKeyboardControl(keyboardControl);
      IsVisible = true;
      finishDlgt();
      TrySetFocus(true);
    }

    public void Close()
    {
      _settings = null;
      _textProperty = null;
      IsVisible = false;
    }

    protected void InitializeStates()
    {
      ShiftState = false;
      AltGrState = false;
    }

    protected void AddToKeyboardControlCache(VirtualKeyboardStyle style, FrameworkElement element)
    {
      _keyboardControlCache[style] = element;
      element.Allocate();
    }

    protected void ClearKeyboardControlCache()
    {
      foreach (FrameworkElement keyboardControl in _keyboardControlCache.Values)
      {
        keyboardControl.Deallocate();
        keyboardControl.Dispose();
      }
      _keyboardControlCache.Clear();
    }

    protected ControlTemplate FindKeyboardLayout(VirtualKeyboardStyle style)
    {
      string styleResourcePrefix;
      switch (_keyboardStyle)
      {
        case VirtualKeyboardStyle.Text:
          styleResourcePrefix = VIRTUAL_KEYBOARD_TEXT_RESOURCE_PREFIX;
          break;
        case VirtualKeyboardStyle.Numbers:
          styleResourcePrefix = VIRTUAL_KEYBOARD_NUMBERS_RESOURCE_PREFIX;
          break;
        case VirtualKeyboardStyle.CellPhoneStyle:
          styleResourcePrefix = VIRTUAL_KEYBOARD_CELLPHONESTYLE_RESOURCE_PREFIX;
          break;
        default:
          styleResourcePrefix = VIRTUAL_KEYBOARD_FULL_RESOURCE_PREFIX;
          break;
      }
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      return FindKeyboardLayout(styleResourcePrefix, localization.CurrentCulture);
    }

    protected ControlTemplate FindKeyboardLayout(string styleResourcePrefix, CultureInfo culture)
    {
      // Try culture specific keyboard layouts
      ControlTemplate result = FindResourceInTheme(styleResourcePrefix + '_' + culture.TwoLetterISOLanguageName) as ControlTemplate;
      if (result != null)
        return result;
      if (!culture.IsNeutralCulture)
        return FindKeyboardLayout(styleResourcePrefix, culture.Parent);
      // Fallback: Take default keyboard
      return FindResourceInTheme(styleResourcePrefix) as ControlTemplate;
    }

    protected object FindResourceInTheme(string resourceKey)
    {
      object result = SkinContext.SkinResources.FindStyleResource(resourceKey);
      if (result == null)
        return null;
      IEnumerable<IBinding> deferredBindings; // Don't execute bindings in copy
      // See comment about the copying in StaticResourceBase.FindResourceInParserContext()
      result = MpfCopyManager.DeepCopyCutLP(result, out deferredBindings);
      MpfCopyManager.ActivateBindings(deferredBindings);
      return result;
    }

    protected VirtualKeyboardPresenter FindVirtualKeyboardPresenter()
    {
      return TemplateControl == null ? null : TemplateControl.FindElement(
          new TypeFinder(typeof(VirtualKeyboardPresenter))) as VirtualKeyboardPresenter;
    }

    protected void SetKeyboardControl(FrameworkElement keyboardControl)
    {
      VirtualKeyboardPresenter presenter = FindVirtualKeyboardPresenter();
      if (presenter == null)
        return;
      presenter.SetKeyboardControl(this, keyboardControl);
    }

    protected override void ArrangeTemplateControl()
    {
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return;
      RectangleF? elementArrangeBounds = _settings.ElementArrangeBounds;
      SizeF keyboardSize = templateControl.DesiredSize;
      RectangleF actualBounds = ActualBounds;
      if (actualBounds.Size.Width < keyboardSize.Width)
        keyboardSize.Width = actualBounds.Size.Width;
      if (actualBounds.Size.Height < keyboardSize.Height)
        keyboardSize.Height = actualBounds.Size.Height;
      RectangleF keyboardRect;
      if (elementArrangeBounds.HasValue)
        // Arrange above or below elementArrangeBounds, horizontally centered in elementArrangeBounds
        keyboardRect = new RectangleF(new PointF( 
            elementArrangeBounds.Value.Left + elementArrangeBounds.Value.Width / 2 - keyboardSize.Width / 2,
            elementArrangeBounds.Value.Bottom + keyboardSize.Height > actualBounds.Bottom ?
            actualBounds.Bottom - keyboardSize.Height : elementArrangeBounds.Value.Bottom),
            keyboardSize);
      else
        // Center in actualBounds
        keyboardRect = new RectangleF(
            actualBounds.Left + (actualBounds.Width - keyboardSize.Width) / 2,
            actualBounds.Top + (actualBounds.Height - keyboardSize.Height) / 2,
            keyboardSize.Width, keyboardSize.Height);
      if (keyboardRect.Left < actualBounds.Left)
        keyboardRect.X = actualBounds.Left;
      if (keyboardRect.Right > actualBounds.Right)
        keyboardRect.X = actualBounds.Right - keyboardSize.Width;
      templateControl.Arrange(keyboardRect);
    }

    public override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);
      FrameworkElement templateControl = TemplateControl;
      if (IsVisible && key == Key.Ok && templateControl != null && !templateControl.IsMouseOver)
        Close();
    }

    #endregion
  }
}
