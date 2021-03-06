﻿using OnlineVideos.Sites.Entities;
using System;
using System.Windows.Forms;



namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public abstract class ViaplayConnectorBase : BrowserUtilConnector
    {
        protected enum State
        {
            None,
            Login,
            PostLogin,
            StartPlaying,
            Playing
        }

        protected State _currentState = State.None;
        protected string _username;
        protected string _password;
        protected bool _showLoading = false;

        public abstract string BaseUrl { get; }
        public abstract string LoginUrl { get; }

        public override EventResult PerformLogin(string username, string password)
        {
            _showLoading = username.Contains("SHOWLOADING");
            username = username.Replace("SHOWLOADING", string.Empty);
            if (_showLoading)
                ShowLoading();
            Cursor.Hide();
            Application.DoEvents();
            _password = password;
            _username = username;
            _currentState = State.Login;
            Url = LoginUrl;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override EventResult PlayVideo(string videoToPlay)
        {
            MessageHandler.Info("videoToPlay: {0}", videoToPlay);
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            _currentState = State.StartPlaying;
            Uri uri = new Uri(BaseUrl + videoToPlay);
            Url = uri.GetLeftPart(UriPartial.Path);
            return EventResult.Complete();
        }

        public override EventResult BrowserDocumentComplete()
        {
            MessageHandler.Info("Url: {0}, State: {1}", Url, _currentState);
            switch (_currentState)
            {
                case State.Login:
                    if (Url == LoginUrl)
                    {
                        InvokeScript(Properties.Resources.ViaplayPlayMovieJs + "setTimeout(\"myLogin('" + _username + "','" + _password + "')\", 500);");
                    }
                    else
                    {
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    break;
                case State.PostLogin:
                    _currentState = State.None;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                case State.StartPlaying:
                    InvokeScript(Properties.Resources.ViaplayPlayMovieJs + " setTimeout(\"myPlay()\", 1000);");
                    if (Url.Contains("/player"))
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        _currentState = State.Playing;
                        if (_showLoading)
                            HideLoading();
                    }
                    break;
                case State.Playing:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                default:
                    break;
            }
            return EventResult.Complete();
        }

        private bool doInit = true;
        private void initJs()
        {
            if (doInit)
            {
                if (Url.Contains("/player") && Browser.Document.GetElementById("videoPlayer") != null)
                {
                    doInit = false;
                    InvokeScript(Properties.Resources.ViaplayVideoControlJs);
                }
            }
        }

        public override void OnAction(string actionEnumName)
        {
            if (_currentState != State.Playing) return;

            if (actionEnumName == "ACTION_MOVE_LEFT")
            {
                initJs();
                // We have to move the cursor to show the OSD
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.FindForm().Location.Y + 200);
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.FindForm().Location.Y + 300);
                Application.DoEvents();
                InvokeScript("try { back(); } catch(e) {}");
            }
            if (actionEnumName == "ACTION_MOVE_RIGHT")
            {
                initJs();
                // We have to move the cursor to show the OSD
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.FindForm().Location.Y + 200);
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.FindForm().Location.Y + 300);
                Application.DoEvents();
                InvokeScript("try { forward(); } catch(e) {}");
            }
        }

        public override EventResult Pause()
        {

            return PlayPause();
        }

        public override EventResult Play()
        {
            return PlayPause();
        }

        protected bool _paused = false;

        private EventResult PlayPause()
        {
            if (_currentState != State.Playing) return EventResult.Complete();
            initJs();

            // We have to move the cursor to show the OSD
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.FindForm().Location.Y + 200);
            Application.DoEvents();
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.FindForm().Location.Y + 300);
            Application.DoEvents();

            if (_paused)
                InvokeScript("try { play(); } catch(e) {}");
            else
                InvokeScript("try { pause(); } catch(e) {}");

            _paused = !_paused;
            return EventResult.Complete();
        }

    }
}
