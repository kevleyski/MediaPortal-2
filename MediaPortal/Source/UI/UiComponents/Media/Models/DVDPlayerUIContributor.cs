#region Copyright (C) 2007-2010 Team MediaPortal

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

using MediaPortal.Core;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Core.General;
using MediaPortal.Core.Commands;

namespace MediaPortal.UiComponents.Media.Models
{
  public class DVDPlayerUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    protected static string[] EMPTY_STRING_ARRAY = new string[] {};

    #region Variables

    protected MediaWorkflowStateType _mediaWorkflowStateType;
    protected IDVDPlayer _player;
    protected AbstractProperty _isOSDAvailableProperty;
    protected AbstractProperty _inDVDMenuProperty;
    protected AbstractProperty _chaptersAvailableProperty;
    protected AbstractProperty _subtitlesAvailableProperty;
    protected string[] _subtitles = EMPTY_STRING_ARRAY;
    private ItemsList _subtitleMenuItems;
    private ItemsList _chapterMenuItems;

    #endregion

    #region Constructor & maintainance

    public DVDPlayerUIContributor() : base(300)
    {
      _isOSDAvailableProperty = new WProperty(typeof(bool), false);
      _inDVDMenuProperty = new WProperty(typeof(bool), false);
      _chaptersAvailableProperty = new WProperty(typeof(bool), false);
      _subtitlesAvailableProperty = new WProperty(typeof(bool), false);
      StartTimer();
    }

    public override void Dispose()
    {
      StopTimer();
    }

    #endregion

    #region Properties

    public MediaWorkflowStateType MediaWorkflowStateType
    {
      get { return _mediaWorkflowStateType; }
    }

    public string Screen
    {
      get
      {
        // Using special screens for DVD player
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return Consts.CURRENTLY_PLAYING_DVD_SCREEN;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return Consts.FULLSCREEN_DVD_SCREEN;
        return null;
      }
    }

    public bool BackgroundDisabled
    {
      get { return MediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent; }
    }

    public AbstractProperty IsOSDAvailableProperty
    {
      get { return _isOSDAvailableProperty; }
    }

    public bool IsOSDAvailable
    {
      get { return (bool)_isOSDAvailableProperty.GetValue(); }
      set { _isOSDAvailableProperty.SetValue(value); }
    }


    public AbstractProperty InDVDMenuProperty
    {
      get { return _inDVDMenuProperty; }
    }

    public bool InDvdMenu
    {
      get { return (bool)_inDVDMenuProperty.GetValue(); }
      set { _inDVDMenuProperty.SetValue(value); }
    }

    public AbstractProperty ChaptersAvailableProperty
    {
      get { return _chaptersAvailableProperty; }
    }

    public bool ChaptersAvailable
    {
      get { return (bool)_chaptersAvailableProperty.GetValue(); }
      set { _chaptersAvailableProperty.SetValue(value); }
    }


    public AbstractProperty SubtitlesAvailableProperty
    {
      get { return _subtitlesAvailableProperty; }
    }

    public bool SubtitlesAvailable
    {
      get { return (bool) _subtitlesAvailableProperty.GetValue(); }
      set { _subtitlesAvailableProperty.SetValue(value); }
    }

    /// <summary>
    /// Provides a list of items to be shown in the subtitle selection menu.
    /// </summary>
    public ItemsList SubtitleMenuItems
    {
      get
      {
        _subtitleMenuItems.Clear();
        ISubtitlePlayer subtitlePlayer = _player as ISubtitlePlayer;
        if (subtitlePlayer != null && _subtitles.Length > 0)
        {
          string currentSubtitle = subtitlePlayer.CurrentSubtitle;
          ListItem item = new ListItem(Consts.NAME_KEY, Consts.SUBTITLE_OFF_RES)
              {
                Command = new MethodDelegateCommand(subtitlePlayer.DisableSubtitle),
                // Check if it is the selected subtitle, then mark it
                Selected = currentSubtitle != null
              };
          _subtitleMenuItems.Add(item); // Subtitles off
          foreach (string subtitle in _subtitles)
          {
            // Use local variable, otherwise delegate argument is not fixed
            string localSubtitle = subtitle;
            
            item = new ListItem(Consts.NAME_KEY, localSubtitle)
                {
                  Command = new MethodDelegateCommand(() => subtitlePlayer.SetSubtitle(localSubtitle)),
                  // Check if it is the selected subtitle, then mark it
                  Selected = localSubtitle == currentSubtitle
                };

            _subtitleMenuItems.Add(item);
          }
        }
        return _subtitleMenuItems;
      }
    }

    /// <summary>
    /// Provides a list of items to be shown in the chapter selection menu.
    /// </summary>
    public ItemsList ChapterMenuItems
    {
      get
      {
        string currentChapter = _player.CurrentDvdChapter;
        _chapterMenuItems.Clear();
        if (ChaptersAvailable)
        {
          foreach (string chapter in _player.DvdChapters)
          {
            // use local variable, otherwise delegate argument is not fixed
            string localChapter = chapter;

            ListItem item = new ListItem(Consts.NAME_KEY, localChapter)
                {
                  Command = new MethodDelegateCommand(() => _player.SetDvdChapter(localChapter)),
                  // check if it is the selected chapter, then mark it
                  Selected = (localChapter == currentChapter)
                };

            _chapterMenuItems.Add(item);
          }
        }
        return _chapterMenuItems;
      }
    }

    #endregion

    #region Public Members

    public void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      _mediaWorkflowStateType = stateType;
      _player = player as IDVDPlayer;
      _subtitleMenuItems = new ItemsList();
      _chapterMenuItems = new ItemsList();
    }

    // update GUI properties
    protected override void Update()
    {
      IsOSDAvailable = !_player.InDvdMenu;
      InDvdMenu = _player.InDvdMenu;
      ChaptersAvailable = _player.ChaptersAvailable;
      ISubtitlePlayer subtitlePlayer = _player as ISubtitlePlayer;
      if (subtitlePlayer != null)
      {
        _subtitles = subtitlePlayer.Subtitles;
        SubtitlesAvailable = _subtitles.Length > 0;
      }
      else
        _subtitles = EMPTY_STRING_ARRAY;
    }

    /// <summary>
    /// Opens the subtitle selection dialog.
    /// </summary>
    public void OpenChooseSubtitleDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseSubtitle");
    }

    /// <summary>
    /// Opens the chapter selection dialog.
    /// </summary>
    public void OpenChooseChapterDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseChapter");
    }

    /// <summary>
    /// Shows the DVD menu.
    /// </summary>
    public void ShowDvdMenu()
    {
      _player.ShowDvdMenu();
    }

    /// <summary>
    /// Skips to previous chapter.
    /// </summary>
    public void PrevChapter()
    {
      _player.PrevChapter();
    }

    /// <summary>
    /// Skips to next chapter.
    /// </summary>
    public void NextChapter()
    {
      _player.NextChapter();
    }

    /// <summary>
    /// Execute selected menu item for subtitle and chapter selection.
    /// </summary>
    /// <param name="item"></param>
    public void ExecuteMenuItem(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    #endregion
  }
}